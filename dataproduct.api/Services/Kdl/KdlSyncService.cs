using System.Collections.Concurrent;
using dataproduct.api.Models;
using dataproduct.api.Repositories;

namespace dataproduct.api.Services.Kdl
{
    /// <summary>
    /// Đẩy số tự động về Kho dữ liệu động cho 1 module:
    ///   1. Chạy các nguồn (IKdlNguon) của module — nguồn cung cấp header chạy trước
    ///   2. Map MaGoc → key (KDL_Key.MaGoc); mã chưa map ghi vào nhật ký để bổ sung cấu hình
    ///   3. Trong 1 transaction: MERGE KDL_DoiTuong → tính key XU_LY_RIENG / CONG_THUC
    ///      → thay giá trị Nguon tự động + tính của khoảng ngày (nhập tay / điều chỉnh giữ nguyên)
    ///   4. Ghi KDL_SyncLog
    /// Dùng chung cho nút "Đồng bộ" và KdlSyncWorker.
    /// </summary>
    public class KdlSyncService
    {
        private const int SoNgayToiDa = 31;
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _khoaTheoModule = new(StringComparer.OrdinalIgnoreCase);

        private readonly IKdlRepository _repo;
        private readonly IEnumerable<IKdlNguon> _nguons;
        private readonly KdlTinhToanService _tinhToan;
        private readonly ILogger<KdlSyncService> _logger;

        public KdlSyncService(IKdlRepository repo, IEnumerable<IKdlNguon> nguons, KdlTinhToanService tinhToan, ILogger<KdlSyncService> logger)
        {
            _repo = repo;
            _nguons = nguons;
            _tinhToan = tinhToan;
            _logger = logger;
        }

        /// <summary>Các module có ít nhất 1 nguồn đăng ký (worker tự đồng bộ các module này).</summary>
        public IReadOnlyList<string> DanhSachModule()
            => _nguons.Select(n => n.MaModule).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        public async Task<KDL_SyncLog> SyncAsync(string maModule, DateOnly tuNgay, DateOnly denNgay, string? nguonChay, CancellationToken ct = default)
        {
            if (denNgay < tuNgay) throw new InvalidOperationException("Đến ngày phải sau Từ ngày.");
            if (denNgay.DayNumber - tuNgay.DayNumber + 1 > SoNgayToiDa)
                throw new InvalidOperationException($"Mỗi lần đồng bộ tối đa {SoNgayToiDa} ngày.");

            var nguons = _nguons
                .Where(n => string.Equals(n.MaModule, maModule, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(n => n.CungCapDoiTuong)
                .ToList();
            if (nguons.Count == 0) throw new InvalidOperationException($"Module {maModule} chưa có nguồn dữ liệu nào.");
            if (!nguons.Any(n => n.CungCapDoiTuong)) throw new InvalidOperationException($"Module {maModule} chưa có nguồn cung cấp đối tượng.");

            var khoa = _khoaTheoModule.GetOrAdd(maModule, _ => new SemaphoreSlim(1, 1));
            if (!await khoa.WaitAsync(0, ct))
                throw new InvalidOperationException("Đang có một lần đồng bộ khác chạy, vui lòng thử lại sau.");

            var log = new KDL_SyncLog
            {
                MaModule = maModule,
                ThoiGianBatDau = DateTime.Now,
                TuNgay = tuNgay,
                DenNgay = denNgay,
                NguonChay = nguonChay,
            };

            try
            {
                await _repo.AddSyncLogAsync(log);
                var (soDong, canhBao) = await DongBoAsync(maModule, tuNgay, denNgay, nguons, ct);
                log.SoDong = soDong;
                log.ThanhCong = true;
                log.LoiChiTiet = canhBao;
            }
            catch (Exception ex)
            {
                log.ThanhCong = false;
                log.LoiChiTiet = ex.Message;
                _logger.LogError(ex, "Đồng bộ KDL {Module} {TuNgay}→{DenNgay} lỗi", maModule, tuNgay, denNgay);
            }
            finally
            {
                log.ThoiGianKetThuc = DateTime.Now;
                try { await _repo.UpdateSyncLogAsync(log); }
                catch (Exception ex) { _logger.LogError(ex, "Không ghi được nhật ký đồng bộ KDL"); }
                khoa.Release();
            }

            return log;
        }

        private async Task<(int SoDong, string? CanhBao)> DongBoAsync(string maModule, DateOnly tuNgay, DateOnly denNgay,
            List<IKdlNguon> nguons, CancellationToken ct)
        {
            var keys = await _repo.GetKeysAsync(maModule);
            var loiCauHinh = _tinhToan.KiemTraCauHinh(keys);
            if (loiCauHinh.Count > 0)
                throw new InvalidOperationException("Cấu hình key lỗi: " + string.Join(" | ", loiCauHinh));
            var maGocMap = new KdlMaGocMap(keys);

            // 1. Đọc nguồn
            var doiTuongVao = new Dictionary<string, KdlDoiTuongVao>(StringComparer.OrdinalIgnoreCase);
            var giaTriVao = new List<(string Nguon, KdlDongDuLieu Dong)>();
            foreach (var nguon in nguons)
            {
                var ctx = new KdlNguonContext
                {
                    MaModule = maModule,
                    TuNgay = tuNgay,
                    DenNgay = denNgay,
                    DoiTuongs = doiTuongVao.Values.ToList(),
                };
                var kq = await nguon.LayAsync(ctx, ct);
                if (nguon.CungCapDoiTuong)
                    foreach (var d in kq.DoiTuongs)
                        doiTuongVao[d.Me] = d;
                giaTriVao.AddRange(kq.GiaTris.Select(g => (nguon.MaNguon, g)));
            }

            // 2. Map mã gốc → key, gộp (đối tượng, key): số cộng dồn, chuỗi lấy giá trị cuối
            var chuaMap = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            var khongCoDoiTuong = 0;
            var tuDong = new Dictionary<(string Me, int IdKey), (KDL_Key Key, decimal? So, string? Chu)>();
            foreach (var (maNguon, dong) in giaTriVao)
            {
                if (!doiTuongVao.ContainsKey(dong.Me)) { khongCoDoiTuong++; continue; }
                var key = maGocMap.Tim(maNguon, dong.MaGoc);
                if (key == null) { chuaMap.Add($"{maNguon}:{dong.MaGoc}"); continue; }

                var khoa = (dong.Me, key.IdKey);
                if (key.KieuDL == KdlConst.KieuDL.Chuoi)
                    tuDong[khoa] = (key, null, dong.GiaTriText ?? dong.GiaTri?.ToString(System.Globalization.CultureInfo.InvariantCulture));
                else
                {
                    var cu = tuDong.TryGetValue(khoa, out var c) ? c.So : null;
                    tuDong[khoa] = (key, (cu ?? 0) + (dong.GiaTri ?? 0), null);
                }
            }

            // 3. Ghi trong 1 transaction
            await using var tx = await _repo.BeginTransactionAsync(ct);

            var doiTuongs = await _repo.UpsertDoiTuongAsync(maModule, tuNgay, denNgay, doiTuongVao.Values.ToList(), danhDauXoa: true);
            var theoMa = doiTuongs.ToDictionary(d => d.Me, StringComparer.OrdinalIgnoreCase);
            var keyTheoId = keys.ToDictionary(k => k.IdKey);

            // Đầu vào để tính: số tự động, phủ bởi nhập tay / điều chỉnh đã có
            var tinh = doiTuongs.ToDictionary(d => d.IdDoiTuong, d => new KdlDoiTuongTinh { DoiTuong = d });
            var rows = new List<KDL_GiaTri>();
            foreach (var ((me, idKey), v) in tuDong)
            {
                var d = theoMa[me];
                rows.Add(new KDL_GiaTri { NgaySX = d.NgaySX, CaSX = d.CaSX, IdDoiTuong = d.IdDoiTuong, IdKey = idKey, Nguon = KdlConst.Nguon.TuDong, GiaTri = v.So, GiaTriText = v.Chu });
                GanGiaTri(tinh[d.IdDoiTuong], v.Key, v.So, v.Chu);
            }

            var minNgay = doiTuongs.Count > 0 ? doiTuongs.Min(d => d.NgaySX) : tuNgay;
            var maxNgay = doiTuongs.Count > 0 ? doiTuongs.Max(d => d.NgaySX) : denNgay;
            var thuCong = await _repo.GetGiaTriAsync(tinh.Keys, Min(minNgay, tuNgay), Max(maxNgay, denNgay),
                [KdlConst.Nguon.NhapTay, KdlConst.Nguon.DieuChinh]);
            foreach (var g in thuCong.OrderBy(g => g.Nguon))   // điều chỉnh (4) ghi sau → được ưu tiên
                if (tinh.TryGetValue(g.IdDoiTuong, out var t) && keyTheoId.TryGetValue(g.IdKey, out var k))
                    GanGiaTri(t, k, g.GiaTri, g.GiaTriText);

            var daTinh = await _tinhToan.TinhAsync(keys, tinh.Values.ToList(), ct);
            foreach (var kq in daTinh)
            {
                var d = tinh[kq.IdDoiTuong].DoiTuong;
                rows.Add(new KDL_GiaTri { NgaySX = d.NgaySX, CaSX = d.CaSX, IdDoiTuong = d.IdDoiTuong, IdKey = kq.IdKey, Nguon = KdlConst.Nguon.Tinh, GiaTri = kq.GiaTri });
            }

            // Thay giá trị tự động + tính của các đối tượng trong khoảng (kể cả đối tượng vừa bị đánh dấu xóa)
            var soDong = await _repo.ThayGiaTriAsync(maModule, Min(minNgay, tuNgay), Max(maxNgay, denNgay),
                [KdlConst.Nguon.TuDong, KdlConst.Nguon.Tinh], idDoiTuongs: null, idKeys: null, rows);

            await tx.CommitAsync(ct);

            var canhBao = new List<string>();
            if (chuaMap.Count > 0) canhBao.Add($"Mã gốc chưa khai báo trong KDL_Key.MaGoc: {string.Join(", ", chuaMap)}");
            if (khongCoDoiTuong > 0) canhBao.Add($"{khongCoDoiTuong} giá trị bỏ qua vì không có đối tượng tương ứng");
            return (soDong, canhBao.Count > 0 ? string.Join(" | ", canhBao) : null);
        }

        internal static void GanGiaTri(KdlDoiTuongTinh t, KDL_Key key, decimal? so, string? chu)
        {
            if (key.KieuDL == KdlConst.KieuDL.Chuoi) t.Chu[key.MaKey] = chu;
            else t.So[key.MaKey] = so;
        }

        private static DateOnly Min(DateOnly a, DateOnly b) => a < b ? a : b;
        private static DateOnly Max(DateOnly a, DateOnly b) => a > b ? a : b;
    }
}

using System.Globalization;
using System.Text.Json;
using ClosedXML.Excel;
using dataproduct.api.Models;
using dataproduct.api.Repositories;

namespace dataproduct.api.Services.Kdl
{
    /// <summary>
    /// 1 nút trong KDL_Form.CotJson. Lá: "key" (KDL_Key.MaKey) hoặc "field" (trường header: KdlConst.TruongDoiTuong).
    /// Nhóm: "label" + "children" (header nhiều tầng).
    /// </summary>
    public class KdlCot
    {
        public string? Key { get; set; }
        public string? Field { get; set; }
        public string? Label { get; set; }
        public int? Width { get; set; }
        /// <summary>Số chữ số thập phân khi hiển thị (mặc định KDL_Key.SoLe).</summary>
        public int? Decimals { get; set; }
        /// <summary>Cho phép nhập / sửa tay trên form (ghi Nguon = nhập tay).</summary>
        public bool? Editable { get; set; }
        public bool? Hidden { get; set; }
        /// <summary>false = không xuất Excel.</summary>
        public bool? Excel { get; set; }
        /// <summary>"left" | "right": cố định cột khi cuộn ngang (Excel: cố định các cột "left" liền nhau từ cột đầu).</summary>
        public string? Fixed { get; set; }
        public List<KdlCot>? Children { get; set; }
    }

    public class KdlKeyDto
    {
        public string MaKey { get; set; } = null!;
        public string? TenKey { get; set; }
        public string? DonVi { get; set; }
        public byte KieuDL { get; set; }
        public byte CachLay { get; set; }
        public byte KieuTongHop { get; set; }
        public byte SoLe { get; set; }
        public string? CongThuc { get; set; }
        public string? MaNguon { get; set; }
    }

    public class KdlCauTruc
    {
        public string MaForm { get; set; } = null!;
        public string TenForm { get; set; } = null!;
        public string MaModule { get; set; } = null!;
        public string? MaBM { get; set; }
        public List<KdlCot> Cot { get; set; } = [];
        public Dictionary<string, KdlKeyDto> Keys { get; set; } = [];
        public List<string> CanhBao { get; set; } = [];
    }

    public class KdlDong
    {
        public int IdDoiTuong { get; set; }
        public string Me { get; set; } = null!;
        public DateOnly NgaySX { get; set; }
        public byte? CaSX { get; set; }
        public string? Kip { get; set; }
        public string? MayDuc { get; set; }
        public string? Mac { get; set; }
        public string? KichThuoc { get; set; }
        public string? LenhSanXuat { get; set; }
        /// <summary>Giá trị đang hiệu lực theo MaKey (số hoặc chuỗi).</summary>
        public Dictionary<string, object?> GiaTri { get; set; } = [];
        /// <summary>Key đang hiển thị giá trị nhập tay / điều chỉnh (ưu tiên hơn số tự động).</summary>
        public List<string> ThuCong { get; set; } = [];
    }

    public class KdlNhom
    {
        public Dictionary<string, object?> Nhom { get; set; } = [];
        public int SoDoiTuong { get; set; }
        public Dictionary<string, decimal?> GiaTri { get; set; } = [];
    }

    /// <summary>
    /// 1 dòng của bảng "Tổng hợp chi tiết": dòng đối tượng (Loai = CT) xen kẽ dòng Tổng ca / ngày / tháng / năm.
    /// Dòng tổng: IdDoiTuong = 0, chỉ điền các chiều thuộc cấp đó.
    /// </summary>
    public class KdlDongTongHopCT : KdlDong
    {
        public string Loai { get; set; } = KdlTongHopCT.ChiTiet;
        public int Nam { get; set; }
        public int Thang { get; set; }
        public int SoDoiTuong { get; set; }
        public string? Nhan { get; set; }
        /// <summary>Dòng "Tổng ca / ngày / tháng / năm" (bảng theo chiều còn có dòng "Theo …" từng giá trị chiều, LaTong = false).</summary>
        public bool LaTong { get; set; }
    }

    public static class KdlTongHopCT
    {
        public const string ChiTiet = "CT";
        public const string Ca = "CA";
        public const string Ngay = "NGAY";
        public const string Thang = "THANG";
        public const string Nam = "NAM";

        /// <summary>Cấp thấp nhất được hiển thị theo bộ lọc "Loại dữ liệu" (tatCa | ca | ngay | thang | nam).</summary>
        public static int CapToiThieu(string? loaiDuLieu) => (loaiDuLieu ?? "").Trim().ToLowerInvariant() switch
        {
            "ca" => 1,
            "ngay" => 2,
            "thang" => 3,
            "nam" => 4,
            _ => 0,
        };

        public static int Cap(string loai) => loai switch
        {
            Ca => 1,
            Ngay => 2,
            Thang => 3,
            Nam => 4,
            _ => 0,
        };
    }

    /// <summary>Xem / tổng hợp / xuất Excel / nhập tay dữ liệu Kho dữ liệu động theo cấu hình form.</summary>
    public class KdlXemService
    {
        private const int ToiDaXuatExcel = 50000;
        private static readonly JsonSerializerOptions JsonCot = new() { PropertyNameCaseInsensitive = true };

        private readonly IKdlRepository _repo;
        private readonly KdlTinhToanService _tinhToan;
        private readonly KdlCongThucHelper _congThuc;

        public KdlXemService(IKdlRepository repo, KdlTinhToanService tinhToan, KdlCongThucHelper congThuc)
        {
            _repo = repo;
            _tinhToan = tinhToan;
            _congThuc = congThuc;
        }

        // ── Cấu trúc ───────────────────────────────────────────────────────────

        public Task<List<KDL_Form>> GetFormsAsync(string? maModule) => _repo.GetFormsAsync(maModule);

        public async Task<List<KdlKeyDto>> GetKeysAsync(string maModule)
            => (await _repo.GetKeysAsync(maModule, chiActive: false)).Select(ToDto).ToList();

        public async Task<KdlCauTruc?> GetCauTrucAsync(string maForm)
            => (await NapFormAsync(maForm))?.CauTruc;

        private sealed record FormDaNap(KdlCauTruc CauTruc, List<KDL_Key> KeysModule, List<KdlCot> La);

        private async Task<FormDaNap?> NapFormAsync(string maForm)
        {
            var form = await _repo.GetFormAsync(maForm);
            if (form == null) return null;

            var cot = JsonSerializer.Deserialize<List<KdlCot>>(form.CotJson, JsonCot) ?? [];
            var keys = await _repo.GetKeysAsync(form.MaModule);
            var theoMa = keys.ToDictionary(k => k.MaKey, StringComparer.OrdinalIgnoreCase);
            var la = LayLa(cot).ToList();

            var ct = new KdlCauTruc { MaForm = form.MaForm, TenForm = form.TenForm, MaModule = form.MaModule, MaBM = form.MaBM, Cot = cot };
            foreach (var c in la)
            {
                if (c.Key != null)
                {
                    if (theoMa.TryGetValue(c.Key, out var k)) ct.Keys[k.MaKey] = ToDto(k);
                    else ct.CanhBao.Add($"Cột \"{c.Label ?? c.Key}\": key {c.Key} không có (hoặc đã ngưng) trong KDL_Key.");
                }
                else if (c.Field == null || !KdlConst.TruongDoiTuong.Contains(c.Field, StringComparer.OrdinalIgnoreCase))
                    ct.CanhBao.Add($"Cột \"{c.Label}\": thiếu key hoặc field không hợp lệ ({c.Field}).");
            }
            return new FormDaNap(ct, keys, la);
        }

        private static IEnumerable<KdlCot> LayLa(IEnumerable<KdlCot> cot)
        {
            foreach (var c in cot)
            {
                if (c.Children is { Count: > 0 })
                    foreach (var con in LayLa(c.Children)) yield return con;
                else yield return c;
            }
        }

        /// <summary>Key cần đọc: key của cột + key mà công thức TINH_KHI_XEM tham chiếu.</summary>
        private List<KDL_Key> KeyCanDoc(FormDaNap f)
        {
            var theoMa = f.KeysModule.ToDictionary(k => k.MaKey, StringComparer.OrdinalIgnoreCase);
            var can = new HashSet<string>(f.CauTruc.Keys.Keys, StringComparer.OrdinalIgnoreCase);
            var hangDoi = new Queue<string>(can);
            while (hangDoi.Count > 0)
            {
                if (!theoMa.TryGetValue(hangDoi.Dequeue(), out var k)) continue;
                if (k.CachLay != KdlConst.CachLay.TinhKhiXem || string.IsNullOrWhiteSpace(k.CongThuc)) continue;
                foreach (var t in _congThuc.LayKeyThamChieu(k.CongThuc))
                    if (can.Add(t)) hangDoi.Enqueue(t);
            }
            return f.KeysModule.Where(k => can.Contains(k.MaKey)).ToList();
        }

        // ── Dữ liệu chi tiết ───────────────────────────────────────────────────

        public async Task<(List<KdlDong> Rows, int Total, Dictionary<string, decimal?> Tong)?> GetDuLieuAsync(
            string maForm, KdlLoc loc, int page, int pageSize)
        {
            var f = await NapFormAsync(maForm);
            if (f == null) return null;
            loc.MaModule = f.CauTruc.MaModule;

            var keys = KeyCanDoc(f);
            var (doiTuongs, total) = await _repo.SearchDoiTuongAsync(loc, page, pageSize);
            var rows = await DungDongAsync(doiTuongs, keys, loc.TuNgay, loc.DenNgay);

            var tong = (await TongHopTheoKeyAsync(loc, keys, [])).FirstOrDefault()?.GiaTri ?? [];
            return (rows, total, tong);
        }

        /// <summary>
        /// Toàn bộ dòng đối tượng theo bộ lọc (không phân trang, tối đa ToiDaXuatExcel) — dùng cho BM lấy dữ liệu
        /// gợi ý từ Kho dữ liệu (vd Biên bản chốt số liệu Xưởng ĐPV). Giá trị đã áp ưu tiên điều chỉnh / nhập tay.
        /// </summary>
        public async Task<List<KdlDong>?> GetTatCaDongAsync(string maForm, KdlLoc loc)
        {
            var f = await NapFormAsync(maForm);
            if (f == null) return null;
            loc.MaModule = f.CauTruc.MaModule;
            var keys = KeyCanDoc(f);
            var doiTuongs = await _repo.GetDoiTuongAsync(loc, ToiDaXuatExcel);
            return await DungDongAsync(doiTuongs, keys, loc.TuNgay, loc.DenNgay);
        }

        private async Task<List<KdlDong>> DungDongAsync(List<KDL_DoiTuong> doiTuongs, List<KDL_Key> keys, DateOnly tuNgay, DateOnly denNgay)
        {
            var keyTheoId = keys.ToDictionary(k => k.IdKey);
            var giaTris = await _repo.GetGiaTriAsync(doiTuongs.Select(d => d.IdDoiTuong).ToList(), tuNgay, denNgay, idKeys: keyTheoId.Keys);
            var theoDoiTuong = giaTris
                .GroupBy(g => g.IdDoiTuong)
                .ToDictionary(g => g.Key, g => g.GroupBy(x => x.IdKey).Select(x => x.OrderBy(UuTien).First()).ToList());

            var rows = new List<KdlDong>(doiTuongs.Count);
            foreach (var d in doiTuongs)
            {
                var dong = new KdlDong
                {
                    IdDoiTuong = d.IdDoiTuong, Me = d.Me, NgaySX = d.NgaySX, CaSX = d.CaSX, Kip = d.Kip,
                    MayDuc = d.MayDuc, Mac = d.Mac, KichThuoc = d.KichThuoc, LenhSanXuat = d.LenhSanXuat,
                };
                var so = new Dictionary<string, decimal?>(StringComparer.OrdinalIgnoreCase);
                foreach (var g in theoDoiTuong.GetValueOrDefault(d.IdDoiTuong) ?? [])
                {
                    var k = keyTheoId[g.IdKey];
                    if (k.KieuDL == KdlConst.KieuDL.Chuoi) dong.GiaTri[k.MaKey] = g.GiaTriText;
                    else { dong.GiaTri[k.MaKey] = g.GiaTri; so[k.MaKey] = g.GiaTri; }
                    if (g.Nguon is KdlConst.Nguon.NhapTay or KdlConst.Nguon.DieuChinh) dong.ThuCong.Add(k.MaKey);
                }
                _tinhToan.TinhKhiXem(keys, so);
                foreach (var k in keys.Where(k => k.CachLay == KdlConst.CachLay.TinhKhiXem && so.ContainsKey(k.MaKey)))
                    dong.GiaTri[k.MaKey] = so[k.MaKey];
                rows.Add(dong);
            }
            return rows;
        }

        // Điều chỉnh (4) > nhập tay (3) > tự động / tính
        private static int UuTien(KDL_GiaTri g) => g.Nguon switch
        {
            KdlConst.Nguon.DieuChinh => 0,
            KdlConst.Nguon.NhapTay => 1,
            _ => 2,
        };

        // ── Tổng hợp ───────────────────────────────────────────────────────────

        public async Task<List<KdlNhom>?> GetTongHopAsync(string maForm, KdlLoc loc, IReadOnlyList<string> cotNhom)
        {
            var f = await NapFormAsync(maForm);
            if (f == null) return null;
            loc.MaModule = f.CauTruc.MaModule;
            return await TongHopTheoKeyAsync(loc, KeyCanDoc(f), cotNhom);
        }

        private async Task<List<KdlNhom>> TongHopTheoKeyAsync(KdlLoc loc, List<KDL_Key> keys, IReadOnlyList<string> cotNhom)
        {
            // Chỉ cộng key số kiểu SUM; tỷ lệ (TINH_KHI_XEM) tính lại trên tổng
            var keySum = keys.Where(k => k.KieuDL == KdlConst.KieuDL.So && k.CachLay != KdlConst.CachLay.TinhKhiXem
                                         && k.KieuTongHop == KdlConst.KieuTongHop.Sum).ToList();
            var keyTheoId = keySum.ToDictionary(k => k.IdKey);

            var nhoms = await _repo.TongHopAsync(loc, cotNhom, keyTheoId.Keys);
            return nhoms
                .Select(n =>
                {
                    var gt = n.GiaTri.ToDictionary(x => keyTheoId[x.Key].MaKey, x => x.Value, StringComparer.OrdinalIgnoreCase);
                    _tinhToan.TinhKhiXem(keys, gt);
                    return new KdlNhom { Nhom = n.Nhom, SoDoiTuong = n.SoDoiTuong, GiaTri = gt };
                })
                .OrderBy(n => string.Join("|", cotNhom.Select(c => Convert.ToString(n.Nhom.GetValueOrDefault(c), CultureInfo.InvariantCulture))))
                .ToList();
        }

        // ── Tổng hợp chi tiết / theo chiều (xen dòng Tổng ca / ngày / tháng / năm) ──

        /// <summary>Chiều được phép cho bảng "tổng hợp theo chiều" (vd theo Mác: dòng "Theo ca" từng mác + dòng "Tổng ca").</summary>
        public static readonly string[] ChieuTheo = ["Mac", "LenhSanXuat", "MayDuc", "KichThuoc"];

        public async Task<(List<KdlDongTongHopCT> Rows, bool VuotGioiHan)?> GetTongHopChiTietAsync(string maForm, KdlLoc loc, string? loaiDuLieu)
        {
            var f = await NapFormAsync(maForm);
            if (f == null) return null;
            return await DungTongHopChiTietAsync(f, loc, loaiDuLieu);
        }

        public async Task<(List<KdlDongTongHopCT> Rows, bool VuotGioiHan)?> GetTongHopTheoChieuAsync(string maForm, KdlLoc loc, string? chieu, string? loaiDuLieu)
        {
            var c = ChuanChieu(chieu);
            var f = await NapFormAsync(maForm);
            if (f == null) return null;
            return await DungTongHopTheoChieuAsync(f, loc, c, loaiDuLieu);
        }

        private static string ChuanChieu(string? chieu)
            => ChieuTheo.FirstOrDefault(c => string.Equals(c, chieu?.Trim(), StringComparison.OrdinalIgnoreCase))
               ?? throw new InvalidOperationException($"Chiều không hợp lệ. Cho phép: {string.Join(", ", ChieuTheo)}.");

        private static string? DocChieu(KdlDong d, string chieu) => chieu switch
        {
            "Mac" => d.Mac,
            "LenhSanXuat" => d.LenhSanXuat,
            "MayDuc" => d.MayDuc,
            "KichThuoc" => d.KichThuoc,
            _ => null,
        };

        private static void GanChieu(KdlDong d, string chieu, string? v)
        {
            switch (chieu)
            {
                case "Mac": d.Mac = v; break;
                case "LenhSanXuat": d.LenhSanXuat = v; break;
                case "MayDuc": d.MayDuc = v; break;
                case "KichThuoc": d.KichThuoc = v; break;
            }
        }

        private sealed class BoCong
        {
            public required string Khoa { get; init; }
            public required KdlDongTongHopCT Dong { get; init; }
            public Dictionary<string, decimal?> Tong { get; } = new(StringComparer.OrdinalIgnoreCase);
            public HashSet<string> Kips { get; } = new(StringComparer.OrdinalIgnoreCase);
        }

        private static readonly string[] NhanCap = ["", "Tổng ca", "Tổng ngày", "Tổng tháng", "Tổng năm"];
        private static readonly string[] NhanTheoCap = ["", "Theo ca", "Theo ngày", "Theo tháng", "Theo năm"];
        private static readonly string[] LoaiCap = [KdlTongHopCT.ChiTiet, KdlTongHopCT.Ca, KdlTongHopCT.Ngay, KdlTongHopCT.Thang, KdlTongHopCT.Nam];

        private static string KhoaCap(KdlDong d, int cap) => cap switch
        {
            1 => $"{d.NgaySX:yyyyMMdd}|{d.CaSX}",
            2 => $"{d.NgaySX:yyyyMMdd}",
            3 => $"{d.NgaySX:yyyyMM}",
            _ => $"{d.NgaySX:yyyy}",
        };

        /// <summary>Đầu vào chung: đối tượng đã sắp Ngày → Ca → Mẻ (KdlRepository.SapXep) + key được cộng / tính lại.</summary>
        private sealed record DuLieuCong(List<KDL_Key> Keys, List<KdlDong> Dongs, bool Vuot, List<string> KeySum, List<string> KeyXem);

        /// <summary>Cộng giống TongHopTheoKeyAsync: key số kiểu SUM (bỏ TINH_KHI_XEM), key TINH_KHI_XEM tính lại trên tổng.</summary>
        private async Task<DuLieuCong> NapDuLieuCongAsync(FormDaNap f, KdlLoc loc)
        {
            loc.MaModule = f.CauTruc.MaModule;
            var keys = KeyCanDoc(f);
            var doiTuongs = await _repo.GetDoiTuongAsync(loc, ToiDaXuatExcel + 1);
            var vuot = doiTuongs.Count > ToiDaXuatExcel;
            if (vuot) doiTuongs = doiTuongs.Take(ToiDaXuatExcel).ToList();
            var dongs = await DungDongAsync(doiTuongs, keys, loc.TuNgay, loc.DenNgay);

            var keySum = keys.Where(k => k.KieuDL == KdlConst.KieuDL.So && k.CachLay != KdlConst.CachLay.TinhKhiXem
                                         && k.KieuTongHop == KdlConst.KieuTongHop.Sum)
                             .Select(k => k.MaKey).ToList();
            var keyXem = keys.Where(k => k.CachLay == KdlConst.CachLay.TinhKhiXem).Select(k => k.MaKey).ToList();
            return new DuLieuCong(keys, dongs, vuot, keySum, keyXem);
        }

        private static BoCong TaoBoCong(KdlDong d, int cap, bool laTong) => new()
        {
            Khoa = KhoaCap(d, cap),
            Dong = new KdlDongTongHopCT
            {
                Loai = LoaiCap[cap],
                LaTong = laTong,
                Nhan = laTong ? NhanCap[cap] : NhanTheoCap[cap],
                Me = "",
                Nam = d.NgaySX.Year,
                Thang = cap <= 3 ? d.NgaySX.Month : 0,
                NgaySX = d.NgaySX,   // dòng THANG / NAM: chỉ để sắp xếp, FE / Excel không hiển thị
                CaSX = cap == 1 ? d.CaSX : null,
            },
        };

        private static void Cong(BoCong b, KdlDong d, List<string> keySum)
        {
            b.Dong.SoDoiTuong++;
            if (!string.IsNullOrWhiteSpace(d.Kip)) b.Kips.Add(d.Kip!);
            foreach (var k in keySum)
                if (d.GiaTri.TryGetValue(k, out var v) && v is decimal so)
                    b.Tong[k] = (b.Tong.GetValueOrDefault(k) ?? 0) + so;
        }

        private KdlDongTongHopCT KetThuc(BoCong b, DuLieuCong dl)
        {
            if (b.Dong.Loai == KdlTongHopCT.Ca && b.Kips.Count == 1) b.Dong.Kip = b.Kips.First();
            _tinhToan.TinhKhiXem(dl.Keys, b.Tong);
            foreach (var k in dl.KeySum.Concat(dl.KeyXem))
                if (b.Tong.TryGetValue(k, out var v) && v.HasValue) b.Dong.GiaTri[k] = v;
            return b.Dong;
        }

        /// <summary>Dòng đối tượng xen dòng Tổng ca / ngày / tháng / năm; duyệt 1 lượt, đẩy dòng tổng khi đổi khóa.</summary>
        private async Task<(List<KdlDongTongHopCT> Rows, bool VuotGioiHan)> DungTongHopChiTietAsync(FormDaNap f, KdlLoc loc, string? loaiDuLieu)
        {
            var dl = await NapDuLieuCongAsync(f, loc);
            var capMin = KdlTongHopCT.CapToiThieu(loaiDuLieu);
            var kq = new List<KdlDongTongHopCT>();
            var dangCong = new BoCong?[5];

            void Day(int cap)
            {
                var b = dangCong[cap];
                if (b == null) return;
                dangCong[cap] = null;
                if (cap >= capMin) kq.Add(KetThuc(b, dl));
            }

            foreach (var d in dl.Dongs)
            {
                // Khóa lồng nhau: cấp trên đổi thì cấp dưới cũng đổi → đẩy theo thứ tự ca → ngày → tháng → năm
                for (int cap = 1; cap <= 4; cap++)
                    if (dangCong[cap] != null && dangCong[cap]!.Khoa != KhoaCap(d, cap)) Day(cap);
                for (int cap = 1; cap <= 4; cap++)
                    Cong(dangCong[cap] ??= TaoBoCong(d, cap, laTong: true), d, dl.KeySum);

                if (capMin == 0)
                    kq.Add(new KdlDongTongHopCT
                    {
                        IdDoiTuong = d.IdDoiTuong, Me = d.Me, NgaySX = d.NgaySX, CaSX = d.CaSX, Kip = d.Kip,
                        MayDuc = d.MayDuc, Mac = d.Mac, KichThuoc = d.KichThuoc, LenhSanXuat = d.LenhSanXuat,
                        GiaTri = d.GiaTri, ThuCong = d.ThuCong,
                        Nam = d.NgaySX.Year, Thang = d.NgaySX.Month, SoDoiTuong = 1,
                    });
            }
            for (int cap = 1; cap <= 4; cap++) Day(cap);

            return (kq, dl.Vuot);
        }

        /// <summary>
        /// Theo chiều (vd Mác): mỗi cấp ca / ngày / tháng / năm gồm các dòng "Theo …" (1 dòng / giá trị chiều, sắp A→Z)
        /// rồi dòng "Tổng …" của cấp đó. Không có dòng đối tượng.
        /// </summary>
        private async Task<(List<KdlDongTongHopCT> Rows, bool VuotGioiHan)> DungTongHopTheoChieuAsync(FormDaNap f, KdlLoc loc, string chieu, string? loaiDuLieu)
        {
            var dl = await NapDuLieuCongAsync(f, loc);
            var capMin = Math.Max(1, KdlTongHopCT.CapToiThieu(loaiDuLieu));
            var kq = new List<KdlDongTongHopCT>();
            var tong = new BoCong?[5];
            var theo = new Dictionary<string, BoCong>?[5];

            void Day(int cap)
            {
                var t = tong[cap];
                if (t == null) return;
                var ds = theo[cap]!;
                tong[cap] = null;
                theo[cap] = null;
                if (cap < capMin) return;
                foreach (var b in ds.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase).Select(x => x.Value))
                    kq.Add(KetThuc(b, dl));
                kq.Add(KetThuc(t, dl));
            }

            foreach (var d in dl.Dongs)
            {
                for (int cap = 1; cap <= 4; cap++)
                    if (tong[cap] != null && tong[cap]!.Khoa != KhoaCap(d, cap)) Day(cap);

                var giaTriChieu = DocChieu(d, chieu);
                var khoaChieu = giaTriChieu?.Trim() ?? "";
                for (int cap = 1; cap <= 4; cap++)
                {
                    Cong(tong[cap] ??= TaoBoCong(d, cap, laTong: true), d, dl.KeySum);
                    var ds = theo[cap] ??= new Dictionary<string, BoCong>(StringComparer.OrdinalIgnoreCase);
                    if (!ds.TryGetValue(khoaChieu, out var b))
                    {
                        b = TaoBoCong(d, cap, laTong: false);
                        GanChieu(b.Dong, chieu, giaTriChieu);
                        ds[khoaChieu] = b;
                    }
                    Cong(b, d, dl.KeySum);
                }
            }
            for (int cap = 1; cap <= 4; cap++) Day(cap);

            return (kq, dl.Vuot);
        }

        // ── Nhập tay ───────────────────────────────────────────────────────────

        /// <summary>
        /// Ghi giá trị nhập tay (Nguon = 3) cho các cột editable của form; giá trị rỗng = xóa nhập tay (quay về số tự động).
        /// Sau đó tính lại key công thức / xử lý riêng của đối tượng.
        /// </summary>
        public async Task<KdlDong?> NhapTayAsync(string maForm, int idDoiTuong, Dictionary<string, JsonElement> giaTri, string? nguoi, CancellationToken ct)
        {
            var f = await NapFormAsync(maForm);
            if (f == null) return null;
            var d = await _repo.GetDoiTuongByIdAsync(idDoiTuong);
            if (d == null || !string.Equals(d.MaModule, f.CauTruc.MaModule, StringComparison.OrdinalIgnoreCase)) return null;

            var choPhep = f.La.Where(c => c.Key != null && c.Editable == true).Select(c => c.Key!).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var theoMa = f.KeysModule.ToDictionary(k => k.MaKey, StringComparer.OrdinalIgnoreCase);

            var ghi = new List<KDL_GiaTri>();
            var keySua = new List<int>();
            foreach (var (maKey, el) in giaTri)
            {
                if (!choPhep.Contains(maKey) || !theoMa.TryGetValue(maKey, out var k))
                    throw new InvalidOperationException($"Cột {maKey} không cho phép nhập tay trên form này.");
                if (k.CachLay == KdlConst.CachLay.TinhKhiXem)
                    throw new InvalidOperationException($"Cột {maKey} được tính khi xem, không nhập được.");
                keySua.Add(k.IdKey);

                var rong = el.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined
                           || (el.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(el.GetString()));
                if (rong) continue;

                var row = new KDL_GiaTri { NgaySX = d.NgaySX, CaSX = d.CaSX, IdDoiTuong = d.IdDoiTuong, IdKey = k.IdKey, Nguon = KdlConst.Nguon.NhapTay, NguoiCapNhat = nguoi };
                if (k.KieuDL == KdlConst.KieuDL.Chuoi)
                    row.GiaTriText = el.ValueKind == JsonValueKind.String ? el.GetString()!.Trim() : el.GetRawText();
                else if (el.ValueKind == JsonValueKind.Number) row.GiaTri = el.GetDecimal();
                else if (el.ValueKind == JsonValueKind.String && decimal.TryParse(el.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var so)) row.GiaTri = so;
                else throw new InvalidOperationException($"Cột {maKey}: giá trị phải là số.");
                ghi.Add(row);
            }
            if (keySua.Count == 0) throw new InvalidOperationException("Không có giá trị nào để lưu.");

            await using var tx = await _repo.BeginTransactionAsync(ct);
            await _repo.ThayGiaTriAsync(d.MaModule, d.NgaySX, d.NgaySX, [KdlConst.Nguon.NhapTay], [d.IdDoiTuong], keySua, ghi);
            await TinhLaiDoiTuongAsync(d, f.KeysModule, ct);
            await tx.CommitAsync(ct);

            return (await DungDongAsync([d], KeyCanDoc(f), d.NgaySX, d.NgaySX)).First();
        }

        private async Task TinhLaiDoiTuongAsync(KDL_DoiTuong d, List<KDL_Key> keys, CancellationToken ct)
        {
            var keyTheoId = keys.ToDictionary(k => k.IdKey);
            var t = new KdlDoiTuongTinh { DoiTuong = d };
            var giaTris = await _repo.GetGiaTriAsync([d.IdDoiTuong], d.NgaySX, d.NgaySX,
                [KdlConst.Nguon.TuDong, KdlConst.Nguon.NhapTay, KdlConst.Nguon.DieuChinh]);
            foreach (var g in giaTris.OrderBy(g => g.Nguon))   // nguồn ưu tiên hơn ghi sau
                if (keyTheoId.TryGetValue(g.IdKey, out var k))
                    KdlSyncService.GanGiaTri(t, k, g.GiaTri, g.GiaTriText);

            var kq = await _tinhToan.TinhAsync(keys, [t], ct);
            var rows = kq.Select(x => new KDL_GiaTri { NgaySX = d.NgaySX, CaSX = d.CaSX, IdDoiTuong = d.IdDoiTuong, IdKey = x.IdKey, Nguon = KdlConst.Nguon.Tinh, GiaTri = x.GiaTri }).ToList();
            await _repo.ThayGiaTriAsync(d.MaModule, d.NgaySX, d.NgaySX, [KdlConst.Nguon.Tinh], [d.IdDoiTuong], idKeys: null, rows);
        }

        // ── Xuất Excel (header nhiều tầng theo CotJson) ─────────────────────────

        public async Task<(byte[] Bytes, string TenForm)?> ExportExcelAsync(string maForm, KdlLoc loc)
        {
            var f = await NapFormAsync(maForm);
            if (f == null) return null;
            loc.MaModule = f.CauTruc.MaModule;

            var keys = KeyCanDoc(f);
            var doiTuongs = await _repo.GetDoiTuongAsync(loc, ToiDaXuatExcel);
            var rows = await DungDongAsync(doiTuongs, keys, loc.TuNgay, loc.DenNgay);
            var tong = (await TongHopTheoKeyAsync(loc, keys, [])).FirstOrDefault()?.GiaTri ?? [];

            var cot = LocCotExcel(f.CauTruc.Cot);
            var la = LayLa(cot).ToList();
            var doSau = DoSau(cot);

            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("DuLieu");

            ws.Cell(1, 1).Value = f.CauTruc.TenForm;
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Cell(2, 1).Value = $"Từ ngày {loc.TuNgay:dd/MM/yyyy} đến ngày {loc.DenNgay:dd/MM/yyyy}";

            const int dongHeader = 4;
            var cotIdx = 1;
            VeHeader(ws, cot, dongHeader, ref cotIdx, doSau, f.CauTruc.Keys);
            var vungHeader = ws.Range(dongHeader, 1, dongHeader + doSau - 1, la.Count);
            vungHeader.Style.Font.Bold = true;
            vungHeader.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9E1F2");
            vungHeader.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            vungHeader.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            vungHeader.Style.Alignment.WrapText = true;

            var dong = dongHeader + doSau;
            for (int i = 0; i < rows.Count; i++, dong++)
                for (int c = 0; c < la.Count; c++)
                    GhiO(ws.Cell(dong, c + 1), la[c], rows[i], i + 1, f.CauTruc.Keys);

            // Dòng tổng
            ws.Cell(dong, 1).Value = "Tổng";
            for (int c = 0; c < la.Count; c++)
                if (la[c].Key != null && tong.TryGetValue(la[c].Key!, out var v) && v.HasValue)
                {
                    ws.Cell(dong, c + 1).Value = (double)v.Value;
                    ws.Cell(dong, c + 1).Style.NumberFormat.Format = DinhDangSo(la[c], f.CauTruc.Keys);
                }
            ws.Range(dong, 1, dong, la.Count).Style.Font.Bold = true;
            ws.Range(dong, 1, dong, la.Count).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF2CC");

            var vung = ws.Range(dongHeader, 1, dong, la.Count);
            vung.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            vung.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            for (int c = 0; c < la.Count; c++)
                ws.Column(c + 1).Width = Math.Max(8, (la[c].Width ?? 100) / 7.0);
            var soCotCoDinh = la.TakeWhile(c => string.Equals(c.Fixed, "left", StringComparison.OrdinalIgnoreCase)).Count();
            ws.SheetView.Freeze(dongHeader + doSau - 1, soCotCoDinh);

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return (ms.ToArray(), f.CauTruc.TenForm);
        }

        // Màu dòng — giữ đồng bộ với KdlBangDong.tsx (FE)
        private static readonly Dictionary<string, string> MauDongTong = new()
        {
            [KdlTongHopCT.Ca] = "#DDEBF7",
            [KdlTongHopCT.Ngay] = "#FFC000",
            [KdlTongHopCT.Thang] = "#70AD47",
            [KdlTongHopCT.Nam] = "#F4B084",
        };

        private static readonly Dictionary<string, string> MauDongTheoChieu = new()
        {
            [KdlTongHopCT.Ca] = "#FFFFFF",
            [KdlTongHopCT.Ngay] = "#FFF2CC",
            [KdlTongHopCT.Thang] = "#E2EFDA",
            [KdlTongHopCT.Nam] = "#D9E1F2",
        };

        private const string TruongNam = "__Nam";
        private const string TruongThang = "__Thang";
        private const string TruongNgay = "__Ngay";
        private const string TruongLoaiDL = "__LoaiDL";
        private const string TruongSoDoiTuong = "__SoDoiTuong";

        /// <summary>Xuất bảng "Tổng hợp chi tiết": cột Năm, Tháng + cây CotJson (bỏ STT), dòng tổng tô màu theo cấp.</summary>
        public async Task<(byte[] Bytes, string TenForm)?> ExportTongHopChiTietExcelAsync(string maForm, KdlLoc loc, string? loaiDuLieu)
        {
            var f = await NapFormAsync(maForm);
            if (f == null) return null;
            var (rows, vuot) = await DungTongHopChiTietAsync(f, loc, loaiDuLieu);

            var cot = new List<KdlCot>
            {
                new() { Field = TruongNam, Label = "Năm", Width = 55, Fixed = "left" },
                new() { Field = TruongThang, Label = "Tháng", Width = 55, Fixed = "left" },
            };
            cot.AddRange(LocLa(LocCotExcel(f.CauTruc.Cot), c => !string.Equals(c.Field, "STT", StringComparison.OrdinalIgnoreCase)));
            var la = LayLa(cot).ToList();
            var cotNhan = la.FindIndex(c => string.Equals(c.Field, "CaSX", StringComparison.OrdinalIgnoreCase));
            if (cotNhan < 0) cotNhan = la.FindIndex(c => string.Equals(c.Field, "Me", StringComparison.OrdinalIgnoreCase));
            if (cotNhan < 0) cotNhan = 2;

            var keys = f.CauTruc.Keys;
            var bytes = GhiBangExcel($"{f.CauTruc.TenForm} — Tổng hợp chi tiết", "TongHopChiTiet", loc, vuot, cot, rows, keys,
                (o, c, idx, r) =>
                {
                    if (c.Field == TruongNam) o.Value = r.Nam;
                    else if (c.Field == TruongThang) { if (r.Thang > 0) o.Value = r.Thang; }
                    else if (r.LaTong && idx == cotNhan) o.Value = r.Nhan;
                    else if (r.LaTong && c.Field != null) GhiTruongDongTong(o, c.Field, r);
                    else GhiO(o, c, r, 0, keys);
                },
                r => r.LaTong ? MauDongTong[r.Loai] : null);
            return (bytes, f.CauTruc.TenForm);
        }

        /// <summary>
        /// Xuất bảng tổng hợp theo chiều (Mác / LSX / Máy đúc…): Năm, Tháng, Ngày, Loại dữ liệu, Ca, Kíp, chiều, số đối tượng
        /// + các cột key số của CotJson. Nền theo cấp, dòng "Tổng …" in đậm.
        /// </summary>
        public async Task<(byte[] Bytes, string TenForm)?> ExportTongHopTheoChieuExcelAsync(string maForm, KdlLoc loc, string? chieu, string? loaiDuLieu)
        {
            var ch = ChuanChieu(chieu);
            var f = await NapFormAsync(maForm);
            if (f == null) return null;
            var (rows, vuot) = await DungTongHopTheoChieuAsync(f, loc, ch, loaiDuLieu);

            string Nhan(string field, string macDinh)
                => f.La.FirstOrDefault(c => string.Equals(c.Field, field, StringComparison.OrdinalIgnoreCase))?.Label ?? macDinh;
            var nhanChieu = Nhan(ch, ch);
            var nhanDoiTuong = Nhan("Me", "");

            var keys = f.CauTruc.Keys;
            var cot = new List<KdlCot>
            {
                new() { Field = TruongNam, Label = "Năm", Width = 55, Fixed = "left" },
                new() { Field = TruongThang, Label = "Tháng", Width = 55, Fixed = "left" },
                new() { Field = TruongNgay, Label = Nhan("NgaySX", "Ngày"), Width = 85, Fixed = "left" },
                new() { Field = TruongLoaiDL, Label = "Loại dữ liệu", Width = 85, Fixed = "left" },
                new() { Field = "CaSX", Label = Nhan("CaSX", "Ca"), Width = 75, Fixed = "left" },
                new() { Field = "Kip", Label = Nhan("Kip", "Kíp"), Width = 45, Fixed = "left" },
                new() { Field = ch, Label = nhanChieu, Width = 110, Fixed = "left" },
                new() { Field = TruongSoDoiTuong, Label = nhanDoiTuong == "" ? "Số lượng" : $"Số {nhanDoiTuong.ToLowerInvariant()}", Width = 60, Fixed = "left" },
            };
            cot.AddRange(LocLa(LocCotExcel(f.CauTruc.Cot),
                c => c.Key != null && keys.TryGetValue(c.Key, out var k) && k.KieuDL != KdlConst.KieuDL.Chuoi));

            var bytes = GhiBangExcel($"{f.CauTruc.TenForm} — Tổng hợp theo {nhanChieu.ToLowerInvariant()}", "TongHopTheoChieu", loc, vuot, cot, rows, keys,
                (o, c, _, r) =>
                {
                    var laCa = r.Loai == KdlTongHopCT.Ca;
                    switch (c.Field)
                    {
                        case TruongNam: o.Value = r.Nam; break;
                        case TruongThang: if (r.Thang > 0) o.Value = r.Thang; break;
                        case TruongNgay:
                            if (laCa || r.Loai == KdlTongHopCT.Ngay)
                            {
                                o.Value = r.NgaySX.ToDateTime(TimeOnly.MinValue);
                                o.Style.DateFormat.Format = "dd/MM/yyyy";
                            }
                            break;
                        case TruongLoaiDL: o.Value = NhanTheoCap[KdlTongHopCT.Cap(r.Loai)]; break;
                        case "CaSX":
                            if (r.LaTong) o.Value = r.Nhan;
                            else if (laCa && r.CaSX.HasValue) o.Value = r.CaSX.Value;
                            break;
                        case "Kip": if (laCa) o.Value = r.Kip; break;
                        case TruongSoDoiTuong: o.Value = r.SoDoiTuong; break;
                        default:
                            if (c.Field == ch) { if (!r.LaTong) o.Value = DocChieu(r, ch); }
                            else GhiO(o, c, r, 0, keys);
                            break;
                    }
                },
                r => MauDongTheoChieu[r.Loai]);
            return (bytes, f.CauTruc.TenForm);
        }

        /// <summary>Ghi 1 bảng: tiêu đề, header nhiều tầng theo cây cột, từng dòng (ô do <paramref name="ghi"/> quyết định), nền theo <paramref name="mau"/>, dòng tổng in đậm.</summary>
        private static byte[] GhiBangExcel(string tieuDe, string tenSheet, KdlLoc loc, bool vuot, List<KdlCot> cot,
            IReadOnlyList<KdlDongTongHopCT> rows, Dictionary<string, KdlKeyDto> keys,
            Action<IXLCell, KdlCot, int, KdlDongTongHopCT> ghi, Func<KdlDongTongHopCT, string?> mau)
        {
            var la = LayLa(cot).ToList();
            var doSau = DoSau(cot);

            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet(tenSheet);

            ws.Cell(1, 1).Value = tieuDe;
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Cell(2, 1).Value = $"Từ ngày {loc.TuNgay:dd/MM/yyyy} đến ngày {loc.DenNgay:dd/MM/yyyy}"
                                  + (vuot ? $" — chỉ lấy {ToiDaXuatExcel:N0} đối tượng đầu, hãy thu hẹp khoảng ngày" : "");

            const int dongHeader = 4;
            var cotIdx = 1;
            VeHeader(ws, cot, dongHeader, ref cotIdx, doSau, keys);
            var vungHeader = ws.Range(dongHeader, 1, dongHeader + doSau - 1, la.Count);
            vungHeader.Style.Font.Bold = true;
            vungHeader.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9E1F2");
            vungHeader.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            vungHeader.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            vungHeader.Style.Alignment.WrapText = true;

            var dong = dongHeader + doSau;
            foreach (var r in rows)
            {
                for (int c = 0; c < la.Count; c++)
                    ghi(ws.Cell(dong, c + 1), la[c], c, r);
                var vung = ws.Range(dong, 1, dong, la.Count);
                var m = mau(r);
                if (m != null) vung.Style.Fill.BackgroundColor = XLColor.FromHtml(m);
                if (r.LaTong) vung.Style.Font.Bold = true;
                dong++;
            }

            var bang = ws.Range(dongHeader, 1, Math.Max(dong - 1, dongHeader + doSau - 1), la.Count);
            bang.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            bang.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            for (int c = 0; c < la.Count; c++)
                ws.Column(c + 1).Width = Math.Max(6, (la[c].Width ?? 100) / 7.0);
            var soCotCoDinh = la.TakeWhile(c => string.Equals(c.Fixed, "left", StringComparison.OrdinalIgnoreCase)).Count();
            ws.SheetView.Freeze(dongHeader + doSau - 1, soCotCoDinh);

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        /// <summary>Dòng tổng chỉ hiện chiều thuộc cấp của nó: Ngày (ca, ngày), Ca + Kíp (ca).</summary>
        private static void GhiTruongDongTong(IXLCell o, string field, KdlDongTongHopCT r)
        {
            switch (field.ToUpperInvariant())
            {
                case "NGAYSX" when r.Loai is KdlTongHopCT.Ca or KdlTongHopCT.Ngay:
                    o.Value = r.NgaySX.ToDateTime(TimeOnly.MinValue);
                    o.Style.DateFormat.Format = "dd/MM/yyyy";
                    break;
                case "CASX" when r.Loai == KdlTongHopCT.Ca && r.CaSX.HasValue:
                    o.Value = r.CaSX.Value;
                    break;
                case "KIP" when r.Loai == KdlTongHopCT.Ca:
                    o.Value = r.Kip;
                    break;
            }
        }

        /// <summary>Lọc lá theo điều kiện, bỏ nhóm rỗng sau khi lọc.</summary>
        private static List<KdlCot> LocLa(List<KdlCot> cot, Func<KdlCot, bool> giu)
            => cot.Select(c => c.Children is { Count: > 0 }
                      ? new KdlCot { Label = c.Label, Children = LocLa(c.Children, giu) }
                      : c)
                  .Where(c => c.Children is { Count: > 0 } || (c.Children == null && giu(c)))
                  .ToList();

        private static List<KdlCot> LocCotExcel(List<KdlCot> cot)
            => cot.Where(c => c.Hidden != true && c.Excel != false)
                  .Select(c => c.Children is { Count: > 0 }
                      ? new KdlCot { Label = c.Label, Children = LocCotExcel(c.Children) }
                      : c)
                  .Where(c => c.Children == null || c.Children.Count > 0)
                  .ToList();

        private static int DoSau(List<KdlCot> cot)
            => cot.Count == 0 ? 0 : cot.Max(c => c.Children is { Count: > 0 } ? 1 + DoSau(c.Children) : 1);

        private static void VeHeader(IXLWorksheet ws, List<KdlCot> cot, int dong, ref int cotIdx, int doSauConLai, Dictionary<string, KdlKeyDto> keys)
        {
            foreach (var c in cot)
            {
                var tieuDe = c.Label ?? (c.Key != null && keys.TryGetValue(c.Key, out var k) ? k.TenKey ?? k.MaKey : c.Field ?? c.Key ?? "");
                if (c.Children is { Count: > 0 })
                {
                    var batDau = cotIdx;
                    VeHeader(ws, c.Children, dong + 1, ref cotIdx, doSauConLai - 1, keys);
                    ws.Cell(dong, batDau).Value = tieuDe;
                    if (cotIdx - 1 > batDau) ws.Range(dong, batDau, dong, cotIdx - 1).Merge();
                }
                else
                {
                    ws.Cell(dong, cotIdx).Value = tieuDe;
                    if (doSauConLai > 1) ws.Range(dong, cotIdx, dong + doSauConLai - 1, cotIdx).Merge();
                    cotIdx++;
                }
            }
        }

        private static void GhiO(IXLCell o, KdlCot c, KdlDong r, int stt, Dictionary<string, KdlKeyDto> keys)
        {
            if (c.Field != null)
            {
                switch (c.Field.ToUpperInvariant())
                {
                    case "STT": o.Value = stt; break;
                    case "ME": o.Value = r.Me; break;
                    case "NGAYSX": o.Value = r.NgaySX.ToDateTime(TimeOnly.MinValue); o.Style.DateFormat.Format = "dd/MM/yyyy"; break;
                    case "CASX": if (r.CaSX.HasValue) o.Value = r.CaSX.Value; break;
                    case "KIP": o.Value = r.Kip; break;
                    case "MAYDUC": o.Value = r.MayDuc; break;
                    case "MAC": o.Value = r.Mac; break;
                    case "KICHTHUOC": o.Value = r.KichThuoc; break;
                    case "LENHSANXUAT": o.Value = r.LenhSanXuat; break;
                }
                return;
            }
            if (c.Key == null || !r.GiaTri.TryGetValue(c.Key, out var v) || v == null) return;
            if (v is decimal d)
            {
                o.Value = (double)d;
                o.Style.NumberFormat.Format = DinhDangSo(c, keys);
            }
            else o.Value = v.ToString();
        }

        private static string DinhDangSo(KdlCot c, Dictionary<string, KdlKeyDto> keys)
        {
            var soLe = c.Decimals ?? (c.Key != null && keys.TryGetValue(c.Key, out var k) ? k.SoLe : 0);
            return soLe > 0 ? "#,##0." + new string('0', soLe) : "#,##0";
        }

        private static KdlKeyDto ToDto(KDL_Key k) => new()
        {
            MaKey = k.MaKey, TenKey = k.TenKey, DonVi = k.DonVi, KieuDL = k.KieuDL, CachLay = k.CachLay,
            KieuTongHop = k.KieuTongHop, SoLe = k.SoLe, CongThuc = k.CongThuc, MaNguon = k.MaNguon,
        };
    }
}

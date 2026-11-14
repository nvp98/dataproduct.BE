using dataproduct.api.Models;

namespace dataproduct.api.Services.Kdl
{
    /// <summary>
    /// Tính các key dẫn xuất của 1 module cho danh sách đối tượng:
    ///   1. XU_LY_RIENG (handler C#) theo ThuTu
    ///   2. CONG_THUC theo thứ tự phụ thuộc (topological sort, chặn vòng lặp)
    /// Giá trị nhập tay / điều chỉnh đã có trong KdlDoiTuongTinh được ưu tiên, không bị ghi đè làm đầu vào.
    /// </summary>
    public class KdlTinhToanService
    {
        private readonly KdlCongThucHelper _congThuc;
        private readonly Dictionary<string, IKdlXuLyRieng> _xuLys;

        public KdlTinhToanService(KdlCongThucHelper congThuc, IEnumerable<IKdlXuLyRieng> xuLys)
        {
            _congThuc = congThuc;
            _xuLys = xuLys.ToDictionary(x => x.TenXuLy, StringComparer.OrdinalIgnoreCase);
        }

        public record KetQuaTinh(int IdDoiTuong, int IdKey, decimal? GiaTri);

        /// <summary>Kiểm tra cấu hình key dẫn xuất của module; trả danh sách lỗi (rỗng = hợp lệ).</summary>
        public List<string> KiemTraCauHinh(IReadOnlyCollection<KDL_Key> keys)
        {
            var loi = new List<string>();
            var maKeys = keys.Select(k => k.MaKey).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var k in keys.Where(k => k.CachLay is KdlConst.CachLay.CongThuc or KdlConst.CachLay.TinhKhiXem))
            {
                if (string.IsNullOrWhiteSpace(k.CongThuc)) { loi.Add($"{k.MaKey}: thiếu công thức."); continue; }
                var e = _congThuc.KiemTra(k.CongThuc);
                if (e != null) { loi.Add($"{k.MaKey}: {e}"); continue; }
                foreach (var t in _congThuc.LayKeyThamChieu(k.CongThuc).Where(t => !maKeys.Contains(t)))
                    loi.Add($"{k.MaKey}: công thức tham chiếu key không tồn tại [{t}].");
            }
            foreach (var k in keys.Where(k => k.CachLay == KdlConst.CachLay.XuLyRieng))
                if (string.IsNullOrWhiteSpace(k.TenXuLy) || !_xuLys.ContainsKey(k.TenXuLy))
                    loi.Add($"{k.MaKey}: không có handler xử lý riêng '{k.TenXuLy}'.");

            try { SapXepCongThuc(keys); }
            catch (InvalidOperationException ex) { loi.Add(ex.Message); }
            return loi;
        }

        public async Task<List<KetQuaTinh>> TinhAsync(IReadOnlyCollection<KDL_Key> keys, IReadOnlyList<KdlDoiTuongTinh> doiTuongs, CancellationToken ct)
        {
            var ketQua = new List<KetQuaTinh>();
            if (doiTuongs.Count == 0) return ketQua;

            // 1. Handler xử lý riêng
            foreach (var k in keys.Where(k => k.IsActive && k.CachLay == KdlConst.CachLay.XuLyRieng).OrderBy(k => k.ThuTu))
            {
                if (string.IsNullOrWhiteSpace(k.TenXuLy) || !_xuLys.TryGetValue(k.TenXuLy, out var xuLy))
                    throw new InvalidOperationException($"Key {k.MaKey}: không có handler xử lý riêng '{k.TenXuLy}'.");

                var giaTris = await xuLy.TinhAsync(k.MaKey, doiTuongs, ct);
                foreach (var dt in doiTuongs)
                {
                    if (!giaTris.TryGetValue(dt.DoiTuong.IdDoiTuong, out var v)) continue;
                    ketQua.Add(new KetQuaTinh(dt.DoiTuong.IdDoiTuong, k.IdKey, v));
                    dt.So.TryAdd(k.MaKey, v);   // nhập tay / điều chỉnh (đã có) được giữ làm đầu vào
                }
            }

            // 2. Công thức
            foreach (var k in SapXepCongThuc(keys))
            {
                foreach (var dt in doiTuongs)
                {
                    var gt = _congThuc.Tinh(k.CongThuc!, ma => dt.So.TryGetValue(ma, out var v)
                        ? new KdlCongThucHelper.GiaTri(true, v)
                        : KdlCongThucHelper.GiaTri.Rong);
                    if (!gt.CoDuLieu) continue;
                    ketQua.Add(new KetQuaTinh(dt.DoiTuong.IdDoiTuong, k.IdKey, gt.So));
                    dt.So.TryAdd(k.MaKey, gt.So);
                }
            }

            return ketQua;
        }

        /// <summary>Tính các key TINH_KHI_XEM trên 1 bộ giá trị (1 dòng hoặc 1 nhóm đã tổng hợp).</summary>
        public void TinhKhiXem(IReadOnlyCollection<KDL_Key> keys, Dictionary<string, decimal?> giaTri)
        {
            foreach (var k in keys.Where(k => k.IsActive && k.CachLay == KdlConst.CachLay.TinhKhiXem && !string.IsNullOrWhiteSpace(k.CongThuc)).OrderBy(k => k.ThuTu))
            {
                var gt = _congThuc.Tinh(k.CongThuc!, ma => giaTri.TryGetValue(ma, out var v)
                    ? new KdlCongThucHelper.GiaTri(true, v)
                    : KdlCongThucHelper.GiaTri.Rong);
                if (!gt.CoDuLieu) continue;
                giaTri[k.MaKey] = gt.So.HasValue && k.HeSo.HasValue ? gt.So * k.HeSo : gt.So;
            }
        }

        /// <summary>Key CONG_THUC theo thứ tự phụ thuộc; lỗi nếu có vòng lặp.</summary>
        private List<KDL_Key> SapXepCongThuc(IReadOnlyCollection<KDL_Key> keys)
        {
            var congThucs = keys
                .Where(k => k.IsActive && k.CachLay == KdlConst.CachLay.CongThuc && !string.IsNullOrWhiteSpace(k.CongThuc))
                .ToDictionary(k => k.MaKey, StringComparer.OrdinalIgnoreCase);

            var ketQua = new List<KDL_Key>();
            var trangThai = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); // 1 đang duyệt, 2 xong

            void Duyet(KDL_Key k, Stack<string> duongDi)
            {
                if (trangThai.TryGetValue(k.MaKey, out var tt))
                {
                    if (tt == 2) return;
                    throw new InvalidOperationException($"Công thức vòng lặp: {string.Join(" → ", duongDi.Reverse())} → {k.MaKey}.");
                }
                trangThai[k.MaKey] = 1;
                duongDi.Push(k.MaKey);
                foreach (var ma in _congThuc.LayKeyThamChieu(k.CongThuc!))
                    if (congThucs.TryGetValue(ma, out var phuThuoc))
                        Duyet(phuThuoc, duongDi);
                duongDi.Pop();
                trangThai[k.MaKey] = 2;
                ketQua.Add(k);
            }

            foreach (var k in congThucs.Values.OrderBy(k => k.ThuTu))
                Duyet(k, new Stack<string>());
            return ketQua;
        }
    }

    /// <summary>
    /// Map (MaNguon, MaGoc) → key từ KDL_Key.MaGoc (nhiều mã cách nhau ';').
    /// Mã khớp chính xác được ưu tiên, sau đó mẫu có hậu tố '*' khớp tiền tố dài nhất. Không phân biệt hoa thường.
    /// </summary>
    public class KdlMaGocMap
    {
        private readonly Dictionary<(string, string), KDL_Key> _chinhXac = new();
        private readonly List<(string Nguon, string TienTo, KDL_Key Key)> _mau = [];

        public KdlMaGocMap(IEnumerable<KDL_Key> keys)
        {
            var trung = new List<string>();
            foreach (var k in keys.Where(k => k.IsActive && !string.IsNullOrWhiteSpace(k.MaNguon) && !string.IsNullOrWhiteSpace(k.MaGoc)))
            {
                var nguon = k.MaNguon!.Trim().ToUpperInvariant();
                foreach (var raw in k.MaGoc!.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    var ma = raw.ToUpperInvariant();
                    if (ma.EndsWith('*'))
                    {
                        var tienTo = ma[..^1];
                        if (_mau.Any(m => m.Nguon == nguon && m.TienTo == tienTo))
                            trung.Add($"{nguon}:{raw}");
                        else
                            _mau.Add((nguon, tienTo, k));
                    }
                    else if (!_chinhXac.TryAdd((nguon, ma), k))
                        trung.Add($"{nguon}:{raw} ({_chinhXac[(nguon, ma)].MaKey}, {k.MaKey})");
                }
            }
            if (trung.Count > 0)
                throw new InvalidOperationException($"Mã gốc khai báo ở nhiều key: {string.Join("; ", trung)}");
            _mau.Sort((a, b) => b.TienTo.Length.CompareTo(a.TienTo.Length));
        }

        public KDL_Key? Tim(string maNguon, string maGoc)
        {
            var nguon = maNguon.Trim().ToUpperInvariant();
            var ma = maGoc.Trim().ToUpperInvariant();
            if (_chinhXac.TryGetValue((nguon, ma), out var k)) return k;
            foreach (var m in _mau)
                if (m.Nguon == nguon && ma.StartsWith(m.TienTo, StringComparison.Ordinal))
                    return m.Key;
            return null;
        }
    }
}

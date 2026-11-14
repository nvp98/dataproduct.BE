namespace dataproduct.api.Services.Kdl.HRC1DPV
{
    /// <summary>
    /// BCSX HRC1 Đúc phôi vuông — KL tính lại = Σ số thanh × đơn trọng (DonTrongPhoi, tra theo Mác + KT).
    /// KT chính (KichThuoc) cho mọi loại, riêng Loại 2 ngắn dài dùng KT ngắn dài (Nguoi_KichThuocND).
    /// Có số thanh mà không tra được đơn trọng → null (không xác định), để cột lệch BKM không báo sai.
    /// </summary>
    public class HRC1DPV_DonTrongTinhLaiXuLy : IKdlXuLyRieng
    {
        private const string KeyKtNganDai = "Nguoi_KichThuocND";
        private const string KeyStNganDai = "Nguoi_SoThanhL2ND";

        private static readonly string[] KeyStKtChinh =
        [
            "Nong_SoThanhL1", "Nong_SoThanhL2BM", "Nong_SoThanhL2TPHH", "Nong_SoThanhL3",
            "Nguoi_SoThanhL1", "Nguoi_SoThanhL2BM", "Nguoi_SoThanhL2TPHH", "Nguoi_SoThanhL2GiaoCa", "Nguoi_SoThanhL3",
        ];

        private readonly DonTrongPhoiService _donTrong;

        public HRC1DPV_DonTrongTinhLaiXuLy(DonTrongPhoiService donTrong)
        {
            _donTrong = donTrong;
        }

        public string TenXuLy => "HRC1DPV_DONTRONG_TINHLAI";

        public async Task<Dictionary<int, decimal?>> TinhAsync(string maKey, IReadOnlyList<KdlDoiTuongTinh> doiTuongs, CancellationToken ct)
        {
            var cache = new Dictionary<(string, string), decimal?>();
            async Task<decimal?> DonTrong(string? mac, string? kt)
            {
                if (string.IsNullOrWhiteSpace(kt)) return null;
                kt = kt.Trim();
                var khoa = ((mac ?? "").Trim().ToUpperInvariant(), kt.ToUpperInvariant());
                if (cache.TryGetValue(khoa, out var v)) return v;

                // DonTrongPhoi lưu KT dạng "150x150x12000mm", BKM dạng "150x150x12000"
                var tra = await _donTrong.TraCuuAsync(mac, kt, idXuong: null);
                if (tra == null && !kt.EndsWith("mm", StringComparison.OrdinalIgnoreCase))
                    tra = await _donTrong.TraCuuAsync(mac, kt + "mm", idXuong: null);
                v = tra is { } t && t.Row.DonTrong > 0 ? t.Row.DonTrong : null;
                cache[khoa] = v;
                return v;
            }

            var kq = new Dictionary<int, decimal?>();
            foreach (var dt in doiTuongs)
            {
                var phan = new List<(decimal St, string? Kt)>();
                foreach (var k in KeyStKtChinh)
                    if (dt.So.TryGetValue(k, out var st) && st is > 0) phan.Add((st.Value, dt.DoiTuong.KichThuoc));
                if (dt.So.TryGetValue(KeyStNganDai, out var stNd) && stNd is > 0)
                    phan.Add((stNd.Value, dt.Chu.GetValueOrDefault(KeyKtNganDai)));
                if (phan.Count == 0) continue;

                decimal? tong = 0;
                foreach (var (st, kt) in phan)
                {
                    var donTrong = await DonTrong(dt.DoiTuong.Mac, kt);
                    if (donTrong == null) { tong = null; break; }
                    tong += st * donTrong.Value;
                }
                kq[dt.DoiTuong.IdDoiTuong] = tong;
            }
            return kq;
        }
    }
}

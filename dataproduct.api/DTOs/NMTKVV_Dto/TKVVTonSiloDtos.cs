namespace dataproduct.api.DTOs.NMTKVV_Dto
{
    // ─── TKVV_TonSilo — Sổ theo dõi Xuất Nhập Tồn Silo ──────────────────────────
    // 1 phiếu = 1 Kíp. 1 dòng bảng = 1 Silo (không gộp theo NVL).

    public class TKVVTonSiloRowDto
    {
        public long Id { get; set; }
        public Guid? PhieuID { get; set; }
        public DateOnly NgaySX { get; set; }
        public int Ca { get; set; }
        public string? Kip { get; set; }
        public int? Scope { get; set; }
        public int? ThuTu { get; set; }
        public int SiloID { get; set; }
        public string? MaSilo { get; set; }
        public string? TenSilo { get; set; }
        public int? NguyenVatLieuID { get; set; }
        public string? TenNVL { get; set; }
        public decimal? DoAm { get; set; }
        public decimal? TonDau { get; set; }
        public decimal? Nhap { get; set; }
        public decimal? Xuat { get; set; }
        public decimal? TonCuoi { get; set; }
        public decimal? TonCuoiAuto { get; set; }
        public string? GhiChu { get; set; }
        public bool IsAdjusted { get; set; }
        public int? AdjustedBy { get; set; }
        public DateTime? AdjustedDate { get; set; }
    }

    public class InitTonSiloRowsRequestDto
    {
        public DateOnly NgaySX { get; set; }
        public int Ca { get; set; }
        public int Scope { get; set; }
        public int? CurrentUserId { get; set; }
        public Guid? PhieuID { get; set; }
    }

    public class SaveTonSiloRowDto
    {
        public long? Id { get; set; }
        public DateOnly NgaySX { get; set; }
        public int Ca { get; set; }
        public int Scope { get; set; }
        public int SiloID { get; set; }
        public int? NguyenVatLieuID { get; set; }
        public string? Kip { get; set; }
        public int? ThuTu { get; set; }
        public decimal? DoAm { get; set; }
        public decimal? TonDau { get; set; }
        public decimal? Nhap { get; set; }
        public decimal? Xuat { get; set; }
        public decimal? TonCuoi { get; set; }
        public decimal? TonCuoiAuto { get; set; }
        public string? GhiChu { get; set; }
    }

    public class SaveTonSiloPhieuRequestDto
    {
        public string MaBM { get; set; } = string.Empty;
        public Guid? PhieuID { get; set; }
        public int CurrentUserId { get; set; }
        public List<SaveTonSiloRowDto> Rows { get; set; } = new();
    }
}

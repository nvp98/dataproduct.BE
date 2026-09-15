namespace dataproduct.api.DTOs.NMLG_Dto
{
    // Một dòng chi tiết BBGN dùng làm nguồn cho Phiếu điều chỉnh số liệu NM.LG
    public class PhieuDieuChinhBBGNDto
    {
        public int IdCtBBGN { get; set; }
        public string? Kip { get; set; }
        public string? Ca { get; set; }
        public DateTime? ThoiGianXuLyBG { get; set; }
        public int? IdXuongBG { get; set; }
        public int? IdXuongBN { get; set; }
        public int? IdVatTu { get; set; }
        public string? TenVatTu { get; set; }
        public string? MaLo { get; set; }
        public double? DoAm { get; set; }
        public double? KhoiLuongBG { get; set; }
        public double? KLQuyKhoBG { get; set; }
        public double? KhoiLuongBN { get; set; }
        public double? KLQuyKhoBN { get; set; }
        public string? GhiChu { get; set; }
    }

    // Chi tiết Phiếu điều chỉnh — ánh xạ bảng LG_PhieuDieuChinh_ChiTiet
    public class PhieuDieuChinhChiTietDto
    {
        public long Id { get; set; }
        public Guid IdPhieu { get; set; }
        public int? IdNVL { get; set; }
        public string TenNVL { get; set; } = string.Empty;
        public int? IdNhomNVL { get; set; }
        public string? Dvt { get; set; }
        public string? MaLo { get; set; }
        public int? ThuTu { get; set; }
        public string? PhongBanXuat { get; set; }
        public string? XuongXuat { get; set; }
        public decimal? KhoiLuongXuat { get; set; }
        public decimal? KhoiLuongQuyKhoXuat { get; set; }
        public string? PhongBanNhap { get; set; }
        public string? XuongNhap { get; set; }
        public decimal? KhoiLuongNhap { get; set; }
        public decimal? KhoiLuongQuyKhoNhap { get; set; }
        public decimal? DoAm { get; set; }
        public string? ViTri { get; set; }
        public string? PhanLoai { get; set; }
        public string? GhiChu { get; set; }
        public string? NguoiTao { get; set; }
        public DateTime ThoiGianTao { get; set; }
        public string? NguoiSua { get; set; }
        public DateTime? ThoiGianSua { get; set; }
    }

    // Dùng khi lưu (ghi đè toàn bộ) danh sách chi tiết của 1 phiếu
    public class SavePhieuDieuChinhChiTietDto
    {
        public int? IdNVL { get; set; }
        public string TenNVL { get; set; } = string.Empty;
        public int? IdNhomNVL { get; set; }
        public string? Dvt { get; set; }
        public string? MaLo { get; set; }
        public int? ThuTu { get; set; }
        public string? PhongBanXuat { get; set; }
        public string? XuongXuat { get; set; }
        public decimal? KhoiLuongXuat { get; set; }
        public decimal? KhoiLuongQuyKhoXuat { get; set; }
        public string? PhongBanNhap { get; set; }
        public string? XuongNhap { get; set; }
        public decimal? KhoiLuongNhap { get; set; }
        public decimal? KhoiLuongQuyKhoNhap { get; set; }
        public decimal? DoAm { get; set; }
        public string? ViTri { get; set; }
        public string? PhanLoai { get; set; }
        public string? GhiChu { get; set; }
    }

    public class ReplacePhieuDieuChinhChiTietRequest
    {
        public string? NguoiSua { get; set; }
        public List<SavePhieuDieuChinhChiTietDto> Items { get; set; } = new();
    }
}

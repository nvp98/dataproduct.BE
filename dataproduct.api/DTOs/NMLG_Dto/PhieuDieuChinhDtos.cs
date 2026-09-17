namespace dataproduct.api.DTOs.NMLG_Dto
{
    // Một dòng chi tiết BBGN dùng làm nguồn cho Phiếu điều chỉnh số liệu NM.LG
    public class PhieuDieuChinhBBGNDto
    {
        public int IdCtBBGN { get; set; }
        public string? Kip { get; set; }
        public string? Ca { get; set; }
        public DateTime? ThoiGianXuLyBG { get; set; }

        // Bên giao — tên Xưởng/Phòng ban tra sẵn từ SP_Get_BBGN (join Tbl_Xuong/Tbl_PhongBan)
        public int? IdXuongBG { get; set; }
        public string? TenXuongGiao { get; set; }
        public int? IdPhongBanGiao { get; set; }
        public string? TenPhongBanGiao { get; set; }
        public string? TenNganPhongBanGiao { get; set; }

        // Bên nhận — tên Xưởng/Phòng ban tra sẵn từ SP_Get_BBGN
        public int? IdXuongBN { get; set; }
        public string? TenXuongNhan { get; set; }
        public int? IdPhongBanNhan { get; set; }
        public string? TenPhongBanNhan { get; set; }
        public string? TenNganPhongBanNhan { get; set; }

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
        public int? IdNVLChiTiet { get; set; }
        public int? IdNhomNVL { get; set; }
        public string? Dvt { get; set; }
        public string? MaLo { get; set; }
        public int? ThuTu { get; set; }
        public int? LoaiDieuChinh { get; set; }
        public int? LoaiSoDieuChinh { get; set; }
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
        public int? IdNVLChiTiet { get; set; }
        public int? IdNhomNVL { get; set; }
        public string? Dvt { get; set; }
        public string? MaLo { get; set; }
        public int? ThuTu { get; set; }
        public int? LoaiDieuChinh { get; set; }
        public int? LoaiSoDieuChinh { get; set; }
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

    // ─── Danh mục NVL cho Phiếu điều chỉnh (LG_PhieuDieuChinh_NVL, PRODUCTDATA) ────

    public class LGPhieuDieuChinhNvlDto
    {
        public int Id { get; set; }
        public string TenNVL { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class CreateLGPhieuDieuChinhNvlDto
    {
        public string TenNVL { get; set; } = string.Empty;
    }

    public class UpdateLGPhieuDieuChinhNvlDto
    {
        public string TenNVL { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}

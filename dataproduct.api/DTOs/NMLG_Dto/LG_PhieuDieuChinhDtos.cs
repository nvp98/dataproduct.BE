namespace dataproduct.api.DTOs.NMLG_Dto
{
    // Một dòng chi tiết dùng làm nguồn cho Phiếu điều chỉnh số liệu NM.LG — có thể đến từ
    // SP_Get_BBGN (LoaiDieuChinh = 1, "Nhập - Xuất") hoặc Sp_GetNVLNapLieuLoCao
    // (LoaiDieuChinh = 2, "Nội bộ - Xuất SX"); xem LG_PhieuDieuChinhService.GetNguonAsync.
    public class LG_PhieuDieuChinhBBGNDto
    {
        public int LoaiDieuChinh { get; set; }
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
    public class LG_PhieuDieuChinhChiTietDto
    {
        public long Id { get; set; }
        public Guid IdPhieu { get; set; }
        public int? IdNVL { get; set; } // legacy — xem IdNvlBBGN/IdNvlNapLieu
        // Rõ ràng theo đúng không gian ID nguồn — chỉ 1 trong 2 có giá trị tùy LoaiDieuChinh.
        public int? IdNvlBBGN { get; set; }      // ID vật tư BBGN (LoaiDieuChinh = 1)
        public int? IdNvlNapLieu { get; set; }   // ID LG_NL_NVL (LoaiDieuChinh = 2)
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
        // Người vừa sửa KL/Độ ẩm (bên giao, FE tự gán) và người tích xác nhận (bên nhận).
        public int? NguoiDieuChinhGiao { get; set; }
        public DateTime? ThoiGianDieuChinhGiao { get; set; }
        public int? NguoiDieuChinhNhan { get; set; }
        public DateTime? ThoiGianDieuChinhNhan { get; set; }
        // 0 = chưa xác nhận, 1 = đã xác nhận (khóa dòng)
        public int TrangThai { get; set; }
        // Khóa liên kết ngược tới dòng BBGN nguồn — dùng để khớp lại khi bấm "Tải dữ liệu" lần
        // sau, tránh mất số liệu điều chỉnh tay đã nhập cho dòng này (null nếu dòng nhập tay).
        public int? IdCtBBGN { get; set; }
    }

    // Dùng khi lưu (ghi đè toàn bộ) danh sách chi tiết của 1 phiếu
    public class SaveLG_PhieuDieuChinhChiTietDto
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
        public int? NguoiDieuChinhGiao { get; set; }
        public DateTime? ThoiGianDieuChinhGiao { get; set; }
        public int? NguoiDieuChinhNhan { get; set; }
        public DateTime? ThoiGianDieuChinhNhan { get; set; }
        public int TrangThai { get; set; }
        public int? IdCtBBGN { get; set; }
    }

    public class ReplaceLG_PhieuDieuChinhChiTietRequest
    {
        public string? NguoiSua { get; set; }
        public List<SaveLG_PhieuDieuChinhChiTietDto> Items { get; set; } = new();
    }

    public class SyncLG_PhieuDieuChinhTuBBGNRequest
    {
        public string? NguoiThucHien { get; set; }
    }

    // Xác nhận (hoặc hủy xác nhận) "Người điều chỉnh nhận" cho 1 dòng chi tiết — lưu ngay
    // xuống LG_PhieuDieuChinh_ChiTiet, không cần chờ Lưu cả phiếu.
    public class XacNhanChiTietDto
    {
        public bool Confirmed { get; set; }
        public int? IdNguoiThucHien { get; set; }
    }

    // ─── Danh mục NVL cho Phiếu điều chỉnh (LG_PhieuDieuChinh_NVL, PRODUCT_FORM) ───

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

using System.ComponentModel.DataAnnotations;

namespace dataproduct.api.Models
{
    public class LG_PhieuDieuChinh_ChiTiet
    {
        [Key]
        public long ID { get; set; }
        public Guid IDPhieu { get; set; }
        // Legacy — mang 2 không gian ID khác nhau tùy LoaiDieuChinh (1=ID vật tư BBGN,
        // 2=ID LG_NL_NVL). Giữ lại để không phá dữ liệu/luồng cũ, nhưng KHÔNG dùng để
        // join/báo cáo nữa — dùng IDNVL_BBGN/IDNVL_NapLieu bên dưới (rõ ràng, không lẫn lộn).
        public int? IDNVL { get; set; }
        // ID vật tư bên BBGN (PRODUCTDATA) — chỉ có giá trị khi LoaiDieuChinh = 1 (Nhập - Xuất).
        public int? IDNVL_BBGN { get; set; }
        // ID LG_NL_NVL (Nạp liệu lò cao, PRODUCT_FORM) — chỉ có giá trị khi LoaiDieuChinh = 2
        // (Nội bộ - Xuất SX).
        public int? IDNVL_NapLieu { get; set; }
        public string TenNVL { get; set; } = string.Empty;
        public int? IDNVLChiTiet { get; set; }
        public int? IDNhomNVL { get; set; }
        public string? DVT { get; set; }
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
        public bool IsDelete { get; set; }
        public int? LoaiDieuChinh { get; set; }
        public int? LoaiSoDieuChinh { get; set; }
        public int? NguoiDieuChinhGiao { get; set; }
        public DateTime? ThoiGianDieuChinhGiao { get; set; }
        public int? NguoiDieuChinhNhan { get; set; }
        public DateTime? ThoiGianDieuChinhNhan { get; set; }
        public int TrangThai { get; set; }
        public int? ID_CT_BBGN { get; set; }
        public int? IDLoCao { get; set; }
        public int? IDCa { get; set; }
        // Phòng ban của phiếu (từ file cấu hình biểu mẫu, bảng LG_PDC_Scope_PhongBan) — mọi loại dòng (Nhập, Nội bộ, Xuất)
        public int? ID_PhongBan { get; set; }
        // Chỉ cho dòng Nhập (LoaiDieuChinh = 1) / Xuất (= 3): xưởng giao / nhận (Tbl_Xuong.ID_Xuong) và giờ giao của dòng
        // BBGN — để dựng lại dòng và chia tab theo xưởng khi đọc từ bảng này
        public int? ID_Xuong_BG { get; set; }
        public int? ID_Xuong_BN { get; set; }
        public DateTime? ThoiGianXuLyBG { get; set; }
    }
}

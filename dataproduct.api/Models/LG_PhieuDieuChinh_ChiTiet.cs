using System.ComponentModel.DataAnnotations;

namespace dataproduct.api.Models
{
    public class LG_PhieuDieuChinh_ChiTiet
    {
        [Key]
        public long ID { get; set; }
        public Guid IDPhieu { get; set; }
        public int? IDNVL { get; set; }
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
    }
}

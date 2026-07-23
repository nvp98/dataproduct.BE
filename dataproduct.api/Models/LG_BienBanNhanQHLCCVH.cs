using System.ComponentModel.DataAnnotations;

namespace dataproduct.api.Models
{
    public class LG_BienBanNhanQHLCCVH
    {
        [Key]
        public int ID { get; set; }
        public DateTime Ngay { get; set; }
        public byte? Ca { get; set; }
        public int IDLoCao { get; set; }
        public byte LoaiPhanBo { get; set; }
        public decimal KhoiLuongNhanVe { get; set; }
        public string? GhiChu { get; set; }
        public int IDNguoiNhap { get; set; }
        public DateTime NgayNhap { get; set; }
        public bool IsDelete { get; set; }
    }
}

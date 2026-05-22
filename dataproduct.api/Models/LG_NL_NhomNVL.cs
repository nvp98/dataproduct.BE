using System.ComponentModel.DataAnnotations;

namespace dataproduct.api.Models
{
    public class LG_NL_NhomNVL
    {
        [Key]
        public int ID { get; set; }
        public int? IDLoCao { get; set; }
        public string? TenNhom { get; set; }
        public int? ThuTu { get; set; }
        public string? GhiChu { get; set; }
        public DateTime? NgayTao { get; set; }

    }
}

using System.ComponentModel.DataAnnotations;

namespace dataproduct.api.Models
{
    public class LG_NL_SiLo
    {
        [Key]
        public int ID { get; set; }
        public int? IDLoCao { get; set; }
        public string? TenSiLo { get; set; }
        public int? ThuTu { get; set; }
        public DateTime? NgayTao { get; set; }
        public string? TagKey { get; set; }
        public bool? IsDelete { get; set; } = false;
    }
}

using System.ComponentModel.DataAnnotations;

namespace dataproduct.api.Models
{
    public class SiLo_LG
    {
        [Key]
        public int ID { get; set; }
        public int? ID_LoCao { get; set; }
        public string? TenSiLo { get; set; }
        public int? ThuTu { get; set; }
        public string? TenNL { get; set; }
        public string? TenNL_DieuChinh { get; set; }
    }
}

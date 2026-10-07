using System.ComponentModel.DataAnnotations;

namespace dataproduct.api.Models
{
    public class LG_PhieuDieuChinh_NVL
    {
        [Key]
        public int ID { get; set; }
        public string TenNVL { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}

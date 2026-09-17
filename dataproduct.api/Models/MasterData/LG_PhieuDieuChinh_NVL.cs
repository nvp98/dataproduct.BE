using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace dataproduct.api.Models.MasterData
{
    [Table("LG_PhieuDieuChinh_NVL")]
    public class LG_PhieuDieuChinh_NVL
    {
        [Key]
        public int ID { get; set; }
        public string TenNVL { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}

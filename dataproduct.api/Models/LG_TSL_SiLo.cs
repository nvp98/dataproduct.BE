using System.ComponentModel.DataAnnotations;

namespace dataproduct.api.Models
{
    public class LG_TSL_SiLo
    {
        [Key]
        public int ID { get; set; }
        public int? ID_LoCao { get; set; }
        public string? TenSiLo { get; set; }
        public int? ThuTu { get; set; }
        public int? ThuTuCoDinh { get; set; }
        public bool? IsDelete { get; set; } = false; 
    }
}

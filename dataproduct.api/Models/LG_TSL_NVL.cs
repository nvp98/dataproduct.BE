using System.ComponentModel.DataAnnotations;

namespace dataproduct.api.Models
{
    public class LG_TSL_NVL
    {
        [Key]
        public int ID { get; set; }
        public string? TenNVL { get; set; }
        public string? TenNVL_Tk { get; set; }
        public string? GhiChu { get; set; }
        public DateTime NgayTao { get; set; }
        public int IDLoCao { get; set; }
        public bool XacNhan { get; set; } = false; 
        public DateTime? NgayXacNhan { get; set; }
        public int? IDNguoiXacNhan { get; set; }
        public bool? IsDelete { get; set; }
    }
}

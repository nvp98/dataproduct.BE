using System.ComponentModel.DataAnnotations;

namespace dataproduct.api.Models
{
    public class LG_TSL_SiLo_Mapping
    {
        [Key]
        public int ID { get; set; }
        public int IDLoCao { get; set; }
        public int IDSiLo { get; set; }
        public int IDNVL { get; set; }
        public DateTime Ngay { get; set; }
        public int Ca { get; set; }
        public string? GhiChu { get; set; }
        public DateTime NgayTao { get; set; }
        public string? NguoiTao { get; set; }
    }
}

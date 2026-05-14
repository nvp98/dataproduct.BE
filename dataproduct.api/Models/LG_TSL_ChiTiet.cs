using System.ComponentModel.DataAnnotations;

namespace dataproduct.api.Models
{
    public class LG_TSL_ChiTiet
    {
        [Key]
        public int ID { get; set; }
        public Guid IDPhieu { get; set; }
        public int IDLoCao { get; set; }
        public DateTime Ngay { get; set; }
        public int Ca { get; set; }
        public int IDSiLo { get; set; }
        public int? IDMapping { get; set; }
        public int? IDNVL { get; set; }
        public string? TenSiLo { get; set; }
        public string? TenNVL { get; set; }
        public decimal? KLTonCuoiKip { get; set; }
        public string? GhiChu { get; set; }
        public int? ThuTu { get; set; }
    }
}

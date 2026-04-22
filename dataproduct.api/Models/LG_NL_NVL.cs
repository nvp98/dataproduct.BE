using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace dataproduct.api.Models
{
    public class LG_NL_NVL
    {
        [Key]
        public int ID { get; set; }
        public int? IDLoCao { get; set; }
        public int? IDNhomNVL { get; set; }
        public string? TenNVL { get; set; }
        public string? DonVi { get; set; }

        [Column(TypeName = "decimal(10,3)")]
        public decimal? SoLuong { get; set; }

        [Column(TypeName = "decimal(10,3)")]
        public decimal? DoAm { get; set; }

        public string? GhiChu { get; set; }
        public DateTime? NgayTao { get; set; }

    }
}

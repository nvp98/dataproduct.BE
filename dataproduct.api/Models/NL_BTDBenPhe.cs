using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace dataproduct.api.Models
{
    [Table("NL_BTDBenPhe", Schema = "dbo")]
    public class NL_BTDBenPhe
    {
        [Key]
        public int ID { get; set; }

        public Guid? IDPhieu { get; set; }
        public DateOnly? NgaySX { get; set; }
        public string? Ca { get; set; }
        public string? Kip { get; set; }
        public string? MaBSX { get; set; }
        public string? SoHieuBen { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal? KhoiLuong { get; set; }

        public string? GhiChu { get; set; }
    }
}

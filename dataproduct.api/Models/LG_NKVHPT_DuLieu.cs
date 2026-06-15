using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace dataproduct.api.Models
{
    public class LG_NKVHPT_DuLieu
    {
            [Key]
            public long ID { get; set; }

            public int ID_LoCao { get; set; }

            public DateTime ThoiGian { get; set; }

            [Column(TypeName = "decimal(10,3)")]
            public decimal? TrongLuongBonPhunThoi1 { get; set; }

            [Column(TypeName = "decimal(10,3)")]
            public decimal? TrongLuongBonPhunThoi2 { get; set; }

            [Column(TypeName = "decimal(10,3)")]
            public decimal? TrongLuongBonPhunThoi3 { get; set; }

            [Column(TypeName = "decimal(10,3)")]
            public decimal? YeuCauTuLoCao { get; set; }

            [Column(TypeName = "decimal(10,3)")]
            public decimal? LuongThanPhunThucTe { get; set; }

            [Column(TypeName = "decimal(10,3)")]
            public decimal? LuyKeLuongPhunThanTrongCa { get; set; }

            public int? SoLuongSungPhun { get; set; }

            public DateTime? CreatedAt { get; set; }
    }
}

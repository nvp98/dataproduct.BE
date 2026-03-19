using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace dataproduct.api.Models.MasterData
{
    [Table("Tbl_Kip")]
    public class Tbl_Kip
    {
        [Key]
        public int ID_Kip { get; set; }

        public DateOnly? NgayLamViec { get; set; }

        /// <summary>Ca sản xuất: 1 = Ca ngày (08h-20h), 2 = Ca đêm (20h-08h)</summary>
        public string? TenCa { get; set; }

        /// <summary>Tên kíp: A, B, C, D, ...</summary>
        [MaxLength(10)]
        public string? TenKip { get; set; }
    }
}

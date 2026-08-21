using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace dataproduct.api.Models.MasterData
{
    [Table("Tbl_Xuong")]
    public class Tbl_Xuong
    {
        [Key]
        public int ID_Xuong { get; set; }
        public string? TenXuong { get; set; }
    }
}

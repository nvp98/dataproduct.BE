using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace dataproduct.api.Models.MasterData
{
    [Table("Tbl_LoCao")]
    public class Tbl_LoCao
    {
        [Key]
        public int ID { get; set; }

        public string? TenLoCao { get; set; }

        /// <summary>Tên kíp: A, B, C, D, ...</summary>
        
        public int? ID_PhongBan { get; set; }
    }
}

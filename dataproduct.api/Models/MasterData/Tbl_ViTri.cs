using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace dataproduct.api.Models.MasterData
{
    public class Tbl_ViTri
    {
        [Key]
        public int ID_ViTri { get; set; }

        [MaxLength(200)]
        public string? TenViTri { get; set; }

        public int? ID_TrangThai { get; set; }
    }
}

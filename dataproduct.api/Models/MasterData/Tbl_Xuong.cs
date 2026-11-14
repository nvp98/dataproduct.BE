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
        public int? ID_PhongBan { get; set; }
        // 1 = đang hoạt động, 0 = ngưng
        public int? ID_TrangThai { get; set; }
    }
}

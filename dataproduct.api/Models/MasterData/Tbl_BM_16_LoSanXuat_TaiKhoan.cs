using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace dataproduct.api.Models.MasterData
{
    [Table("Tbl_BM_16_LoSanXuat_TaiKhoan")]
    public class Tbl_BM_16_LoSanXuat_TaiKhoan
{
    [Key]
    public int ID { get; set; }
    public int ID_TaiKhoan { get; set; }
    public int ID_LoSanXuat { get; set; }
}
}

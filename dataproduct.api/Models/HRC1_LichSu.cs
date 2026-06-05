using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace dataproduct.api.Models;

public class HRC1_LichSu
{
    public int Id { get; set; }

    public int MeId { get; set; }

    public int? TaiKhoanId { get; set; }

    [MaxLength(30)]
    public string? HanhDong { get; set; }  // tao | chinh_sua | xac_nhan | bo_xac_nhan | chot | lam_moi

    [Column(TypeName = "nvarchar(MAX)")]
    public string? DuLieuCu { get; set; }

    [Column(TypeName = "nvarchar(MAX)")]
    public string? DuLieuMoi { get; set; }

    public DateTime? Luc { get; set; }
}

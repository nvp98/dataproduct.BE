using System.ComponentModel.DataAnnotations;

namespace dataproduct.api.Models;

public class HRC1_MePhanCong
{
    public int Id { get; set; }

    public int MeId { get; set; }

    public Guid IdPhieu { get; set; }          // FK→BM_Phieu.IDPhieu

    [Required]
    [MaxLength(20)]
    public string CongDoan { get; set; } = null!;  // lo_thoi | tinh_luyen | duc

    // null = lần TL đầu; 1, 2, ... = TL lần 2+
    public int? ThuTuTL { get; set; }

    // TL: mẻ thổi đích chuyển sang sau khi tinh luyện (null = giữ nguyên mẻ hiện tại)
    public int? ChuyenVeMeId { get; set; }

    public int? XacNhanBoi { get; set; }       // ID_TaiKhoan
    public DateTime? XacNhanLuc { get; set; }
}

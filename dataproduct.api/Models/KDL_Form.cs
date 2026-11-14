namespace dataproduct.api.Models;

/// <summary>Kho dữ liệu động — form xem/xuất; CotJson = cây cột (header key), xem KdlCot.</summary>
public partial class KDL_Form
{
    public int IdForm { get; set; }
    public string MaForm { get; set; } = null!;
    public string TenForm { get; set; } = null!;
    public string MaModule { get; set; } = null!;
    public string? MaBM { get; set; }
    public string CotJson { get; set; } = "[]";
    public bool IsActive { get; set; }
    public string? NguoiCapNhat { get; set; }
    public DateTime? NgayCapNhat { get; set; }
}

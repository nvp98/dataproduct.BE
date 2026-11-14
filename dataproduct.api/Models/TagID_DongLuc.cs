namespace dataproduct.api.Models;

/// <summary>Danh mục tag EMS của vùng Động lực (Kho dữ liệu). TinhTrang: 1 = kéo dữ liệu, 0 = ngưng.</summary>
public partial class TagID_DongLuc
{
    public int Id { get; set; }
    public string TagIDEMS { get; set; } = null!;
    public string MaHeader { get; set; } = null!;
    public string? YNghia { get; set; }
    public string MaXuong { get; set; } = null!;
    public byte TinhTrang { get; set; } = 1;
    public int ThuTu { get; set; }
    public bool IsDelete { get; set; }
    public string? NguoiTao { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.Now;
    public string? NguoiCapNhat { get; set; }
    public DateTime? NgayCapNhat { get; set; }
}

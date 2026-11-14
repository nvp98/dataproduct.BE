namespace dataproduct.api.Models;

/// <summary>
/// Giá trị động lực theo ca, kéo từ API EMS (chỉ đọc trên phần mềm). Khóa nghiệp vụ (Ngay, CaSX, TagID).
/// CaSX: 1 = 08:00–20:00, 2 = 20:00–08:00 (tính vào ngày bắt đầu ca).
/// </summary>
public partial class DuLieu_DongLuc
{
    public long Id { get; set; }
    public DateOnly Ngay { get; set; }
    public byte CaSX { get; set; }
    public string MaHeader { get; set; } = null!;
    public string TagID { get; set; } = null!;
    public decimal? GiaTri { get; set; }
    public DateTime NgayCapNhat { get; set; }
    public string MaXuong { get; set; } = null!;
}

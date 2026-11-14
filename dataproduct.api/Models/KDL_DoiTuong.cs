namespace dataproduct.api.Models;

/// <summary>
/// Kho dữ liệu động — header: 1 dòng / mẻ | ca | ngày … tùy module. Khóa nghiệp vụ (MaModule, Me).
/// Chiều đặt tên chung: MayDuc (máy đúc, lò…), Mac (mác, vật liệu…), KichThuoc (kích thước…), LenhSanXuat (LSX).
/// </summary>
public partial class KDL_DoiTuong
{
    public int IdDoiTuong { get; set; }
    public string MaModule { get; set; } = null!;
    public string Me { get; set; } = null!;
    public DateOnly NgaySX { get; set; }
    public byte? CaSX { get; set; }
    public string? Kip { get; set; }
    public string? MayDuc { get; set; }
    public string? Mac { get; set; }
    public string? KichThuoc { get; set; }
    public string? LenhSanXuat { get; set; }
    public string? ThuocTinhJson { get; set; }
    public bool IsDelete { get; set; }
    public DateTime NgayDongBo { get; set; }
}

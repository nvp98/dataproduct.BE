namespace dataproduct.api.Models;

/// <summary>Nhật ký mỗi lần đồng bộ Động lực từ EMS. NguonChay: "auto" (worker) hoặc username.</summary>
public partial class DongLuc_SyncLog
{
    public int Id { get; set; }
    public DateTime ThoiGianBatDau { get; set; }
    public DateTime? ThoiGianKetThuc { get; set; }
    public DateOnly TuNgay { get; set; }
    public DateOnly DenNgay { get; set; }
    public string? MaXuong { get; set; }
    public int SoDong { get; set; }
    public bool ThanhCong { get; set; }
    public string? LoiChiTiet { get; set; }
    public string? NguonChay { get; set; }
}

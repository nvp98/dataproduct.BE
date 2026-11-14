namespace dataproduct.api.Models;

/// <summary>Kho dữ liệu động — nhật ký đồng bộ theo module.</summary>
public partial class KDL_SyncLog
{
    public int Id { get; set; }
    public string MaModule { get; set; } = null!;
    public string? MaNguon { get; set; }
    public DateOnly TuNgay { get; set; }
    public DateOnly DenNgay { get; set; }
    public int SoDong { get; set; }
    public bool ThanhCong { get; set; }
    public string? LoiChiTiet { get; set; }
    public string? NguonChay { get; set; }
    public DateTime ThoiGianBatDau { get; set; }
    public DateTime? ThoiGianKetThuc { get; set; }
}

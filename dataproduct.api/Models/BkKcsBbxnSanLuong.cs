namespace dataproduct.api.Models;

public partial class BkKcsBbxnSanLuong
{
    public long Id { get; set; }
    public string? Ca { get; set; }
    public string? SanPham { get; set; }
    public string? MacThep { get; set; }
    public double? ChieuDai { get; set; }
    public long? SoBo { get; set; }
    public decimal? SoThanh { get; set; }
    public double? KhoiLuong { get; set; }
    public string? TenPhanLoai { get; set; }
    public DateOnly? NgaySX { get; set; }
    public string? TenCa { get; set; }
    public string? IDXuongCan { get; set; }
    public string? TenXuongCan { get; set; }
    public DateTime? NgayTao { get; set; }
    public Guid? IDPhieu { get; set; }
    public int? TinhTrang { get; set; }
}

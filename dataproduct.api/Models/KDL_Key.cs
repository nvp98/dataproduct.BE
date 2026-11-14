namespace dataproduct.api.Models;

/// <summary>
/// Kho dữ liệu động — từ điển key (header key) của 1 module: ý nghĩa, cách lấy, công thức, mapping mã gốc nguồn.
/// Mã enum xem KdlConst (Services/Kdl/KdlConst.cs).
/// </summary>
public partial class KDL_Key
{
    public int IdKey { get; set; }
    public string MaModule { get; set; } = null!;
    public string MaKey { get; set; } = null!;
    public string? TenKey { get; set; }
    public string? DonVi { get; set; }
    public byte KieuDL { get; set; }
    public byte CachLay { get; set; }
    public byte KieuTongHop { get; set; }
    public string? MaNguon { get; set; }
    /// <summary>Mã gốc của nguồn, nhiều mã cách nhau ';'. Hậu tố '*' = khớp tiền tố (mã khớp chính xác được ưu tiên).</summary>
    public string? MaGoc { get; set; }
    public string? CongThuc { get; set; }
    public string? TenXuLy { get; set; }
    public string? MaKeyTuSo { get; set; }
    public string? MaKeyMauSo { get; set; }
    public decimal? HeSo { get; set; }
    public byte SoLe { get; set; }
    public int ThuTu { get; set; }
    public bool IsActive { get; set; }
    public string? NguoiTao { get; set; }
    public DateTime NgayTao { get; set; }
    public string? NguoiCapNhat { get; set; }
    public DateTime? NgayCapNhat { get; set; }
}

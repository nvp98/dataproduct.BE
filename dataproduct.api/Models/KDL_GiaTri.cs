namespace dataproduct.api.Models;

/// <summary>
/// Kho dữ liệu động — giá trị dọc (columnstore, không có PK). Duy nhất theo (IdDoiTuong, IdKey, Nguon).
/// Nguon: 1 tự động, 2 tính, 3 nhập tay, 4 điều chỉnh. Ghi bằng SQL / SqlBulkCopy, EF chỉ đọc.
/// </summary>
public partial class KDL_GiaTri
{
    public DateOnly NgaySX { get; set; }
    public byte? CaSX { get; set; }
    public int IdDoiTuong { get; set; }
    public int IdKey { get; set; }
    public byte Nguon { get; set; }
    public decimal? GiaTri { get; set; }
    public string? GiaTriText { get; set; }
    public string? NguoiCapNhat { get; set; }
    public DateTime NgayCapNhat { get; set; }
}

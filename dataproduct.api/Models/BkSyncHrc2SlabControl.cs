namespace dataproduct.api.Models;

public class BkSyncHrc2SlabControl
{
    public int Id { get; set; }
    public string? TableName { get; set; }
    public string? TrangThai { get; set; }
    public string? GhiChu { get; set; }
    public DateTime? BatDauLuc { get; set; }
    public DateTime? KetThucLuc { get; set; }
}

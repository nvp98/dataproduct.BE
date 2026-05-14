namespace dataproduct.api.Models;

public partial class MayDuc
{
    public int Id { get; set; }
    public string TenMayDuc { get; set; } = null!;
    public byte NhaMay { get; set; }
    public bool? IsLock { get; set; }
}


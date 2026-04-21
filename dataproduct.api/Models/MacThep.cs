namespace dataproduct.api.Models;

public partial class MacThep
{
    public int Id { get; set; }
    public string TenMacThep { get; set; } = null!;
    public byte NhaMay { get; set; }
    public bool? IsLock { get; set; }
    public bool? IsXacNhan { get; set; }
    public int? IdMayDuc {get;set;}
}


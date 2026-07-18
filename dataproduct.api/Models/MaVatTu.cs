namespace dataproduct.api.Models;

public class MaVatTu
{
    public int Id { get; set; }
    public string NhaMay { get; set; } = null!;
    public string MacThep { get; set; } = null!;
    public string VatTuCode { get; set; } = "";
    public string TenVatTu { get; set; } = "";
    public bool? IsLock { get; set; }
    public DateTime NgayTao { get; set; }
}

namespace dataproduct.api.Models;

public partial class DonTrongPhoi
{
    public int Id { get; set; }
    public string MacPhoi { get; set; } = null!;
    public decimal DonTrong { get; set; }
    public string? Mac { get; set; }
    public string? KichThuoc { get; set; }
}

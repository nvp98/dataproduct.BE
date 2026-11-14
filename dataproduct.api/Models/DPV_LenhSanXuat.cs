namespace dataproduct.api.Models;

/// <summary>Lệnh sản xuất Xưởng Đúc phôi vuông (NM.HRC1) — 1 dòng / (Mác, KT, Máy đúc), không theo tháng.</summary>
public partial class DPV_LenhSanXuat
{
    public int Id { get; set; }
    public int? IdPB { get; set; }
    public int? IdXuong { get; set; }
    public string Mac { get; set; } = null!;
    public string KichThuoc { get; set; } = null!;
    public int MayDuc { get; set; }
    public string? MaLenh { get; set; }
    public string? LenhSanXuat { get; set; }
    public string? GhiChu { get; set; }
    public bool IsDelete { get; set; }
    public string? NguoiTao { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.Now;
    public string? NguoiCapNhat { get; set; }
    public DateTime? NgayCapNhat { get; set; }
}

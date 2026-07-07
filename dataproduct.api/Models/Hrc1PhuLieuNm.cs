using System;

namespace dataproduct.api.Models;

public partial class Hrc1PhuLieuNm
{
    public int ID { get; set; }
    public string TenPhuLieu { get; set; } = null!;
    public string? TenPhuLieuNM { get; set; }
    public bool DangSuDung { get; set; } = true;
    public bool IsNM { get; set; }
    public int? ThuTu { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.Now;
    public string? NguoiTao { get; set; }
}

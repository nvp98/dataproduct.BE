using System;

namespace dataproduct.api.Models;

public partial class Hrc1TieuHao
{
    public int ID { get; set; }
    public int? IDNM { get; set; }
    public bool IsNM { get; set; } = true;
    public bool IsEdited { get; set; }
    public string? BieuMau { get; set; }
    public int? Scope { get; set; }
    public string? MeThoi { get; set; }
    public string? MacThep { get; set; }
    public double? O2 { get; set; }
    public double? N2 { get; set; }
    public double? AR { get; set; }
    public bool? IsChuyenCa { get; set; }
    public int? CaChuyen { get; set; }
    public bool? IsTrungMeThoi { get; set; }
    public int? QueLayMau { get; set; }
    public int? QueDoNhiet { get; set; }
    public string? GhiChu { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? NgayXoa { get; set; }
    public int? NguoiXoa { get; set; }
    public DateTime? ThoiDiemKetThuc { get; set; }
    public decimal? KLGang { get; set; }
    public decimal? KLGangLongCCT { get; set; }
    public decimal? KLThepPhe { get; set; }
    public decimal? KLThepPheGang { get; set; }
    public decimal? KLThepLong { get; set; }
    public byte? Ca { get; set; }
    public DateOnly? NgaySanXuat { get; set; }
    public DateTime? ThoiDiemBatDau { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.Now;
    public int? NguoiTao { get; set; }
    public DateTime? NgayCapNhat { get; set; }
    public int? NguoiCapNhat { get; set; }
}

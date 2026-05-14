using System;

namespace dataproduct.api.Models;

public partial class CtdPhieuXuLyKph
{
    public int Id { get; set; }

    public Guid? IdPhieu { get; set; }

    public string? InSanPham { get; set; }

    public string? InMacThep { get; set; }

    public string? InChieuDai { get; set; }

    public string? InSoMe { get; set; }

    public int? InSoThanh { get; set; }

    public decimal? InKhoiLuong { get; set; }

    public string? InCaNgaySx { get; set; }

    public string? InLoai { get; set; }

    public string? Reason { get; set; }

    public string? Measures { get; set; }

    public string? NewSanPham { get; set; }

    public string? NewMacThep { get; set; }

    public string? NewChieuDai { get; set; }

    public string? NewSoMe { get; set; }

    public int? NewSoThanh { get; set; }

    public decimal? NewKhoiLuong { get; set; }

    public string? NewLoai { get; set; }

    public DateTime? CreatedAt { get; set; }

    // Processing info
    public DateOnly? NgayXL { get; set; }
    public int? CaXL { get; set; }
    public string? KipXL { get; set; }

    public string? LenhSanXuat { get; set; }

}

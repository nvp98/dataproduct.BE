using System;
using System.Collections.Generic;

namespace dataproduct.api.Models;

public partial class BkNguyenLieu
{
    public int Id { get; set; }

    public string? TenNvl { get; set; }

    public DateOnly? NgaySx { get; set; }

    public int? Ca { get; set; }

    public string? Kip { get; set; }

    public string? GioLayMau { get; set; }

    public string? Silo { get; set; }

    public string? GhiChu { get; set; }

    public decimal? DoAm { get; set; }

    public string? GioNhapBk { get; set; }

    public int? TronId { get; set; }
}

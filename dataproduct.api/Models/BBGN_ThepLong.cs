using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;

namespace dataproduct.api.Models;

public partial class BBGN_ThepLong
{
    public int Id { get; set; }
    public string? MayDuc { get; set; }
    public string? Me { get; set; }
    public string? MacThep { get; set; }
    public string? ThungSo { get; set; }
    public string? ThoiGian { get; set; }
    public decimal? KlLan1 { get; set; }
    public decimal? KlLan2 { get; set; }
    public decimal? KlLan3 { get; set; }
    public decimal? KlThepLong { get; set; }
    public string? GhiChu { get; set; }
    public string? TinhLuyenLenThang { get; set; }
    public string? PhanLoai { get; set; }
    public DateTime? NgaySX { get; set; }
    public int? Ca { get; set; }
    public string? BieuMau { get; set; }
    public int? Scope { get; set; }
    public Guid IdPhieu { get; set; }
    public bool? IsGhost { get; set; }
    public bool? IsTrungMeThoi { get; set; }
}



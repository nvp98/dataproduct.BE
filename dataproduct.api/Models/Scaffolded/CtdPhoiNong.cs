using System;
using System.Collections.Generic;

namespace dataproduct.api.Models.Scaffolded;

public partial class CtdPhoiNong
{
    public int Id { get; set; }

    public Guid? Idphieu { get; set; }

    public DateOnly? NgaySx { get; set; }

    public int? Ca { get; set; }

    public string? Kip { get; set; }

    public string? Me { get; set; }

    public string? Mac { get; set; }

    public string? KichThuoc { get; set; }

    public int? Nmcan { get; set; }

    public int? SoThanhLoai1 { get; set; }

    public double? KhoiLuongLoai1 { get; set; }

    public int? SoThanhLoai2 { get; set; }

    public double? KhoiLuongLoai2 { get; set; }

    public int? SoThanhLoai3 { get; set; }

    public double? KhoiLuongLoai3 { get; set; }

    public int? TongSt { get; set; }

    public double? TongKl { get; set; }

    public string? CaKip { get; set; }

    public int? IdBkPhoiThep { get; set; }

    public int? TinhTrang { get; set; }

    public string? GhiChu { get; set; }

    public DateTime? NgayTao { get; set; }

    public int? TinhTrangCtd { get; set; }

    public int? TinhTrangQlcl { get; set; }
}

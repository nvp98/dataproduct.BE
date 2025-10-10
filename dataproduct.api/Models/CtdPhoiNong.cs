using System;
using System.Collections.Generic;

namespace dataproduct.api.Models;

public partial class CtdPhoiNong
{
    public int Id { get; set; }

    public Guid? Idphieu { get; set; }

    public string? Me { get; set; }

    public string? Mac { get; set; }

    public string? KichThuoc { get; set; }

    public int? SoThanhLoai1 { get; set; }

    public double? KhoiLuongLoai1 { get; set; }

    public int? SoThanhLoai2 { get; set; }

    public double? KhoiLuongLoai2 { get; set; }

    public int? SoThanhLoai3 { get; set; }

    public double? KhoiLuongLoai3 { get; set; }

    public double? TongKl { get; set; }
}

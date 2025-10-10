using System;
using System.Collections.Generic;

namespace dataproduct.api.Models;

public partial class CtdPhoiNguoi
{
    public int Id { get; set; }

    public Guid? PhieuId { get; set; }

    public string? Me { get; set; }

    public string? Mac { get; set; }

    public string? KichThuoc { get; set; }

    public int? SoThanh { get; set; }

    public double? KhoiLuong { get; set; }

    public double? TongKl { get; set; }

    public string? GhiChu { get; set; }
}

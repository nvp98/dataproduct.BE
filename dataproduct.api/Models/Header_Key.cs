using System;
using System.Collections.Generic;

namespace dataproduct.api.Models;

public partial class Header_Key
{
    public int Id { get; set; }
   
    public Guid KeyGuid  { get; set; } = Guid.NewGuid();
    public string TenHienThi { get; set; }
    public string? Mota { get; set; }
    public string? LoaiPhieu { get; set; }
    public bool IsActive { get; set; }
    public DateTime? NgayTao { get; set; }
    public decimal? ThuTu { get; set; }
}



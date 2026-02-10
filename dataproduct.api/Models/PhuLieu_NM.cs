using System;

namespace dataproduct.api.Models;

public partial class PhuLieu_NM
{
    public int Id { get; set; }
    public int ID_PhuLieu { get; set; }
    public string? TenPhuLieu { get; set; }
    public DateTime? NgayTao { get; set; }
    public bool IsActive { get; set; }
}



using System;
namespace dataproduct.api.Models;

public partial class BmKiemKePhuLieu
{
    public long ID { get; set; }

    public DateTime NgaySX { get; set; }

    public string Ca { get; set; } = null!;

    public int? Scope { get; set; }

    public int ID_HeaderKey { get; set; }

    public int ID_PhuLieuNM { get; set; }

    public int ID_Silo { get; set; }

    public decimal? TheTich { get; set; }

    public decimal? TyTrong { get; set; }

    public DateTime NgayTao { get; set; } = DateTime.Now;
}



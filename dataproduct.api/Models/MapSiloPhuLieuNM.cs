using System;
namespace dataproduct.api.Models;

public partial class MapSiloPhuLieuNM
{
    public int ID { get; set; }

    public int ID_Silo { get; set; }

    public int ID_PhuLieuNM { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime NgayBatDau { get; set; }

    public DateTime? NgayKetThuc { get; set; }
}



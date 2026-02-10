using System;
namespace dataproduct.api.Models;

public partial class Silo
{
    public int Id { get; set; }
    public required string TenSilo { get; set; }
    public decimal TheTich {get; set;}
    public required string BieuMau { get; set; }
    public int? Scope {get; set;}
    public DateTime? NgayTao { get; set; }
    public bool TinhTrang { get; set; }
    public int NhaMay { get; set; }
}



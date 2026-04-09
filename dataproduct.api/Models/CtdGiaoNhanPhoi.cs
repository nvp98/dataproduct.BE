using System;

namespace dataproduct.api.Models;

public partial class CtdGiaoNhanPhoi
{
    public int Id { get; set; }

    public string? MaViTri { get; set; }

    public string? ViTri { get; set; }

    public string? MacThep { get; set; }

    public string? KichThuoc { get; set; }

    public int? SoCay { get; set; }

    public string? GhiChu { get; set; }

    public Guid? IdPhieu { get; set; }
}

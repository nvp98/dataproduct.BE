using System;

namespace dataproduct.api.Models;

public partial class CtdSoTheoDoi
{
    public int Id { get; set; }

    public Guid? Idphieu { get; set; }

    public int? LoaiMacPhoi { get; set; }

    public string? TenMacPhoi { get; set; }

    public string? KichThuoc { get; set; }

    public int? PhoiRaLo { get; set; }

    public int? PhoiHoiLo { get; set; }

    public int? PhoiRaSan { get; set; }

    public int? PhoiPheCn { get; set; }

    public string? LoaiSp { get; set; }

    public string? MacThep { get; set; }

    public string? LenhSanXuat { get; set; }
    public int? LoaiPhoi { get; set; }
}

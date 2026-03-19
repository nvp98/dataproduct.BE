using System;

namespace dataproduct.api.Models;

public partial class CtdStdDienBien
{
    public int Id { get; set; }

    public Guid? Idphieu { get; set; }

    public TimeOnly? TuGio { get; set; }

    public TimeOnly? DenGio { get; set; }

    public string? ThietBi { get; set; }

    public string? MoTa { get; set; }

    public string? LoaiSuCo { get; set; }

    public string? PheCongNghe { get; set; }
}

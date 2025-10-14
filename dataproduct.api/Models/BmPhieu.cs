using System;
using System.Collections.Generic;

namespace dataproduct.api.Models;

public partial class BmPhieu
{
    public Guid Idphieu { get; set; }

    public string MaBm { get; set; } = null!;

    public string? SoPhieu { get; set; }

    public int? XuongId { get; set; }

    public int? IdphongBan { get; set; }

    public int? Idkip { get; set; }

    public int? Ca { get; set; }

    public string? Kip { get; set; }
    public DateOnly? NgaySX { get; set; }

    public DateTime? NgayTao { get; set; }

    public int? MayDuc { get; set; }

    public int? NguoiTaoId { get; set; }

    public int? TinhTrang { get; set; }

    public string? DataJson { get; set; }

    public int? IsDelete { get; set; }

    public int? IsLock { get; set; }
}

using System;
using System.Collections.Generic;

namespace dataproduct.api.Models;

public partial class BmPheDuyet
{
    public int Id { get; set; }

    public Guid? PhieuId { get; set; }

    public int? CapDuyet { get; set; }

    public int? NguoiDuyetId { get; set; }

    public DateTime? NgayDuyet { get; set; }

    public string? GhiChu { get; set; }

    public int? TinhTrang { get; set; }
}

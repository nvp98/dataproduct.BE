using System;
using System.Collections.Generic;

namespace dataproduct.api.Models;

public partial class BmPhieuChiTiet
{
    public int Id { get; set; }

    public Guid? PhieuId { get; set; }

    public string? ThongSo { get; set; }

    public string? GiaTri { get; set; }

    public Guid? RowId { get; set; }
}

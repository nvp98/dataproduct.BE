using System;
using System.Collections.Generic;

namespace dataproduct.api.Models;

public partial class DLNM_HRC2
{
    public int Id { get; set; }
    public int REPORT_NO { get; set; }
    public DateTime? NgaySx { get; set; }
    public DateTime? Ngay { get; set; }   // ❗ đổi lại DateTime, vì SQL là datetime
    public int Ca { get; set; }
    public string? BieuMau { get; set; }
    public int? Scope { get; set; }
    public string? MeThoi { get; set; }
    public string? MacThep { get; set; }

    public double? O2 { get; set; }
    public double? AR_RH { get; set; }
    public double? N2 { get; set; }
    public double? AR_BOF { get; set; }
    public double? AR_LF { get; set; }

    public int? ID_PhuLieu { get; set; }
    public string? TenPhuLieu { get; set; }

    public double? KLPhuGia { get; set; }
    public double? KLGangLong { get; set; }
    public double? KLThepPhe { get; set; }
}



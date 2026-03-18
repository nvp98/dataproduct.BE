using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace dataproduct.api.Models;

public partial class DLNM_HRC2
{
    public long ID { get; set; }
    public int? REPORT_NO { get; set; }
    public DateTime? NgaySx { get; set; }
    public DateTime? Ngay { get; set; }  
    public int? Ca { get; set; }
    public string? BieuMau { get; set; }
    public int? Scope { get; set; }
    public string? MeThoi { get; set; }
    public string? MacThep { get; set; }
    public double? O2 { get; set; }
    public double? AR_RH { get; set; }
    public double? N2 { get; set; }
    public double? AR_BOF { get; set; }
    public double? AR_LF { get; set; }
    public double? KLGangLong { get; set; }
    public double? KLThepPhe { get; set; }
    public double? KLThepLong { get; set; }
    public int? QueLayMau { get; set; }
    public int? QueDoNhiet { get; set; }
    public string? GhiChu { get; set; }
    public bool? IsNM { get; set; }
    public bool? IsChuyenCa  { get; set; }
    public double? KLGangLongCCT { get; set; }
    public double? KLGangLongCR { get; set; }
    public bool? IsTrungMeThoi { get; set; } = false;

    [NotMapped]
    public Guid TempKey { get; set; }
}



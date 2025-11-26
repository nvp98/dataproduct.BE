using System;
using System.Collections.Generic;

namespace dataproduct.api.Models;

public partial class HRC2_NM
{
    public decimal REPORT_NO { get; set; }
    public DateTime? PRODUCTION_DATE { get; set; } // PRODUCT_
    public DateTime? ShiftDate { get; set; }
    public byte? Shift { get; set; } 
    public string? PLANT { get; set; } // PRODUCT_ID
    public decimal? PLANT_NO { get; set; }// PRODUCT_ID
    public string? PRODUCT_ID { get; set; }        // PRODUCT_ID
    public string? GRADE_ID_PLAN { get; set; }       // GRADE_ID_PLAN
    public double? O2 { get; set; }
    public double? AR_RH { get; set; }
    public double? N2 { get; set; }
    public double? AR_BOF { get; set; }
    public double? AR_LF { get; set; }
    public decimal? MATERIAL_NO { get; set; }
    public string? DESCRIPTION_EN { get; set; }
    public double? KLPhuGia { get; set; }
    public double? KLGangLong { get; set; }
    public double? KLThepPhe { get; set; }
}



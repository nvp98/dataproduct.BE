using System;

namespace dataproduct.api.Models;

public partial class BkKcscanBbxlSanxuat
{
    public long Id { get; set; }

    public string? WorkshopName { get; set; }

    public string? Order { get; set; }

    public DateOnly? ProcessProductionDate { get; set; }

    public string? ProcessShiftName { get; set; }

    public string? NewProductName { get; set; }

    public string? Product { get; set; }

    public string? NewGradeCode { get; set; }

    public float? NewLength { get; set; }

    public int? NewNumOfBar { get; set; }

    public float? NewWeight { get; set; }

    public string? NewClassifyCode { get; set; }

    public string? Reason { get; set; }

    public string? Measures { get; set; }

    public string? InProductName { get; set; }

    public string? InProduct { get; set; }

    public string? InGradeCode { get; set; }

    public float? InLength { get; set; }

    public int? InNumOfBar { get; set; }

    public float? InWeight { get; set; }

    public string? InClassifyCode { get; set; }

    public string? InShiftName { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateOnly? NgayXL { get; set; }

    public string? CaXL { get; set; }

    public int? XuongCan { get; set; }
}

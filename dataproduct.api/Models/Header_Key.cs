using System;
using System.Collections.Generic;

namespace dataproduct.api.Models;

public partial class Header_Key
{
    public int Id { get; set; }

    public Guid KeyGuid  { get; set; } = Guid.NewGuid();
    public string TenHienThi { get; set; }
    public string? Mota { get; set; }
    public string? LoaiPhieu { get; set; }
    public bool IsActive { get; set; }
    public DateTime? NgayTao { get; set; }


    public bool? IsUsedNXT { get; set; }
    public decimal? TyTrong { get; set; }
    public bool? IsUsedThongKe { get; set; }
    public byte? LoaiThongKe { get; set; }
    /// <summary>Thứ tự hiển thị trong ThongKe BOF</summary>
    public int? ThuTu_TK_BOF { get; set; }
    /// <summary>Thứ tự hiển thị trong ThongKe LF/RH</summary>
    public int? ThuTu_TK_LFRH { get; set; }

    // --- Excel export columns ---
    public bool? IsUsed_Excel { get; set; }
    /// <summary>1 = BOF, 2 = LF/RH, 3 = All</summary>
    public byte? LoaiExcel { get; set; }
    /// <summary>Thứ tự hiển thị trong Excel BOF</summary>
    public int? ThuTu_Excel_BOF { get; set; }
    /// <summary>Thứ tự hiển thị trong Excel LF/RH</summary>
    public int? ThuTu_Excel_LFRH { get; set; }

    /// <summary>FK → Header_Nhom.Id. Khi có giá trị, Header_Key này bị ẩn khỏi cột riêng;
    /// giá trị được cộng vào cột nhóm (IDHeaderKey = -Header_Nhom.Id).</summary>
    public int? ID_NhomKey { get; set; }

    /// <summary>Mã vật tư bên hệ thống chi phí. NULL = không feed sang ChiPhi_ProductionData.</summary>
    public string? MaVatTuChiPhi { get; set; }
}

/// <summary>Các cột "thứ tự" của Header_Key — mỗi cột là 1 không gian số thứ tự độc lập
/// (vd TT_TK_BOF=1 và TT_TK_LFRH=1 không phải trùng nhau).</summary>
public enum ThuTuColumn
{
    TK_BOF,
    TK_LFRH,
    Excel_BOF,
    Excel_LFRH
}

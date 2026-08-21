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
    /// <summary>Bitmask: BOF=1, LF=2, RH=4 (kết hợp tự do, vd LF+RH=6, cả 3=7)</summary>
    public byte? LoaiThongKe { get; set; }
    /// <summary>Thứ tự hiển thị trong ThongKe BOF</summary>
    public int? ThuTu_TK_BOF { get; set; }
    /// <summary>Thứ tự hiển thị trong ThongKe LF</summary>
    public int? ThuTu_TK_LF { get; set; }
    /// <summary>Thứ tự hiển thị trong ThongKe RH</summary>
    public int? ThuTu_TK_RH { get; set; }

    // --- Excel export columns ---
    public bool? IsUsed_Excel { get; set; }
    /// <summary>Bitmask: BOF=1, LF=2, RH=4 (kết hợp tự do, vd LF+RH=6, cả 3=7)</summary>
    public byte? LoaiExcel { get; set; }
    /// <summary>Thứ tự hiển thị trong Excel BOF</summary>
    public int? ThuTu_Excel_BOF { get; set; }
    /// <summary>Thứ tự hiển thị trong Excel LF</summary>
    public int? ThuTu_Excel_LF { get; set; }
    /// <summary>Thứ tự hiển thị trong Excel RH</summary>
    public int? ThuTu_Excel_RH { get; set; }

    /// <summary>FK → Header_Nhom.Id. Khi có giá trị, Header_Key này bị ẩn khỏi cột riêng;
    /// giá trị được cộng vào cột nhóm (IDHeaderKey = -Header_Nhom.Id).</summary>
    public int? ID_NhomKey { get; set; }

    /// <summary>Mã vật tư bên hệ thống chi phí. NULL = không feed sang ChiPhi_ProductionData.</summary>
    public string? MaVatTuChiPhi { get; set; }
}

/// <summary>Các cột "thứ tự" của Header_Key — mỗi cột là 1 không gian số thứ tự độc lập
/// (vd TT_TK_BOF=1 và TT_TK_LF=1 không phải trùng nhau).</summary>
public enum ThuTuColumn
{
    TK_BOF,
    TK_LF,
    TK_RH,
    Excel_BOF,
    Excel_LF,
    Excel_RH
}

/// <summary>Bitmask cho Header_Key.LoaiThongKe / LoaiExcel — 1 Header_Key có thể thuộc
/// nhiều biểu mẫu cùng lúc (vd LF+RH nhưng không BOF).</summary>
[Flags]
public enum LoaiBieuMauFlags : byte
{
    BOF = 1,
    LF = 2,
    RH = 4,
}

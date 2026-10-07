using System;

namespace dataproduct.api.Models;

public partial class Hrc1PhuLieu
{
    public int ID { get; set; }
    public int MeID { get; set; }
    public int? PhuLieuID { get; set; }
    public string? TenPhuLieu { get; set; }
    public decimal? KLPhuGia { get; set; }
    public decimal? KLPhanBo { get; set; }
    public int? ID_HeaderKey { get; set; }
    public bool IsManual { get; set; }
    public decimal? KLPhuGia_Manual { get; set; }
    public bool IsAddManual { get; set; }
    /// <summary>True = dòng phân bổ chênh lệch xuất-nhập-tồn (module HRC1_STD_NXT), PhuLieuID vẫn giữ
    /// nguyên giá trị đang được phân bổ — không phải phụ liệu thực đo. Mirror PhuLieu_HRC2.IsPhanBo.</summary>
    public bool IsPhanBo { get; set; }
    public bool IsNM { get; set; } = true;
    public bool IsEdited { get; set; }
    public bool IsDeleted { get; set; }
    public string? NguoiTao { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.Now;
    public DateTime? NgayCapNhat { get; set; }
    public string? NguoiCapNhat { get; set; }
}

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace dataproduct.api.Models;

public class STD_NXT_TOTAL_HRC1
{
    [Key]
    public int Id { get; set; }

    public int Ca { get; set; }

    public DateTime NgaySX { get; set; }

    /// <summary>FK HRC1_PhuLieuNM.ID.</summary>
    public int PhuLieuID { get; set; }

    [StringLength(200)]
    public string? TenNguyenLieu { get; set; }

    [Column(TypeName = "decimal(18,3)")]
    public decimal? TongTonDauCa { get; set; }

    [Column(TypeName = "decimal(18,3)")]
    public decimal? TongTonNhapTrongCa { get; set; }

    [Column(TypeName = "decimal(18,3)")]
    public decimal? TongTonCuoiCa { get; set; }

    [Column(TypeName = "decimal(18,3)")]
    public decimal? TongSuDung { get; set; }

    [Column(TypeName = "decimal(18,3)")]
    public decimal? TongSDTrenSoSach { get; set; }

    [Column(TypeName = "decimal(18,3)")]
    public decimal? ChenhLech { get; set; }

    public Guid Id_Phieu { get; set; }
    public bool? HasPhanBo { get; set; }

    [Column(TypeName = "decimal(9,4)")]
    public decimal? TyLeBOF { get; set; }

    /// <summary>KHÔNG có TyLeRH — HRC1 không có công đoạn RH.</summary>
    [Column(TypeName = "decimal(9,4)")]
    public decimal? TyLeLF { get; set; }

    [Column(TypeName = "decimal(18,3)")]
    public decimal? KLPB_BOF { get; set; }

    [Column(TypeName = "decimal(18,3)")]
    public decimal? KLPB_LF { get; set; }

    /// <summary>Tổng khối lượng "Thống Kê" hiệu lực của PhuLieuID này, gộp mọi Scope BOF (1-5), tại thời
    /// điểm bấm Phân bổ/Không phân bổ gần nhất — cùng công thức api/DLNMHRC1/search-thongke, đã bao gồm
    /// phần phân bổ nếu có. NULL nếu chưa bấm 1 trong 2 nút, hoặc vừa Thu hồi phân bổ (reset chờ quyết định
    /// lại). Xem STD_XNT_HRC1Repository.ComputeKLTKAsync.</summary>
    [Column(TypeName = "decimal(18,3)")]
    public decimal? KLTK_BOF { get; set; }

    /// <summary>Như KLTK_BOF, gộp mọi Scope LF (6-10).</summary>
    [Column(TypeName = "decimal(18,3)")]
    public decimal? KLTK_LF { get; set; }
}

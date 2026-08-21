using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace dataproduct.api.Models;

public class STD_XUAT_NHAP_TON_HRC1
{
    [Key]
    public int Id { get; set; }

    public int Ca { get; set; }

    public DateTime NgaySX { get; set; }

    /// <summary>Giá trị enum ToHopSTDNXT_HRC1 (1-10), KHÔNG phải số lò/tổ vật lý.</summary>
    public int Scope { get; set; }
    public int? ViTri { get; set; }

    [StringLength(50)]
    public string? BieuMau { get; set; }

    /// <summary>FK HRC1_PhuLieuNM.ID.</summary>
    public int PhuLieuID { get; set; }

    [StringLength(200)]
    public string? TenNguyenLieu { get; set; }

    [Column(TypeName = "decimal(18,3)")]
    public decimal? TonDauCa { get; set; }

    [StringLength(200)]
    public string? TuongQuanDauCa { get; set; }

    [Column(TypeName = "decimal(18,3)")]
    public decimal? NhapVaoTrongCa { get; set; }

    [Column(TypeName = "decimal(18,3)")]
    public decimal? MucLieu { get; set; }

    [Column(TypeName = "decimal(18,3)")]
    public decimal? TheTich { get; set; }

    [Column(TypeName = "decimal(18,3)")]
    public decimal? TyTrong { get; set; }

    [Column(TypeName = "decimal(18,3)")]
    public decimal? TonCuoiCa { get; set; }

    [StringLength(200)]
    public string? TuongQuanCuoiCa { get; set; }

    [Column(TypeName = "decimal(18,3)")]
    public decimal? TongThucTe { get; set; }

    public Guid Id_Phieu { get; set; }
    public int? IDSilo { get; set; }

    /// <summary>Thứ tự hiển thị dòng trong bảng (theo Scope), do người dùng sắp xếp trên UI.</summary>
    public int? ThuTu { get; set; }

    [Column(TypeName = "decimal(18,3)")]
    public decimal? LuongSuDungKiemKe { get; set; }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace dataproduct.api.Models;

public class STD_XUAT_NHAP_TON_HRC2
{
    [Key]
    public int Id { get; set; }

    public int Ca { get; set; }

    public DateTime NgaySX { get; set; }

    public int Scope { get; set; }
    public int? ViTri { get; set; }

    [StringLength(200)]
    public string? BieuMau { get; set; }

    public int Id_HeaderKey { get; set; }

    [StringLength(255)]
    public string? TenNguyenLieu { get; set; }

    [Column(TypeName = "decimal(18,3)")]
    public decimal? TonDauCa { get; set; }

    [StringLength(255)]
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

    [StringLength(255)]
    public string? TuongQuanCuoiCa { get; set; }

    [Column(TypeName = "decimal(18,3)")]
    public decimal? TongThucTe { get; set; }

    public Guid Id_Phieu { get; set; }
    public int? IDSilo { get; set; }
}


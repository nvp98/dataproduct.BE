using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace dataproduct.api.Models;

public class STD_NXT_TOTAL_HRC2
{
    [Key]
    public int Id { get; set; }

    public int Ca { get; set; }

    public DateTime NgaySX { get; set; }

    public int Id_HeaderKey { get; set; }

    [StringLength(255)]
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

    
}


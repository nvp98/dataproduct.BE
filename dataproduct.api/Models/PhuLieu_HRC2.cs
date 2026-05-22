using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace dataproduct.api.Models;

public partial class PhuLieu_HRC2
{
    public int ID { get; set; }
    public int? REPORT_NO { get; set; }
    public string? BieuMau { get; set; }
    public string? MeThoi { get; set; }
    public int? ID_PhuLieu { get; set; }
    public string? TenPhuLieu { get; set; }
    public double? KLPhuGia { get; set; }
    public int? ID_HeaderKey { get; set; }
    public string? TenHienThi { get; set; }
    public long ID_MeThoi { get; set; }
    public bool? IsPhanBo { get; set; } = false;
    /// <summary>Không map ra DB (bảng PhuLieu_HRC2 không có cột Id_Phieu). Dùng [NotMapped] để tránh lỗi Invalid column name.</summary>
    [NotMapped]
    public Guid? Id_Phieu { get; set; }
    [NotMapped]
    public Guid TempKey { get; set; }

    public bool? IsManual { get; set; } = false;
    public double? KLPhuGia_Manual { get; set; }
    public bool? IsAddManual { get; set; } = false;
    
}


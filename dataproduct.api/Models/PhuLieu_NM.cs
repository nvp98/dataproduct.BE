using System;

namespace dataproduct.api.Models;

// Bảng master lưu danh sách Phụ liệu NM (được sync tự động từ PhuLieu_HRC2)
public partial class PhuLieu_NM
{
    public int Id { get; set; }
    public int ID_PhuLieu { get; set; }
    public string? TenPhuLieu { get; set; }
    public DateTime? NgayTao { get; set; }
    public bool IsActive { get; set; }
}



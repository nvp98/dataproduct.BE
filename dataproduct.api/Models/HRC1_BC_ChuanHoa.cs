namespace dataproduct.api.Models;

// Bảng tra cứu cho form Giao nhận Xưởng ĐPV (module KDL HRC1_DPV_GN)
// SQL: migrations/BCSX_HRC1_DucVuong/hrc1_bc_chuanhoa.sql — nghiệp vụ: docs/BCSX_HRC1_DucVuong/03-GiaoNhan-Form-Dong.md §5
// ID_Xuong / ID_PhongBan / ID_VatTu tham chiếu PRODUCTDATA (khác DB, không có FK).

/// <summary>Xưởng + bộ phận → mã viết tắt (sheet Excel Xuong_ChuanHoa_BCSX).</summary>
public class HRC1_BC_XuongChuanHoa
{
    public int ID { get; set; }
    public int? ID_Xuong { get; set; }
    public string Xuong { get; set; } = null!;
    public int? ID_PhongBan { get; set; }
    public string BoPhan { get; set; } = null!;
    public string VietTat { get; set; } = null!;
    public int ThuTu { get; set; }
    public bool IsActive { get; set; } = true;
    public string? NguoiTao { get; set; }
    public DateTime NgayTao { get; set; }
    public string? NguoiCapNhat { get; set; }
    public DateTime? NgayCapNhat { get; set; }
}

/// <summary>
/// Phân bổ BBGN: điều chỉnh tay + lý do cho 1 (ca, LSX, Nhóm 1, Xưởng); Nhom1 = XuongVietTat = "" là ghi chú cả dòng LSX.
/// SQL: migrations/BCSX_HRC1_DucVuong/hrc1_bc_phanbo.sql
/// </summary>
public class HRC1_BC_PhanBoDieuChinh
{
    public int ID { get; set; }
    public DateOnly NgaySX { get; set; }
    public byte CaSX { get; set; }
    public string? Kip { get; set; }
    public string LenhSanXuat { get; set; } = null!;
    public string Nhom1 { get; set; } = "";
    public string XuongVietTat { get; set; } = "";
    public decimal? DieuChinh { get; set; }
    public string? LyDo { get; set; }
    public string? GhiChu { get; set; }
    public string? NguoiTao { get; set; }
    public DateTime NgayTao { get; set; }
    public string? NguoiCapNhat { get; set; }
    public DateTime? NgayCapNhat { get; set; }
}

/// <summary>Vật tư + xưởng viết tắt (bên nhận) → Nhóm 1 = "Nội dung 1" (sheet Excel NVL_ChuanHoa_BCSX).</summary>
public class HRC1_BC_NVLChuanHoa
{
    public int ID { get; set; }
    public int? ID_VatTu { get; set; }
    public string TenVatTu { get; set; } = null!;
    public string XuongVietTat { get; set; } = null!;
    public string Nhom1 { get; set; } = null!;
    public bool CauHinhPhanBo { get; set; }
    public int ThuTu { get; set; }
    public bool IsActive { get; set; } = true;
    public string? NguoiTao { get; set; }
    public DateTime NgayTao { get; set; }
    public string? NguoiCapNhat { get; set; }
    public DateTime? NgayCapNhat { get; set; }
}

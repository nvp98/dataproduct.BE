namespace dataproduct.api.Models;

// Biên bản chốt số liệu Xưởng Đúc phôi vuông (BM.14/QT.05.11, MaBM = HRC1_BCSX_DPV).
// Phiếu ở BM_Phieu; 4 bảng chi tiết theo vùng biên bản. Script: migrations/BCSX_HRC1_DucVuong/hrc1_bc_bienbanchot.sql
// Tài liệu: docs/BCSX_HRC1_DucVuong/02-BienBan-ChotSoLieu.md

/// <summary>Cột chung của 4 bảng HRC1_BC_*.</summary>
public abstract class HRC1_BC_DongBase
{
    public int ID { get; set; }
    /// <summary>= BM_Phieu.IDPhieu (không FK vật lý).</summary>
    public Guid PhieuID { get; set; }
    public DateOnly NgaySX { get; set; }
    /// <summary>= PRODUCTDATA.Tbl_Kip.ID_Kip (khác DB, không FK).</summary>
    public int? IdKip { get; set; }
    public string? Kip { get; set; }
    public byte? CaSX { get; set; }
    public string? GhiChu { get; set; }
    /// <summary>1 tự động (lấy từ Kho dữ liệu), 3 nhập tay, 4 điều chỉnh — cùng mã KdlConst.Nguon.</summary>
    public byte Nguon { get; set; } = 3;
    public bool IsDelete { get; set; }
    public string? NguoiTao { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.Now;
    public string? NguoiCapNhat { get; set; }
    public DateTime? NgayCapNhat { get; set; }
}

/// <summary>I. Thép lỏng — 1 dòng / Kíp × Mác × LSX.</summary>
public class HRC1_BC_ThepLong : HRC1_BC_DongBase
{
    public string Mac { get; set; } = null!;
    public string DonViTinh { get; set; } = "kg";
    public int? SoLuongMe { get; set; }
    public int? MayDuc { get; set; }
    public decimal? TonDau { get; set; }
    public decimal? Nhap { get; set; }
    public decimal? Xuat { get; set; }
    public decimal? TonCuoi { get; set; }
    public string? LenhSanXuat { get; set; }
}

/// <summary>II. Điện — 1 dòng / Kíp × Nội dung.</summary>
public class HRC1_BC_Dien : HRC1_BC_DongBase
{
    public string NoiDung { get; set; } = "Điện";
    public string DonViTinh { get; set; } = "Kwh";
    public decimal? SoLieu { get; set; }
}

/// <summary>III. Nguyên vật liệu xuất cho các nhà máy.</summary>
public class HRC1_BC_NguyenVatLieu : HRC1_BC_DongBase
{
    public string NoiDung { get; set; } = null!;
    public string DonViTinh { get; set; } = "Kg";
    public decimal? SoLieu { get; set; }
    public decimal? SoLieuBoPhan { get; set; }
    public string? BoPhanLienQuan { get; set; }
    /// <summary>Không bắt buộc (vd Mê thép đúc).</summary>
    public string? LenhSanXuat { get; set; }
}

/// <summary>IV. Thành phẩm — 1 dòng / Mẻ × Kích thước (dòng ngắn dài: LaNganDai = true).</summary>
public class HRC1_BC_ThanhPham : HRC1_BC_DongBase
{
    public string Me { get; set; } = null!;
    public string? Mac { get; set; }
    public string? KichThuoc { get; set; }
    public int? MayDuc { get; set; }
    public string? LenhSanXuat { get; set; }
    public bool LaNganDai { get; set; }

    public int? Nguoi_SoThanh { get; set; }
    public decimal? Nguoi_Loai1 { get; set; }
    public decimal? Nguoi_Loai2BM { get; set; }
    public decimal? Nguoi_Loai2TPHH { get; set; }
    public decimal? Nguoi_Loai3 { get; set; }

    public int? Nong_SoThanh { get; set; }
    public decimal? Nong_Loai1 { get; set; }
    public decimal? Nong_Loai2BM { get; set; }
    public decimal? Nong_Loai2TPHH { get; set; }
    public decimal? Nong_Loai3 { get; set; }

    /// <summary>Cột tính PERSISTED trong DB (tổng 8 cột khối lượng) — chỉ đọc.</summary>
    public decimal? Tong { get; set; }
}

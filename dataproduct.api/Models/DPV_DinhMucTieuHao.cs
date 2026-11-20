namespace dataproduct.api.Models;

/// <summary>Định mức tiêu hao sản xuất — Xưởng Đúc phôi vuông (NM.HRC1). Tham chiếu cho BẢNG CHI TIẾT SẢN LƯỢNG - TIÊU HAO.</summary>
public partial class DinhMucTieuHao
{
    public int Id { get; set; }
    public int? IdPB { get; set; }
    public int? IdXuong { get; set; }
    public string? DayChuyen { get; set; }
    public string? BaoCao { get; set; }
    public string ChiTieu { get; set; } = null!;
    public string? DonViTinh { get; set; }
    // Định mức
    public decimal? MucSanLuong { get; set; }
    public decimal? DieuKienSanLuong { get; set; }
    // Điều kiện áp dụng
    public decimal? MucTieuHaoLDG { get; set; }
    public decimal? DieuKienTieuHaoLDG { get; set; }
    public decimal? MucTyLePhe { get; set; }
    public decimal? DieuKienTyLePhe { get; set; }
    // Thời gian áp dụng
    public DateOnly? TuNgay { get; set; }
    public DateOnly? DenNgay { get; set; }
    public string? GhiChu { get; set; }
    public bool IsDelete { get; set; }
    public string? NguoiTao { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.Now;
    public string? NguoiCapNhat { get; set; }
    public DateTime? NgayCapNhat { get; set; }
}

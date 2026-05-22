namespace dataproduct.api.DTOs.Export
{
    /// <summary>
    /// DTO cho tổng hợp Biên bản xác nhận sản lượng
    /// </summary>
    public class BmTongHopBbxnSanLuongRow
    {
        // Thông tin phiếu
        public Guid IdPhieu { get; set; }
        public DateOnly? NgaySX { get; set; }
        public string? TenCa { get; set; }
        public int? Ca { get; set; }
        public string? IDXuongCan { get; set; }
        public string? SoPhieu { get; set; }
        public int? TinhTrang { get; set; }

        // Thông tin sản phẩm
        public string? SanPham { get; set; }
        public string? MacThep { get; set; }
        public string? ChieuDai { get; set; }
        public long? SoBo { get; set; }
        public double? KhoiLuong { get; set; }
        public decimal? SoThanh { get; set; }
        public string? TenPhanLoai { get; set; }

        // Thông tin phê duyệt
        public string? NguoiLapPhieu { get; set; }
        public string? NguoiPheDuyet { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}


namespace dataproduct.api.DTOs.Export
{
    public class BmTongHopPhieuSanPhamKphRow
    {
        // Thông tin phiếu
        public Guid IdPhieu { get; set; }
        public DateOnly? NgaySX { get; set; }
        public int? CaSX { get; set; }
        public string? KipSX { get; set; }

        public DateOnly? NgayXL { get; set; }
        public int? CaXL { get; set; }
        public string? KipXL { get; set; }

        public string? LenhSanXuat { get; set; }

        public string? MayDuc { get; set; }
        public string? SoPhieu { get; set; }
        public int? TinhTrang { get; set; }

        // Thông tin sản phẩm đầu vào
        public string? InSanPham { get; set; }
        public string? InMacThep { get; set; }
        public string? InChieuDai { get; set; }
        public string? InSoMe { get; set; }
        public int? InSoThanh { get; set; }
        public decimal? InKhoiLuong { get; set; }
        public string? InCaNgaySx { get; set; }
        public string? InLoai { get; set; }

        // Lý do và biện pháp xử lý
        public string? Reason { get; set; }
        public string? Measures { get; set; }

        // Thông tin sản phẩm mới/sau xử lý
        public string? NewSanPham { get; set; }
        public string? NewMacThep { get; set; }
        public string? NewChieuDai { get; set; }
        public string? NewSoMe { get; set; }
        public int? NewSoThanh { get; set; }
        public decimal? NewKhoiLuong { get; set; }
        public string? NewLoai { get; set; }

        // Phê duyệt
        public string? NguoiLapPhieu { get; set; }
        public string? NguoiPheDuyet { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}

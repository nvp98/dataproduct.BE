namespace dataproduct.api.DTOs
{
    public class BK_PhoiThepDto
    {
        public int Id { get; set; }

        public int Ca { get; set; }

        public string Kip { get; set; } = null!;

        public DateOnly NgaySx { get; set; }

        public string? KichThuoc { get; set; }

        public double? ChieuDai { get; set; }

        public string Me { get; set; } = null!;

        public string? Mac { get; set; }

        public string? MauThu { get; set; }

        public int? MayDuc { get; set; }

        public int? SoThanh { get; set; }

        public double? TongKhoiLuog { get; set; }

        public int? LoaiId { get; set; }

        public string? TenLoai { get; set; }

        public string? LoaiPhoi { get; set; }

        public DateTime? NgayTaoBk { get; set; }

        public string? TenPhanLoai { get; set; }

        public string? VanChuyen { get; set; }

        public string? GhiChu { get; set; }

        public int? IsChuyen { get; set; }

        public int? IsNm { get; set; }

        public int? StDaChuyen { get; set; }

        public int? PhanLoaiPhoi { get; set; }

        public int? LoaiChatLuong { get; set; }
    }
}

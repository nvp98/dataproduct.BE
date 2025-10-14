namespace dataproduct.api.DTOs
{
    public class BM_PheDuyetDto
    {
        public int Id { get; set; }

        public Guid? PhieuId { get; set; }

        public int? CapDuyet { get; set; }

        public int? NguoiDuyetId { get; set; }
        public string? TenNguoiDuyet { get; set; }

        public DateTime? NgayDuyet { get; set; }

        public string? GhiChu { get; set; }

        public int? TinhTrang { get; set; }
    }
}

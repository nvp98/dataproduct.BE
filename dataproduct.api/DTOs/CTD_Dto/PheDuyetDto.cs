namespace dataproduct.api.DTOs.CTD_Dto
{
    public class PheDuyetDto
    {
        public int? CapDuyet { get; set; }
        public int? NguoiDuyetID { get; set; }

        public string HoVaTen { get; set; }
        public string? ChuKy { get; set; }

        public string? TenPhongBan { get; set; }
        public string? TenViTri { get; set; }

        public DateTime? NgayDuyet { get; set; }
        public int? TinhTrang { get; set; }
        public string? GhiChu { get; set; }
    }
}

namespace dataproduct.api.DTOs
{
    public class BmQuyenXlDto
    {
        public int Id { get; set; }
        public int? IdTaiKhoan { get; set; }
        public string? MaBm { get; set; }
        public string? MaKhuVuc { get; set; }
    }

    public class BmQuyenXlCreateDto
    {
        public int? IdTaiKhoan { get; set; }
        public string? MaBm { get; set; }
        public string? MaKhuVuc { get; set; }
    }

    public class BmQuyenXlUpdateDto
    {
        public int? IdTaiKhoan { get; set; }
        public string? MaBm { get; set; }
        public string? MaKhuVuc { get; set; }
    }
}

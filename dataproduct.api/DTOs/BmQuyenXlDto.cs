namespace dataproduct.api.DTOs
{
    public class BmQuyenXlDto
    {
        public int Id { get; set; }
        public int? IdTaiKhoan { get; set; }
        public string? MaBm { get; set; }
        public string? MaKhuVuc { get; set; }
        /// <summary>1=XULY, 2=PHEDUYET, 3=CHOT, 4=XULY_VA_PHEDUYET, 5=XEM</summary>
        public byte? QuyenChucNang { get; set; }
    }

    /// <summary>Dùng cho menu: Việc tôi bắt đầu, Việc đến tôi và Danh sách chỉ xem.</summary>
    public class MenuPermissionsDto
    {
        public IReadOnlyList<string> ProcessingForms { get; set; } = new List<string>();
        public IReadOnlyList<string> ApprovingForms { get; set; } = new List<string>();
        public IReadOnlyList<string> ViewingForms { get; set; } = new List<string>();
    }

    public class BmQuyenXlCreateDto
    {
        public int? IdTaiKhoan { get; set; }
        public string? MaBm { get; set; }
        public string? MaKhuVuc { get; set; }
        public byte? QuyenChucNang { get; set; }
    }

    public class BmQuyenXlUpdateDto
    {
        public int? IdTaiKhoan { get; set; }
        public string? MaBm { get; set; }
        public string? MaKhuVuc { get; set; }
        public byte? QuyenChucNang { get; set; }
    }
}

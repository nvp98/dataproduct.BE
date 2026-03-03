namespace dataproduct.api.DTOs
{
    public class BmQuyenXlDto
    {
        public int Id { get; set; }
        public int? IdTaiKhoan { get; set; }
        public string? MaBm { get; set; }
        public string? MaKhuVuc { get; set; }
        /// <summary>1=XULY, 2=PHEDUYET, 3=CHOT</summary>
        public byte? QuyenChucNang { get; set; }
    }

    /// <summary>Dùng cho menu: Việc tôi bắt đầu (chỉnh sửa) và Việc đến tôi (duyệt).</summary>
    public class MenuPermissionsDto
    {
        public IReadOnlyList<string> ProcessingForms { get; set; } = new List<string>();
        public IReadOnlyList<string> ApprovingForms { get; set; } = new List<string>();
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

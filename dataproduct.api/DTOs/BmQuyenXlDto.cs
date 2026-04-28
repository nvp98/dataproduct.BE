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

    public class BmQuyenXlRowDto
    {
        public string? MaBm { get; set; }
        public List<string> MaKhuVucs { get; set; } = new();
        /// <summary>Danh sách quyền. Tích Descartes với MaKhuVucs → mỗi tổ hợp = 1 dòng.</summary>
        public List<byte> QuyenChucNangs { get; set; } = new();
    }

    /// <summary>
    /// Payload lưu hàng loạt: mỗi tổ hợp (MaBm × MaKhuVuc × QuyenChucNang) lưu thành 1 dòng trong BmQuyenXl.
    /// IdsToDelete: danh sách ID cần xóa trước khi tạo mới (dùng khi cập nhật toàn bộ quyền của user).
    /// </summary>
    public class BmQuyenXlBulkSaveDto
    {
        public int IdTaiKhoan { get; set; }
        public List<int> IdsToDelete { get; set; } = new();
        public List<BmQuyenXlRowDto> Items { get; set; } = new();
    }
}

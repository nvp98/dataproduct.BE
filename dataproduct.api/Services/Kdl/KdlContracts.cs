using dataproduct.api.Models;

namespace dataproduct.api.Services.Kdl
{
    /// <summary>Header 1 đối tượng do nguồn đẩy về (mẻ, ca…).</summary>
    public class KdlDoiTuongVao
    {
        public string Me { get; set; } = null!;
        public DateOnly NgaySX { get; set; }
        public byte? CaSX { get; set; }
        public string? Kip { get; set; }
        public string? MayDuc { get; set; }
        public string? Mac { get; set; }
        public string? KichThuoc { get; set; }
        public string? LenhSanXuat { get; set; }
    }

    /// <summary>1 giá trị do nguồn đẩy về; MaGoc được map sang key qua KDL_Key.MaGoc.</summary>
    public record KdlDongDuLieu(string Me, string MaGoc, decimal? GiaTri, string? GiaTriText = null);

    public class KdlKetQuaDoc
    {
        public List<KdlDoiTuongVao> DoiTuongs { get; } = [];
        public List<KdlDongDuLieu> GiaTris { get; } = [];
    }

    public class KdlNguonContext
    {
        public string MaModule { get; init; } = null!;
        public DateOnly TuNgay { get; init; }
        public DateOnly DenNgay { get; init; }
        /// <summary>Đối tượng đã có từ các nguồn cung cấp header (chạy trước).</summary>
        public IReadOnlyList<KdlDoiTuongVao> DoiTuongs { get; init; } = [];
    }

    /// <summary>
    /// Adapter đọc 1 nguồn dữ liệu cho 1 module. Đặt tên class hậu tố "Nguon" để Scrutor tự đăng ký.
    /// Nguồn CungCapDoiTuong = true chạy trước và quyết định danh sách đối tượng (mẻ) của module.
    /// </summary>
    public interface IKdlNguon
    {
        string MaModule { get; }
        string MaNguon { get; }
        bool CungCapDoiTuong { get; }
        Task<KdlKetQuaDoc> LayAsync(KdlNguonContext ctx, CancellationToken ct);
    }

    /// <summary>Giá trị đang hiệu lực của 1 đối tượng, dùng khi tính công thức / handler.</summary>
    public class KdlDoiTuongTinh
    {
        public KDL_DoiTuong DoiTuong { get; init; } = null!;
        /// <summary>Không có key = không có dữ liệu (tính như 0); có key với null = không xác định (lan truyền null).</summary>
        public Dictionary<string, decimal?> So { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string?> Chu { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Logic tính riêng cho key CachLay = XU_LY_RIENG, gọi theo KDL_Key.TenXuLy.
    /// Đặt tên class hậu tố "XuLy" để Scrutor tự đăng ký. Chạy trước các key công thức,
    /// nên chỉ được dùng giá trị nguồn / nhập tay, không dùng key công thức.
    /// </summary>
    public interface IKdlXuLyRieng
    {
        string TenXuLy { get; }
        /// <summary>Trả giá trị theo IdDoiTuong; không có trong kết quả = không có dữ liệu.</summary>
        Task<Dictionary<int, decimal?>> TinhAsync(string maKey, IReadOnlyList<KdlDoiTuongTinh> doiTuongs, CancellationToken ct);
    }
}

namespace dataproduct.api.Services.Kdl
{
    /// <summary>Bộ lọc đối tượng khi xem / tổng hợp.</summary>
    public class KdlLoc
    {
        public string MaModule { get; set; } = null!;
        public DateOnly TuNgay { get; set; }
        public DateOnly DenNgay { get; set; }
        public byte? CaSX { get; set; }
        public string? Kip { get; set; }
        public string? MayDuc { get; set; }
        public string? Mac { get; set; }
        public string? KichThuoc { get; set; }
        public string? LenhSanXuat { get; set; }
        /// <summary>Tìm theo Me (chứa chuỗi).</summary>
        public string? SearchKey { get; set; }
    }

    /// <summary>1 nhóm tổng hợp: giá trị các chiều nhóm + Σ giá trị đang hiệu lực theo IdKey.</summary>
    public class KdlNhomTongHop
    {
        public Dictionary<string, object?> Nhom { get; } = new(StringComparer.OrdinalIgnoreCase);
        public int SoDoiTuong { get; set; }
        public Dictionary<int, decimal?> GiaTri { get; } = [];
    }
}

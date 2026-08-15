using System.ComponentModel.DataAnnotations;

namespace dataproduct.api.Models
{
    // Mapping Tag PLC (TagIDEMS) -> Scope (xưởng) + Ca. 1 Tag = 1 BM/xưởng/ca (báo
    // TỔNG khối lượng cả ca) — ca ngày và ca đêm dùng 2 Tag khác nhau nên bắt buộc
    // khai báo Ca. Không gắn với 1 sản phẩm cụ thể — KTV/KCS tự chọn sản phẩm VÀ tự
    // chia Loại 1/2/3/Phế phẩm khi nhập biên bản.
    public class TKVV_SanLuongMapping
    {
        [Key]
        public long ID { get; set; }
        public string TagID { get; set; } = string.Empty;
        public string MaKey { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public byte Ca { get; set; }
        public int? ThuTu { get; set; }
        public DateOnly? TuNgay { get; set; }
        public DateOnly? DenNgay { get; set; }
        public bool TrangThai { get; set; } = true;
        public string? GhiChu { get; set; }
        public DateTime NgayTao { get; set; }
        public int? NguoiTaoID { get; set; }
    }
}

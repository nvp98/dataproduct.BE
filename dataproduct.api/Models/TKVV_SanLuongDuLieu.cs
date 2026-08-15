using System.ComponentModel.DataAnnotations;

namespace dataproduct.api.Models
{
    // Dữ liệu sản lượng thô nhận từ PLC — readonly, nguồn gốc (tương đương LG1_DuLieuNL).
    // Đã được tiến trình nạp dữ liệu gắn sẵn Ngay/Ca/Scope nên không cần dò timeline như LGNL.
    public class TKVV_SanLuongDuLieu
    {
        [Key]
        public long ID { get; set; }
        public string TagID { get; set; } = string.Empty;
        public string? MaKey { get; set; }
        public decimal? Value { get; set; }
        public DateOnly Ngay { get; set; }
        public byte Ca { get; set; }
        public string Scope { get; set; } = string.Empty;
        public DateTime? ThoiGian { get; set; }
        public DateTime NgayTao { get; set; }
    }
}

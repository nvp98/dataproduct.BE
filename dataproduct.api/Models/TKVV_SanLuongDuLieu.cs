using System.ComponentModel.DataAnnotations;

namespace dataproduct.api.Models
{

    public class TKVV_SanLuongDuLieu
    {
        [Key]
        public long ID { get; set; }
        public string? TagID { get; set; } = string.Empty;
        public decimal? GiaTriTuDong { get; set; }
        public decimal? GiaTriDieuChinh { get; set; }
        public DateOnly Ngay { get; set; }
        public byte Ca { get; set; }
        public string Scope { get; set; } = string.Empty;
        public DateTime? ThoiGian { get; set; }
        public DateTime NgayTao { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace dataproduct.api.Models
{

    public class TKVV_SanLuongMapping
    {
        [Key]
        public long ID { get; set; }
        public string TagID { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public string? Kip { get; set; }
        public DateOnly? TuNgay { get; set; }
        public DateOnly? DenNgay { get; set; }
        public bool TrangThai { get; set; } = true;
        public string? GhiChu { get; set; }
        public DateTime NgayTao { get; set; }
        public int? NguoiTaoID { get; set; }
        public byte Ca { get; set; }
    }
}

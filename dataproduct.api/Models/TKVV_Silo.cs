using System.ComponentModel.DataAnnotations;

namespace dataproduct.api.Models
{
    public class TKVV_Silo
    {
        [Key]
        public int ID { get; set; }
        public string? MaXuong { get; set; }
        public string? Scope { get; set; }
        public string? TenScope { get; set; }
        public string? MaSilo { get; set; }
        public string TenSilo { get; set; } = string.Empty;
        public string? GhiChu { get; set; }
        public bool TrangThai { get; set; } = true;
        public DateTime NgayCapNhat { get; set; }
    }
}

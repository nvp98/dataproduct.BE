using System.ComponentModel.DataAnnotations;

namespace dataproduct.api.Models
{
    public class TKVV_NVL_SiloMapping
    {
        [Key]
        public int ID { get; set; }
        public string? MaBM { get; set; }
        public int NguyenVatLieuID { get; set; }
        public string? Scope { get; set; }
        public int? SiloID { get; set; }
        public int Ca { get; set; }
        public DateOnly NgaySX { get; set; }
        public int? ThuTu { get; set; }
        public string? GhiChu { get; set; }
        public bool TrangThai { get; set; } = true;
        public DateTime NgayCapNhat { get; set; }
    }
}

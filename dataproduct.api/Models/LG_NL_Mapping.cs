using System.ComponentModel.DataAnnotations;

namespace dataproduct.api.Models
{
    public class LG_NL_Mapping
    {
        [Key]
        public int ID { get; set; }
        public DateOnly? Ngay { get; set; }
        public int? IDCa { get; set; }
        public int? IDLoCao { get; set; }
        public int? IDSiLo { get; set; }
        public int? IDNVL { get; set; }
        // null = từ đầu ca; có giá trị = đổi NVL giữa ca tại thời điểm này
        public DateTime? ThoiDiemBD { get; set; }
        // null = config đang active; có giá trị = config đã bị thay thế
        public DateOnly? NgayHetHL { get; set; }
        public int? IDCaHetHL { get; set; }
        public string? GhiChu { get; set; }
        public DateTime? NgayTao { get; set; }
    }
}

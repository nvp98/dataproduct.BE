using System.ComponentModel.DataAnnotations.Schema;

namespace dataproduct.api.Models
{
    public class NMLG_QuyKhoNapLieu
    {
        public int? ID { get; set; }
        public DateOnly? Ngay { get; set; }
        public int? IDCa { get; set; }
        public int? IDLoCao { get; set; }
        public string? DataIndex { get; set; }
        public string? TenNVL { get; set; }
        [Column(TypeName = "decimal(10,3)")]
        public decimal? TongCong { get; set; }
        [Column(TypeName = "decimal(10,3)")]
        public decimal? DoAm { get; set; }
        [Column(TypeName = "decimal(10,3)")]
        public decimal? QuyKho { get; set; }
        public DateTime? NgayTao { get; set; }
    }

}

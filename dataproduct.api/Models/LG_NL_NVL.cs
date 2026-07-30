using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace dataproduct.api.Models
{
    public class LG_NL_NVL
    {
        [Key]
        public int ID { get; set; }
        public int? IDLoCao { get; set; }
        public int? IDNhomNVL { get; set; }
        public string? TenNVL_NM { get; set; }
        public string? TenNVL_TK { get; set; }
        public string? MaVatTu { get; set; }
        public int? ThuTu { get; set; }

        public string? GhiChu { get; set; }
        public DateTime? NgayTao { get; set; }
        public bool? XacNhan { get; set; }
        public DateTime? NgayXacNhan { get; set; }
        public int? IDNguoiXacNhan { get;set; }
        public bool? IsDelete { get; set; }

    }
}

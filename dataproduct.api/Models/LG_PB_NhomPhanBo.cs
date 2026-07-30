using System.ComponentModel.DataAnnotations;

namespace dataproduct.api.Models
{
    public class LG_PB_NhomPhanBo
    {
        [Key]
        public int ID { get; set; }
        public string TenNhom { get; set; } = string.Empty;
        public byte LoaiPhanBo { get; set; }
        public byte PhuongThucPhanBo { get; set; }
        public string? MaVatTu { get; set; }
        public int? ThuTu { get; set; }
        public bool IsDelete { get; set; }
    }
}

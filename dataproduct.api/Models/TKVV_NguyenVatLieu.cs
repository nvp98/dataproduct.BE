using System.ComponentModel.DataAnnotations;

namespace dataproduct.api.Models
{
    // Danh mục sản phẩm/nguyên vật liệu theo từng biểu mẫu (MaBM) của nhà máy TKVV.
    public class TKVV_NguyenVatLieu
    {
        [Key]
        public int ID { get; set; }
        public string MaBM { get; set; } = string.Empty;
        public string TenNVL { get; set; } = string.Empty;
        public string? DonViTinh { get; set; }
        public int? ThuTu { get; set; }
        public bool TrangThai { get; set; } = true;
        public string? GhiChu { get; set; }
    }
}

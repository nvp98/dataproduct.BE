using System.ComponentModel.DataAnnotations;

namespace dataproduct.api.Models
{
    // Mapping: NVL ↔ Tag EMS + Ca. Mỗi bản ghi cho biết Tag EMS nào (ca ngày/đêm) dùng để
    // đọc sản lượng cho NVL đó. Scope kế thừa từ TKVV_NguyenVatLieu.Scope — không lưu lại ở đây.
    public class TKVV_NVL_TagMapping
    {
        [Key]
        public int ID { get; set; }
        public int NguyenVatLieuID { get; set; }
        public string TagIDEMS { get; set; } = string.Empty;
        public string? GhiChu { get; set; }
        public bool TrangThai { get; set; } = true;
        public DateTime NgayCapNhat { get; set; }
        public int Ca { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace dataproduct.api.Models
{
    // Cấu hình RIÊNG cho từng (Ngay, Ca, IDLoCao) — không kế thừa từ ngày/ca trước
    public class LG_NVL_NhomPhanBo
    {
        [Key]
        public int ID { get; set; }
        public int IDNVL { get; set; }
        public int IDNhomPhanBo { get; set; }
        public DateTime Ngay { get; set; }
        public byte Ca { get; set; }
        public int IDLoCao { get; set; }
        public int ThuTuUuTien { get; set; }
        public bool IsDelete { get; set; }
    }
}

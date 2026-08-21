using System.ComponentModel.DataAnnotations;

namespace dataproduct.api.Models
{
    // % phân bổ áp dụng chung cho cả nhóm (chỉ dùng cho nhóm PhuongThucPhanBo=TyLeNhapTay) —
    // khi lưu sẽ cascade xuống LG_PB_TyLePhanBo cho từng NVL thành viên tại đúng (Ngay, Ca, IDLoCao).
    public class LG_PB_TyLeNhom
    {
        [Key]
        public int ID { get; set; }
        public int IDNhomPhanBo { get; set; }
        public DateTime Ngay { get; set; }
        public byte Ca { get; set; }
        public int IDLoCao { get; set; }
        public decimal TyLe { get; set; }
        public string? GhiChu { get; set; }
        public int IDNguoiNhap { get; set; }
        public DateTime NgayNhap { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace dataproduct.api.Models
{
    // Map phạm vi của Phiếu điều chỉnh theo phòng ban. ID_Xuong = xưởng thuộc phòng ban (Tbl_Xuong.ID_Xuong), dùng cho
    // phần Nhập/Xuất (1 phòng ban có nhiều xưởng nên nhiều dòng cùng ID_PhongBan). Scope = mã scope bên V2, dùng cho phần
    // Nội bộ (NM.LG: id lò cao 1–6); phòng ban không có phần Nội bộ thì Scope để NULL. IsDelete = xóa mềm.
    public class LG_PDC_Scope_PhongBan
    {
        [Key]
        public int ID { get; set; }
        public int? Scope { get; set; }
        public int? ID_PhongBan { get; set; }
        public int? ID_Xuong { get; set; }
        public bool IsDelete { get; set; }
    }
}

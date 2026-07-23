using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace dataproduct.api.Models.MasterData
{
    // Xác nhận qua INFORMATION_SCHEMA.COLUMNS thực tế trên PRODUCTDATA (192.168.240.3,1434).
    // Ca/Kip là nchar(1) chứa giá trị dạng text ("1"/"2", "A"/"B"/"C"), không phải số nguyên.
    [Table("Tbl_BienBanGiaoNhan")]
    public class Tbl_BienBanGiaoNhan
    {
        [Key]
        public int ID_BBGN { get; set; }
        public DateTime? ThoiGianXuLyBG { get; set; }
        public string? Ca { get; set; }
        public string? Kip { get; set; }
        public int? ID_Xuong_BG { get; set; }
        public int? ID_TrangThai_BBGN { get; set; }
    }
}

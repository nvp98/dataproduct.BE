using System.ComponentModel.DataAnnotations;

namespace dataproduct.api.Models
{
    public class LG_NL_ChiTiet
    {
        [Key]
        public int ID { get; set; }
        public Guid IDPhieu { get; set; }
        public int? IDLoCao { get; set; }
        public DateTime? Ngay { get; set; }
        public int? IDCa { get; set; }
        public string? ThoiGianNapLieu { get; set; }
        public decimal? SoMe { get; set; }
        public string? MeGio { get; set; }
        public string? CheDo { get; set; }
        public decimal? ThuocThamLieu1 { get; set; }
        public decimal? ThuocThamLieu2 { get; set; }
        public string? GhiChu { get; set; }
        public int IDNVL { get; set; }
        public decimal? GiaTri { get; set; }
        public int? ThuTu { get; set; }
        public DateTime? NgayTao { get; set; }
        // Độ ẩm và Quy khô — per (IDPhieu, IDNVL), lưu lặp lại trên mỗi row cùng nhóm
        public decimal? DoAm { get; set; }
        public decimal? QuyKho { get; set; }
        // Theo dõi nhập tay: true nếu GiaTri được người dùng ghi đè thủ công
        public bool ManualGiaTri { get; set; }
        public decimal? GiaTri_Goc { get; set; }
    }
}

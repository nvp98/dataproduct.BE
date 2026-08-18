using System.ComponentModel.DataAnnotations;

namespace dataproduct.api.Models
{

    public class TKVV_SanLuongChiTiet
    {
        [Key]
        public long ID { get; set; }
        public Guid IDPhieu { get; set; }
        public int? Scope { get; set; }
        public DateOnly? Ngay { get; set; }
        public byte? Ca { get; set; }
        public int NguyenVatLieuID { get; set; }
        public int? ThuTuDong { get; set; }
        public TimeOnly? ThoiGian { get; set; }
        public decimal? Loai1 { get; set; }
        public decimal? Loai2 { get; set; }
        public decimal? Loai3 { get; set; }
        public decimal? PhePham { get; set; }
        public bool IsEdited { get; set; }
        public int? NguoiSuaID { get; set; }
        public DateTime? NgaySua { get; set; }
        public string? LyDoSua { get; set; }
        public string? GhiChu { get; set; }
        public bool IsDelete { get; set; }
        public DateTime NgayTao { get; set; }
    }
}

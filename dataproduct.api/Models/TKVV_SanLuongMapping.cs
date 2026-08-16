using System.ComponentModel.DataAnnotations;

namespace dataproduct.api.Models
{
    // Mapping Tag PLC -> Scope + Ca + PhanLoai dùng cho tích lũy sản lượng PLC.
    // Bảng này cấu hình cách gán dữ liệu từ TKVV_SanLuongDuLieu vào đúng xưởng/ca/loại.
    // Khác với TKVV_NVL_TagMapping — bảng đó quản lý NVL ↔ TagIDEMS cho admin UI.
    public class TKVV_SanLuongMapping
    {
        [Key]
        public long ID { get; set; }
        public string TagID { get; set; } = string.Empty;
        public string? MaKey { get; set; }
        public string Scope { get; set; } = string.Empty;
        public string? PhanLoai { get; set; }
        public int? ThuTu { get; set; }
        public DateOnly? TuNgay { get; set; }
        public DateOnly? DenNgay { get; set; }
        public bool TrangThai { get; set; } = true;
        public string? GhiChu { get; set; }
        public DateTime NgayTao { get; set; }
        public int? NguoiTaoID { get; set; }
        public byte Ca { get; set; }
    }
}

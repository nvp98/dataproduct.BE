using System.ComponentModel.DataAnnotations;

namespace dataproduct.api.Models.MasterData
{
    public class PhongBan
    {
        [Key]
        public int ID_PhongBan { get; set; }

        [MaxLength(200)]
        public string? TenPhongBan { get; set; }

        [MaxLength(200)]
        public string? TenNgan { get; set; }

        public int? ID_TrangThai { get; set; }

        // Quan hệ 1-n với Tài khoản
        public virtual ICollection<TaiKhoan>? TaiKhoans { get; set; }
    }
}

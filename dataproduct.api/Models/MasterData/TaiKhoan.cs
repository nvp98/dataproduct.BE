using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace dataproduct.api.Models.MasterData
{
    public class TaiKhoan
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID_TaiKhoan { get; set; }

        [MaxLength(50)]
        public string? TenTaiKhoan { get; set; }

        [MaxLength(250)]
        public string? MatKhau { get; set; }

        [MaxLength(250)]
        public string? HoVaTen { get; set; }

        public int? ID_PhongBan { get; set; }

        [ForeignKey(nameof(ID_PhongBan))]
        public virtual PhongBan? PhongBan { get; set; }

        public int? ID_PhanXuong { get; set; }

        public int? ID_ChucVu { get; set; }

        [MaxLength(250)]
        public string? Email { get; set; }

        [MaxLength(250)]
        public string? SoDienThoai { get; set; }

        [Column(TypeName = "date")]
        public DateTime? NgayTao { get; set; }

        public int? ID_Quyen { get; set; }

        [Column(TypeName = "nvarchar(MAX)")]
        public string? ChuKy { get; set; }

        public int? ID_TrangThai { get; set; }

        [Column(TypeName = "nvarchar(MAX)")]
        public string? PhongBan_Them { get; set; }

        [MaxLength(20)]
        public string? Quyen_Them { get; set; }

        [MaxLength(250)]
        public string? PhongBan_API { get; set; }

        [MaxLength(100)]
        public string? Xuong_API { get; set; }
    }
}

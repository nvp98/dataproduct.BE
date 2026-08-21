using System.ComponentModel.DataAnnotations;

namespace dataproduct.api.Models
{
    public class LG_PB_TyLePhanBo
    {
        [Key]
        public int ID { get; set; }
        public int IDNVL { get; set; }
        public DateTime Ngay { get; set; }
        public byte? Ca { get; set; }
        public decimal TyLe { get; set; }
        public string? GhiChu { get; set; }
        public int IDNguoiNhap { get; set; }
        public DateTime NgayNhap { get; set; }
        public bool IsDelete { get; set; }
    }
}

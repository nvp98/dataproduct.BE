using System.ComponentModel.DataAnnotations;

namespace dataproduct.api.Models
{
    public class LG_PB_KetQuaPhanBo
    {
        [Key]
        public int ID { get; set; }
        public DateTime Ngay { get; set; }
        public byte? Ca { get; set; }
        public int IDLoCao { get; set; }
        public byte LoaiPhanBo { get; set; }
        public int IDNVL { get; set; }
        public int IDNhomPhanBo { get; set; }
        public int IDBienBanNhan { get; set; }
        public decimal KhoiLuongNapLieu { get; set; }
        public decimal TyLePhanBo { get; set; }
        public decimal KhoiLuongPhanBo { get; set; }
        public decimal KhoiLuongChotCuoi { get; set; }
        public bool LaDongConLai { get; set; }
        public byte TrangThai { get; set; }
        public int IDNguoiTao { get; set; }
        public DateTime NgayTao { get; set; }
        public int? IDNguoiXacNhan { get; set; }
        public DateTime? NgayXacNhan { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace dataproduct.api.Models.MasterData
{
    // Xác nhận qua INFORMATION_SCHEMA.COLUMNS thực tế trên PRODUCTDATA (192.168.240.3,1434).
    // KL_QuyKho_BG là float (double), không phải decimal.
    [Table("Tbl_ChiTiet_BienBanGiaoNhan")]
    public class Tbl_ChiTiet_BienBanGiaoNhan
    {
        [Key]
        public int ID_CT_BBGN { get; set; }
        public int ID_BBGN { get; set; }
        public int? ID_VatTu { get; set; }
        public double? KL_QuyKho_BG { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace dataproduct.api.Models.MasterData
{
    // Danh mục vật tư SAP dùng chung toàn công ty, DB PRODUCTDATA.
    [Table("Tbl_VatTu")]
    public class Tbl_VatTu
    {
        [Key]
        public int ID_VatTu { get; set; }
        public string? TenVatTu { get; set; }
        public string? MaVatTu_Sap { get; set; }
        public string? TenVatTu_Sap { get; set; }
        public string? DonViTinh { get; set; }
        public int? ID_NhomVatTu { get; set; }
        public string? PhongBan { get; set; }
        public int? ID_TrangThai { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace dataproduct.api.Models
{
    // Ánh xạ 1 NVL (TKVV_NguyenVatLieu) tới 1 Vật tư BBGN (PRODUCTDATA.Tbl_VatTu),
    // dùng để đối chiếu/quy đổi dữ liệu giữa BM nội bộ NM.TKVV và Biên bản giao nhận SAP.
    public class TKVV_NVL_BBGN_Mapping
    {
        [Key]
        public int ID { get; set; }
        public int TKVV_NVL_ID { get; set; }
        public int ID_VatTu_BBGN { get; set; }
        public bool TrangThai { get; set; } = true;
        public string? GhiChu { get; set; }
        public DateTime NgayTao { get; set; }
    }
}

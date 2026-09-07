using System.ComponentModel.DataAnnotations;

namespace dataproduct.api.Models
{
    public class TKVV_Scope_Xuong_Mapping
    {
        [Key]
        public int ID { get; set; }
        public int Scope { get; set; }
        public string? MaXuong { get; set; }
        public string? TenXuong { get; set; }
        public int? ID_Xuong_BBGN { get; set; }
        public int? ID_NVL_BBGN_ThanhPham { get; set; }
    }
}

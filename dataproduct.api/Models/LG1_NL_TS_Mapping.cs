using System.ComponentModel.DataAnnotations;

namespace dataproduct.api.Models
{
    /// <summary>
    /// Bảng lookup tĩnh: TagKey ↔ TS_Index.
    /// Ví dụ: TagKey="XUAT.TS1" → TS_Index="TS1"
    /// Dùng để populate dropdown chọn TagKey khi quản lý LG_NL_SiLo.
    /// </summary>
    public class LG1_NL_TS_Mapping
    {
        [Key]
        public int ID { get; set; }
        public string? TagKey { get; set; }
        public int? TS_Index { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
}

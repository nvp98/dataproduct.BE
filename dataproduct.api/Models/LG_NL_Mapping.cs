using System.ComponentModel.DataAnnotations;

namespace dataproduct.api.Models
{
    /// <summary>
    /// Bảng mapping Silo ↔ NVL theo ngày và ca.
    /// Ca 1: 07:30–19:30 | Ca 2: 19:30–07:30 hôm sau.
    /// </summary>
    public class LG_NL_Mapping
    {
        [Key]
        public int ID { get; set; }
        public DateOnly? Ngay { get; set; }
        public int? IDCa { get; set; }
        public int? IDLoCao { get; set; }
        public int? IDSiLo { get; set; }   // FK → LG_NL_SiLo.ID
        public int? IDNVL { get; set; }    // FK → LG_NL_NVL.ID
        public string? GhiChu { get; set; }
        public DateTime? NgayTao { get; set; }
    }
}

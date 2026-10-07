using System.ComponentModel.DataAnnotations;

namespace dataproduct.api.Models
{
    public class TKVV_Silo_TagMapping
    {
        [Key]
        public int ID { get; set; }
        public int SiloID { get; set; }
        public string MaBM { get; set; } = string.Empty;
        public string LoaiDuLieu { get; set; } = string.Empty;
        public string? TagIDEMS { get; set; }
        public string? TagName { get; set; }
        public string? TagIDEMS_Ngay { get; set; }
        public string? TagName_Ngay { get; set; }
        public string? TagIDEMS_Dem { get; set; }
        public string? TagName_Dem { get; set; }
        public string? GhiChu { get; set; }
        public bool TrangThai { get; set; } = true;
        public DateTime NgayCapNhat { get; set; }
    }
}

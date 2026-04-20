using System.ComponentModel.DataAnnotations;

namespace dataproduct.api.Models
{
    public class LG1_DuLieuNL
    {
        [Key]
        public long ID { get; set; }
        public int? SourceID { get; set; }
        public string? TagName { get; set; }
        public DateTime? Time { get; set; }
        public double? Value { get; set; }
        public int? ID_LoCao { get; set; }
        public string? TagKey { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
}

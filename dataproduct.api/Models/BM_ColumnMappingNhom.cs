using System.Text.Json.Serialization;

namespace dataproduct.api.Models
{
    public class BM_ColumnMappingNhom
    {
        public int Id { get; set; }
        public int? LoCao { get; set; }
        public string TenNhom { get; set; } = "";
        public int? ThuTu { get; set; }
        public bool IsVisible { get; set; } = true;

        // Dùng khi nhóm không có cột con — tự render như leaf column
        public string? SourceField { get; set; }
        public string? DataIndex { get; set; }
        public string? Format { get; set; }

        [JsonIgnore]
        public virtual ICollection<BM_ColumnMapping> Columns { get; set; } = [];
    }
}

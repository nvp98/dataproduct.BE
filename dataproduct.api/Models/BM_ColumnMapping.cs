namespace dataproduct.api.Models
{
    public class BM_ColumnMapping
    {
        public int Id { get; set; }
        public int NhomId { get; set; }

        public string? TenCot { get; set; } = "";
        public string? DataIndex { get; set; } = "";
        public string? SourceField { get; set; } = "";
        public int? ThuTu { get; set; }
        public bool IsVisible { get; set; } = true;
        public string? Format { get; set; }

        public BM_ColumnMappingNhom Nhom { get; set; } = null!;
    }
}

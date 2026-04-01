namespace dataproduct.api.DTOs.NMLG_Dto
{
    public class AddSiLoLGDto
    {
        public int? ID_LoCao { get; set; }
        public string? TenSiLo { get; set; }
        public int? ThuTu { get; set; }
        public string? TenNL { get; set; }
        public string? TenNL_DieuChinh { get; set; }
    }

    public class UpdateSiLoLGDto
    {
        public int? ID_LoCao { get; set; }
        public string? TenSiLo { get; set; }
        public int? ThuTu { get; set; }
        public string? TenNL { get; set; }
        public string? TenNL_DieuChinh { get; set; }
    }

    public class SiLoLGResponseDto
    {
        public int ID { get; set; }
        public int? ID_LoCao { get; set; }
        public string? TenSiLo { get; set; }
        public int? ThuTu { get; set; }
        public string? TenNL { get; set; }
        public string? TenNL_DieuChinh { get; set; }
    }
    // --- Nhom (cột cha) ---
    public class ColumnMappingNhomCreateDto
    {
        public int LoCao { get; set; }
        public string TenNhom { get; set; }
        public int? ThuTu { get; set; }
        public bool IsVisible { get; set; } = true;
        public string? SourceField { get; set; }
        public string? DataIndex { get; set; }
        public string? Format { get; set; }
    }

    public class ColumnMappingNhomUpdateDto : ColumnMappingNhomCreateDto
    {
        public int Id { get; set; }
    }

    // --- Column (cột con) ---
    public class ColumnMappingCreateDto
    {
        public int NhomId { get; set; }
        public string TenCot { get; set; }
        public string DataIndex { get; set; }
        public string? SourceField { get; set; }
        public int? ThuTu { get; set; }
        public bool IsVisible { get; set; } = true;
        public string? Format { get; set; }
    }

    public class ColumnMappingUpdateDto : ColumnMappingCreateDto
    {
        public int Id { get; set; }
    }

    // --- Response cho Ant Design Table ---
    public class ColumnDto
    {
        public string title { get; set; }
        // Leaf column (nhóm không có cột con)
        public string? dataIndex { get; set; }
        public string? format { get; set; }
        // Group column (nhóm có cột con)
        public List<ColumnChildDto>? children { get; set; }
    }

    public class ColumnChildDto
    {
        public string title { get; set; }
        public string dataIndex { get; set; }
        public string? format { get; set; }
    }

}

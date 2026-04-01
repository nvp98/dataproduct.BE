namespace dataproduct.api.DTOs.NMLG_Dto
{
    public class QuyKhoItemDto
    {
        public string? DataIndex { get; set; }
        public string? TenNVL { get; set; }
        public decimal? TongCong { get; set; }
        public decimal? DoAm { get; set; }
    }

    public class UpsertQuyKhoRequest
    {
        public DateOnly Ngay { get; set; }
        public int IDCa { get; set; }
        public int IDLoCao { get; set; }
        public List<QuyKhoItemDto> Items { get; set; } = [];
    }
}

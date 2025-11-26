
namespace dataproduct.api.ResponseModels
{
    public class HeaderKey_ResponeModels
    {
        public Guid? KeyGuid { get; set; }
        public string? TenHienThi{ get; set; }
        public int? ID_HeaderKey { get; set; }
        public string? TenNguonDuLieu { get; set; }
        public int ID_PhuLieu { get; set; }
        public string? TenPhuLieu { get; set; }
        public double? KLPhuGia { get; set; }
        public int? MappingId { get; set; }
    }
    public class HeaderKeyGroupedByReportNoModel
    {
        public Guid? KeyGuid { get; set; }
        public string? TenHienThi{ get; set; }
        public int? ID_HeaderKey { get; set; }
        public string? TenNguonDuLieu { get; set; }
        public int ID_PhuLieu { get; set; }
        public string? TenPhuLieu { get; set; }
        public double? KLPhuGia { get; set; }
        public string? LoaiPhuLieu { get; set; }
        public double? KLPhuGiaTotal { get; set; }
        public int? MappingId { get; set; }
    }
}

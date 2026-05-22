using dataproduct.api.DTOs;
using dataproduct.api.DTOs.CTD_Dto;
using dataproduct.api.Models;

namespace dataproduct.api.ResponseModels
{
    public class SearchPhieuResponseModel
    {
        public Guid Idphieu { get; set; }
        public string SoPhieu { get; set; } = string.Empty;
        public string MaBm { get; set; } = string.Empty;
        public DateOnly NgaySX { get; set; }
        public int? Ca { get; set; }
        public string? Kip { get; set; }
        public int? Scope { get; set; }
        public int? MayDuc { get; set; }
        public int? TinhTrang { get; set; }
        public int? NguoiTao { get; set; }
        public string? TenScope { get; set; }
        public List<PheDuyetDto>? PheDuyet { get; set; }
    }

}

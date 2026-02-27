using dataproduct.api.Models;

namespace dataproduct.api.ResponseModels
{
    public class DLNM_HRC2_ResponseModels
    {
        public long ID { get; set; }
        public decimal? REPORT_NO { get; set; }
        public DateTime? NgaySx { get; set; }
        public DateTime? Ngay { get; set; }  
        public int? Ca { get; set; }
        public string? BieuMau { get; set; }
        public int? Scope { get; set; }
        public string? MeThoi { get; set; }
        public string? MacThep { get; set; }
        public double? O2 { get; set; }
        public double? AR_RH { get; set; }
        public double? N2 { get; set; }
        public double? AR_BOF { get; set; }
        public double? AR_LF { get; set; }
        public double? KLGangLong { get; set; }
        public double? KLThepPhe { get; set; }
        public bool? IsNM { get; set; }
        public bool? IsChuyenCa { get; set; }
        public double? KLGangLongCCT { get; set; }
        public double? KLGangLongCR { get; set; }
        public double? KLThepLong { get; set; }
        public bool? IsTrungMeThoi { get; set; }
    }
    public class HRC2DetailByReportNoModel
    {
        public DLNM_HRC2_ResponseModels? data { get; set; }
        public List<HeaderKey_ResponeModels> phulieus { get; set; }
    }

    public class HRC2GroupedByReportNoModel
    {
        public DLNM_HRC2_ResponseModels? data { get; set; }
        public List<HeaderKeyGroupedByReportNoModel> mappedPhulieus { get; set; } = new List<HeaderKeyGroupedByReportNoModel>();
        public List<HeaderKeyGroupedByReportNoModel> unmappedPhulieus { get; set; } = new List<HeaderKeyGroupedByReportNoModel>();
        public List<HeaderKeyGroupedByReportNoModel> phanBoPhulieus { get; set; } = new List<HeaderKeyGroupedByReportNoModel>(); // Dữ liệu phân bổ (IsPhanBo = true)
    }

    public class FilterSTD_NXTResponse
    {
        public string? BieuMau { get; set; }
        public int Scope { get; set; }
        public List<PhuLieuNM>? PhuLieus { get; set; }
        public double? TotalKLPhuGia { get; set; }
        public int? HeaderKeyId { get; set; }
        public string? HeaderKeyName { get; set; }
    }

    public class PhuLieuNM
    {
        public int? ID_PhuLieu { get; set; }
        public string? TenPhuLieu { get; set; }
    }
}

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
        public int? QueLayMau { get; set; }
        public int? QueDoNhiet { get; set; }
        public string? GhiChu { get; set; }
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
        public List<HeaderKeyGroupedByReportNoModel> manualAdjustPhulieus { get; set; } = new List<HeaderKeyGroupedByReportNoModel>(); // Điều chỉnh tay (IsManual = true, IsPhanBo != true)
    }

    public class HRC2FilterThongKe
    {
        public List<PhuLieuHeaderTable> phuLieuHeaderTables { get; set; }
        public HRC2GroupedByReportNoModel dulieu { get; set; }

    }

    // Đơn giản hóa dữ liệu thống kê: mỗi HeaderKey -> một giá trị KLPhuGia đã sum
    public class HRC2ThongKeValue
    {
        public int IDHeaderKey { get; set; }
        /// <summary>Giá trị tự động từ NM.</summary>
        public double? KLPhuGia { get; set; }
        /// <summary>Giá trị chỉnh tay trên UI (ưu tiên hơn KLPhuGia).</summary>
        public double? KLPhuGia_Manual { get; set; }
        public bool? IsManual { get; set; }
        /// <summary>Lượng được phân bổ từ PhanBoAsync (null nếu không có phân bổ).</summary>
        public double? KLPhanBo { get; set; }
        /// <summary>KL hiển thị trên cột phụ liệu: KLPhuGia_Manual ?? KLPhuGia (không cộng phân bổ).</summary>
        public double? EffectiveKL { get; set; }
        /// <summary>
        /// Giá trị tổng hợp cuối cùng để thống kê (effectiveKL + phân bổ nếu có).
        /// </summary>
        public double? TotalKLPhuGia { get; set; }
    }

    public class HRC2ThongKeRow
    {
        public DLNM_HRC2_ResponseModels? Data { get; set; }
        public List<HRC2ThongKeValue> Values { get; set; } = new List<HRC2ThongKeValue>();
    }

    public class PhuLieuHeaderTable
    {
        public int IDHeaderKey { get; set; }
        public string TenPhuLieu { get; set; }
        public byte LoaiThongKe { get; set; }
    }

    public class SearchThongKeApiResponse
    {
        public List<PhuLieuHeaderTable> PhuLieuHeaderTables { get; set; } = new();
        public List<HRC2ThongKeRow> Data { get; set; } = new();
        public int TotalRecords { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    /// <summary>Mỗi phần tử là tổng TotalKLPhuGia theo HeaderKey trên toàn khoảng lọc.</summary>
    public class ThongKeSumItem
    {
        public int IDHeaderKey { get; set; }
        public string? TenPhuLieu { get; set; }
        public double? TotalKLPhuGia { get; set; }
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

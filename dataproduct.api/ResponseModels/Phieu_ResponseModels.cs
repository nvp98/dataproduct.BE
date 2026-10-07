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
        /// <summary>Số lượng ID Slab của phiếu đã được từng bộ phận xác nhận (so với tổng
        /// <see cref="SoLuongSlab"/>) — do IPhieuSearchEnricher tương ứng (vd Hrc2BbgnPhoiTamEnricher,
        /// Hrc1BbgnPhoiTamEnricher) tính, null nếu biểu mẫu không có enricher hỗ trợ hoặc không có
        /// bộ phận đó (vd HRC2 không có Cán/C4, HRC1 không có Kho).</summary>
        public int? SoLuongXNDuc { get; set; }
        public int? SoLuongXNKho { get; set; }
        public int? SoLuongXNCan { get; set; }
        public int? SoLuongXNC4 { get; set; }
        public int? SoLuongXNPKH { get; set; }
        public int? NguoiTao { get; set; }
        public string? TenNguoiTao { get; set; }
        public string? TenScope { get; set; }
        public int? IsCheck { get; set; }
        public int? SoLuongMe { get; set; }
        /// <summary>Số lượng ID Slab thuộc phiếu — do IPhieuSearchEnricher tương ứng (vd
        /// Hrc2BbgnPhoiTamEnricher) tính, null nếu biểu mẫu không có enricher hỗ trợ.</summary>
        public int? SoLuongSlab { get; set; }
        public List<PheDuyetDto>? PheDuyet { get; set; }
    }

}

using dataproduct.api.Models;

namespace dataproduct.api.Services.PhieuEnrichers;

/// <summary>
/// Implement thêm bởi các IPhieuSearchEnricher ghi đè TinhTrang thành trạng thái tổng hợp
/// (VD 11/12 — không lưu trong cột BM_Phieu.TinhTrang thật, chỉ tính ở EnrichAsync sau khi
/// fetch). Không có interface này, filter TinhTrang cho các MaBm đó ở
/// PhieuRepository.SearchWithPagingByUserAsync luôn ra rỗng vì so sánh trực tiếp cột DB không
/// bao giờ khớp giá trị tổng hợp.
///
/// Tính theo batch (không phải Expression&lt;IQueryable&gt;) vì logic tổng hợp (đặc biệt HRC1 —
/// slab tự nhiên + slab chuyển ca) không dịch được sang SQL qua EF Core; thay vào đó lấy các
/// phiếu ứng viên (đã qua các filter khác + phân trang CHƯA áp dụng) rồi tính lại đúng logic
/// EnrichAsync trên bộ nhớ.
/// </summary>
public interface IPhieuTinhTrangFilterEnricher
{
    string MaBm { get; }

    /// <summary>Trả về Idphieu (trong candidates) có TinhTrang tổng hợp khớp giá trị yêu cầu.</summary>
    Task<List<Guid>> FilterByTinhTrangAsync(List<BmPhieu> candidates, int tinhTrang);
}

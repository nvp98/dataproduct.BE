using dataproduct.api.Models;

namespace dataproduct.api.Repositories.PhieuSearchFilters;

/// <summary>
/// Cung cấp logic lọc phiếu riêng cho 1 (hoặc nhiều) MaBm có luồng phân quyền/tìm kiếm
/// khác với chuẩn "scope-based + phê duyệt" mặc định của PhieuRepository.SearchWithPagingByUserAsync.
/// Đăng ký thêm 1 BM đặc biệt = thêm 1 class implement interface này, không sửa PhieuRepository.
/// </summary>
public interface IPhieuSearchFilterProvider
{
    /// <summary>Các MaBm mà provider này chịu trách nhiệm xử lý (sẽ bị loại khỏi query mặc định).</summary>
    IReadOnlyCollection<string> MaBms { get; }

    /// <summary>Query phiếu hợp lệ cho user theo loaiVung (1=Việc tôi bắt đầu, 2=Việc đến tôi, 3=Chỉ xem).</summary>
    IQueryable<BmPhieu> BuildQuery(ProductFormContext context, int loaiVung, int userId);

    /// <summary>False nếu phiếu của BM này không có Scope (VD: HRC1_LoThoi/TinhLuyen) — ảnh hưởng cách lọc ScopeFilters ở BƯỚC 4.</summary>
    bool HasScope => true;
}

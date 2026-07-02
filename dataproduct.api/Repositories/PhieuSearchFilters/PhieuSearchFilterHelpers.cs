using dataproduct.api.Models;

namespace dataproduct.api.Repositories.PhieuSearchFilters;

/// <summary>
/// Các mảnh LINQ dùng chung, giữ nguyên hệt logic "regularQueryN" gốc trong PhieuRepository
/// (scope-based: MaKhuVuc == "ALL" hoặc khớp Scope của phiếu) để các provider tái sử dụng
/// cho loaiVung 1/3 mà không thay đổi hành vi.
/// </summary>
internal static class PhieuSearchFilterHelpers
{
    /// <summary>LoaiVung 1 (Việc tôi bắt đầu): quyền Xử lý(1) hoặc Xử lý+PheDuyet(4), theo scope.</summary>
    public static IQueryable<BmPhieu> WithScopeBasedXuLy(IQueryable<BmPhieu> baseQuery, ProductFormContext context, int userId)
        => baseQuery.Where(x => context.BmQuyenXls.Any(q =>
            q.IdTaiKhoan == userId && q.MaBm == x.MaBm &&
            (q.MaKhuVuc == "ALL" || q.MaKhuVuc == x.Scope.ToString()) &&
            (q.QuyenChucNang == 1 || q.QuyenChucNang == 4)));

    /// <summary>LoaiVung 3 (Chỉ xem): quyền Xem(5), theo scope.</summary>
    public static IQueryable<BmPhieu> WithScopeBasedXem(IQueryable<BmPhieu> baseQuery, ProductFormContext context, int userId)
        => baseQuery.Where(x => context.BmQuyenXls.Any(q =>
            q.IdTaiKhoan == userId && q.MaBm == x.MaBm &&
            (q.MaKhuVuc == "ALL" || q.MaKhuVuc == x.Scope.ToString()) &&
            q.QuyenChucNang == 5));
}

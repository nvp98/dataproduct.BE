using dataproduct.api.Models;

namespace dataproduct.api.Repositories.PhieuSearchFilters;

/// <summary>
/// HRC1_BBSL_PhoiTam: workflow slab (Đúc/Cán/PKH).
///  - loaiVung 1/3: xử lý như phiếu thường (scope-based).
///  - loaiVung 2 (Việc đến tôi): không dùng BmPheDuyet, chỉ cần quyền 2|4 cho maBm (không check scope).
/// </summary>
public class Hrc1PhoiTamSearchFilterProvider : IPhieuSearchFilterProvider
{
    private const string MaBmValue = "HRC1_BBSL_PhoiTam";

    public IReadOnlyCollection<string> MaBms { get; } = new[] { MaBmValue };

    public IQueryable<BmPhieu> BuildQuery(ProductFormContext context, int loaiVung, int userId)
    {
        var baseQuery = context.BmPhieus
            .Where(x => x.IsDelete != 1 && x.IsLock != 1 && x.MaBm == MaBmValue);

        return loaiVung switch
        {
            1 => PhieuSearchFilterHelpers.WithScopeBasedXuLy(baseQuery, context, userId),
            2 => baseQuery.Where(x => context.BmQuyenXls.Any(q =>
                    q.IdTaiKhoan == userId && q.MaBm == x.MaBm &&
                    (q.QuyenChucNang == 2 || q.QuyenChucNang == 4))),
            3 => PhieuSearchFilterHelpers.WithScopeBasedXem(baseQuery, context, userId),
            _ => baseQuery.Where(_ => false),
        };
    }
}

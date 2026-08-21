using dataproduct.api.Models;

namespace dataproduct.api.Repositories.PhieuSearchFilters;

/// <summary>
/// HRC1_BBGN_ThepLong:
///  - loaiVung 1/3: xử lý như phiếu thường (scope-based qua MaKhuVuc&lt;-&gt;Scope).
///  - loaiVung 2 (Việc đến tôi): trả tất cả, user xử lý từng mẻ trong phiếu, không phê duyệt phiếu.
/// </summary>
public class Hrc1BbgnThepLongSearchFilterProvider : IPhieuSearchFilterProvider
{
    private const string MaBmValue = "HRC1_BBGN_ThepLong";

    public IReadOnlyCollection<string> MaBms { get; } = new[] { MaBmValue };

    public IQueryable<BmPhieu> BuildQuery(ProductFormContext context, int loaiVung, int userId)
    {
        var baseQuery = context.BmPhieus
            .Where(x => x.IsDelete != 1 && x.IsLock != 1 && x.MaBm == MaBmValue);

        return loaiVung switch
        {
            1 => PhieuSearchFilterHelpers.WithScopeBasedXuLy(baseQuery, context, userId),
            2 => baseQuery,
            3 => PhieuSearchFilterHelpers.WithScopeBasedXem(baseQuery, context, userId),
            _ => baseQuery.Where(_ => false),
        };
    }
}

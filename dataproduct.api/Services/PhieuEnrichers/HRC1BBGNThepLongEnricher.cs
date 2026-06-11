using dataproduct.api.Models;
using dataproduct.api.ResponseModels;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Services.PhieuEnrichers;

public class HRC1BBGNThepLongEnricher : IPhieuSearchEnricher
{
    private readonly ProductFormContext _context;
    public string MaBm => "HRC1_BBGN_ThepLong";

    public HRC1BBGNThepLongEnricher(ProductFormContext context) => _context = context;

    public async Task EnrichAsync(SearchPhieuResponseModel item)
    {
        if (!item.Scope.HasValue || !item.Ca.HasValue) return;

        var ngayStart = item.NgaySX.ToDateTime(TimeOnly.MinValue);
        var ngayEnd   = item.NgaySX.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var idMayDuc  = item.Scope.Value;
        var ca        = item.Ca.Value;

        item.SoLuongMe = await _context.HRC1_MeTheps.CountAsync(m =>
            m.IdMayDucDich == idMayDuc &&
            (
                (m.NgayNhanTL.HasValue && m.CaTinhLuyen == ca &&
                 m.NgayNhanTL.Value >= ngayStart && m.NgayNhanTL.Value < ngayEnd)
                ||
                (m.DichChuyen == "len_thang" && m.Ca == ca &&
                 m.NgayTao >= ngayStart && m.NgayTao < ngayEnd)
            )
        );
    }
}

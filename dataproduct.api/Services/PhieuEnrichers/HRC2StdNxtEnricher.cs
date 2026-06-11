using dataproduct.api.Models;
using dataproduct.api.ResponseModels;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Services.PhieuEnrichers;

public class HRC2StdNxtEnricher : IPhieuSearchEnricher
{
    private readonly ProductFormContext _context;
    public string MaBm => "HRC2_STD_NXT";

    public HRC2StdNxtEnricher(ProductFormContext context) => _context = context;

    public async Task EnrichAsync(SearchPhieuResponseModel item)
    {
        item.TinhTrang = await GetStatusAsync(item.NgaySX, item.Ca ?? 0);
    }

    private async Task<int> GetStatusAsync(DateOnly workDate, int shift)
    {
        var idPhieus = await _context.BmPhieus
            .Where(p => p.MaBm == "HRC2_STD_NXT"
                     && p.NgaySX == workDate
                     && p.Ca == shift
                     && p.IsDelete != 1)
            .Select(p => p.Idphieu)
            .ToListAsync();

        bool phanBoComplete = idPhieus.Any()
            && !await _context.STD_NXT_TOTAL_HRC2s
                .Where(r => idPhieus.Contains(r.Id_Phieu) && r.HasPhanBo == null)
                .AnyAsync();

        var nauLuyenMaBms = new[] { "HRC2_BB_NauLuyen_BOF", "HRC2_BB_NauLuyen_LF", "HRC2_BB_NauLuyen_RH" };
        var relatedStatuses = await _context.BmPhieus
            .Where(p => nauLuyenMaBms.Contains(p.MaBm)
                     && p.NgaySX == workDate
                     && p.Ca == shift
                     && p.IsDelete != 1
                     && p.IsLock != 1)
            .Select(p => p.TinhTrang)
            .ToListAsync();

        bool relatedComplete = relatedStatuses.Any()
            && relatedStatuses.All(t => t == 2 || t == 5);

        return (phanBoComplete && relatedComplete) ? 2 : 1;
    }
}

using dataproduct.api.Models;
using dataproduct.api.ResponseModels;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Services.PhieuEnrichers;

public class HRC1StdNxtEnricher : IPhieuSearchEnricher
{
    private readonly ProductFormContext _context;
    public string MaBm => "HRC1_STD_NXT";

    public HRC1StdNxtEnricher(ProductFormContext context) => _context = context;

    public async Task EnrichAsync(SearchPhieuResponseModel item)
    {
        item.TinhTrang = await GetStatusAsync(item.NgaySX, item.Ca ?? 0);
    }

    private async Task<int> GetStatusAsync(DateOnly workDate, int shift)
    {
        var idPhieus = await _context.BmPhieus
            .Where(p => p.MaBm == "HRC1_STD_NXT"
                     && p.NgaySX == workDate
                     && p.Ca == shift
                     && p.IsDelete != 1)
            .Select(p => p.Idphieu)
            .ToListAsync();

        bool phanBoComplete = idPhieus.Any()
            && !await _context.STD_NXT_TOTAL_HRC1s
                .Where(r => idPhieus.Contains(r.Id_Phieu) && r.HasPhanBo == null)
                .AnyAsync();

        var tieuHaoMaBms = new[] { "HRC1_BB_TieuHao_BOF", "HRC1_BB_TieuHao_LF" };
        var relatedStatuses = await _context.BmPhieus
            .Where(p => tieuHaoMaBms.Contains(p.MaBm)
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

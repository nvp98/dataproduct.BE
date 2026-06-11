using dataproduct.api.Models;
using dataproduct.api.ResponseModels;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Services.PhieuEnrichers;

public class CtdPhoiNongEnricher : IPhieuSearchEnricher
{
    private readonly ProductFormContext _context;
    public string MaBm => "CTD_BB_Phoinong";

    public CtdPhoiNongEnricher(ProductFormContext context) => _context = context;

    public async Task EnrichAsync(SearchPhieuResponseModel item)
    {
        var hasPendingCTD = await _context.CtdPhoiNongs
            .AnyAsync(x => x.NgaySx == item.NgaySX && x.Ca == item.Ca
                        && x.NmCan == item.MayDuc && x.TinhTrangCTD != 1);
        var hasPendingQLCL = await _context.CtdPhoiNongs
            .AnyAsync(x => x.NgaySx == item.NgaySX && x.Ca == item.Ca
                        && x.NmCan == item.MayDuc && x.TinhTrangQLCL != 1);

        if (hasPendingCTD)
        {
            var ctd = item.PheDuyet?.FirstOrDefault(x => x.CapDuyet == 2);
            if (ctd != null) ctd.TinhTrang = 0;
        }
        if (hasPendingQLCL)
        {
            var qlcl = item.PheDuyet?.FirstOrDefault(x => x.CapDuyet == 1);
            if (qlcl != null) qlcl.TinhTrang = 0;
        }
    }
}

using dataproduct.api.Models;
using dataproduct.api.ResponseModels;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Services.PhieuEnrichers;

public class HRC1GiaoNhanPhoiNhapKhoEnricher : IPhieuSearchEnricher
{
    private readonly ProductFormContext _context;
    public string MaBm => "HRC1_BB_GiaoNhanPhoiNhapKho";

    public HRC1GiaoNhanPhoiNhapKhoEnricher(ProductFormContext context) => _context = context;

    public async Task EnrichAsync(SearchPhieuResponseModel item)
    {
        var start   = item.NgaySX.ToDateTime(TimeOnly.MinValue);
        var nextDay = start.AddDays(1);
        var q = _context.BM_PhoiNhapKho.Where(x =>
            x.NgaySX >= start && x.NgaySX < nextDay &&
            x.Ca == item.Ca && x.MayDuc == item.MayDuc);

        var hasPendingCap0 = await q.AnyAsync() && await q.AllAsync(x => x.TinhTrangCap0 == 1);
        var hasPendingCap1 = await q.AnyAsync() && await q.AllAsync(x => x.TinhTrangCap1 == 1);
        var hasPendingCap2 = await q.AnyAsync() && await q.AllAsync(x => x.TinhTrangCap2 == 1);

        if (hasPendingCap0) { var c = item.PheDuyet?.FirstOrDefault(x => x.CapDuyet == 0); if (c != null) c.TinhTrang = 1; }
        if (hasPendingCap1) { var c = item.PheDuyet?.FirstOrDefault(x => x.CapDuyet == 1); if (c != null) c.TinhTrang = 1; }
        if (hasPendingCap2) { var c = item.PheDuyet?.FirstOrDefault(x => x.CapDuyet == 2); if (c != null) c.TinhTrang = 1; }
    }
}

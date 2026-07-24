using dataproduct.api.Models;
using dataproduct.api.ResponseModels;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Services.PhieuEnrichers;

/// <summary>
/// Ghi đè TinhTrang trong danh sách phiếu (chỉ ở response, không đụng cột DB thật) thành
/// trạng thái tổng hợp riêng cho BBGN Phôi tấm HRC2 — không dùng chung enum TrangThaiPhieuConst:
///   1 = Chưa hoàn thành (còn slab chưa được cả Đúc và Kho xác nhận)
///   2 = Đã hoàn thành   (mọi slab đã được Đúc + Kho xác nhận, nhưng PKH chưa chốt)
///   3 = Đã chốt         (BmPhieu.TinhTrang == 5, set bởi api/hrc2-slab/chot-phieu)
/// Logic hoàn thành y hệt getComputedPhieuStatus() phía FE (BkHrc2SlabTable.tsx).
/// </summary>
public class Hrc2BbgnPhoiTamEnricher : IPhieuSearchEnricher
{
    private readonly ProductFormContext _context;
    public string MaBm => "HRC2_BBGN_PhoiTam";

    public Hrc2BbgnPhoiTamEnricher(ProductFormContext context) => _context = context;

    public async Task EnrichAsync(SearchPhieuResponseModel item)
    {
        if (item.TinhTrang == 5)
        {
            item.TinhTrang = 3;
            return;
        }

        var records = await _context.BkHrc2SlabTrangThais
            .AsNoTracking()
            .Where(t => t.IdPhieuBBSL == item.Idphieu)
            .Select(t => new { t.TrangThaiDuc, t.TrangThaiKho })
            .ToListAsync();

        var hoanThanh = records.Count > 0 && records.All(t => t.TrangThaiDuc == 1 && t.TrangThaiKho == 1);
        item.TinhTrang = hoanThanh ? 2 : 1;
    }
}

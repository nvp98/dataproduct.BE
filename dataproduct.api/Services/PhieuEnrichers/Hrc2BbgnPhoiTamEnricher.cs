using dataproduct.api.Models;
using dataproduct.api.ResponseModels;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Services.PhieuEnrichers;

/// <summary>
/// Ghi đè TinhTrang trong danh sách phiếu (chỉ ở response, không đụng cột DB thật) thành
/// trạng thái tổng hợp riêng cho BBGN Phôi tấm HRC2 — không dùng chung enum TrangThaiPhieuConst:
///   11 = Chưa hoàn thành (còn slab chưa được cả Đúc và Kho xác nhận)
///   12 = Đã hoàn thành   (mọi slab đã được Đúc + Kho xác nhận, nhưng PKH chưa chốt)
///   5  = Đã chốt         (giữ nguyên TinhTrang thật của BmPhieu, KHÔNG override — 5 đã khớp
///                         nghĩa "Chốt" trong TrangThaiPhieuConst dùng chung)
///
/// Dùng 11/12 (ngoài dải 0-7 của TrangThaiPhieuConst) để tránh trùng mã với enum trạng thái
/// phiếu gốc khi hiển thị chung ở các màn hình dùng PHIEU_STATUS_CONFIG (vd trang Thống kê) —
/// trước đây dùng 1/2/3 nên phiếu "Đã chốt" (3) bị đọc nhầm thành "Đã thu hồi" ở trang Thống kê.
/// Logic hoàn thành y hệt getComputedPhieuStatus() phía FE (BkHrc2SlabTable.tsx).
/// </summary>
public class Hrc2BbgnPhoiTamEnricher : IPhieuSearchEnricher, IPhieuTinhTrangFilterEnricher
{
    private readonly ProductFormContext _context;
    public string MaBm => "HRC2_BBSL_PhoiTam";

    public Hrc2BbgnPhoiTamEnricher(ProductFormContext context) => _context = context;

    public async Task EnrichAsync(SearchPhieuResponseModel item)
    {
        // Số lượng ID Slab đã xác nhận theo từng bộ phận (Đúc/Kho/PKH) trên tổng số ID Slab của
        // phiếu — tính cho MỌI phiếu, kể cả đã chốt (TinhTrang == 5), vì UI danh sách phiếu cần
        // hiển thị dù phiếu đã chốt hay chưa. FE tự suy ra 3 trạng thái hiển thị (Chưa XN / Đã XN
        // một phần kèm số lượng / Đã XN hết) từ cặp (SoLuongXN*, SoLuongSlab).
        var records = await _context.BkHrc2SlabTrangThais
            .AsNoTracking()
            .Where(t => t.IdPhieuBBSL == item.Idphieu)
            .Select(t => new { t.TrangThaiDuc, t.TrangThaiKho, t.TrangThaiPKH })
            .ToListAsync();

        item.SoLuongSlab = records.Count;
        item.SoLuongXNDuc = records.Count(t => t.TrangThaiDuc == 1);
        item.SoLuongXNKho = records.Count(t => t.TrangThaiKho == 1);
        item.SoLuongXNPKH = records.Count(t => t.TrangThaiPKH == 1);

        if (item.TinhTrang == 5) return;

        var hoanThanh = item.SoLuongSlab > 0 && item.SoLuongXNDuc == item.SoLuongSlab && item.SoLuongXNKho == item.SoLuongSlab;
        item.TinhTrang = hoanThanh ? 12 : 11;
    }

    /// <summary>Bản batch của EnrichAsync ở trên, dùng để lọc TinhTrang trước khi phân trang.</summary>
    public async Task<List<Guid>> FilterByTinhTrangAsync(List<BmPhieu> candidates, int tinhTrang)
    {
        if (tinhTrang == 5)
            return candidates.Where(p => p.TinhTrang == 5).Select(p => p.Idphieu).ToList();

        if (tinhTrang != 11 && tinhTrang != 12)
            return [];

        var openCandidates = candidates.Where(p => p.TinhTrang != 5).ToList();
        if (openCandidates.Count == 0) return [];

        var idphieux = openCandidates.Select(p => p.Idphieu).ToList();
        var records = await _context.BkHrc2SlabTrangThais
            .AsNoTracking()
            .Where(t => t.IdPhieuBBSL != null && idphieux.Contains(t.IdPhieuBBSL.Value))
            .Select(t => new { t.IdPhieuBBSL, t.TrangThaiDuc, t.TrangThaiKho })
            .ToListAsync();

        var hoanThanhMap = records
            .GroupBy(r => r.IdPhieuBBSL!.Value)
            .ToDictionary(g => g.Key, g => g.All(r => r.TrangThaiDuc == 1 && r.TrangThaiKho == 1));

        return openCandidates
            .Where(p =>
            {
                var hoanThanh = hoanThanhMap.TryGetValue(p.Idphieu, out var v) && v;
                return tinhTrang == 12 ? hoanThanh : !hoanThanh;
            })
            .Select(p => p.Idphieu)
            .ToList();
    }
}

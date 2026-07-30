using dataproduct.api.Models;
using dataproduct.api.ResponseModels;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Services.PhieuEnrichers;

/// <summary>
/// Ghi đè TinhTrang trong danh sách phiếu (chỉ ở response, không đụng cột DB thật) thành
/// trạng thái tổng hợp riêng cho BBGN Phôi tấm HRC1 — không dùng chung enum TrangThaiPhieuConst:
///   11 = Chưa hoàn thành (còn slab chưa được Đúc + Cán + C4 xác nhận đầy đủ)
///   12 = Đã hoàn thành   (mọi slab đã được Đúc + Cán + C4 xác nhận, nhưng PKH chưa chốt)
///   5  = Đã chốt         (giữ nguyên TinhTrang thật của BmPhieu, KHÔNG override — 5 đã khớp
///                         nghĩa "Chốt" trong TrangThaiPhieuConst dùng chung)
///
/// Dùng 11/12 (ngoài dải 0-7 của TrangThaiPhieuConst) để tránh trùng mã với enum trạng thái
/// phiếu gốc khi hiển thị chung ở các màn hình dùng PHIEU_STATUS_CONFIG (vd trang Thống kê) —
/// trước đây dùng 1/2/3 nên phiếu "Đã chốt" (3) bị đọc nhầm thành "Đã thu hồi" ở trang Thống kê.
///
/// Cách xác định slab thuộc phiếu y hệt điều kiện validate trong
/// Hrc1SlabRepository.ChotPhieuAsync: slab "tự nhiên" (khớp NgaySX + Ca, chưa bị chuyển ca)
/// cộng với slab "được chuyển ca" vào đúng phiếu này (IsChuyenCa + IdPhieuBBSL).
/// </summary>
public class Hrc1BbgnPhoiTamEnricher : IPhieuSearchEnricher
{
    private readonly ProductFormContext _context;
    public string MaBm => "HRC1_BBSL_PhoiTam";

    public Hrc1BbgnPhoiTamEnricher(ProductFormContext context) => _context = context;

    public async Task EnrichAsync(SearchPhieuResponseModel item)
    {
        if (item.TinhTrang == 5) return;

        var caStr = item.Ca?.ToString();

        var naturalIds = await _context.Hrc1Slabs
            .AsNoTracking()
            .Where(s => s.NgaySX == item.NgaySX && s.CaSX == caStr
                        && !_context.Hrc1SlabTrangThais.Any(t => t.IdSlab == s.Id && t.IsChuyenCa))
            .Select(s => s.Id)
            .ToListAsync();

        var naturalTTMap = naturalIds.Count > 0
            ? await _context.Hrc1SlabTrangThais
                .AsNoTracking()
                .Where(t => naturalIds.Contains(t.IdSlab))
                .ToDictionaryAsync(t => t.IdSlab)
            : new Dictionary<int, Hrc1SlabTrangThai>();

        var transferredRecords = await _context.Hrc1SlabTrangThais
            .AsNoTracking()
            .Where(t => t.IsChuyenCa && t.IdPhieuBBSL == item.Idphieu)
            .ToListAsync();

        if (naturalIds.Count == 0 && transferredRecords.Count == 0)
        {
            item.TinhTrang = 11;
            return;
        }

        var chuaXacNhan = naturalIds.Count(id =>
                !naturalTTMap.TryGetValue(id, out var tt) || tt.TrangThaiDuc != 1 || tt.TrangThaiCan != 1 || !tt.TrangThaiC4)
            + transferredRecords.Count(t => t.TrangThaiDuc != 1 || t.TrangThaiCan != 1 || !t.TrangThaiC4);

        item.TinhTrang = chuaXacNhan == 0 ? 12 : 11;
    }
}

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
public class Hrc2BbgnPhoiTamEnricher : IPhieuSearchEnricher
{
    private readonly ProductFormContext _context;
    public string MaBm => "HRC2_BBSL_PhoiTam";

    public Hrc2BbgnPhoiTamEnricher(ProductFormContext context) => _context = context;

    public async Task EnrichAsync(SearchPhieuResponseModel item)
    {
        if (item.TinhTrang == 5) return;

        var records = await _context.BkHrc2SlabTrangThais
            .AsNoTracking()
            .Where(t => t.IdPhieuBBSL == item.Idphieu)
            .Select(t => new { t.TrangThaiDuc, t.TrangThaiKho })
            .ToListAsync();

        var hoanThanh = records.Count > 0 && records.All(t => t.TrangThaiDuc == 1 && t.TrangThaiKho == 1);
        item.TinhTrang = hoanThanh ? 12 : 11;
    }
}

using dataproduct.api.DTOs.Export;
using dataproduct.api.Models;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Services.Exporters
{
    public class HRC1_BBGNDetailExcelExporter : IPhieuExcelExporter
    {
        private readonly ProductFormContext _ctx;
        private readonly HRC1_BBGNService _svc;

        private static readonly string[] _supported =
        {
            "HRC1_LoThoi",
            "HRC1_TinhLuyen",
            "HRC1_BBGN_ThepLong",
        };

        public HRC1_BBGNDetailExcelExporter(ProductFormContext ctx, HRC1_BBGNService svc)
        {
            _ctx = ctx;
            _svc = svc;
        }

        public bool CanHandle(string? maBm) =>
            !string.IsNullOrWhiteSpace(maBm) &&
            _supported.Any(s => s.Equals(maBm, StringComparison.OrdinalIgnoreCase));

        public Task<ExportFileResult> ExportTongHopExcelAsync(DateOnly? fromDate, DateOnly? toDate) =>
            throw new NotSupportedException("Dùng endpoint /api/hrc1/export/excel để xuất tổng hợp HRC1.");

        public async Task<ExportFileResult> ExportDetailExcelAsync(Guid phieuId)
        {
            var phieu = await _ctx.BmPhieus.FirstOrDefaultAsync(p => p.Idphieu == phieuId)
                ?? throw new KeyNotFoundException($"Không tìm thấy phiếu {phieuId}.");

            if (!phieu.NgaySX.HasValue || !phieu.Ca.HasValue)
                throw new InvalidOperationException("Phiếu thiếu NgaySX hoặc Ca, không thể xuất Excel.");

            var query = new dataproduct.api.DTOs.HRC1_ExportQuery
            {
                TuNgay  = phieu.NgaySX,
                DenNgay = phieu.NgaySX,
                Ca      = phieu.Ca,
            };

            switch (phieu.MaBm)
            {
                case "HRC1_TinhLuyen":
                    query.TlSo = phieu.Scope;
                    return await _svc.ExportExcelAsync(query);

                case "HRC1_BBGN_ThepLong":
                    query.IdMayDuc = phieu.Scope;
                    query.Kip      = phieu.Kip;
                    return await _svc.ExportThepLongISOAsync(query);

                default:
                    if (phieu.Scope.HasValue) query.LoSo = phieu.Scope;
                    return await _svc.ExportExcelAsync(query);
            }
        }
    }
}

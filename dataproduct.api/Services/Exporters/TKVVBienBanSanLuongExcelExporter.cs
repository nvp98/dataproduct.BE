using dataproduct.api.DTOs.Export;
using dataproduct.api.Services;

namespace dataproduct.api.Services.Exporters
{
    public class TKVVBienBanSanLuongExcelExporter : IPhieuExcelExporter
    {
        private readonly TKVV_BBSLService _service;

        public TKVVBienBanSanLuongExcelExporter(TKVV_BBSLService service)
        {
            _service = service;
        }

        public bool CanHandle(string? maBm)
        {
            if (string.IsNullOrWhiteSpace(maBm))
                return false;

            return maBm.Equals("TKVV_BB_SanLuong", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM.01-QT.05.03", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM.01/QT.05.03", StringComparison.OrdinalIgnoreCase);
        }

        public Task<ExportFileResult> ExportTongHopExcelAsync(DateOnly? fromDate, DateOnly? toDate)
            => throw new NotSupportedException("Chưa hỗ trợ export tổng hợp Excel cho biên bản xác nhận sản lượng.");

        public Task<ExportFileResult> ExportDetailExcelAsync(Guid phieuId)
            => _service.ExportBienBanExcelAsync(phieuId);
    }
}

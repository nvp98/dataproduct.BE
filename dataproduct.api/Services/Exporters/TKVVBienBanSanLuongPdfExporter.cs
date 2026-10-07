using dataproduct.api.DTOs.Export;
using dataproduct.api.Services;

namespace dataproduct.api.Services.Exporters
{
    public class TKVVBienBanSanLuongPdfExporter : IPhieuPdfExporter
    {
        private readonly TKVV_BBSLService _service;

        public TKVVBienBanSanLuongPdfExporter(TKVV_BBSLService service)
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

        public Task<ExportFileResult> ExportPdfAsync(Guid phieuId)
            => _service.ExportBienBanPdfAsync(phieuId);
    }
}

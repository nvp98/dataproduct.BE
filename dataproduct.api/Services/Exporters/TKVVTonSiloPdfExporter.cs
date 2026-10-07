using dataproduct.api.DTOs.Export;
using dataproduct.api.Services.NMTKVV;

namespace dataproduct.api.Services.Exporters
{
    public class TKVVTonSiloPdfExporter : IPhieuPdfExporter
    {
        private readonly TKVV_TonSiloService _service;

        public TKVVTonSiloPdfExporter(TKVV_TonSiloService service)
        {
            _service = service;
        }

        public bool CanHandle(string? maBm)
        {
            if (string.IsNullOrWhiteSpace(maBm))
                return false;

            return maBm.Equals("TKVV_TONSILO", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM.05-QT.05.03", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM.05/QT.05.03", StringComparison.OrdinalIgnoreCase);
        }

        public Task<ExportFileResult> ExportPdfAsync(Guid phieuId)
            => _service.ExportTonSiloPdfAsync(phieuId);
    }
}

using dataproduct.api.DTOs.Export;

namespace dataproduct.api.Services.Exporters
{
    public class NLBTDBenPhePdfExporter : IPhieuPdfExporter
    {
        private readonly NLBTDBenPheService _service;

        public NLBTDBenPhePdfExporter(NLBTDBenPheService service)
        {
            _service = service;
        }

        public bool CanHandle(string? maBm)
        {
            if (string.IsNullOrWhiteSpace(maBm))
                return false;

            return maBm.Equals("NL_BB_TheoDoiBenPhe", StringComparison.OrdinalIgnoreCase);
        }

        public Task<ExportFileResult> ExportPdfAsync(Guid phieuId)
        {
            return _service.ExportPdfAsync(phieuId);
        }
        public Task<ExportFileResult> ExportPdfAsyncExtra(Guid phieuId, List<string>? filters)
        {
            return _service.ExportPdfAsync(phieuId, filters);
        }
    }
}

using dataproduct.api.DTOs.Export;

namespace dataproduct.api.Services.Exporters
{
    public class BBGNThepLongPdfExporter : IPhieuPdfExporter
    {
        private readonly BBGN_ThepLongService _service;

        public BBGNThepLongPdfExporter(BBGN_ThepLongService service)
        {
            _service = service;
        }

        public bool CanHandle(string? maBm)
        {
            if (string.IsNullOrWhiteSpace(maBm))
                return false;

            return maBm.Equals("HRC2_BBGN_ThepLong", StringComparison.OrdinalIgnoreCase);
        }

        public Task<ExportFileResult> ExportPdfAsync(Guid phieuId)
        {
            return _service.ExportPdfAsync(phieuId);
        }
    }
}

using dataproduct.api.DTOs.Export;

namespace dataproduct.api.Services.Exporters
{
    public class CTDSoTheoDoiPdfExporter : IPhieuPdfExporter
    {
        private readonly CTDSoTheoDoiService _service;

        public CTDSoTheoDoiPdfExporter(CTDSoTheoDoiService service)
        {
            _service = service;
        }

        public bool CanHandle(string? maBm)
        {
            if (string.IsNullOrWhiteSpace(maBm))
                return false;

            return maBm.Equals("CTD_STD_Sanxuat", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("CTD_SoTheoDoi", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM.09-QT.05.13", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM.09/QT.05.13", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM09-QT.05.13", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM09/QT.05.13", StringComparison.OrdinalIgnoreCase);
        }

        public Task<ExportFileResult> ExportPdfAsync(Guid phieuId)
        {
            return _service.ExportPdfAsync(phieuId);
        }
    }
}

using dataproduct.api.DTOs.Export;

namespace dataproduct.api.Services.Exporters
{
    public class CTDPhieuXuLyKphPdfExporter : IPhieuPdfExporter
    {
        private readonly CTDPhieuXuLyKphService _service;

        public CTDPhieuXuLyKphPdfExporter(CTDPhieuXuLyKphService service)
        {
            _service = service;
        }

        public bool CanHandle(string? maBm)
        {
            if (string.IsNullOrWhiteSpace(maBm))
                return false;

            return maBm.Equals("CTD_KPH_Sanxuat", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("CTD_XuLyKPH", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM.01C-QT.11", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM.01C/QT.11", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM01C-QT.11", StringComparison.OrdinalIgnoreCase)
                || maBm.Contains("KPH", StringComparison.OrdinalIgnoreCase);
        }

        public Task<ExportFileResult> ExportPdfAsync(Guid phieuId)
        {
            return _service.ExportPdfXuLyKphAsync(phieuId);
        }
    }
}

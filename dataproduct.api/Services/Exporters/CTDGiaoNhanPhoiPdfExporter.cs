using dataproduct.api.DTOs.Export;

namespace dataproduct.api.Services.Exporters
{
    public class CTDGiaoNhanPhoiPdfExporter : IPhieuPdfExporter
    {
        private readonly CTDGiaoNhanPhoiService _service;

        public CTDGiaoNhanPhoiPdfExporter(CTDGiaoNhanPhoiService service)
        {
            _service = service;
        }

        public bool CanHandle(string? maBm)
        {
            if (string.IsNullOrWhiteSpace(maBm))
                return false;

            return maBm.Equals("CTD_BB_GNP", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("CTD_BB_GiaoNhanPhoi", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM.05-QT.05.13", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM.05/QT.05.13", StringComparison.OrdinalIgnoreCase);
        }

        public Task<ExportFileResult> ExportPdfAsync(Guid phieuId)
        {
            return _service.ExportPdfAsync(phieuId);
        }
    }
}

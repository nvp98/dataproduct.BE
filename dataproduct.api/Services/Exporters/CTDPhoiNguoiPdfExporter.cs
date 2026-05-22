using dataproduct.api.DTOs.Export;

namespace dataproduct.api.Services.Exporters
{
    public class CTDPhoiNguoiPdfExporter : IPhieuPdfExporter
    {
        private readonly CTDPhoiNapNguoiService _service;

        public CTDPhoiNguoiPdfExporter(CTDPhoiNapNguoiService service)
        {
            _service = service;
        }

        public bool CanHandle(string? maBm)
        {
            if (string.IsNullOrWhiteSpace(maBm))
                return false;

            return maBm.Equals("CTD_BB_Phoinapnguoi", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("CTD_BB_PhoiNguoi", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM.02-QT.05.13", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM.02/QT.05.13", StringComparison.OrdinalIgnoreCase);
        }

        public Task<ExportFileResult> ExportPdfAsync(Guid phieuId)
        {
            return _service.ExportPdfAsync(phieuId);
        }
    }
}

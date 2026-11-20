using dataproduct.api.DTOs.Export;

namespace dataproduct.api.Services.Exporters
{
    /// <summary>PDF Biên bản chốt số liệu Xưởng ĐPV (BM.14/QT.05.11, MaBM HRC1_BCSX_DPV).</summary>
    public class HRC1_BCSXPdfExporter : IPhieuPdfExporter
    {
        private readonly HRC1_BCSXExportService _service;

        public HRC1_BCSXPdfExporter(HRC1_BCSXExportService service)
        {
            _service = service;
        }

        public bool CanHandle(string? maBm)
            => string.Equals(maBm, HRC1_BCSXService.MaBM, StringComparison.OrdinalIgnoreCase);

        public Task<ExportFileResult> ExportPdfAsync(Guid phieuId) => _service.ExportPdfAsync(phieuId);
    }
}

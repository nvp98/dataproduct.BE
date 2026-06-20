using dataproduct.api.DTOs.Export;

namespace dataproduct.api.Services.Exporters
{
    public class LGNLNapLieuPdfExporter : IPhieuPdfExporter
    {
        private readonly LGNLService _service;
        private readonly PheDuyetService _pheDuyetService;

        public LGNLNapLieuPdfExporter(LGNLService service, PheDuyetService pheDuyetService)
        {
            _service = service;
            _pheDuyetService = pheDuyetService;
        }

        public bool CanHandle(string? maBm)
        {
            if (string.IsNullOrWhiteSpace(maBm))
                return false;

            return maBm.Equals("NMLG_BM_NapLieuLoCao", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<ExportFileResult> ExportPdfAsync(Guid phieuId)
        {
            var pheDuyets = await _pheDuyetService.GetPheDuyetPhieuAsync(phieuId);
            return await _service.ExportNapLieuPdfAsync(phieuId, pheDuyets);
        }
    }
}

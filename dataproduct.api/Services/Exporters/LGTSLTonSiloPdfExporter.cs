using dataproduct.api.DTOs.Export;

namespace dataproduct.api.Services.Exporters
{
    public class LGTSLTonSiloPdfExporter : IPhieuPdfExporter
    {
        private readonly LGTSLService _service;
        private readonly PheDuyetService _pheDuyetService;

        public LGTSLTonSiloPdfExporter(LGTSLService service, PheDuyetService pheDuyetService)
        {
            _service = service;
            _pheDuyetService = pheDuyetService;
        }

        public bool CanHandle(string? maBm)
        {
            if (string.IsNullOrWhiteSpace(maBm))
                return false;

            return maBm.Equals("NMLG_BM_TonSiLoLoCao", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<ExportFileResult> ExportPdfAsync(Guid phieuId)
        {
            var pheDuyets = await _pheDuyetService.GetPheDuyetPhieuAsync(phieuId);
            return await _service.ExportTonSiloPdfAsync(phieuId, pheDuyets);
        }
    }
}

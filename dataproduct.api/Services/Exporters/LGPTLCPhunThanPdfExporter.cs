using dataproduct.api.DTOs.Export;

namespace dataproduct.api.Services.Exporters
{
    public class LGPTLCPhunThanPdfExporter : IPhieuPdfExporter
    {
        private readonly LGPTLCService _service;
        private readonly PheDuyetService _pheDuyetService;

        public LGPTLCPhunThanPdfExporter(LGPTLCService service, PheDuyetService pheDuyetService)
        {
            _service = service;
            _pheDuyetService = pheDuyetService;
        }

        public bool CanHandle(string? maBm)
        {
            if (string.IsNullOrWhiteSpace(maBm))
                return false;

            return maBm.Equals("NMLG_NK_VHPTLC", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<ExportFileResult> ExportPdfAsync(Guid phieuId)
        {
            var pheDuyets = await _pheDuyetService.GetPheDuyetPhieuAsync(phieuId);
            return await _service.ExportPhunThanPdfAsync(phieuId, pheDuyets);
        }
    }
}

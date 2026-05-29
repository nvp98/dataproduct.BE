using dataproduct.api.DTOs.Export;

namespace dataproduct.api.Services.Exporters
{
    public class LGPTLCPhunThanExcelExporter : IPhieuExcelExporter
    {
        private readonly LGPTLCService _service;

        public LGPTLCPhunThanExcelExporter(LGPTLCService service)
        {
            _service = service;
        }

        public bool CanHandle(string? maBm)
        {
            if (string.IsNullOrWhiteSpace(maBm))
                return false;

            return maBm.Equals("NMLG_NK_VHPTLC", StringComparison.OrdinalIgnoreCase);
        }

        public Task<ExportFileResult> ExportTongHopExcelAsync(DateOnly? fromDate, DateOnly? toDate)
            => throw new NotSupportedException("Chưa hỗ trợ export tổng hợp Excel cho phun than lò cao.");

        public Task<ExportFileResult> ExportExcelPhieuAsync(Guid phieuId)
            => _service.ExportPhunThanExcelAsync(phieuId);
    }
}

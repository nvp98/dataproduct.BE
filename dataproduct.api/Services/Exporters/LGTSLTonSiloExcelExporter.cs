using dataproduct.api.DTOs.Export;

namespace dataproduct.api.Services.Exporters
{
    public class LGTSLTonSiloExcelExporter : IPhieuExcelExporter
    {
        private readonly LGTSLService _service;

        public LGTSLTonSiloExcelExporter(LGTSLService service)
        {
            _service = service;
        }

        public bool CanHandle(string? maBm)
        {
            if (string.IsNullOrWhiteSpace(maBm))
                return false;

            return maBm.Equals("NMLG_BM_TonSiLoLoCao", StringComparison.OrdinalIgnoreCase);
        }

        public Task<ExportFileResult> ExportTongHopExcelAsync(DateOnly? fromDate, DateOnly? toDate)
            => throw new NotSupportedException("Chưa hỗ trợ export tổng hợp Excel cho tồn silo lò cao.");

        public Task<ExportFileResult> ExportExcelPhieuAsync(Guid phieuId)
            => _service.ExportTonSiloExcelAsync(phieuId);
    }
}

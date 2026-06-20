using dataproduct.api.DTOs.Export;

namespace dataproduct.api.Services.Exporters
{
    public class LGNLNapLieuExcelExporter : IPhieuExcelExporter
    {
        private readonly LGNLService _service;

        public LGNLNapLieuExcelExporter(LGNLService service)
        {
            _service = service;
        }

        public bool CanHandle(string? maBm)
        {
            if (string.IsNullOrWhiteSpace(maBm))
                return false;

            return maBm.Equals("NMLG_BM_NapLieuLoCao", StringComparison.OrdinalIgnoreCase);
        }

        public Task<ExportFileResult> ExportTongHopExcelAsync(DateOnly? fromDate, DateOnly? toDate)
            => throw new NotSupportedException("Chưa hỗ trợ export tổng hợp Excel cho nạp liệu lò cao.");

        public Task<ExportFileResult> ExportExcelPhieuAsync(Guid phieuId)
            => _service.ExportNapLieuExcelAsync(phieuId);
    }
}

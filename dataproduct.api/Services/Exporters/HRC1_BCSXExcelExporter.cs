using dataproduct.api.DTOs.Export;

namespace dataproduct.api.Services.Exporters
{
    /// <summary>Excel Biên bản chốt số liệu Xưởng ĐPV: 1 phiếu (bố cục biên bản) + tổng hợp theo khoảng ngày.</summary>
    public class HRC1_BCSXExcelExporter : IPhieuExcelExporter
    {
        private readonly HRC1_BCSXExportService _service;

        public HRC1_BCSXExcelExporter(HRC1_BCSXExportService service)
        {
            _service = service;
        }

        public bool CanHandle(string? maBm)
            => string.Equals(maBm, HRC1_BCSXService.MaBM, StringComparison.OrdinalIgnoreCase);

        public Task<ExportFileResult> ExportTongHopExcelAsync(DateOnly? fromDate, DateOnly? toDate)
            => _service.ExportTongHopExcelAsync(fromDate, toDate);

        public Task<ExportFileResult> ExportDetailExcelAsync(Guid phieuId) => _service.ExportDetailExcelAsync(phieuId);
    }
}

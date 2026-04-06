using dataproduct.api.DTOs.Export;

namespace dataproduct.api.Services.Exporters
{
    public interface IPhieuExcelExporter
    {
        bool CanHandle(string? maBm);
        Task<ExportFileResult> ExportTongHopExcelAsync(DateOnly? fromDate, DateOnly? toDate);
        Task<ExportFileResult> ExportDetailExcelAsync(Guid phieuId) =>
            throw new NotSupportedException("Chưa hỗ trợ export detail Excel cho biểu mẫu này.");
    }
}

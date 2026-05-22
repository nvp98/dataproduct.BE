using dataproduct.api.DTOs.Export;

namespace dataproduct.api.Services.Exporters
{
    public interface IPhieuPdfExporter
    {
        bool CanHandle(string? maBm);
        Task<ExportFileResult> ExportPdfAsync(Guid phieuId);

        /// <summary>
        /// Export PDF với các điều kiện lọc động (nếu có)
        /// Default: gọi ExportPdfAsync (bỏ qua filters). Override nếu muốn xử lý filters.
        /// </summary>
        async Task<ExportFileResult> ExportPdfAsyncExtra(Guid phieuId, List<string>? filters)
        {
            // Default: bỏ qua filters, chỉ export toàn bộ dữ liệu
            return await ExportPdfAsync(phieuId);
        }
    }
}

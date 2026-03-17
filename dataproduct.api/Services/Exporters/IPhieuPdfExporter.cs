using dataproduct.api.DTOs.Export;

namespace dataproduct.api.Services.Exporters
{
    public interface IPhieuPdfExporter
    {
        bool CanHandle(string? maBm);
        Task<ExportFileResult> ExportPdfAsync(Guid phieuId);
    }
}

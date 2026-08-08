using dataproduct.api.DTOs.Export;

namespace dataproduct.api.Services.Exporters
{
    // Route PDF cho HRC1_BB_TieuHao_BOF / HRC1_BB_TieuHao_LF vào api/Phieus/{id}/export-pdf — mirror
    // HRC1TieuHaoExcelExporter.cs, xem DLNMHRC1Service.ExportTieuHaoPdfAsync.
    public class HRC1TieuHaoPdfExporter : IPhieuPdfExporter
    {
        private readonly DLNMHRC1Service _svc;

        private static readonly string[] _supported =
        {
            "HRC1_BB_TieuHao_BOF",
            "HRC1_BB_TieuHao_LF",
        };

        public HRC1TieuHaoPdfExporter(DLNMHRC1Service svc)
        {
            _svc = svc;
        }

        public bool CanHandle(string? maBm) =>
            !string.IsNullOrWhiteSpace(maBm) &&
            _supported.Any(s => s.Equals(maBm, StringComparison.OrdinalIgnoreCase));

        public Task<ExportFileResult> ExportPdfAsync(Guid phieuId) =>
            _svc.ExportTieuHaoPdfAsync(phieuId);
    }
}

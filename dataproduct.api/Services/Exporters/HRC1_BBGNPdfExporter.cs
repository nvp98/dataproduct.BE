using dataproduct.api.DTOs.Export;

namespace dataproduct.api.Services.Exporters
{
    public class HRC1_BBGNPdfExporter : IPhieuPdfExporter
    {
        private readonly HRC1_BBGNService _svc;

        private static readonly string[] _supported =
        {
            "HRC1_LoThoi",
            "HRC1_TinhLuyen",
            "HRC1_BBGN_ThepLong",
        };

        public HRC1_BBGNPdfExporter(HRC1_BBGNService svc)
        {
            _svc = svc;
        }

        public bool CanHandle(string? maBm) =>
            !string.IsNullOrWhiteSpace(maBm) &&
            _supported.Any(s => s.Equals(maBm, StringComparison.OrdinalIgnoreCase));

        public Task<ExportFileResult> ExportPdfAsync(Guid phieuId) =>
            _svc.ExportPdfAsync(phieuId);
    }
}

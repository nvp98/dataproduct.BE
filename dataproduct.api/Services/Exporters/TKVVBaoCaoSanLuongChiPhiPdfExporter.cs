using dataproduct.api.DTOs.Export;
using dataproduct.api.Services.NMTKVV;

namespace dataproduct.api.Services.Exporters
{
    public class TKVVBaoCaoSanLuongChiPhiPdfExporter : IPhieuPdfExporter
    {
        private readonly TKVV_BCSL_ChiPhiService _service;

        public TKVVBaoCaoSanLuongChiPhiPdfExporter(TKVV_BCSL_ChiPhiService service)
        {
            _service = service;
        }

        public bool CanHandle(string? maBm)
        {
            if (string.IsNullOrWhiteSpace(maBm))
                return false;

            return maBm.Equals("TKVV_BC_SanLuongChiPhi", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM.06-QT.05.03", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM.06/QT.05.03", StringComparison.OrdinalIgnoreCase);
        }

        public Task<ExportFileResult> ExportPdfAsync(Guid phieuId)
            => _service.ExportBaoCaoPdfAsync(phieuId);
    }
}

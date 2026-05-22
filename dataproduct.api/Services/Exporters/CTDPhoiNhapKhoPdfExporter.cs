using dataproduct.api.DTOs.Export;

namespace dataproduct.api.Services.Exporters
{
    public class CTDPhoiNhapKhoPdfExporter : IPhieuPdfExporter
    {
        private readonly BMDucCTDService _service;

        public CTDPhoiNhapKhoPdfExporter(BMDucCTDService service)
        {
            _service = service;
        }

        public bool CanHandle(string? maBm)
        {
            if (string.IsNullOrWhiteSpace(maBm))
                return false;

            return maBm.Equals("HRC1_BB_GiaoNhanPhoiNhapKho", StringComparison.OrdinalIgnoreCase);
        }

        public Task<ExportFileResult> ExportPdfAsync(Guid phieuId)
        {
            return _service.ExportPdfPhoiNhapKhoByPhieuAsync(phieuId);
        }
    }
}

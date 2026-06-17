using dataproduct.api.DTOs.Export;

namespace dataproduct.api.Services.Exporters
{
    public class CTDSoTheoDoiExcelExporter : IPhieuExcelExporter
    {
        private readonly CTDSoTheoDoiService _service;

        public CTDSoTheoDoiExcelExporter(CTDSoTheoDoiService service)
        {
            _service = service;
        }

        public bool CanHandle(string? maBm)
        {
            if (string.IsNullOrWhiteSpace(maBm))
                return false;

            return maBm.Equals("CTD_STD_Sanxuat", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("CTD_SoTheoDoi", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM.09-QT.05.13", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM.09/QT.05.13", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM09-QT.05.13", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM09/QT.05.13", StringComparison.OrdinalIgnoreCase);
        }

        public Task<ExportFileResult> ExportTongHopExcelAsync(DateOnly? fromDate, DateOnly? toDate)
        {
            return _service.ExportTongHopExcelAsync(fromDate, toDate);
        }

        public Task<ExportFileResult> ExportDetailExcelAsync(Guid phieuId)
        {
            return _service.ExportChiTietExcelAsync(phieuId);
        }

        public Task<ExportFileResult> ExportChiTietExcelAsync(Guid phieuId)
        {
            return _service.ExportChiTietExcelAsync(phieuId);
        }
    }
}

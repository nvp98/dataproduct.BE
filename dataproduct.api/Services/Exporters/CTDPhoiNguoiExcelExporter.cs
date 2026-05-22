using dataproduct.api.DTOs.Export;

namespace dataproduct.api.Services.Exporters
{
    public class CTDPhoiNguoiExcelExporter : IPhieuExcelExporter
    {
        private readonly CTDPhoiNapNguoiService _service;

        public CTDPhoiNguoiExcelExporter(CTDPhoiNapNguoiService service)
        {
            _service = service;
        }

        public bool CanHandle(string? maBm)
        {
            if (string.IsNullOrWhiteSpace(maBm))
                return false;

            return maBm.Equals("CTD_BB_Phoinapnguoi", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("CTD_BB_PhoiNguoi", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM.02-QT.05.13", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM.02/QT.05.13", StringComparison.OrdinalIgnoreCase);
        }

        public Task<ExportFileResult> ExportTongHopExcelAsync(DateOnly? fromDate, DateOnly? toDate)
        {
            return _service.ExportTongHopExcelByPhieuAsync(fromDate, toDate);
        }
        public Task<ExportFileResult> ExportExcelPhieuAsync(Guid phieuId)
        {
            return _service.ExportExcelByPhieuAsync(phieuId);
        }
    }
}

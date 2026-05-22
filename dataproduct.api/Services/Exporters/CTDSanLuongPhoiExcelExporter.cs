using dataproduct.api.DTOs.Export;

namespace dataproduct.api.Services.Exporters
{
    public class CTDSanLuongPhoiExcelExporter : IPhieuExcelExporter
    {
        private readonly BMDucCTDService _service;

        public CTDSanLuongPhoiExcelExporter(BMDucCTDService service)
        {
            _service = service;
        }

        public bool CanHandle(string? maBm)
        {
            if (string.IsNullOrWhiteSpace(maBm))
                return false;

            return maBm.Equals("CTD_BB_SanLuongPhoi", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("CTD_BB_SanluongPhoi", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM.11-QT.05.11", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM.11/QT.05.11", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM11-QT.05.11", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM11/QT.05.11", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<ExportFileResult> ExportTongHopExcelAsync(DateOnly? fromDate, DateOnly? toDate)
        {
            var content = await _service.ExportExcelSanLuongPhoiAsync(fromDate, toDate);
            return new ExportFileResult
            {
                Content = content,
                FileName = $"TongHopSanLuongPhoi_{DateTime.Now:yyyyMMddHHmmss}.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            };
        }
    }
}

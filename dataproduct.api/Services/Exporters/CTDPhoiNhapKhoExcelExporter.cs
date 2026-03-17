using dataproduct.api.DTOs.Export;

namespace dataproduct.api.Services.Exporters
{
    public class CTDPhoiNhapKhoExcelExporter : IPhieuExcelExporter
    {
        private readonly BMDucCTDService _service;

        public CTDPhoiNhapKhoExcelExporter(BMDucCTDService service)
        {
            _service = service;
        }

        public bool CanHandle(string? maBm)
        {
            if (string.IsNullOrWhiteSpace(maBm))
                return false;

            return maBm.Equals("CTD_BB_GiaoNhanPhoiNhapKho", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("CTD_BB_PhoiNhapkho", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM12-QT.05.11", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM.12-QT.05.11", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM12/QT.05.11", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM.12/QT.05.11", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<ExportFileResult> ExportTongHopExcelAsync(DateOnly? fromDate, DateOnly? toDate)
        {
            var content = await _service.ExportExcelByBmPhieuAsync(fromDate, toDate);
            return new ExportFileResult
            {
                Content = content,
                FileName = $"TongHopPhoiNhapKho_{DateTime.Now:yyyyMMddHHmmss}.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            };
        }
    }
}

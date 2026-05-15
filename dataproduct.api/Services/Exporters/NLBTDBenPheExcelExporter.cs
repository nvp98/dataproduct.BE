using dataproduct.api.DTOs.Export;

namespace dataproduct.api.Services.Exporters
{
    public class NLBTDBenPheExcelExporter : IPhieuExcelExporter
    {
        private readonly NLBTDBenPheService _service;

        public NLBTDBenPheExcelExporter(NLBTDBenPheService service)
        {
            _service = service;
        }

        public bool CanHandle(string? maBm)
        {
            if (string.IsNullOrWhiteSpace(maBm))
                return false;

            return maBm.Equals("NL_BB_TheoDoiBenPhe", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<ExportFileResult> ExportTongHopExcelAsync(DateOnly? fromDate, DateOnly? toDate)
        {
            var content = await _service.ExportExcelByBmPhieuAsync(fromDate, toDate);
            return new ExportFileResult
            {
                Content = content,
                FileName = $"BM.18-HD.25.08_TongHopTheoDoiBenPhe_{DateTime.Now:yyyyMMddHHmmss}.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            };
        }
    }
}

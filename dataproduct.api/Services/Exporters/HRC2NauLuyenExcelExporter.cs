using ClosedXML.Excel;
using dataproduct.api.DTOs.Export;
using dataproduct.api.Models;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Services.Exporters
{
    public class HRC2NauLuyenExcelExporter : IPhieuExcelExporter
    {
        private readonly ProductFormContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly PhieuDetailExcelService _excelService;

        private static readonly string[] _supportedMaBm =
        {
            "HRC2_BB_NauLuyen_BOF",
            "HRC2_BB_NauLuyen_LF",
            "HRC2_BB_NauLuyen_RH",
        };

        public HRC2NauLuyenExcelExporter(ProductFormContext context, IWebHostEnvironment env, PhieuDetailExcelService excelService)
        {
            _context = context;
            _env = env;
            _excelService = excelService;
        }

        public bool CanHandle(string? maBm) =>
            !string.IsNullOrWhiteSpace(maBm) &&
            _supportedMaBm.Any(s => s.Equals(maBm, StringComparison.OrdinalIgnoreCase));

        public Task<ExportFileResult> ExportTongHopExcelAsync(DateOnly? fromDate, DateOnly? toDate) =>
            throw new NotSupportedException("Chưa hỗ trợ export tổng hợp cho biểu mẫu HRC2 Nấu Luyện.");

        // public async Task<ExportFileResult> ExportDetailExcelAsync(Guid phieuId)
        // {
        //     var phieu = await _context.BmPhieus.FirstOrDefaultAsync(x => x.Idphieu == phieuId)
        //         ?? throw new Exception("Không tìm thấy phiếu.");

        //     if (!phieu.NgaySX.HasValue || !phieu.Ca.HasValue || !phieu.Scope.HasValue)
        //         throw new Exception("Phiếu thiếu thông tin NgaySX / Ca / Scope để truy vấn dữ liệu.");

        //     var templatePath = Path.Combine(_env.WebRootPath, "templates", $"{phieu.MaBm}.xlsx");
        //     if (!File.Exists(templatePath))
        //         throw new NotSupportedException($"Chưa có template Excel cho '{phieu.MaBm}'. Đặt file tại: wwwroot/templates/{phieu.MaBm}.xlsx");

        //     // Lấy dữ liệu từ DB (DLNM_HRC2 + PhuLieu_HRC2) thay vì DataJson
        //     var (headersBOF, headersLFRH, rows) = await _excelService.GetExportDataAsync(
        //         phieu.NgaySX.Value,
        //         phieu.Ca.Value,
        //         phieu.MaBm,
        //         phieu.Scope.Value);

        //     bool isBof = phieu.MaBm.Contains("BOF", StringComparison.OrdinalIgnoreCase);
        //     var headers = isBof ? headersBOF : headersLFRH;

        //     using var workbook = new XLWorkbook(templatePath);
        //     var ws = workbook.Worksheets.First();

        //     _excelService.ReplaceMarkers(ws, phieu);
        //     _excelService.RenderBodyFromDb(ws, phieu.MaBm, phieu.Scope.Value, headers, rows);

        //     ws.Columns().AdjustToContents();

        //     using var stream = new MemoryStream();
        //     workbook.SaveAs(stream);

        //     var fileName = $"{phieu.MaBm}_{phieu.SoPhieu?.Replace("/", "-")}.xlsx";
        //     return new ExportFileResult
        //     {
        //         Content = stream.ToArray(),
        //         FileName = fileName,
        //         ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        //     };
        // }
    }
}

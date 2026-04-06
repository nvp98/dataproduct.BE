using dataproduct.api.DTOs;
using dataproduct.api.DTOs.Export;
using dataproduct.api.Models;
using dataproduct.api.Services;
using dataproduct.api.ResponseModels;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;

namespace dataproduct.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DLNMHRC2Controller : ControllerBase
    {
        private readonly DLNMHRC2Service _service;
        private readonly HRC2_NMSyncService _hrc2NMSyncService;
        private readonly PhieuDetailExcelService _excelService;
        private readonly IWebHostEnvironment _env;

        public DLNMHRC2Controller(
            DLNMHRC2Service service,
            HRC2_NMSyncService hrc2NMSyncService,
            PhieuDetailExcelService excelService,
            IWebHostEnvironment env)
        {
            _service = service;
            _hrc2NMSyncService = hrc2NMSyncService;
            _excelService = excelService;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(DateTime? NgaySX, int? Ca, string? LoaiBM, int? KhuVuc)
        {
            try
            {
                var data = await _service.GetAllAsync(NgaySX, Ca, LoaiBM, KhuVuc);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var result = await _service.GetByIdAsync(id);
                return result == null ? NotFound() : Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpGet("report/{reportNo}")]
        public async Task<IActionResult> GetByReportNo(int reportNo)
        {
            try
            {
                var result = await _service.GetByReportNoAsync(reportNo);
                return result == null ? NotFound() : Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPost("filter")]
        public async Task<IActionResult> Filter([FromBody] SyncFromNM_HRC2_Request request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("Thiếu dữ liệu bộ lọc.");
                }
                var result = await _service.FilterGroupedAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DLNM_HRC2 model)
        {
            try
            {
                var created = await _service.CreateAsync(model);
                return CreatedAtAction(nameof(GetById), new { id = created.ID }, created);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] DLNM_HRC2 model)
        {
            try
            {
                var ok = await _service.UpdateAsync(id, model);
                return ok ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var ok = await _service.DeleteAsync(id);
                return ok ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search(DateTime? NgaySX, int? Ca, string? LoaiBM, int? Scope, string? searchText, int page = 1, int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100; // Giới hạn tối đa 100 records mỗi trang

            try
            {
                var result = await _service.SearchWithPagingAsync(
                    NgaySX,
                    Ca,
                    LoaiBM,
                    Scope,
                    searchText,
                    page,
                    pageSize
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpGet("search-grouped")]
        public async Task<IActionResult> SearchGrouped(DateTime? NgaySX, int? Ca, string? LoaiBM, int? Scope, string? searchText, int page = 1, int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100; // Giới hạn tối đa 100 records mỗi trang

            try
            {
                var result = await _service.SearchGroupedWithPagingAsync(
                    NgaySX,
                    Ca,
                    LoaiBM,
                    Scope,
                    searchText,
                    page,
                    pageSize
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        // Thống kê: trả về list Header_Key IsUsedThongKe + dữ liệu theo REPORT_NO
        [HttpPost("search-thongke")]
        public async Task<IActionResult> SearchThongKe([FromBody] SearchThongKe dto)
        {
            try
            {
                var result = await _service.SearchThongKeAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        // Tổng cộng từng phụ liệu trên toàn khoảng lọc — tách riêng để FE gọi lazy
        [HttpPost("sum-thongke")]
        public async Task<IActionResult> SumThongKe([FromBody] SearchThongKe dto)
        {
            try
            {
                var result = await _service.GetThongKeSumAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPost("export-thongke")]
        public async Task<IActionResult> ExportThongKe([FromBody] SearchThongKe dto)
        {
            try
            {
                var file = await _service.ExportThongKeExcelAsync(dto);
                return File(file.Content, file.ContentType, file.FileName);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }


        [HttpPost("chuyen-me-thoi")]
        public async Task<IActionResult> ChuyenMeThoi([FromBody] ChuyenMeThoiRequest request)
        {
            try
            {
                var result = await _service.ChuyenMeThoiAsync(request);
                return Ok(new { data = new { message = "Chuyển mẻ thành công" } });

            }
            catch (ApplicationException ex)
            {
                // Trả về đúng message nghiệp vụ được throw từ Repository
                return BadRequest(new { data = new { message = ex.Message } });
            }

        }

        [HttpGet("export-excel-detail")]
        public async Task<IActionResult> ExportExcelDetail(
            [FromQuery] DateOnly ngay,
            [FromQuery] int ca,
            [FromQuery] int scope,
            [FromQuery] string bieuMau,
            [FromQuery] Guid idPhieu)
        {
            try
            {
                var phieu = await _excelService.GetBmPhieuByIdOrThrowAsync(idPhieu);
                if (!phieu.NgaySX.HasValue || !phieu.Ca.HasValue)
                    throw new InvalidOperationException("Phiếu thiếu NgaySX/Ca để export Excel.");

                var ngayPhieu = phieu.NgaySX.Value;
                var caPhieu = phieu.Ca.Value;
                var kipPhieu = phieu.Kip ?? "";
                var scopePhieu = phieu.Scope ?? scope;

                var templateName = bieuMau.ToUpper() switch
                {
                    "BOF" => "HRC2_BB_NauLuyen_BOF",
                    "LF"  => "HRC2_BB_NauLuyen_LF",
                    "RH"  => "HRC2_BB_NauLuyen_RH",
                    _     => throw new NotSupportedException($"Biểu mẫu '{bieuMau}' không hợp lệ. Chỉ hỗ trợ: BOF, LF, RH.")
                };

                var templatePath = Path.Combine(_env.WebRootPath, "templates", $"{templateName}.xlsx");
                if (!System.IO.File.Exists(templatePath))
                    throw new NotSupportedException(
                        $"Chưa có template Excel cho '{templateName}'. Đặt file tại: wwwroot/templates/{templateName}.xlsx");

                var (headersBOF, headersLFRH, rows) =
                    await _excelService.GetExportDataAsync(ngayPhieu, caPhieu, bieuMau, scopePhieu);

                bool isBof = bieuMau.Equals("BOF", StringComparison.OrdinalIgnoreCase);
                var headers = isBof ? headersBOF : headersLFRH;

                var dateStr = ngayPhieu.ToString("ddMMyyyy");
                var fileName = $"{templateName}_Ca{caPhieu}_{dateStr}.xlsx";

                // ClosedXML bị lỗi khi save workbook có drawing phức tạp ra MemoryStream
                // Workaround: save ra temp file rồi đọc lại bytes
                var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.xlsx");
                try
                {
                    using (var workbook = new XLWorkbook(templatePath))
                    {
                        var ws = workbook.Worksheets.First();
                        _excelService.RenderBodyFromDb(
                            ws,
                            templateName,
                            headers,
                            rows,
                            scopePhieu,
                            ngayPhieu: ngayPhieu,
                            caPhieu: caPhieu,
                            kip: kipPhieu);
                        workbook.SaveAs(tempPath);
                    }
                    var bytes = System.IO.File.ReadAllBytes(tempPath);
                    // XLSX là file zip; kiểm tra chữ ký "PK" để tránh trả về bytes lỗi (FE sẽ download thành .xlsx và Excel báo không mở được).
                    if (bytes == null || bytes.Length < 4 || bytes[0] != (byte)'P' || bytes[1] != (byte)'K')
                    {
                        throw new InvalidOperationException(
                            $"Generated excel bytes không phải định dạng XLSX hợp lệ (template: {templateName}).");
                    }
                    // Kiểm tra zip structure (XLSX là file ZIP).
                    // Không dùng ClosedXML để re-read vì nó không đọc được file có drawing.
                    try
                    {
                        using var ms  = new System.IO.MemoryStream(bytes);
                        using var zip = new ZipArchive(ms, ZipArchiveMode.Read, true);
                        if (zip.GetEntry("[Content_Types].xml") == null)
                            throw new InvalidOperationException("XLSX thiếu entry [Content_Types].xml.");
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(
                            $"File XLSX sinh ra không hợp lệ. Template: {templateName}. Lỗi: {ex.Message}");
                    }
                    return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
                finally
                {
                    if (System.IO.File.Exists(tempPath)) System.IO.File.Delete(tempPath);
                }
            }
            catch (Exception ex)
            {
                // Trả message thật về FE để dễ debug nguyên nhân export lỗi.
                // (FE hiện tại đang chỉ show message chung, nên cần phần này để thấy lý do.)
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { message = ex.Message }
                );
            }
        }

        [HttpGet("export-pdf-detail")]
        public async Task<IActionResult> ExportPdfDetail(
            [FromQuery] DateOnly ngay,
            [FromQuery] int ca,
            [FromQuery] int scope,
            [FromQuery] string bieuMau,
            [FromQuery] Guid idPhieu)
        {
            try
            {
                var file = await _excelService.ExportPdfDetailAsync(ngay, ca, bieuMau, scope, idPhieu);
                return File(file.Content, file.ContentType, file.FileName);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPost("filterSTD_NXT")]

        public async Task<IActionResult> FilterSTD_NXT([FromBody] FilterSTD_NXTRequest request)
        {
            try
            {
                var result = await _service.FilterSTD_NXTAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }   
        }
    }


}

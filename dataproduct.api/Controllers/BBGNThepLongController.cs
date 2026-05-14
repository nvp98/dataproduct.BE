using dataproduct.api.DTOs;
using dataproduct.api.DTOs.Export;
using dataproduct.api.Models;
using dataproduct.api.Services;
using dataproduct.api.ResponseModels;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;
using System.IO;

namespace dataproduct.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BBGNThepLongController : ControllerBase
    {
        private readonly BBGN_ThepLongService _service;
        private readonly SyncPhanLoaiService _syncPhanLoaiService;
        private readonly IWebHostEnvironment _env;

        public BBGNThepLongController(
            BBGN_ThepLongService service,
            SyncPhanLoaiService syncPhanLoaiService,
            IWebHostEnvironment env)
        {
            _service = service;
            _syncPhanLoaiService = syncPhanLoaiService;
            _env = env;
        }

        
        // [HttpGet("{id}")]
        // public async Task<IActionResult> GetById(int id)
        // {
        //     try
        //     {
        //         var result = await _service.GetByIdAsync(id);
        //         return result == null ? NotFound() : Ok(result);
        //     }
        //     catch (Exception ex)
        //     {
        //         return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        //     }
        // }

        

        // [HttpGet("search")]
        // public async Task<IActionResult> Search(DateTime? NgaySX, int? Ca, string? LoaiBM, int? Scope, string? searchText, int page = 1, int pageSize = 10)
        // {
        //     if (page < 1) page = 1;
        //     if (pageSize < 1) pageSize = 10;
        //     if (pageSize > 100) pageSize = 100; // Giới hạn tối đa 100 records mỗi trang

        //     try
        //     {
        //         var result = await _service.SearchWithPagingAsync(
        //             NgaySX,
        //             Ca,
        //             LoaiBM,
        //             Scope,
        //             searchText,
        //             page,
        //             pageSize
        //         );

        //         return Ok(result);
        //     }
        //     catch (Exception ex)
        //     {
        //         return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        //     }
        // }

        [HttpPost("/api/phan-loai/sync")]
        public async Task<IActionResult> SyncPhanLoai([FromBody] SyncPhanLoaiRequest request)
        {
            try
            {
                var result = await _syncPhanLoaiService.SyncAsync(request);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPost("fetch")]
        public async Task<IActionResult> Fetch([FromBody] FetchMeThoiRequest request)
        {
            var result = await _service.FetchMeThoiAsync(request);
            return Ok(result);
        }

        [HttpPost("load")]
        public async Task<IActionResult> Load([FromBody] LoadBBGNThepLongRequest request)
        {
            try
            {
                var result = await _service.LoadAsync(request);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteRow(int id)
        {
            var deleted = await _service.DeleteRowAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }

        [HttpPost("search-thongke")]
        public async Task<IActionResult> SearchThongKe([FromBody] SearchThongKeBBGNThepLongRequest request)
        {
            try
            {
                var result = await _service.SearchThongKeAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPost("sum-thongke")]
        public async Task<IActionResult> SumThongKe([FromBody] SearchThongKeBBGNThepLongRequest request)
        {
            try
            {
                var result = await _service.SumThongKeAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpGet("export-pdf")]
        public async Task<IActionResult> ExportPdf([FromQuery] Guid idPhieu)
        {
            try
            {
                var file = await _service.ExportPdfAsync(idPhieu);
                return File(file.Content, file.ContentType, file.FileName);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpGet("export-excel")]
        public async Task<IActionResult> ExportExcel([FromQuery] Guid idPhieu)
        {
            try
            {
                var templatePath = Path.Combine(_env.WebRootPath, "templates", "BBGN_ThepLong.xlsx");
                if (!System.IO.File.Exists(templatePath))
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        "Chưa có template Excel. Đặt file tại: wwwroot/templates/BBGN_ThepLong.xlsx");

                var rows  = await _service.LoadByIdPhieuAsync(idPhieu);
                var phieu = await _service.GetBmPhieuByIdAsync(idPhieu);
                var fileName = $"BBGN_ThepLong_{idPhieu:N}.xlsx";

                var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.xlsx");
                try
                {
                    using (var workbook = new XLWorkbook(templatePath))
                    {
                        var ws = workbook.Worksheets.First();
                        _service.RenderBBGNThepLongExcel(ws, rows, phieu);
                        workbook.SaveAs(tempPath);
                    }
                    var bytes = System.IO.File.ReadAllBytes(tempPath);
                    return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
                finally
                {
                    if (System.IO.File.Exists(tempPath)) System.IO.File.Delete(tempPath);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
    }
}

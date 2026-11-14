using dataproduct.api.Services;
using Microsoft.AspNetCore.Mvc;

namespace dataproduct.api.Controllers
{
    /// <summary>Kho dữ liệu — Động lực: danh mục tag EMS, tra cứu dữ liệu theo ca, đồng bộ từ EMS.</summary>
    [Route("api/DongLuc")]
    [ApiController]
    public class DongLucController : ControllerBase
    {
        private readonly DongLucService _service;
        private readonly DongLucSyncService _syncService;

        public DongLucController(DongLucService service, DongLucSyncService syncService)
        {
            _service = service;
            _syncService = syncService;
        }

        private string? NguoiThaoTac => User.FindFirst("username")?.Value;

        // ── Tag ────────────────────────────────────────────────────────────────

        [HttpGet("tag")]
        public async Task<IActionResult> GetTags(string? maXuong, byte? tinhTrang, string? searchKey)
            => Ok(await _service.GetTagsAsync(maXuong, tinhTrang, searchKey));

        [HttpPost("tag")]
        public async Task<IActionResult> CreateTag([FromBody] TagDongLucPayload payload)
        {
            try
            {
                return Ok(await _service.CreateTagAsync(payload, NguoiThaoTac));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("tag/{id}")]
        public async Task<IActionResult> UpdateTag(int id, [FromBody] TagDongLucPayload payload)
        {
            try
            {
                var ok = await _service.UpdateTagAsync(id, payload, NguoiThaoTac);
                return ok ? NoContent() : NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Xóa mềm
        [HttpDelete("tag/{id}")]
        public async Task<IActionResult> DeleteTag(int id)
        {
            var ok = await _service.DeleteTagAsync(id, NguoiThaoTac);
            return ok ? NoContent() : NotFound();
        }

        // ── Dữ liệu (chỉ đọc) ──────────────────────────────────────────────────

        [HttpGet("du-lieu/search")]
        public async Task<IActionResult> SearchDuLieu(DateOnly tuNgay, DateOnly denNgay, byte? caSX, string? maXuong, string? maHeader, int page = 1, int pageSize = 50)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;
            if (pageSize > 500) pageSize = 500;

            var (rows, total) = await _service.SearchDuLieuAsync(tuNgay, denNgay, caSX, maXuong, maHeader, page, pageSize);
            return Ok(new
            {
                data = rows,
                totalRecords = total,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling((double)total / pageSize)
            });
        }

        [HttpGet("du-lieu/export-excel")]
        public async Task<IActionResult> ExportExcel(DateOnly tuNgay, DateOnly denNgay, byte? caSX, string? maXuong, string? maHeader)
        {
            var bytes = await _service.ExportDuLieuExcelAsync(tuNgay, denNgay, caSX, maXuong, maHeader);
            var fileName = $"DuLieuDongLuc_{tuNgay:yyyyMMdd}_{denNgay:yyyyMMdd}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // ── Đồng bộ ────────────────────────────────────────────────────────────

        [HttpPost("dong-bo")]
        public async Task<IActionResult> DongBo(DateOnly tuNgay, DateOnly denNgay, string? maXuong)
        {
            try
            {
                var log = await _syncService.SyncAsync(tuNgay, denNgay, maXuong, NguoiThaoTac, HttpContext.RequestAborted);
                return log.ThanhCong ? Ok(log) : BadRequest(new { message = $"Đồng bộ lỗi: {log.LoiChiTiet}", log });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("dong-bo/lan-gan-nhat")]
        public async Task<IActionResult> LanGanNhat()
            => Ok(await _service.GetSyncLogGanNhatAsync());
    }
}

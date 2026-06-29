using dataproduct.api.DTOs;
using dataproduct.api.Services;
using Microsoft.AspNetCore.Mvc;

namespace dataproduct.api.Controllers
{
    [Route("api/hrc2-slab")]
    [ApiController]
    public class Hrc2SlabController : ControllerBase
    {
        private readonly Hrc2SlabService _svc;

        public Hrc2SlabController(Hrc2SlabService svc)
        {
            _svc = svc;
        }

        // ── Đọc dữ liệu ─────────────────────────────────────────────────────

        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] Hrc2SlabSearchRequest request)
        {
            var (data, total) = await _svc.SearchAsync(request);
            return Ok(new
            {
                data,
                totalCount = total,
                page = request.Page,
                pageSize = request.PageSize
            });
        }

        [HttpGet("tong-hop")]
        public async Task<IActionResult> GetTongHop(
            [FromQuery] string? tuNgay,
            [FromQuery] string? denNgay,
            [FromQuery] string? ca,
            [FromQuery] string? kip)
        {
            var data = await _svc.GetTongHopAsync(tuNgay, denNgay, ca, kip);
            return Ok(data);
        }

        [HttpGet("phieu-bbsl")]
        public async Task<IActionResult> GetPhieuBBSL(
            [FromQuery] string? kip,
            [FromQuery] int? ca)
        {
            var data = await _svc.GetPhieuBBSLAsync(kip, ca);
            return Ok(data);
        }

        [HttpGet("ruot-phieu/{idPhieu:guid}")]
        public async Task<IActionResult> GetRuotPhieu(Guid idPhieu)
        {
            var data = await _svc.GetRuotPhieuAsync(idPhieu);
            return Ok(data);
        }

        [HttpGet("slabs-by-phieu/{idPhieu:guid}")]
        public async Task<IActionResult> GetSlabsByPhieu(Guid idPhieu)
        {
            var data = await _svc.GetSlabsByPhieuAsync(idPhieu);
            return Ok(data);
        }

        // ── Workflow ─────────────────────────────────────────────────────────

        [HttpPost("xac-nhan")]
        public async Task<IActionResult> XacNhan([FromBody] Hrc2XacNhanRequest request)
        {
            if (request.IdSlabs.Count == 0)
                return BadRequest("Danh sách slab không được rỗng.");
            if (request.LoaiXacNhan != "KCS" && request.LoaiXacNhan != "Duc"
                && request.LoaiXacNhan != "Kho" && request.LoaiXacNhan != "PKH")
                return BadRequest("LoaiXacNhan phải là 'KCS', 'Duc', 'Kho' hoặc 'PKH'.");

            await _svc.XacNhanAsync(request);
            return Ok(new { success = true, message = $"Đã xác nhận ({request.LoaiXacNhan}) {request.IdSlabs.Count} slab.", affectedRows = request.IdSlabs.Count });
        }

        [HttpPost("huy-xac-nhan")]
        public async Task<IActionResult> HuyXacNhan([FromBody] Hrc2XacNhanRequest request)
        {
            if (request.IdSlabs.Count == 0)
                return BadRequest("Danh sách slab không được rỗng.");
            if (request.LoaiXacNhan != "KCS" && request.LoaiXacNhan != "Duc"
                && request.LoaiXacNhan != "Kho" && request.LoaiXacNhan != "PKH")
                return BadRequest("LoaiXacNhan phải là 'KCS', 'Duc', 'Kho' hoặc 'PKH'.");

            await _svc.HuyXacNhanAsync(request);
            return Ok(new { success = true, message = $"Đã hủy xác nhận ({request.LoaiXacNhan}) {request.IdSlabs.Count} slab.", affectedRows = request.IdSlabs.Count });
        }

        [HttpPost("chot-phieu")]
        public async Task<IActionResult> ChotPhieu([FromBody] Hrc2ChotPhieuRequest request)
        {
            await _svc.ChotPhieuAsync(request);
            return Ok(new { success = true, message = "Đã chốt phiếu thành công.", affectedRows = 1 });
        }

        [HttpPost("huy-chot-phieu")]
        public async Task<IActionResult> HuyChotPhieu([FromBody] Hrc2ChotPhieuRequest request)
        {
            await _svc.HuyChotPhieuAsync(request);
            return Ok(new { success = true, message = "Đã hủy chốt phiếu thành công.", affectedRows = 1 });
        }

        // ── Chuyển / Thu hồi slab ────────────────────────────────────────────

        [HttpPost("chuyen-bbsl")]
        public async Task<IActionResult> ChuyenBbsl([FromBody] Hrc2ChuyenBbslRequest request)
        {
            if (request.IdSlabs.Count == 0)
                return BadRequest("Danh sách slab không được rỗng.");
            try
            {
                var affected = await _svc.ChuyenBbslAsync(request);
                return Ok(new { success = true, message = $"Đã chuyển {affected} slab vào phiếu.", affectedRows = affected });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("thu-hoi")]
        public async Task<IActionResult> ThuHoi([FromBody] Hrc2ChuyenBbslRequest request)
        {
            if (request.IdSlabs.Count == 0)
                return BadRequest("Danh sách slab không được rỗng.");

            var affected = await _svc.ThuHoiAsync(request);
            return Ok(new { success = true, message = $"Đã thu hồi {affected} slab.", affectedRows = affected });
        }

        // ── Sync ─────────────────────────────────────────────────────────────

        [HttpPost("sync")]
        public IActionResult Sync([FromBody] object? request)
        {
            // Sync HRC2 được xử lý bởi HRC2_NMSyncService (background service)
            // Endpoint này chỉ trả về status hiện tại
            return Ok(new { trangThai = "RUNNING", ghiChu = "Sync được xử lý bởi background service" });
        }

        [HttpGet("sync/status")]
        public async Task<IActionResult> GetSyncStatus()
        {
            var status = await _svc.GetSyncStatusAsync();
            return Ok(status);
        }

        // ── Export (placeholder — sẽ bổ sung sau) ────────────────────────────

        [HttpGet("export/excel")]
        public IActionResult ExportExcel([FromQuery] Guid idPhieu, [FromQuery] string tab = "chitiet")
        {
            return StatusCode(501, new { message = "Chức năng export Excel HRC2 đang phát triển." });
        }

        [HttpGet("export/pdf")]
        public IActionResult ExportPdf([FromQuery] Guid idPhieu)
        {
            return StatusCode(501, new { message = "Chức năng export PDF HRC2 đang phát triển." });
        }
    }
}

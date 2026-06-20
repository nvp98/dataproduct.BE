using dataproduct.api.DTOs.NMLG_Dto;
using dataproduct.api.Services;
using Microsoft.AspNetCore.Mvc;

namespace dataproduct.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LGPTLCController : ControllerBase
    {
        private readonly LGPTLCService _service;
        private readonly PheDuyetService _pdservice;

        public LGPTLCController(LGPTLCService service, PheDuyetService pdservice)
        {
            _service = service;
            _pdservice = pdservice;
        }

        // ─── Auto data (from SCADA/PLC source) ───────────────────────────────────

        [HttpGet("get-phunthan-auto-data")]
        public async Task<IActionResult> GetDLPhunThanAutoData(
            [FromQuery] int idLoCao,
            [FromQuery] DateTime ngaySanXuat,
            [FromQuery] Guid? idPhieu = null)
        {
            try
            {
                var result = await _service.GetAutoDataAsync(new GetAutoDataRequest
                {
                    IdLoCao = idLoCao,
                    NgaySanXuat = ngaySanXuat,
                    IdPhieu = idPhieu
                });
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // ─── Chi tiết (LG_NKVHPT_ChiTiet) ────────────────────────────────────────

        [HttpGet("chitiet/{idPhieu}")]
        public async Task<IActionResult> GetChiTietByPhieu(Guid idPhieu)
        {
            try
            {
                return Ok(await _service.GetChiTietByPhieuAsync(idPhieu));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPost("chitiet/update-manual")]
        public async Task<IActionResult> UpdateManualValues([FromBody] UpdateLGPTLCManualDto dto)
        {
            try
            {
                await _service.UpdateManualValuesAsync(dto);
                return Ok(new { message = "Cập nhật giá trị thủ công thành công." });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPut("chitiet/{idPhieu}/summary")]
        public async Task<IActionResult> UpdateSummary(Guid idPhieu, [FromBody] UpdateLGPTLCSummaryDto dto)
        {
            try
            {
                await _service.UpdatePhieuSummaryAsync(idPhieu, dto);
                return Ok(new { message = "Cập nhật tổng hợp thành công." });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // ─── Export PDF ───────────────────────────────────────────────────────────

        [HttpGet("export-pdf/{idPhieu}")]
        public async Task<IActionResult> ExportPdf(Guid idPhieu)
        {
            try
            {
                var pheDuyets = await _pdservice.GetPheDuyetPhieuAsync(idPhieu);
                var file = await _service.ExportPhunThanPdfAsync(idPhieu, pheDuyets);
                return File(file.Content, file.ContentType, file.FileName);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // ─── Export Excel ─────────────────────────────────────────────────────────

        [HttpGet("export-excel/{idPhieu}")]
        public async Task<IActionResult> ExportExcel(Guid idPhieu)
        {
            try
            {
                var file = await _service.ExportPhunThanExcelAsync(idPhieu);
                return File(file.Content, file.ContentType, file.FileName);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}

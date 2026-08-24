using dataproduct.api.DTOs.NMTKVV_Dto;
using dataproduct.api.Services.NMTKVV;
using Microsoft.AspNetCore.Mvc;

namespace dataproduct.api.Controllers.NMTKVV
{
    [Route("api/[controller]")]
    [ApiController]
    public class TKVV_TonSiloController : ControllerBase
    {
        private readonly TKVV_TonSiloService _service;

        public TKVV_TonSiloController(TKVV_TonSiloService service)
        {
            _service = service;
        }

        // Khởi tạo dữ liệu kíp: INSERT/UPDATE TKVV_TonSilo với TonCuoiAuto từ SP,
        // giữ nguyên giá trị người dùng đã nhập nếu bản ghi đã tồn tại.
        [HttpPost("init-rows")]
        public async Task<IActionResult> InitRows([FromBody] InitTonSiloRowsRequestDto request)
        {
            try
            {
                if (request.Ca != 1 && request.Ca != 2)
                    return BadRequest(new { message = "ca chỉ nhận 1 hoặc 2." });
                if (request.Scope < 1 || request.Scope > 6)
                    return BadRequest(new { message = "scope phải từ 1 đến 6." });
                return Ok(await _service.InitRowsAsync(request));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, detail = ex.InnerException?.Message });
            }
        }

        [HttpGet("rows-by-phieu/{phieuId}")]
        public async Task<IActionResult> GetRowsByPhieuId(Guid phieuId)
        {
            try
            {
                return Ok(await _service.GetRowsByPhieuIdAsync(phieuId));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("save-phieu-rows")]
        public async Task<IActionResult> SavePhieuRows([FromBody] SaveTonSiloPhieuRequestDto request)
        {
            try
            {
                if (request.Rows == null || request.Rows.Count == 0)
                    return BadRequest(new { message = "Danh sách dòng trống." });
                await _service.SavePhieuRowsAsync(request);
                return Ok(new { message = "Đã lưu." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, detail = ex.InnerException?.Message });
            }
        }
    }
}

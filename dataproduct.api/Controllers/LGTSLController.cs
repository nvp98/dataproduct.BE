using dataproduct.api.DTOs.NMLG_Dto;
using dataproduct.api.Services;
using Microsoft.AspNetCore.Mvc;

namespace dataproduct.api.Controllers
{
    /// <summary>
    /// Tồn Silo Lò Cao: quản lý LG_TSL_SiLo, LG_TSL_NVL, LG_TSL_SiLo_Mapping
    /// Base route: /api/LGTSL
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class LGTSLController : ControllerBase
    {
        private readonly LGTSLService _service;

        public LGTSLController(LGTSLService service)
        {
            _service = service;
        }

        // ─── SiLo + NVL view theo Ngày/Ca/LoCao (dùng trong tạo phiếu tồn silo) ──

        /// <summary>
        /// Lấy danh sách Silo kèm NVL đang được mapping theo Ngày/Ca/LoCao.
        /// Dùng để load dữ liệu khi tạo phiếu tồn silo lò cao.
        /// </summary>
        [HttpGet("tonsilo-silo-mapping")]
        public async Task<IActionResult> GetSiLoByMapping(
            [FromQuery] int? idLoCao,
            [FromQuery] DateTime? ngay,
            [FromQuery] int? ca)
        {
            try
            {
                if (ca.HasValue && ca != 1 && ca != 2)
                    return BadRequest(new { message = "ca chỉ nhận giá trị 1 hoặc 2." });

                return Ok(await _service.GetSiLoByMappingAsync(idLoCao, ngay, ca));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // ─── SiLo ────────────────────────────────────────────────────────────────

        [HttpGet("tonsilo-silo")]
        public async Task<IActionResult> GetSiLoList([FromQuery] int? idLoCao)
        {
            try { return Ok(await _service.GetAllSiLoListAsync(idLoCao)); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("tonsilo-silo/{id}")]
        public async Task<IActionResult> GetSiLoById(int id)
        {
            try
            {
                var r = await _service.GetSiLoByIdAsync(id);
                return r == null ? NotFound(new { message = $"Không tìm thấy Silo ID={id}" }) : Ok(r);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPost("tonsilo-silo")]
        public async Task<IActionResult> CreateSiLo([FromBody] CreateLGTSSiLoDto dto)
        {
            try
            {
                var r = await _service.AddSiLoAsync(dto);
                return CreatedAtAction(nameof(GetSiLoById), new { id = r.ID }, r);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPut("tonsilo-silo/{id}")]
        public async Task<IActionResult> UpdateSiLo(int id, [FromBody] UpdateLGTSSiLoDto dto)
        {
            try
            {
                var r = await _service.UpdateSiLoAsync(id, dto);
                return r == null ? NotFound(new { message = $"Không tìm thấy Silo ID={id}" }) : Ok(r);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpDelete("tonsilo-silo/{id}")]
        public async Task<IActionResult> DeleteSiLo(int id)
        {
            try
            {
                var ok = await _service.DeleteSiLoAsync(id);
                return ok ? Ok(new { message = "Đã xóa thành công." }) : NotFound(new { message = $"Không tìm thấy Silo ID={id}" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // ─── NVL ─────────────────────────────────────────────────────────────────

        [HttpGet("tonsilo-nvl")]
        public async Task<IActionResult> GetNvlList([FromQuery] int? idLoCao)
        {
            try { return Ok(await _service.GetNvlListAsync(idLoCao)); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("tonsilo-nvl/{id}")]
        public async Task<IActionResult> GetNvlById(int id)
        {
            try
            {
                var r = await _service.GetNvlByIdAsync(id);
                return r == null ? NotFound(new { message = $"Không tìm thấy NVL ID={id}" }) : Ok(r);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPost("tonsilo-nvl")]
        public async Task<IActionResult> CreateNvl([FromBody] CreateLGTSNvlDto dto)
        {
            try
            {
                var r = await _service.AddNvlAsync(dto);
                return CreatedAtAction(nameof(GetNvlById), new { id = r.ID }, r);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message,
                    inner = ex.InnerException?.Message
                });
            }
        }

        [HttpPut("tonsilo-nvl/{id}")]
        public async Task<IActionResult> UpdateNvl(int id, [FromBody] UpdateLGTSNvlDto dto)
        {
            try
            {
                var r = await _service.UpdateNvlAsync(id, dto);
                return r == null ? NotFound(new { message = $"Không tìm thấy NVL ID={id}" }) : Ok(r);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpDelete("tonsilo-nvl/{id}")]
        public async Task<IActionResult> DeleteNvl(int id)
        {
            try
            {
                var ok = await _service.DeleteNvlAsync(id);
                return ok ? Ok(new { message = "Đã xóa thành công." }) : NotFound(new { message = $"Không tìm thấy NVL ID={id}" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPut("tonsilo-nvl/xac-nhan")]
        public async Task<IActionResult> UpdateXacNhan([FromBody] UpdateLGTSXacNhanDto dto)
        {
            try
            {
                var ok = await _service.UpdateXacNhanAsync(dto);
                return ok
                    ? Ok(new { message = "Cập nhật xác nhận thành công." })
                    : NotFound(new { message = $"Không tìm thấy NVL ID={dto.ID}" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // ─── Mapping ─────────────────────────────────────────────────────────────

        [HttpGet("tonsilo-mapping")]
        public async Task<IActionResult> GetMappingList(
            [FromQuery] int? idLoCao,
            [FromQuery] DateTime? ngay,
            [FromQuery] int? ca)
        {
            try
            {
                if (ca.HasValue && ca != 1 && ca != 2)
                    return BadRequest(new { message = "ca chỉ nhận giá trị 1 hoặc 2." });

                return Ok(await _service.GetMappingListAsync(idLoCao, ngay, ca));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("tonsilo-mapping/{id}")]
        public async Task<IActionResult> GetMappingById(int id)
        {
            try
            {
                var r = await _service.GetMappingByIdAsync(id);
                return r == null ? NotFound(new { message = $"Không tìm thấy Mapping ID={id}" }) : Ok(r);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPost("tonsilo-mapping")]
        public async Task<IActionResult> CreateMapping([FromBody] CreateLGTSMappingDto dto)
        {
            try
            {
                if (dto.Ca != 1 && dto.Ca != 2)
                    return BadRequest(new { message = "Ca chỉ nhận giá trị 1 hoặc 2." });

                var r = await _service.AddMappingAsync(dto);
                return CreatedAtAction(nameof(GetMappingById), new { id = r.ID }, r);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPut("tonsilo-mapping/{id}")]
        public async Task<IActionResult> UpdateMapping(int id, [FromBody] UpdateLGTSMappingDto dto)
        {
            try
            {
                if (dto.Ca != 1 && dto.Ca != 2)
                    return BadRequest(new { message = "Ca chỉ nhận giá trị 1 hoặc 2." });

                var r = await _service.UpdateMappingAsync(id, dto);
                return r == null ? NotFound(new { message = $"Không tìm thấy Mapping ID={id}" }) : Ok(r);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpDelete("tonsilo-mapping/{id}")]
        public async Task<IActionResult> DeleteMapping(int id)
        {
            try
            {
                var ok = await _service.DeleteMappingAsync(id);
                return ok ? Ok(new { message = "Đã xóa thành công." }) : NotFound(new { message = $"Không tìm thấy Mapping ID={id}" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}

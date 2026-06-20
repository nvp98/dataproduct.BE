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
        private readonly PheDuyetService _pdservice;

        public LGTSLController(LGTSLService service, PheDuyetService pdservice)
        {
            _service = service;
            _pdservice = pdservice;
        }

        // ─── SiLo + NVL view theo Ngày/Ca/LoCao (dùng trong tạo phiếu tồn silo) ──

        [HttpGet("get-tonsilo-silo-mapping")]
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
        [HttpPost("post-sync-chitiet/{idPhieu}")]
        public async Task<IActionResult> SyncChiTiet(Guid idPhieu)
        {
            try
            {
                var result = await _service.SyncChiTietFromScadaAsync(idPhieu);
                return Ok(result);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // ─── SiLo ────────────────────────────────────────────────────────────────

        [HttpGet("get-tonsilo-silo")]
        public async Task<IActionResult> GetSiLoList([FromQuery] int? idLoCao)
        {
            try { return Ok(await _service.GetAllSiLoListAsync(idLoCao)); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("get-tonsilo-silo/{id}")]
        public async Task<IActionResult> GetSiLoById(int id)
        {
            try
            {
                var r = await _service.GetSiLoByIdAsync(id);
                return r == null ? NotFound(new { message = $"Không tìm thấy Silo ID={id}" }) : Ok(r);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPost("post-tonsilo-silo")]
        public async Task<IActionResult> CreateSiLo([FromBody] CreateLGTSSiLoDto dto)
        {
            try
            {
                var r = await _service.AddSiLoAsync(dto);
                return CreatedAtAction(nameof(GetSiLoById), new { id = r.Id }, r);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPut("put-tonsilo-silo/{id}")]
        public async Task<IActionResult> UpdateSiLo(int id, [FromBody] UpdateLGTSSiLoDto dto)
        {
            try
            {
                var r = await _service.UpdateSiLoAsync(id, dto);
                return r == null ? NotFound(new { message = $"Không tìm thấy Silo ID={id}" }) : Ok(r);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpDelete("delete-tonsilo-silo/{id}")]
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

        [HttpGet("get-tonsilo-nvl")]
        public async Task<IActionResult> GetNvlList([FromQuery] int? idLoCao)
        {
            try { return Ok(await _service.GetNvlListAsync(idLoCao)); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("get-tonsilo-nvl/{id}")]
        public async Task<IActionResult> GetNvlById(int id)
        {
            try
            {
                var r = await _service.GetNvlByIdAsync(id);
                return r == null ? NotFound(new { message = $"Không tìm thấy NVL ID={id}" }) : Ok(r);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPost("post-tonsilo-nvl")]
        public async Task<IActionResult> CreateNvl([FromBody] CreateLGTSNvlDto dto)
        {
            try
            {
                var r = await _service.AddNvlAsync(dto);
                return CreatedAtAction(nameof(GetNvlById), new { id = r.Id }, r);
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

        [HttpPut("put-tonsilo-nvl/{id}")]
        public async Task<IActionResult> UpdateNvl(int id, [FromBody] UpdateLGTSNvlDto dto)
        {
            try
            {
                var r = await _service.UpdateNvlAsync(id, dto);
                return r == null ? NotFound(new { message = $"Không tìm thấy NVL ID={id}" }) : Ok(r);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpDelete("delete-tonsilo-nvl/{id}")]
        public async Task<IActionResult> DeleteNvl(int id)
        {
            try
            {
                var ok = await _service.DeleteNvlAsync(id);
                return ok ? Ok(new { message = "Đã xóa thành công." }) : NotFound(new { message = $"Không tìm thấy NVL ID={id}" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPut("put-tonsilo-nvl/xac-nhan")]
        public async Task<IActionResult> UpdateXacNhan([FromBody] UpdateLGTSXacNhanDto dto)
        {
            try
            {
                var ok = await _service.UpdateXacNhanAsync(dto);
                return ok
                    ? Ok(new { message = "Cập nhật xác nhận thành công." })
                    : NotFound(new { message = $"Không tìm thấy NVL ID={dto.Id}" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // ─── Mapping ─────────────────────────────────────────────────────────────

        [HttpGet("get-tonsilo-mapping")]
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

        [HttpGet("get-tonsilo-mapping/{id}")]
        public async Task<IActionResult> GetMappingById(int id)
        {
            try
            {
                var r = await _service.GetMappingByIdAsync(id);
                return r == null ? NotFound(new { message = $"Không tìm thấy Mapping ID={id}" }) : Ok(r);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPost("post-tonsilo-mapping")]
        public async Task<IActionResult> CreateMapping([FromBody] CreateLGTSMappingDto dto)
        {
            try
            {
                if (dto.Ca != 1 && dto.Ca != 2)
                    return BadRequest(new { message = "Ca chỉ nhận giá trị 1 hoặc 2." });

                var r = await _service.AddMappingAsync(dto);
                return CreatedAtAction(nameof(GetMappingById), new { id = r.Id }, r);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPut("put-tonsilo-mapping/{id}")]
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

        [HttpDelete("delete-tonsilo-mapping/{id}")]
        public async Task<IActionResult> DeleteMapping(int id)
        {
            try
            {
                var ok = await _service.DeleteMappingAsync(id);
                return ok ? Ok(new { message = "Đã xóa thành công." }) : NotFound(new { message = $"Không tìm thấy Mapping ID={id}" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // ─── Chi tiết tồn silo theo phiếu ────────────────────────────────────────

        /// <summary>
        /// Lưu chi tiết tồn silo theo phiếu (xóa cũ, insert mới theo IDPhieu).
        /// Gọi sau khi lưu phiếu thành công.
        /// </summary>
        [HttpPost("post-chitiet/upsert")]
        public async Task<IActionResult> UpsertChiTiet([FromBody] UpsertLGTSChiTietDto dto)
        {
            try
            {
                await _service.UpsertChiTietAsync(dto);
                return Ok(new { message = "Lưu chi tiết tồn silo thành công." });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>
        /// Lấy chi tiết tồn silo theo IDPhieu.
        /// </summary>
        [HttpGet("get-chitiet/{idPhieu}")]
        public async Task<IActionResult> GetChiTietByPhieu(Guid idPhieu)
        {
            try
            {
                return Ok(await _service.GetChiTietByPhieuAsync(idPhieu));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // ─── Export PDF ───────────────────────────────────────────────────────────

        [HttpGet("get-export-pdf/{idPhieu}")]
        public async Task<IActionResult> ExportPdf(Guid idPhieu, [FromQuery] bool useKeHoachName = false)
        {
            try
            {
                var pheDuyets = await _pdservice.GetPheDuyetPhieuAsync(idPhieu);
                var file = await _service.ExportTonSiloPdfAsync(idPhieu, pheDuyets, useKeHoachName);
                return File(file.Content, file.ContentType, file.FileName);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // ─── Export Excel ─────────────────────────────────────────────────────────

        [HttpGet("get-export-excel/{idPhieu}")]
        public async Task<IActionResult> ExportExcel(Guid idPhieu)
        {
            try
            {
                var file = await _service.ExportTonSiloExcelAsync(idPhieu);
                return File(file.Content, file.ContentType, file.FileName);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}

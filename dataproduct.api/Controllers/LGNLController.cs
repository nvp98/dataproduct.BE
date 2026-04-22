using dataproduct.api.DTOs.LGNL_Dto;
using dataproduct.api.Services;
using Microsoft.AspNetCore.Mvc;

namespace dataproduct.api.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class LGNLController : ControllerBase
    {
        private readonly LGNLService _service;
        public LGNLController(LGNLService service)
        {
            _service = service;
        }


        [HttpGet("ts-mapping")]
        public async Task<IActionResult> GetTsMapping()
        {
            try { return Ok(await _service.GetTsMappingListAsync()); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("silo-master")]
        public async Task<IActionResult> GetSiLoMasterList([FromQuery] int? idLoCao)
        {
            try { return Ok(await _service.GetSiLoMasterListAsync(idLoCao)); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("silo-master/{id}")]
        public async Task<IActionResult> GetSiLoMasterById(int id)
        {
            try
            {
                var r = await _service.GetSiLoMasterByIdAsync(id);
                return r == null ? NotFound(new { message = $"Không tìm thấy Silo ID={id}" }) : Ok(r);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPost("silo-master")]
        public async Task<IActionResult> CreateSiLoMaster([FromBody] CreateLGNLSiLoMasterDto dto)
        {
            try
            {
                var r = await _service.AddSiLoMasterAsync(dto);
                return CreatedAtAction(nameof(GetSiLoMasterById), new { id = r.ID }, r);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPut("silo-master/{id}")]
        public async Task<IActionResult> UpdateSiLoMaster(int id, [FromBody] UpdateLGNLSiLoMasterDto dto)
        {
            try
            {
                var r = await _service.UpdateSiLoMasterAsync(id, dto);
                return r == null ? NotFound(new { message = $"Không tìm thấy Silo ID={id}" }) : Ok(r);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpDelete("silo-master/{id}")]
        public async Task<IActionResult> DeleteSiLoMaster(int id)
        {
            try
            {
                var ok = await _service.DeleteSiLoMasterAsync(id);
                return ok ? Ok(new { message = "Đã xóa thành công." }) : NotFound(new { message = $"Không tìm thấy Silo ID={id}" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }


        [HttpGet("mapping")]
        public async Task<IActionResult> GetMappingList(
            [FromQuery] DateOnly? ngay,
            [FromQuery] int? idCa,
            [FromQuery] int? idLoCao)
        {
            try
            {
                if (idCa.HasValue && idCa != 1 && idCa != 2)
                    return BadRequest(new { message = "idCa chỉ nhận giá trị 1 hoặc 2." });
                return Ok(await _service.GetMappingListAsync(ngay, idCa, idLoCao));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("mapping/{id}")]
        public async Task<IActionResult> GetMappingById(int id)
        {
            try
            {
                var r = await _service.GetMappingByIdAsync(id);
                return r == null ? NotFound(new { message = $"Không tìm thấy Mapping ID={id}" }) : Ok(r);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPost("mapping")]
        public async Task<IActionResult> CreateMapping([FromBody] CreateLGNLMappingDto dto)
        {
            try
            {
                if (dto.IDCa != 1 && dto.IDCa != 2)
                    return BadRequest(new { message = "IDCa chỉ nhận giá trị 1 hoặc 2." });
                var r = await _service.AddMappingAsync(dto);
                return CreatedAtAction(nameof(GetMappingById), new { id = r.ID }, r);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPut("mapping/{id}")]
        public async Task<IActionResult> UpdateMapping(int id, [FromBody] UpdateLGNLMappingDto dto)
        {
            try
            {
                if (dto.IDCa != 1 && dto.IDCa != 2)
                    return BadRequest(new { message = "IDCa chỉ nhận giá trị 1 hoặc 2." });
                var r = await _service.UpdateMappingAsync(id, dto);
                return r == null ? NotFound(new { message = $"Không tìm thấy Mapping ID={id}" }) : Ok(r);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpDelete("mapping/{id}")]
        public async Task<IActionResult> DeleteMapping(int id)
        {
            try
            {
                var ok = await _service.DeleteMappingAsync(id);
                return ok ? Ok(new { message = "Đã xóa thành công." }) : NotFound(new { message = $"Không tìm thấy Mapping ID={id}" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }


        [HttpGet("nhom-nvl")]
        public async Task<IActionResult> GetNhomNvlList([FromQuery] int? idLoCao)
        {
            try { return Ok(await _service.GetNhomNvlListAsync(idLoCao)); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("nhom-nvl/{id}")]
        public async Task<IActionResult> GetNhomNvlById(int id)
        {
            try
            {
                var r = await _service.GetNhomNvlByIdAsync(id);
                return r == null ? NotFound(new { message = $"Không tìm thấy Nhóm NVL ID={id}" }) : Ok(r);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPost("nhom-nvl")]
        public async Task<IActionResult> CreateNhomNvl([FromBody] CreateLGNLNhomNvlDto dto)
        {
            try
            {
                var r = await _service.AddNhomNvlAsync(dto);
                return CreatedAtAction(nameof(GetNhomNvlById), new { id = r.ID }, r);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPut("nhom-nvl/{id}")]
        public async Task<IActionResult> UpdateNhomNvl(int id, [FromBody] UpdateLGNLNhomNvlDto dto)
        {
            try
            {
                var r = await _service.UpdateNhomNvlAsync(id, dto);
                return r == null ? NotFound(new { message = $"Không tìm thấy Nhóm NVL ID={id}" }) : Ok(r);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpDelete("nhom-nvl/{id}")]
        public async Task<IActionResult> DeleteNhomNvl(int id)
        {
            try
            {
                var ok = await _service.DeleteNhomNvlAsync(id);
                return ok ? Ok(new { message = "Đã xóa thành công." }) : NotFound(new { message = $"Không tìm thấy Nhóm NVL ID={id}" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("nvl")]
        public async Task<IActionResult> GetNvlList([FromQuery] int? idLoCao)
        {
            try
            {
                return Ok(await _service.GetNvlListAsync(idLoCao));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("nvl/{id}")]
        public async Task<IActionResult> GetNvlById(int id)
        {
            try
            {
                var r = await _service.GetNvlByIdAsync(id);
                return r == null ? NotFound(new { message = $"Không tìm thấy NVL ID={id}" }) : Ok(r);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPost("nvl")]
        public async Task<IActionResult> CreateNvl([FromBody] CreateLGNLNvlDto dto)
        {
            try
            {
                var r = await _service.AddNvlAsync(dto);
                return CreatedAtAction(nameof(GetNvlById), new { id = r.ID }, r);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPut("nvl/{id}")]
        public async Task<IActionResult> UpdateNvl(int id, [FromBody] UpdateLGNLNvlDto dto)
        {
            try
            {
                var r = await _service.UpdateNvlAsync(id, dto);
                return r == null ? NotFound(new { message = $"Không tìm thấy NVL ID={id}" }) : Ok(r);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpDelete("nvl/{id}")]
        public async Task<IActionResult> DeleteNvl(int id)
        {
            try
            {
                var ok = await _service.DeleteNvlAsync(id);
                return ok ? Ok(new { message = "Đã xóa thành công." }) : NotFound(new { message = $"Không tìm thấy NVL ID={id}" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // ─── Dữ liệu theo LoCao, Ngày ───────────────────────────────

        [HttpGet("datanaplieu-filter")]
        public async Task<IActionResult> GetDataNapLieuFilter(
            [FromQuery] int? idLoCao,
            [FromQuery] DateTime? ngayBatDau,
            [FromQuery] DateTime? ngayKetThuc)
        {
            try
            {
                var result = await _service.GetDataByFilterAsync(idLoCao, ngayBatDau, ngayKetThuc);
                return Ok(result);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        /// <summary>
        /// Pivot dữ liệu nạp liệu theo Silo mapping.
        /// Trả về { columns, rows } cho config-driven rendering ở CustomTableLG.
        /// </summary>
        [HttpGet("dulieu-silo")]
        public async Task<IActionResult> GetDuLieuSilo(
            [FromQuery] string ngay,
            [FromQuery] int idCa,
            [FromQuery] int idLoCao)
        {
            try
            {
                if (!DateOnly.TryParse(ngay, out var parsedNgay))
                    return BadRequest(new { message = "ngay không hợp lệ. Định dạng: yyyy-MM-dd" });
                if (idCa != 1 && idCa != 2)
                    return BadRequest(new { message = "idCa chỉ nhận giá trị 1 hoặc 2." });

                var result = await _service.GetDuLieuSiloPivotAsync(parsedNgay, idCa, idLoCao);
                return Ok(result);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}

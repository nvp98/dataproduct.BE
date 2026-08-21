using dataproduct.api.DTOs.NMLG_Dto;
using dataproduct.api.Services;
using Microsoft.AspNetCore.Mvc;

namespace dataproduct.api.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class LGNLController : ControllerBase
    {
        private readonly LGNLService _service;
        private readonly PheDuyetService _pdservice;

        public LGNLController(LGNLService service, PheDuyetService pdservice)
        {
            _service   = service;
            _pdservice = pdservice;
        }

        // ─── TS Mapping ───────────────────────────────────────────────────────────

        [HttpGet("get-ts-mapping")]
        public async Task<IActionResult> GetTsMapping()
        {
            try { return Ok(await _service.GetTsMappingListAsync()); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // ─── Silo Master ──────────────────────────────────────────────────────────

        [HttpGet("get-silo-master")]
        public async Task<IActionResult> GetSiLoMasterList([FromQuery] int? idLoCao)
        {
            try { return Ok(await _service.GetSiLoMasterListAsync(idLoCao)); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("get-silo-master/{id}")]
        public async Task<IActionResult> GetSiLoMasterById(int id)
        {
            try
            {
                var r = await _service.GetSiLoMasterByIdAsync(id);
                return r == null ? NotFound(new { message = $"Không tìm thấy Silo ID={id}" }) : Ok(r);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPost("create-silo-master")]
        public async Task<IActionResult> CreateSiLoMaster([FromBody] CreateLGNLSiLoMasterDto dto)
        {
            try
            {
                var r = await _service.AddSiLoMasterAsync(dto);
                return CreatedAtAction(nameof(GetSiLoMasterById), new { id = r.Id }, r);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPut("update-silo-master/{id}")]
        public async Task<IActionResult> UpdateSiLoMaster(int id, [FromBody] UpdateLGNLSiLoMasterDto dto)
        {
            try
            {
                var r = await _service.UpdateSiLoMasterAsync(id, dto);
                return r == null ? NotFound(new { message = $"Không tìm thấy Silo ID={id}" }) : Ok(r);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpDelete("delete-silo-master/{id}")]
        public async Task<IActionResult> DeleteSiLoMaster(int id)
        {
            try
            {
                var ok = await _service.DeleteSiLoMasterAsync(id);
                return ok ? Ok(new { message = "Đã xóa thành công." }) : NotFound(new { message = $"Không tìm thấy Silo ID={id}" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // ─── Mapping Silo ↔ NVL ───────────────────────────────────────────────────

        [HttpGet("get-mapping")]
        public async Task<IActionResult> GetMappingList(
            [FromQuery] DateTime? ngay,
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

        [HttpGet("get-mapping/{id}")]
        public async Task<IActionResult> GetMappingById(int id)
        {
            try
            {
                var r = await _service.GetMappingByIdAsync(id);
                return r == null ? NotFound(new { message = $"Không tìm thấy Mapping ID={id}" }) : Ok(r);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPost("create-mapping")]
        public async Task<IActionResult> CreateMapping([FromBody] CreateLGNLMappingDto dto)
        {
            try
            {
                if (dto.IdCa != 1 && dto.IdCa != 2)
                    return BadRequest(new { message = "IdCa chỉ nhận giá trị 1 hoặc 2." });
                var r = await _service.AddMappingAsync(dto);
                return CreatedAtAction(nameof(GetMappingById), new { id = r.Id }, r);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPut("update-mapping/{id}")]
        public async Task<IActionResult> UpdateMapping(int id, [FromBody] UpdateLGNLMappingDto dto)
        {
            try
            {
                if (dto.IdCa != 1 && dto.IdCa != 2)
                    return BadRequest(new { message = "IdCa chỉ nhận giá trị 1 hoặc 2." });
                var r = await _service.UpdateMappingAsync(id, dto);
                return r == null ? NotFound(new { message = $"Không tìm thấy Mapping ID={id}" }) : Ok(r);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpDelete("delete-mapping/{id}")]
        public async Task<IActionResult> DeleteMapping(int id)
        {
            try
            {
                var ok = await _service.DeleteMappingAsync(id);
                return ok ? Ok(new { message = "Đã xóa thành công." }) : NotFound(new { message = $"Không tìm thấy Mapping ID={id}" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPost("doi-nvl")]
        public async Task<IActionResult> DoiNVLGiuaCa([FromBody] LGNLChangeSiLoNVLDto dto)
        {
            try
            {
                if (dto.IdCa != 1 && dto.IdCa != 2)
                    return BadRequest(new { message = "IdCa chỉ nhận giá trị 1 hoặc 2." });
                if (dto.IdSiLo <= 0 || dto.IdNVLMoi <= 0)
                    return BadRequest(new { message = "IdSiLo và IdNVLMoi phải lớn hơn 0." });

                var result = await _service.ChangeSiLoNVLAsync(
                    dto.IdLoCao, dto.Ngay, dto.IdCa,
                    dto.IdSiLo, dto.IdNVLMoi, dto.ThoiDiem, dto.GhiChu);

                return Ok(new { message = "Đổi NVL thành công.", id = result.ID });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPost("undo-doi-nvl")]
        public async Task<IActionResult> UndoDoiNVLGiuaCa([FromBody] LGNLUndoChangeSiLoNVLDto dto)
        {
            try
            {
                if (dto.IdCa != 1 && dto.IdCa != 2)
                    return BadRequest(new { message = "IdCa chỉ nhận giá trị 1 hoặc 2." });
                if (dto.IdSiLo <= 0)
                    return BadRequest(new { message = "IdSiLo phải lớn hơn 0." });

                var deleted = await _service.UndoChangeSiLoNVLAsync(
                    dto.IdLoCao, dto.Ngay, dto.IdCa, dto.IdSiLo);

                return Ok(new { message = $"Đã hoàn tác {deleted} lần đổi NVL.", deleted });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // ─── Nhóm NVL ─────────────────────────────────────────────────────────────

        [HttpGet("get-nhom-nvl")]
        public async Task<IActionResult> GetNhomNvlList()
        {
            try { return Ok(await _service.GetNhomNvlListAsync(null)); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("get-nhom-nvl/{id}")]
        public async Task<IActionResult> GetNhomNvlById(int id)
        {
            try
            {
                var r = await _service.GetNhomNvlByIdAsync(id);
                return r == null ? NotFound(new { message = $"Không tìm thấy Nhóm NVL ID={id}" }) : Ok(r);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPost("create-nhom-nvl")]
        public async Task<IActionResult> CreateNhomNvl([FromBody] CreateLGNLNhomNvlDto dto)
        {
            try
            {
                var r = await _service.AddNhomNvlAsync(dto);
                return CreatedAtAction(nameof(GetNhomNvlById), new { id = r.Id }, r);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPut("update-nhom-nvl/{id}")]
        public async Task<IActionResult> UpdateNhomNvl(int id, [FromBody] UpdateLGNLNhomNvlDto dto)
        {
            try
            {
                var r = await _service.UpdateNhomNvlAsync(id, dto);
                return r == null ? NotFound(new { message = $"Không tìm thấy Nhóm NVL ID={id}" }) : Ok(r);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpDelete("delete-nhom-nvl/{id}")]
        public async Task<IActionResult> DeleteNhomNvl(int id)
        {
            try
            {
                var ok = await _service.DeleteNhomNvlAsync(id);
                return ok ? Ok(new { message = "Đã xóa thành công." }) : NotFound(new { message = $"Không tìm thấy Nhóm NVL ID={id}" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // ─── NVL ──────────────────────────────────────────────────────────────────

        [HttpGet("get-nvl")]
        public async Task<IActionResult> GetNvlList([FromQuery] int? idLoCao)
        {
            try { return Ok(await _service.GetNvlListAsync(idLoCao)); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("get-nvl/{id}")]
        public async Task<IActionResult> GetNvlById(int id)
        {
            try
            {
                var r = await _service.GetNvlByIdAsync(id);
                return r == null ? NotFound(new { message = $"Không tìm thấy NVL ID={id}" }) : Ok(r);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPost("create-nvl")]
        public async Task<IActionResult> CreateNvl([FromBody] CreateLGNLNvlDto dto)
        {
            try
            {
                var r = await _service.AddNvlAsync(dto);
                return CreatedAtAction(nameof(GetNvlById), new { id = r.Id }, r);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPut("update-nvl/{id}")]
        public async Task<IActionResult> UpdateNvl(int id, [FromBody] UpdateLGNLNvlDto dto)
        {
            try
            {
                var r = await _service.UpdateNvlAsync(id, dto);
                return r == null ? NotFound(new { message = $"Không tìm thấy NVL ID={id}" }) : Ok(r);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpDelete("delete-nvl/{id}")]
        public async Task<IActionResult> DeleteNvl(int id)
        {
            try
            {
                var ok = await _service.DeleteNvlAsync(id);
                return ok ? Ok(new { message = "Đã xóa thành công." }) : NotFound(new { message = $"Không tìm thấy NVL ID={id}" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPut("update-nvl-xac-nhan")]
        public async Task<IActionResult> UpdateXacNhan([FromBody] UpdateXacNhanDto dto)
        {
            try
            {
                var ok = await _service.UpdateXacNhanAsync(dto);
                return ok ? Ok(new { message = "Cập nhật xác nhận thành công." }) : NotFound(new { message = $"Không tìm thấy NVL ID={dto.Id}" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // ─── Dữ liệu nạp liệu ────────────────────────────────────────────────────

        [HttpGet("get-datanaplieu-filter")]
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

        [HttpGet("get-dulieu-silo")]
        public async Task<IActionResult> GetDuLieuSilo(
            [FromQuery] DateTime ngay,
            [FromQuery] int idCa,
            [FromQuery] int idLoCao)
        {
            try
            {
                if (idCa != 1 && idCa != 2)
                    return BadRequest(new { message = "idCa chỉ nhận giá trị 1 hoặc 2." });

                var result = await _service.GetDuLieuSiloPivotAsync(ngay, idCa, idLoCao);
                return Ok(result);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPost("sync-chitiet/{idPhieu}")]
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

        [HttpPost("copy-mapping-from-previous-shift")]
        public async Task<IActionResult> CopyMappingFromPreviousShift([FromBody] CopyMappingFromPreviousShiftDto dto)
        {
            try
            {
                if (dto.IdCa != 1 && dto.IdCa != 2)
                    return BadRequest(new { message = "IdCa chỉ nhận giá trị 1 hoặc 2." });

                var result = await _service.CopyMappingFromPreviousShiftAsync(dto.IdLoCao, dto.Ngay, dto.IdCa);
                return Ok(result);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("get-snapshot-silo")]
        public async Task<IActionResult> GetSnapshotSilo(
            [FromQuery] DateTime ngay,
            [FromQuery] int idCa,
            [FromQuery] int idLoCao)
        {
            try
            {
                if (idCa != 1 && idCa != 2)
                    return BadRequest(new { message = "idCa chỉ nhận giá trị 1 hoặc 2." });
                var result = await _service.GetSiloSnapshotAsync(idLoCao, ngay, idCa);
                return Ok(result);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("get-chitiet/{idPhieu}")]
        public async Task<IActionResult> GetChiTietByPhieu(Guid idPhieu)
        {
            try { return Ok(await _service.GetChiTietByPhieuAsync(idPhieu)); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("get-export-pdf/{idPhieu}")]
        public async Task<IActionResult> ExportPdf(Guid idPhieu, [FromQuery] bool useKeHoachName = false)
        {
            try
            {
                var pheDuyets = await _pdservice.GetPheDuyetPhieuAsync(idPhieu);
                var file = await _service.ExportNapLieuPdfAsync(idPhieu, pheDuyets, useKeHoachName);
                return File(file.Content, file.ContentType, file.FileName);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("get-export-excel/{idPhieu}")]
        public async Task<IActionResult> ExportExcel(Guid idPhieu)
        {
            try
            {
                var file = await _service.ExportNapLieuExcelAsync(idPhieu);
                return File(file.Content, file.ContentType, file.FileName);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}

using dataproduct.api.DTOs.NMLG_Dto;
using dataproduct.api.Services;
using Microsoft.AspNetCore.Mvc;

namespace dataproduct.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NhomPhanBoController : ControllerBase
    {
        private readonly NhomPhanBoService _service;

        public NhomPhanBoController(NhomPhanBoService service)
        {
            _service = service;
        }

        [HttpGet("get-list")]
        public async Task<IActionResult> GetList([FromQuery] byte? loaiPhanBo)
        {
            try { return Ok(await _service.GetListAsync(loaiPhanBo)); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateNhomPhanBoDto dto)
        {
            try { return Ok(await _service.CreateAsync(dto)); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPut("update/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateNhomPhanBoDto dto)
        {
            try { return Ok(await _service.UpdateAsync(id, dto)); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var ok = await _service.DeleteAsync(id);
                return ok ? Ok(new { message = "Đã xóa thành công." }) : NotFound(new { message = $"Không tìm thấy nhóm phân bổ ID={id}" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("{id}/get-nvl")]
        public async Task<IActionResult> GetNvl(int id, [FromQuery] DateTime ngay, [FromQuery] byte ca)
        {
            try { return Ok(await _service.GetNvlByNhomAsync(id, ngay, ca)); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPost("{id}/add-nvl")]
        public async Task<IActionResult> AddNvl(int id, [FromBody] AddNvlNhomPhanBoDto dto)
        {
            try { return Ok(await _service.AddNvlAsync(id, dto)); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpDelete("{id}/remove-nvl/{idNvl}")]
        public async Task<IActionResult> RemoveNvl(int id, int idNvl, [FromQuery] DateTime ngay, [FromQuery] byte ca)
        {
            try
            {
                var ok = await _service.RemoveNvlAsync(id, idNvl, ngay, ca);
                return ok ? Ok(new { message = "Đã xóa thành công." }) : NotFound(new { message = "Không tìm thấy NVL trong nhóm." });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}

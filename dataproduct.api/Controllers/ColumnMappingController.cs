using dataproduct.api.DTOs.NMLG_Dto;
using dataproduct.api.Models;
using dataproduct.api.Services;
using Microsoft.AspNetCore.Mvc;

namespace dataproduct.api.Controllers
{
    [ApiController]
    [Route("api/column-mapping")]
    public class ColumnMappingController : ControllerBase
    {
        private readonly ColumnMappingService _service;

        public ColumnMappingController(ColumnMappingService service)
        {
            _service = service;
        }

        // ── Nhom (cột cha) ────────────────────────────────────

        // GET: api/column-mapping/nhom?loCao=5
        [HttpGet("nhom")]
        public async Task<IActionResult> GetAllNhom([FromQuery] int? loCao)
        {
            var data = await _service.GetAllNhom(loCao);
            return Ok(data);
        }

        // GET: api/column-mapping/nhom/5
        [HttpGet("nhom/{id}")]
        public async Task<IActionResult> GetNhomById(int id)
        {
            var data = await _service.GetNhomById(id);
            if (data == null) return NotFound();
            return Ok(data);
        }

        // POST: api/column-mapping/nhom
        [HttpPost("nhom")]
        public async Task<IActionResult> CreateNhom([FromBody] ColumnMappingNhomCreateDto dto)
        {
            var result = await _service.CreateNhom(dto);
            return Ok(result);
        }

        // PUT: api/column-mapping/nhom
        [HttpPut("nhom")]
        public async Task<IActionResult> UpdateNhom([FromBody] ColumnMappingNhomUpdateDto dto)
        {
            try
            {
                var result = await _service.UpdateNhom(dto);
                if (result == null) return NotFound();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // PATCH: api/column-mapping/nhom/5/toggle-visible
        [HttpPatch("nhom/{id}/toggle-visible")]
        public async Task<IActionResult> ToggleVisibleNhom(int id)
        {
            var result = await _service.ToggleVisibleNhom(id);
            if (result == null) return NotFound();
            return Ok(new { result.Id, result.IsVisible });
        }

        // DELETE: api/column-mapping/nhom/5
        [HttpDelete("nhom/{id}")]
        public async Task<IActionResult> DeleteNhom(int id)
        {
            var ok = await _service.DeleteNhom(id);
            if (!ok) return NotFound();
            return Ok("Deleted");
        }

        // ── Column (cột con) ──────────────────────────────────

        // GET: api/column-mapping?loCao=5
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? loCao)
        {
            var data = await _service.GetAll(loCao);
            return Ok(data);
        }

        // GET: api/column-mapping/columns?loCao=5
        [HttpGet("columns")]
        public async Task<IActionResult> GetColumns([FromQuery] int loCao)
        {
            var data = await _service.GetColumns(loCao);
            return Ok(data);
        }

        // GET: api/column-mapping/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var data = await _service.GetById(id);
            if (data == null) return NotFound();
            return Ok(data);
        }

        // POST: api/column-mapping
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ColumnMappingCreateDto dto)
        {
            try
            {
                var result = await _service.Create(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // PUT: api/column-mapping
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] ColumnMappingUpdateDto dto)
        {
            try
            {
                var result = await _service.Update(dto);
                if (result == null) return NotFound();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // PATCH: api/column-mapping/5/toggle-visible
        [HttpPatch("{id}/toggle-visible")]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var entity = await _service.GetById(id);
            if (entity == null) return NotFound();

            entity.IsVisible = !entity.IsVisible;

            await _service.SaveChanges();

            return Ok(new { entity.Id, entity.IsVisible });
        }

        // DELETE: api/column-mapping/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _service.Delete(id);
            if (!ok) return NotFound();
            return Ok("Deleted");
        }
    }
}

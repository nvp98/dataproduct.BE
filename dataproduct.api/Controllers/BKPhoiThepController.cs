using dataproduct.api.Models;
using dataproduct.api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using dataproduct.api.DTOs;

namespace dataproduct.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BKPhoiThepController : ControllerBase
    {
        private readonly BKPhoiThepService _service;

        public BKPhoiThepController(BKPhoiThepService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(DateOnly? NgaySX, int? Ca, string? Kip, int? LoaiPhoi, int? MayDuc) => Ok(await _service.GetAllAsync(NgaySX, Ca, Kip, LoaiPhoi, MayDuc));

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] BkPhoiThep model)
        {
            var created = await _service.CreateAsync(model);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] BkPhoiThep model)
        {
            var ok = await _service.UpdateAsync(id, model);
            return ok ? NoContent() : NotFound();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _service.DeleteAsync(id);
            return ok ? NoContent() : NotFound();
        }

        [HttpPut("{id}/st-dachuyen")]
        public async Task<IActionResult> UpdateStDaChuyen(int id, [FromBody] UpdateStDaChuyenRequest request)
        {
            if (request == null) return BadRequest();
            var ok = await _service.UpdateStDaChuyenAsync(id, request.ST_DaChuyen);
            return ok ? NoContent() : NotFound();
        }

        [HttpPut("st-dachuyen-bulk")]
        public async Task<IActionResult> UpdateStDaChuyenBulk([FromBody] List<BKPhoiThepStUpdate> items)
        {
            if (items == null || items.Count == 0) return BadRequest();
            var count = await _service.UpdateStDaChuyenRangeAsync(items);
            return Ok(new { updated = count });
        }

        [HttpPut("{id}/st-thuhoi")]
        public async Task<IActionResult> RevokeStDaChuyen(int id, [FromBody] RevokeStRequest request)
        {
            if (request == null) return BadRequest();
            var newVal = await _service.RevokeStDaChuyenAsync(id, request.SoThuHoi);
            return newVal.HasValue ? Ok(new { newStDaChuyen = newVal.Value }) : NotFound();
        }

        [HttpPut("st-thuhoi-bulk")]
        public async Task<IActionResult> RevokeStDaChuyenBulk([FromBody] List<BKPhoiThepStRevoke> items)
        {
            if (items == null || items.Count == 0) return BadRequest();
            var count = await _service.RevokeStDaChuyenRangeAsync(items);
            return Ok(new { updated = count });
        }
    }

    public class UpdateStDaChuyenRequest
    {
        public int ST_DaChuyen { get; set; }
    }

    public class RevokeStRequest
    {
        public int SoThuHoi { get; set; }
    }
}

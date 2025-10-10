using dataproduct.api.Business;
using dataproduct.api.DTOs;
using dataproduct.api.Models;
using Microsoft.AspNetCore.Mvc;

namespace dataproduct.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PhieusController : ControllerBase
    {
        private readonly PhieuService _service;

        public PhieusController(PhieuService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PhieuDto>>> GetAll()
            => Ok(await _service.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<ActionResult<PhieuDto>> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<BmPhieu>> Create([FromBody] BmPhieu model)
        {
            var created = await _service.CreateAsync(model);
            return CreatedAtAction(nameof(GetById), new { id = created.Idphieu }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] BmPhieu model)
        {
            var ok = await _service.UpdateAsync(id, model);
            return ok ? NoContent() : NotFound();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var ok = await _service.DeleteAsync(id);
            return ok ? NoContent() : NotFound();
        }
    }
}

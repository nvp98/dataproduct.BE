using dataproduct.api.Services;
using Microsoft.AspNetCore.Mvc;

namespace dataproduct.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Hrc1PhuLieuNmController : ControllerBase
    {
        private readonly Hrc1PhuLieuNmService _service;

        public Hrc1PhuLieuNmController(Hrc1PhuLieuNmService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(bool? dangSuDung, string? searchKey)
            => Ok(await _service.GetAllAsync(dangSuDung, searchKey));

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Hrc1PhuLieuNmUpsertDto dto)
        {
            try
            {
                var created = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.ID }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Hrc1PhuLieuNmUpsertDto dto)
        {
            try
            {
                var ok = await _service.UpdateAsync(id, dto);
                return ok ? NoContent() : NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPatch("{id}/xac-nhan")]
        public async Task<IActionResult> ToggleDangSuDung(int id)
        {
            var newValue = await _service.ToggleDangSuDungAsync(id);
            if (newValue == null) return NotFound();
            return Ok(new { id, dangSuDung = newValue });
        }
    }
}

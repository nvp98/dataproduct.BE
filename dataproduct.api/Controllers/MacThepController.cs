using dataproduct.api.Models;
using dataproduct.api.Services;
using Microsoft.AspNetCore.Mvc;

namespace dataproduct.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MacThepController : ControllerBase
    {
        private readonly MacThepService _service;

        public MacThepController(MacThepService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(byte? nhaMay, bool? isLock, string? tenMacThep)
            => Ok(await _service.GetAllAsync(nhaMay, isLock, tenMacThep));

        /// <summary>
        /// API autocomplete: tìm theo tên mác thép + nhà máy.
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> Search(string? searchKey, byte? nhaMay, bool? isLock, int page = 1, int pageSize = 30)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 30;
            if (pageSize > 200) pageSize = 200;

            var rows = await _service.GetAllAsync(nhaMay, isLock, searchKey);
            var ordered = rows.OrderBy(x => x.TenMacThep).ToList();

            var totalCount = ordered.Count;
            var data = ordered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new { x.Id, x.TenMacThep, x.NhaMay, x.IsLock });

            return Ok(new
            {
                data,
                totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] MacThep model)
        {
            try
            {
                var created = await _service.CreateAsync(model);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] MacThep model)
        {
            try
            {
                var ok = await _service.UpdateAsync(id, model);
                return ok ? NoContent() : NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _service.DeleteAsync(id);
            return ok ? NoContent() : NotFound();
        }
    }
}


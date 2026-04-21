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
        public async Task<IActionResult> GetAll(byte? nhaMay, bool? isLock, string? tenMacThep, int? idMayDuc)
            => Ok(await _service.GetAllAsync(nhaMay, isLock, tenMacThep, idMayDuc));

        /// <summary>
        /// API autocomplete: tìm theo tên mác thép + nhà máy + máy đúc + ca/kíp.
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> Search(
            string? searchKey,
            byte? nhaMay,
            bool? isLock,
            int? idMayDuc,
            int? ca,
            string? kip,
            string? maBm,
            int page = 1,
            int pageSize = 30)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 30;
            if (pageSize > 200) pageSize = 200;

            var rows = await _service.SearchWithMayDucAsync(nhaMay, isLock, searchKey, idMayDuc, ca, kip, maBm);

            var totalCount = rows.Count;
            var data = rows
                .Skip((page - 1) * pageSize)
                .Take(pageSize);

            return Ok(new
            {
                data,
                totalRecords = totalCount,
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

        [HttpPatch("{id}/xac-nhan")]
        public async Task<IActionResult> ToggleXacNhan(int id)
        {
            var newValue = await _service.ToggleXacNhanAsync(id);
            if (newValue == null) return NotFound();
            return Ok(new { id, isXacNhan = newValue });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _service.DeleteAsync(id);
            return ok ? NoContent() : NotFound();
        }
    }
}


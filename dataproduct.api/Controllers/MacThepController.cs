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
        public async Task<IActionResult> GetAll(byte? nhaMay, bool? isLock, string? tenMacThep, [FromQuery] List<int>? idMayDucs)
            => Ok(await _service.GetAllAsync(nhaMay, isLock, tenMacThep, idMayDucs));

        /// <summary>
        /// API autocomplete: tìm theo tên mác thép + nhà máy + máy đúc + ca/kíp.
        /// </summary>
        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] MacThepSearchRequestDto dto)
        {
            var page     = dto.Page < 1 ? 1 : dto.Page;
            var pageSize = dto.PageSize < 1 ? 30 : dto.PageSize > 200 ? 200 : dto.PageSize;

            var rows = await _service.SearchWithMayDucAsync(dto.NhaMay, dto.IsLock, dto.SearchKey, dto.IdMayDucs, dto.Ca, dto.Kip, dto.MaBm);

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
        public async Task<IActionResult> Create([FromBody] MacThepUpsertDto dto)
        {
            try
            {
                var created = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] MacThepUpsertDto dto)
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

        [HttpGet("{id}/may-duc")]
        public async Task<IActionResult> GetMayDucs(int id)
        {
            var result = await _service.GetMayDucsAsync(id);
            return Ok(result);
        }

        [HttpPut("{id}/may-duc")]
        public async Task<IActionResult> SetMayDucs(int id, [FromBody] List<int> idMayDucs)
        {
            try
            {
                var ok = await _service.SetMayDucsAsync(id, idMayDucs);
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


using dataproduct.api.DTOs;
using dataproduct.api.Models;
using dataproduct.api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace dataproduct.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DLNMHRC2Controller : ControllerBase
    {
        private readonly DLNMHRC2Service _service;

        public DLNMHRC2Controller(DLNMHRC2Service service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(DateTime? NgaySX, int? Ca, string? LoaiBM, int? KhuVuc) => Ok(await _service.GetAllAsync(NgaySX,Ca,LoaiBM,KhuVuc));

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpGet("report/{reportNo}")]
        public async Task<IActionResult> GetByReportNo(int reportNo)
        {
            var result = await _service.GetByReportNoAsync(reportNo);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DLNM_HRC2 model)
        {
            var created = await _service.CreateAsync(model);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] DLNM_HRC2 model)
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

        [HttpGet("search")]
        public async Task<IActionResult> Search(DateTime? NgaySX, int? Ca, string? LoaiBM, int? Scope, int page = 1, int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100; // Giới hạn tối đa 100 records mỗi trang

            var result = await _service.SearchWithPagingAsync(
                NgaySX,
                Ca,
                LoaiBM,
                Scope,
                page,
                pageSize
            );

            return Ok(result);
        }
    }
}

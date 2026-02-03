using dataproduct.api.Models;
using dataproduct.api.Services;
using Microsoft.AspNetCore.Mvc;
using dataproduct.api.DTOs;

namespace dataproduct.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CtdPhoiNongController : ControllerBase
    {
        private readonly CtdPhoiNongService _service;

        public CtdPhoiNongController(CtdPhoiNongService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] DateOnly? NgaySX, [FromQuery] int? Ca, [FromQuery] string? Kip, [FromQuery] int? Xuong, [FromQuery] string? Me)
            => Ok(await _service.GetAllAsync(NgaySX, Ca, Kip, Xuong, Me));

        [HttpGet("by-phieu/{phieuId}")]
        public async Task<IActionResult> GetByPhieu(Guid phieuId)
            => Ok(await _service.GetByPhieuIdAsync(phieuId));

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CtdPhoiNong model)
        {
            var created = await _service.CreateAsync(model);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPost("bulk")]
        public async Task<IActionResult> CreateBulk([FromBody] List<CtdPhoiNong> models)
        {
            if (models == null || models.Count == 0) return BadRequest();
            var (created, updated) = await _service.UpsertListAsync(models);
            return Ok(new { created, updated });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CtdPhoiNong model)
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

        [HttpPut("update-status")]
        public async Task<IActionResult> UpdateStatus([FromBody] List<CtdPhoiNongStatusUpdate> items)
        {
            if (items == null || items.Count == 0) return BadRequest();
            var count = await _service.UpdateStatusesAsync(items);
            return Ok(new { updated = count });
        }

        [HttpPut("update-chot")]
        public async Task<IActionResult> UpdateStatusDone([FromQuery] DateOnly? NgaySX, [FromQuery] int? Ca, [FromQuery] string? Kip, [FromQuery] int? Xuong, [FromQuery] string? Me, [FromQuery] int? status)
        {
            return Ok(await _service.UpdateStatusDone(NgaySX, Ca, Kip, Xuong, Me, status));
        }

        [HttpGet("export-excel")]
        public async Task<IActionResult> ExportExcel(
      [FromQuery] DateOnly? NgaySX, [FromQuery] int? Ca, [FromQuery] string? Kip, [FromQuery] int? Xuong, [FromQuery] string? Me)
        {
            var result = await _service.ExportExcelAsync(
                NgaySX, Ca, Kip, Xuong, Me
            );

            return File(
                result.Content,
                result.ContentType,
                result.FileName
            );
        }

        [HttpGet("export-pdf")]
        public async Task<IActionResult> ExportPdf(DateOnly? NgaySX, int? Ca, string? Kip, int? Xuong, string? Me, Guid id)
        {
            var file = await _service.ExportPdfAsync(NgaySX, Ca, Kip, Xuong, Me, id);

            return File(file.Content, file.ContentType, file.FileName);
        }

        [HttpGet("export-excel-tonghop")]
        public async Task<IActionResult> ExportExcelPKH(
     [FromQuery] DateOnly? NgaySX, [FromQuery] int? Ca, [FromQuery] string? Kip, [FromQuery] int? Xuong, [FromQuery] string? Me)
        {
            var result = await _service.PKH_ExportExcelAsync(
                NgaySX, Ca, Kip, Xuong, Me
            );

            return File(
                result.Content,
                result.ContentType,
                result.FileName
            );
        }

    }
}
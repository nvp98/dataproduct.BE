using dataproduct.api.DTOs;
using dataproduct.api.Services;
using Microsoft.AspNetCore.Mvc;

namespace dataproduct.api.Controllers
{
    [Route("api/hrc1-ma-vat-tu")]
    [ApiController]
    public class MaVatTuController : ControllerBase
    {
        private readonly MaVatTuService _svc;

        public MaVatTuController(MaVatTuService svc)
        {
            _svc = svc;
        }

        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] MaVatTuSearchRequest req)
        {
            var (data, total) = await _svc.SearchAsync(req);
            return Ok(new { data, totalCount = total, page = req.Page, pageSize = req.PageSize });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _svc.GetByIdAsync(id);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost("bulk")]
        public async Task<IActionResult> BulkCreate([FromBody] MaVatTuBulkCreateRequest req)
        {
            if (req.Items == null || req.Items.Count == 0)
                return BadRequest(new { message = "Danh sách rỗng." });

            var result = await _svc.BulkCreateAsync(req.Items);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] MaVatTuUpsertDto dto)
        {
            try
            {
                var created = await _svc.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] MaVatTuUpsertDto dto)
        {
            try
            {
                var ok = await _svc.UpdateAsync(id, dto);
                return ok ? NoContent() : NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _svc.DeleteAsync(id);
            return ok ? NoContent() : NotFound();
        }
    }
}

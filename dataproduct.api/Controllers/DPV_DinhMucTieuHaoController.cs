using dataproduct.api.Services;
using Microsoft.AspNetCore.Mvc;

namespace dataproduct.api.Controllers
{
    /// <summary>Quản trị Định mức tiêu hao sản xuất — Xưởng Đúc phôi vuông NM.HRC1.</summary>
    [Route("api/[controller]")]
    [ApiController]
    public class DinhMucTieuHaoController : ControllerBase
    {
        private readonly DinhMucTieuHaoService _service;

        public DinhMucTieuHaoController(DinhMucTieuHaoService service)
        {
            _service = service;
        }

        private string? NguoiThaoTac => User.FindFirst("username")?.Value;

        [HttpGet("search")]
        public async Task<IActionResult> Search(int? idPB, int? idXuong, string? baoCao, string? chiTieu, DateOnly? ngayApDung, int page = 1, int pageSize = 50)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;
            if (pageSize > 500) pageSize = 500;

            var rows = await _service.SearchAsync(idPB, idXuong, baoCao, chiTieu, ngayApDung);
            var total = rows.Count;
            return Ok(new
            {
                data = rows.Skip((page - 1) * pageSize).Take(pageSize),
                totalRecords = total,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling((double)total / pageSize),
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DinhMucTieuHaoPayload payload)
        {
            try
            {
                var created = await _service.CreateAsync(payload, NguoiThaoTac);
                return Ok(created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] DinhMucTieuHaoPayload payload)
        {
            try
            {
                var ok = await _service.UpdateAsync(id, payload, NguoiThaoTac);
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
            var ok = await _service.DeleteAsync(id, NguoiThaoTac);
            return ok ? NoContent() : NotFound();
        }
    }
}

using dataproduct.api.DTOs;
using dataproduct.api.Models;
using dataproduct.api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace dataproduct.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HeaderKeyController : ControllerBase
    {
        private readonly HeaderKeyService _service;

        public HeaderKeyController(HeaderKeyService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Header_Key model)
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
        public async Task<IActionResult> Update(int id, [FromBody] Header_Key model)
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
            try
            {
                var ok = await _service.DeleteAsync(id);
                return ok ? NoContent() : NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search(
            string? searchKey,
            string? LoaiPhieu,
            string? TrangThai,
            bool? IsUsedNXT,
            bool? IsUsedThongKe,
            DateTime? FromDate,
            DateTime? ToDate,
            string? SortThuTu,
            int? IdNhom,
            int page = 1,
            int pageSize = 10
        )
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var result = await _service.SearchMappingsWithPagingAsync(
                searchKey,
                LoaiPhieu,
                TrangThai,
                IsUsedNXT,
                IsUsedThongKe,
                FromDate,
                ToDate,
                SortThuTu,
                IdNhom,
                page,
                pageSize
            );

            return Ok(result);
        }

        [HttpGet("search-autocomplete")]
        public async Task<IActionResult> SearchMappingsWithPagingAutocomplete(string? searchKey, string? LoaiPhieu, int page = 1, int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100; 

            // Trả về theo Header_Mappings (1-1), mỗi dòng là 1 mapping (mỗi ID_PhuLieu trả về 1 dòng với 1 HeaderKey)
            var result = await _service.SearchWithPagingAsync(
                searchKey,
                LoaiPhieu,
                page,
                pageSize
            );

            return Ok(result);
        }
    }
}

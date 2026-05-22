using dataproduct.api.DTOs;
using dataproduct.api.Models;
using dataproduct.api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace dataproduct.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SiloController : ControllerBase
    {
        private readonly SiloService _service;

        public SiloController(SiloService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

        // Phải đặt các route cụ thể trước route {id} để tránh conflict
        [HttpGet("phu-lieu-nm")]
        public async Task<IActionResult> GetAllPhuLieuNM(
            string? searchKey,
            int page = 1,
            int pageSize = 50
        )
        {
            try
            {
                // Nếu không có searchKey và pageSize lớn, trả về tất cả (backward compatible)
                if (string.IsNullOrEmpty(searchKey) && pageSize >= 1000)
                {
                    var allResult = await _service.GetAllPhuLieuNMAsync();
                    return Ok(allResult);
                }

                // Có search hoặc phân trang
                var result = await _service.SearchPhuLieuNMWithPagingAsync(
                    searchKey,
                    page,
                    pageSize
                );
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search(
            string? searchKey,
            int? scope,
            bool? tinhTrang,
            string? BieuMau,
            int? nhaMay,
            int page = 1,
            int pageSize = 10
        )
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100; // Giới hạn tối đa 100 records mỗi trang

            // Trả về danh sách Silo với phân trang
            var result = await _service.SearchMappingsWithPagingAsync(
                searchKey,
                scope,
                tinhTrang,
                BieuMau,
                nhaMay,
                page,
                pageSize
            );

            return Ok(result);
        }

        // MapSiloPhuLieuNM endpoints - phải đặt trước route {id}
        [HttpGet("mappings/silo/{siloId}")]
        public async Task<IActionResult> GetMappingsBySiloId(int siloId)
        {
            try
            {
                var result = await _service.GetMappingsBySiloIdAsync(siloId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Silo model)
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
        public async Task<IActionResult> Update(int id, [FromBody] Silo model)
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


        [HttpPost("mappings")]
        public async Task<IActionResult> CreateMapping([FromBody] MapSiloPhuLieuNMPayload payload)
        {
            try
            {
                var result = await _service.CreateMappingAsync(payload);
                return CreatedAtAction(
                    nameof(GetMappingsBySiloId), 
                    new { siloId = payload.SiloId }, 
                    result
                );
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPut("mappings/{id}")]
        public async Task<IActionResult> UpdateMapping(int id, [FromBody] MapSiloPhuLieuNMPayload payload)
        {
            try
            {
                var ok = await _service.UpdateMappingAsync(id, payload);
                return ok ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpDelete("mappings/{id}")]
        public async Task<IActionResult> DeleteMapping(int id)
        {
            try
            {
                var ok = await _service.DeleteMappingAsync(id);
                return ok ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // Resolve Silo và PhuLieuNM endpoints
        [HttpPost("resolve-silo")]
        public async Task<IActionResult> ResolveSilo([FromBody] ResolveSiloRequest request)
        {
            try
            {
                var result = await _service.ResolveSiloAsync(request.PhuLieuNMIds, request.NgaySX);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("resolve-phu-lieu")]
        public async Task<IActionResult> ResolvePhuLieuWhenChangeSilo([FromBody] ResolvePhuLieuWhenChangeSiloRequest request)
        {
            try
            {
                var result = await _service.ResolvePhuLieuWhenChangeSiloAsync(
                    request.SiloId,
                    request.PhuLieuNMIds,
                    request.NgaySX
                );
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("validate-before-save")]
        public async Task<IActionResult> ValidateBeforeSave([FromBody] ValidateBeforeSaveRequest request)
        {
            try
            {
                await _service.ValidateBeforeSaveAsync(
                    request.SiloId,
                    request.PhuLieuNMId,
                    request.NgaySX
                );
                return Ok(new { isValid = true });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message, isValid = false });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, isValid = false });
            }
        }

        [HttpPost("get-by-headerkey")]
        public async Task<IActionResult> GetSilosByHeaderKey([FromBody] GetSiloByHeaderKeyRequest request)
        {
            try
            {
                var result = await _service.GetSilosByHeaderKeyAsync(
                    request.HeaderKeyId, 
                    request.NgaySX,
                    request.NhaMay,
                    request.BieuMau
                );
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

    }
}

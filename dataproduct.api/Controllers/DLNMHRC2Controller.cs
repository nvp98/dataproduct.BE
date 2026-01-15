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
        private readonly HRC2_NMSyncService _hrc2NMSyncService;
        public DLNMHRC2Controller(DLNMHRC2Service service, HRC2_NMSyncService hrc2NMSyncService)
        {
            _service = service;
            _hrc2NMSyncService = hrc2NMSyncService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(DateTime? NgaySX, int? Ca, string? LoaiBM, int? KhuVuc)
        {
            try
            {
                var data = await _service.GetAllAsync(NgaySX, Ca, LoaiBM, KhuVuc);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var result = await _service.GetByIdAsync(id);
                return result == null ? NotFound() : Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpGet("report/{reportNo}")]
        public async Task<IActionResult> GetByReportNo(int reportNo)
        {
            try
            {
                var result = await _service.GetByReportNoAsync(reportNo);
                return result == null ? NotFound() : Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPost("filter")]
        public async Task<IActionResult> Filter([FromBody] SyncFromNM_HRC2_Request request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("Thiếu dữ liệu bộ lọc.");
                }
                var result = await _service.FilterGroupedAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DLNM_HRC2 model)
        {
            try
            {
                var created = await _service.CreateAsync(model);
                return CreatedAtAction(nameof(GetById), new { id = created.ID }, created);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] DLNM_HRC2 model)
        {
            try
            {
                var ok = await _service.UpdateAsync(id, model);
                return ok ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
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
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search(DateTime? NgaySX, int? Ca, string? LoaiBM, int? Scope, string? searchText, int page = 1, int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100; // Giới hạn tối đa 100 records mỗi trang

            try
            {
                var result = await _service.SearchWithPagingAsync(
                    NgaySX,
                    Ca,
                    LoaiBM,
                    Scope,
                    searchText,
                    page,
                    pageSize
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }


        [HttpPost("chuyen-me-thoi")]
        public async Task<IActionResult> ChuyenMeThoi([FromBody] ChuyenMeThoiRequest request)
        {
            try
            {
                var result = await _service.ChuyenMeThoiAsync(request);
                return Ok(new { data = new { message = "Chuyển mẻ thành công" } });

            }
            catch (ApplicationException ex)
            {
                // Trả về đúng message nghiệp vụ được throw từ Repository
                return BadRequest(new { data = new { message = ex.Message } });
            }

        }

        [HttpPost("filterSTD_NXT")]
        public async Task<IActionResult> FilterSTD_NXT([FromBody] FilterSTD_NXTRequest request)
        {
            try
            {
                var result = await _service.FilterSTD_NXTAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }   
        }
    }
}

using dataproduct.api.Business;
using dataproduct.api.DTOs;
using dataproduct.api.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace dataproduct.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PhieusController : ControllerBase
    {
        private readonly PhieuService _service;

        public PhieusController(PhieuService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PhieuDto>>> GetAll(string? MaBM, int? NguoiTaoID, int? NguoiDuyetID, int? isCheckDuyet)
            => Ok(await _service.GetAllAsync(MaBM, NguoiTaoID, NguoiDuyetID, isCheckDuyet));

        [HttpGet("{id}")]
        public async Task<ActionResult<PhieuDto>> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<BmPhieu>> Create([FromBody] JsonElement formData)
        {
            var created = await _service.CreateAsync(formData);
            if (created == null)
            {
                return BadRequest(new { message = "Tạo phiếu thất bại (CreateAsync trả về null)" });
            }
            return CreatedAtAction(nameof(GetById), new { id = created.Idphieu }, created);
        }

        [HttpPost("auto-create-phieu")]
        public async Task<ActionResult<BmPhieu>> AutoCreatePhieu([FromBody] JsonElement formData)
        {
            var created = await _service.CreateAsync(formData);
            if (created == null)
            {
                return BadRequest(new { message = "Tạo phiếu thất bại (CreateAsync trả về null)" });
            }
            return Ok(new
            {
                success = true,
                id = created.Idphieu
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] JsonElement formData)
        {
            var ok = await _service.UpdateAsync(id, formData);
            return ok != null ? NoContent() : NotFound();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var ok = await _service.DeleteAsync(id);
            return ok ? NoContent() : NotFound();
        }

        [HttpPost("{id}/clone")]
        public async Task<ActionResult<BmPhieu>> Clone(Guid id, [FromBody] JsonElement formData)
        {
            try
            {
                var cloned = await _service.CloneAsync(id, formData);
                return cloned != null ? Ok(cloned) : NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeStatusRequest request)
        {
            try
            {
                var ok = await _service.ChangeStatusAsync(id, request.Status);
                return ok ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("exist")]
        public async Task<IActionResult> CheckExistSoPhieu([FromQuery] string maBm,
        [FromQuery] DateOnly ngaySX,
        [FromQuery] int ca,
        [FromQuery] int? scope,
        [FromQuery] int? mayduc)
        {
            var exists = await _service.CheckExistsAsync(maBm, ngaySX, ca, scope, mayduc);
            return Ok(new { exists });
        }

        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] SearchPhieuRequest request)
        {
            try
            {
                var result = await _service.SearchWithPagingAsync(request);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPost("{id:guid}/initialize")]
        public async Task<IActionResult> Initialize(Guid id)
        {
            var ok = await _service.InitializeAsync(id);
            if (!ok)
                return BadRequest(new { success = false, message = "Initialize thất bại" });

            return Ok(new { success = true });
        }


        [HttpPut("{id}/status-extended")]
        public async Task<IActionResult> UpdateStatusExtended(Guid id, [FromBody] UpdatePhieuStatusRequest request)
        {
            var ok = await _service.UpdateStatusExtendedAsync(id, request.Status, request.IsLock, request.IsDelete);
            return ok ? NoContent() : NotFound();
        }

        [HttpPut("{id}/sync-nguoi-tao")]
        public async Task<IActionResult> SyncNguoiTao(Guid id, [FromBody] int? NguoiTaoID)
        {
            try
            {
                var result = await _service.UpdateNguoiTaoAsync(id, NguoiTaoID);
                if (result == null)
                {
                    return BadRequest(new { success = false, message = "Đồng bộ người tạo thất bại. Kiểm tra dữ liệu đầu vào." });
                }
                return Ok(new { success = true, message = "Đồng bộ người tạo thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { success = false, message = ex.Message });
            }
        }
    }


    public class ChangeStatusRequest
    {
        public int Status { get; set; }
    }

    public class UpdatePhieuStatusRequest
    {
        public int? Status { get; set; }
        public int? IsLock { get; set; }
        public int? IsDelete { get; set; }
    }
}

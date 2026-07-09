using dataproduct.api.DTOs;
using dataproduct.api.Services;
using Microsoft.AspNetCore.Mvc;

namespace dataproduct.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DLNMHRC1Controller : ControllerBase
    {
        private readonly DLNMHRC1Service _service;

        public DLNMHRC1Controller(DLNMHRC1Service service)
        {
            _service = service;
        }

        [HttpPost("filter")]
        public async Task<IActionResult> Filter([FromBody] SyncFromNM_HRC1_Request request)
        {
            try
            {
                if (request == null)
                    return BadRequest("Thiếu dữ liệu bộ lọc.");
                var result = await _service.FilterGroupedAsync(request);
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
                await _service.ChuyenMeThoiAsync(request);
                return Ok(new { data = new { message = "Chuyển mẻ thành công" } });
            }
            catch (ApplicationException ex)
            {
                // Trả về đúng message nghiệp vụ được throw từ Repository
                return BadRequest(new { data = new { message = ex.Message } });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPost("force-sync")]
        public async Task<IActionResult> ForceSync([FromBody] SyncFromNM_HRC1_Request request)
        {
            try
            {
                if (request == null)
                    return BadRequest("Thiếu dữ liệu bộ lọc.");
                await _service.ForceSyncAsync(request);
                return Ok(new { message = "Đồng bộ thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>Xóa vĩnh viễn 1 mẻ thêm tay (IsNM = false) — dùng cho dòng "+ Thêm dòng" trên UI.</summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var ok = await _service.DeleteManualRowAsync(id);
                return ok ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>Xóa mềm 1 mẻ từ NM (IsNM = true) — chỉ đánh dấu IsDeleted, không xóa khỏi DB.</summary>
        [HttpDelete("delete-row-nm/{id}")]
        public async Task<IActionResult> DeleteRowNM(int id)
        {
            try
            {
                var ok = await _service.DeleteRowNMAsync(id);
                return ok ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPost("search-thongke")]
        public async Task<IActionResult> SearchThongKe([FromBody] SearchThongKeHrc1 dto)
        {
            try
            {
                var result = await _service.SearchThongKeAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        // Tổng cộng từng phụ liệu trên toàn khoảng lọc — tách riêng để FE gọi lazy
        [HttpPost("sum-thongke")]
        public async Task<IActionResult> SumThongKe([FromBody] SearchThongKeHrc1 dto)
        {
            try
            {
                var result = await _service.GetThongKeSumAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPost("export-thongke")]
        public async Task<IActionResult> ExportThongKe([FromBody] SearchThongKeHrc1 dto)
        {
            try
            {
                var file = await _service.ExportThongKeExcelAsync(dto);
                return File(file.Content, file.ContentType, file.FileName);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>Xuất biên bản 1 phiếu (mẫu HRC1_BB_NauLuyen_BOF.xlsx). Ngày/Ca/Lò lấy theo phiếu nếu phiếu có sẵn.</summary>
        [HttpGet("export-excel-detail")]
        public async Task<IActionResult> ExportExcelDetail(
            [FromQuery] DateOnly ngay, [FromQuery] int ca, [FromQuery] int scope,
            [FromQuery] string bieuMau, [FromQuery] Guid idPhieu)
        {
            try
            {
                var file = await _service.ExportExcelDetailAsync(ngay, ca, scope, idPhieu);
                return File(file.Content, file.ContentType, file.FileName);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        /// <summary>Xuất biên bản 1 phiếu dạng PDF (mẫu HRC1_BB_NauLuyen.html).</summary>
        [HttpGet("export-pdf-detail")]
        public async Task<IActionResult> ExportPdfDetail(
            [FromQuery] DateOnly ngay, [FromQuery] int ca, [FromQuery] int scope,
            [FromQuery] string bieuMau, [FromQuery] Guid idPhieu)
        {
            try
            {
                var file = await _service.ExportPdfDetailAsync(ngay, ca, scope, idPhieu);
                return File(file.Content, file.ContentType, file.FileName);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }
    }
}

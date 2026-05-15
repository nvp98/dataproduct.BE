using dataproduct.api.DTOs;
using dataproduct.api.Models;
using dataproduct.api.Services;
using Microsoft.AspNetCore.Mvc;

namespace dataproduct.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BmQuyenXlController : ControllerBase
    {
        private readonly BmQuyenXlService _service;

        public BmQuyenXlController(BmQuyenXlService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy danh sách quyền xử lý
        /// </summary>
        /// <param name="idTaiKhoan">ID tài khoản</param>
        /// <param name="maBm">Mã biểu mẫu</param>
        /// <param name="maKhuVuc">Mã khu vực</param>
        /// <returns>Danh sách quyền xử lý</returns>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? idTaiKhoan, [FromQuery] string? maBm, [FromQuery] string? maKhuVuc)
        {
            try
            {
                var data = await _service.GetAllAsync(idTaiKhoan, maBm, maKhuVuc);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// Lấy quyền menu theo tài khoản: Việc tôi bắt đầu (processingForms) và Việc đến tôi (approvingForms).
        /// </summary>
        [HttpGet("menu-permissions")]
        public async Task<IActionResult> GetMenuPermissions([FromQuery] int idTaiKhoan)
        {
            try
            {
                if (idTaiKhoan <= 0)
                    return BadRequest("idTaiKhoan phải lớn hơn 0.");
                var data = await _service.GetMenuPermissionsAsync(idTaiKhoan);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// Lấy danh sách quyền theo ID tài khoản
        /// </summary>
        [HttpGet("tai-khoan/{idTaiKhoan:int}")]
        public async Task<IActionResult> GetByTaiKhoanId(int idTaiKhoan)
        {
            try
            {
                var data = await _service.GetByTaiKhoanIdAsync(idTaiKhoan);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// Lấy quyền xử lý theo ID
        /// </summary>
        /// <param name="id">ID quyền xử lý</param>
        /// <returns>Chi tiết quyền xử lý</returns>
        [HttpGet("{id:int}")]
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

        /// <summary>
        /// Lưu hàng loạt: mỗi tổ hợp (MaBm × MaKhuVuc) thành 1 dòng.
        /// Truyền IdToDelete để xóa bản ghi cũ trước khi tạo mới (dùng khi cập nhật).
        /// </summary>
        [HttpPost("save")]
        public async Task<IActionResult> BulkSave([FromBody] BmQuyenXlBulkSaveDto dto)
        {
            try
            {
                var created = await _service.BulkSaveAsync(dto);
                return Ok(new { count = created.Count });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// Tạo mới quyền xử lý
        /// </summary>
        /// <param name="dto">Dữ liệu tạo mới</param>
        /// <returns>Quyền xử lý vừa tạo</returns>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] BmQuyenXlCreateDto dto)
        {
            try
            {
                var created = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// Cập nhật quyền xử lý
        /// </summary>
        /// <param name="id">ID quyền xử lý</param>
        /// <param name="dto">Dữ liệu cập nhật</param>
        /// <returns>NoContent nếu thành công</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] BmQuyenXlUpdateDto dto)
        {
            try
            {
                var ok = await _service.UpdateAsync(id, dto);
                return ok ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// Xóa quyền xử lý theo ID
        /// </summary>
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

        /// <summary>
        /// Xóa toàn bộ quyền xử lý của một tài khoản
        /// </summary>
        [HttpDelete("tai-khoan/{idTaiKhoan:int}")]
        public async Task<IActionResult> DeleteByTaiKhoan(int idTaiKhoan)
        {
            try
            {
                await _service.DeleteByTaiKhoanAsync(idTaiKhoan);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
    }
}

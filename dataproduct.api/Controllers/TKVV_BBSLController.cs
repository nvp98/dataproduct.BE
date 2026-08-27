using dataproduct.api.DTOs.NMTKVV_Dto;
using dataproduct.api.Services;
using Microsoft.AspNetCore.Mvc;

namespace dataproduct.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TKVV_BBSLController : ControllerBase
    {
        private readonly TKVV_BBSLService _service;

        public TKVV_BBSLController(TKVV_BBSLService service)
        {
            _service = service;
        }

        // ─── Danh mục NVL ────────────────────────────────────────────────────

        [HttpGet("get-nvl")]
        public async Task<IActionResult> GetNvlList([FromQuery] string? maBM, [FromQuery] string? scope)
        {
            try { return Ok(await _service.GetNvlListAsync(maBM, scope)); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("get-nvl/{id}")]
        public async Task<IActionResult> GetNvlById(int id)
        {
            try
            {
                var r = await _service.GetNvlByIdAsync(id);
                return r == null ? NotFound(new { message = $"Không tìm thấy NVL ID={id}" }) : Ok(r);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPost("create-nvl")]
        public async Task<IActionResult> CreateNvl([FromBody] CreateTKVVNguyenVatLieuDto dto)
        {
            try
            {
                var r = await _service.AddNvlAsync(dto);
                return CreatedAtAction(nameof(GetNvlById), new { id = r.Id }, r);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPut("update-nvl/{id}")]
        public async Task<IActionResult> UpdateNvl(int id, [FromBody] UpdateTKVVNguyenVatLieuDto dto)
        {
            try
            {
                var r = await _service.UpdateNvlAsync(id, dto);
                return r == null ? NotFound(new { message = $"Không tìm thấy NVL ID={id}" }) : Ok(r);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpDelete("delete-nvl/{id}")]
        public async Task<IActionResult> DeleteNvl(int id)
        {
            try
            {
                var ok = await _service.DeleteNvlAsync(id);
                return ok ? Ok(new { message = "Đã xóa thành công." }) : NotFound(new { message = $"Không tìm thấy NVL ID={id}" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // ─── Dữ liệu sản lượng ────────────────────────────────────────────────

        [HttpGet("get-datasanluong-filter")]
        public async Task<IActionResult> GetDataSanLuongFilter(
            [FromQuery] string? scope,
            [FromQuery] DateTime? ngayBatDau,
            [FromQuery] DateTime? ngayKetThuc)
        {
            try { return Ok(await _service.GetDataByFilterAsync(scope, ngayBatDau, ngayKetThuc)); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // KTV/KCS chỉnh tay GiaTriDieuChinh cho 1 dòng dữ liệu thô khi nghi ngờ PLC báo sai
        [HttpPut("dulieu-tho/dieu-chinh/{id}")]
        public async Task<IActionResult> UpdateGiaTriDieuChinh(long id, [FromBody] UpdateGiaTriDieuChinhRequestDto dto)
        {
            try
            {
                var ok = await _service.UpdateGiaTriDieuChinhAsync(id, dto.GiaTriDieuChinh);
                return ok ? Ok(new { message = "Đã lưu giá trị điều chỉnh." }) : NotFound(new { message = $"Không tìm thấy dữ liệu ID={id}" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("get-tong-tudong")]
        public async Task<IActionResult> GetTongTuDong(
            [FromQuery] DateTime ngay,
            [FromQuery] int ca,
            [FromQuery] int scope)
        {
            try
            {
                if (ca != 1 && ca != 2)
                    return BadRequest(new { message = "ca chỉ nhận giá trị 1 hoặc 2." });
                return Ok(await _service.GetTongTuDongAsync(ngay, ca, scope));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("get-chitiet/{idPhieu}")]
        public async Task<IActionResult> GetChiTietByPhieu(Guid idPhieu)
        {
            try { return Ok(await _service.GetChiTietByPhieuAsync(idPhieu)); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

    }
}

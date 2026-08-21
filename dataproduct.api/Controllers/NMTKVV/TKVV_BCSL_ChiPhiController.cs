using dataproduct.api.DTOs.NMTKVV_Dto;
using dataproduct.api.Services.NMTKVV;
using Microsoft.AspNetCore.Mvc;

namespace dataproduct.api.Controllers.NMTKVV
{
    [Route("api/[controller]")]
    [ApiController]
    public class TKVV_BCSL_ChiPhiController : ControllerBase
    {
        private readonly TKVV_BCSL_ChiPhiService _service;

        public TKVV_BCSL_ChiPhiController(TKVV_BCSL_ChiPhiService service)
        {
            _service = service;
        }

        // Đổ giá trị NVL tự động từ EMS (SP_TKVV_GetGiaTriNVL_Auto) vào bảng khi
        // nhấn "Làm mới dữ liệu" trên phiếu Báo cáo Sản lượng & Chi phí.
        // Trả danh sách NVL kèm GiaTri (tổng cân băng tải EMS) theo maBM + scope + ngày + ca.
        // FE map: TenNVL → nguyenLieu, GiaTri → klAm.
        [HttpGet("get-dulieu-can")]
        public async Task<IActionResult> GetDuLieuCan(
            [FromQuery] DateTime ngay,
            [FromQuery] int ca,
            [FromQuery] string maBM,
            [FromQuery] string loaiDuLieu = "SANLUONG",
            [FromQuery] int scope = 1)
        {
            try
            {
                if (ca != 1 && ca != 2)
                    return BadRequest(new { message = "ca chỉ nhận 1 hoặc 2." });
                if (scope < 1 || scope > 6)
                    return BadRequest(new { message = "scope phải từ 1 đến 6." });
                if (string.IsNullOrWhiteSpace(maBM))
                    return BadRequest(new { message = "Thiếu tham số maBM." });
                return Ok(await _service.GetDuLieuCanAsync(ngay, ca, maBM, loaiDuLieu, scope));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, detail = ex.InnerException?.Message });
            }
        }

        [HttpGet("get-giatri-nvl-auto")]
        public async Task<IActionResult> GetGiaTriNVLAuto(
            [FromQuery] DateTime ngay,
            [FromQuery] int ca,
            [FromQuery] int scope,
            [FromQuery] string maBM)
        {
            try
            {
                if (ca != 1 && ca != 2)
                    return BadRequest(new { message = "ca chỉ nhận giá trị 1 hoặc 2." });
                if (string.IsNullOrWhiteSpace(maBM))
                    return BadRequest(new { message = "Thiếu tham số maBM." });
                if (scope < 1 || scope > 6)
                    return BadRequest(new { message = "scope phải từ 1 đến 6." });
                return Ok(await _service.GetGiaTriNVLAutoAsync(ngay, ca, scope, maBM));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, detail = ex.InnerException?.Message });
            }
        }

        // Tải dữ liệu cân từ EMS (cả Ca 1 + Ca 2), upsert vào TKVV_BaoCaoSanLuongChiPhi,
        // trả về dữ liệu đã lưu kèm KLAmAuto và IsAdjusted.
        [HttpPost("load-dulieu")]
        public async Task<IActionResult> LoadDuLieu([FromBody] LoadDuLieuCanRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.MaBM))
                    return BadRequest(new { message = "Thiếu tham số maBM." });
                if (request.Scope < 1 || request.Scope > 6)
                    return BadRequest(new { message = "scope phải từ 1 đến 6." });
                var result = await _service.LoadAndSaveAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, detail = ex.InnerException?.Message });
            }
        }

        // Lấy dữ liệu đã lưu theo ngày và scope int 1-6 (dùng khi load lại phiếu).
        [HttpGet("get-baocao-data")]
        public async Task<IActionResult> GetBaoCaoData(
            [FromQuery] DateOnly ngaySX,
            [FromQuery] string maBM,
            [FromQuery] int scope)
        {
            try
            {
                if (scope < 1 || scope > 6)
                    return BadRequest(new { message = "scope phải từ 1 đến 6." });
                var result = await _service.GetBaoCaoDataAsync(ngaySX, maBM, scope);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, detail = ex.InnerException?.Message });
            }
        }

        // Lưu giá trị người dùng nhập (KLAm, DoAm, ThanhPham...) — backend tự xác định IsAdjusted.
        [HttpPost("save-phieu-rows")]
        public async Task<IActionResult> SavePhieuRows([FromBody] SaveBcSlPhieuRequestDto request)
        {
            try
            {
                if (request.Rows == null || request.Rows.Count == 0)
                    return BadRequest(new { message = "Danh sách dòng trống." });
                await _service.SavePhieuRowsAsync(request);
                return Ok(new { message = "Đã lưu." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, detail = ex.InnerException?.Message });
            }
        }
    }
}

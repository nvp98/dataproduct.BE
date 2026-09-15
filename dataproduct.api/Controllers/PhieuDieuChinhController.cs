using dataproduct.api.DTOs.NMLG_Dto;
using dataproduct.api.Services;
using Microsoft.AspNetCore.Mvc;

namespace dataproduct.api.Controllers
{
    /// <summary>
    /// Phiếu điều chỉnh số liệu NM.LG.
    /// Base route: /api/PhieuDieuChinh
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class PhieuDieuChinhController : ControllerBase
    {
        private readonly PhieuDieuChinhService _service;

        public PhieuDieuChinhController(PhieuDieuChinhService service)
        {
            _service = service;
        }

        // Danh sách chi tiết BBGN (gồm Tên NVL) theo Ngày/Ca/Kíp — nguồn dữ liệu cho Phiếu điều chỉnh.
        [HttpGet("get-bbgn")]
        public async Task<IActionResult> GetBBGN(
            [FromQuery] DateTime ngay,
            [FromQuery] int? ca,
            [FromQuery] string? kip)
        {
            try
            {
                if (ca.HasValue && ca != 1 && ca != 2)
                    return BadRequest(new { message = "ca chỉ nhận giá trị 1 hoặc 2." });

                return Ok(await _service.GetBBGNAsync(ngay, ca, kip));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // Danh sách chi tiết đã lưu của 1 Phiếu điều chỉnh (LG_PhieuDieuChinh_ChiTiet)
        [HttpGet("{idPhieu}/chi-tiet")]
        public async Task<IActionResult> GetChiTiet(Guid idPhieu)
        {
            try
            {
                return Ok(await _service.GetChiTietByPhieuAsync(idPhieu));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // Ghi đè toàn bộ chi tiết của phiếu (xóa cũ, ghi lại theo danh sách mới) — gọi mỗi lần Lưu
        [HttpPut("{idPhieu}/chi-tiet")]
        public async Task<IActionResult> ReplaceChiTiet(Guid idPhieu, [FromBody] ReplacePhieuDieuChinhChiTietRequest request)
        {
            try
            {
                await _service.ReplaceChiTietAsync(idPhieu, request.Items, request.NguoiSua);
                return Ok(await _service.GetChiTietByPhieuAsync(idPhieu));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}

using dataproduct.api.Services;
using Microsoft.AspNetCore.Mvc;

namespace dataproduct.api.Controllers
{
    /// <summary>
    /// Biên bản chốt số liệu Xưởng Đúc phôi vuông (BM.14/QT.05.11, MaBM = HRC1_BCSX_DPV).
    /// Lưu / duyệt / ký phiếu đi qua PhieusController; controller này chỉ có API đặc thù.
    /// Tài liệu: docs/BCSX_HRC1_DucVuong/02-BienBan-ChotSoLieu.md
    /// </summary>
    [Route("api/HRC1_BCSX")]
    [ApiController]
    public class HRC1_BCSXController : ControllerBase
    {
        private readonly HRC1_BCSXService _service;

        public HRC1_BCSXController(HRC1_BCSXService service)
        {
            _service = service;
        }

        /// <summary>Số liệu gợi ý từ Kho dữ liệu (CSDL HRC1_DPV) cho ngày ngaySX — kỳ 08h ngày N → 08h ngày N+1.</summary>
        [HttpGet("goi-y")]
        public async Task<IActionResult> GetGoiY([FromQuery] DateOnly ngaySX)
        {
            if (ngaySX == default) return BadRequest(new { message = "Thiếu ngày (ngaySX)." });
            return Ok(await _service.GetGoiYAsync(ngaySX));
        }

        /// <summary>4 vùng chi tiết đã lưu của 1 phiếu (đọc từ bảng HRC1_BC_*).</summary>
        [HttpGet("phieu/{idPhieu:guid}/chi-tiet")]
        public async Task<IActionResult> GetChiTiet(Guid idPhieu)
            => Ok(await _service.GetChiTietAsync(idPhieu));
    }
}

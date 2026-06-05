using dataproduct.api.Business;
using Microsoft.AspNetCore.Mvc;

namespace dataproduct.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KipController : ControllerBase
    {
        private readonly PhieuService _service;

        public KipController(PhieuService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy dữ liệu ca kíp theo ngày và ca
        /// </summary>
        /// <param name="ngayLamViec">Ngày làm việc (định dạng: yyyy-MM-dd)</param>
        /// <param name="ca">Ca làm việc: 1 = Ca ngày, 2 = Ca đêm</param>
        /// <returns>Dữ liệu ca kíp hoặc null nếu không tìm thấy</returns>
        [HttpGet("by-date-ca")]
        public async Task<IActionResult> GetKipByDateAndCa([FromQuery] DateOnly ngayLamViec, [FromQuery] int ca)
        {
            try
            {
                if (ca < 1 || ca > 2)
                    return BadRequest(new { message = "Ca phải là 1 (Ca ngày) hoặc 2 (Ca đêm)" });

                var result = await _service.GetKipByDateAndCaAsync(ngayLamViec, ca);

                if (result == null)
                    return NotFound(new { message = $"Không tìm thấy dữ liệu ca kíp cho ngày {ngayLamViec} ca {ca}" });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }
    }
}

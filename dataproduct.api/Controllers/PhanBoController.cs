using dataproduct.api.DTOs.NMLG_Dto;
using dataproduct.api.Services;
using Microsoft.AspNetCore.Mvc;

namespace dataproduct.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PhanBoController : ControllerBase
    {
        private readonly PhanBoService _service;

        public PhanBoController(PhanBoService service)
        {
            _service = service;
        }

        [HttpPost("tinh")]
        public async Task<IActionResult> Tinh([FromBody] TinhPhanBoRequestDto dto)
       {
            try
            {
                await _service.TinhPhanBoAsync(dto.Ngay, dto.IdNguoiThucThi);
                return Ok(new { message = "Đã tính lại phân bổ." });
            }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("get-ket-qua")]
        public async Task<IActionResult> GetKetQua(
            [FromQuery] DateTime ngay,
            [FromQuery] byte loaiPhanBo,
            [FromQuery] int idLoCao,
            [FromQuery] byte? ca)
        {
            try { return Ok(await _service.LayKetQuaAsync(ngay, loaiPhanBo, idLoCao, ca)); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("get-ket-qua-than-coc")]
        public async Task<IActionResult> GetKetQuaThanCoc(
            [FromQuery] DateTime ngay,
            [FromQuery] int idLoCao,
            [FromQuery] byte? ca)
        {
            try { return Ok(await _service.LayKetQuaThanCocAsync(ngay, idLoCao, ca)); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPost("chot")]
        public async Task<IActionResult> Chot([FromBody] ChotPhanBoRequestDto dto)
        {
            try
            {
                await _service.ChotPhanBoAsync(dto.Ngay, dto.IdNguoiXacNhan);
                return Ok(new { message = "Đã chốt phân bổ." });
            }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("bao-cao")]
        public async Task<IActionResult> BaoCao(
            [FromQuery] DateTime tuNgay,
            [FromQuery] DateTime denNgay,
            [FromQuery] int? idLoCao,
            [FromQuery] byte? loaiPhanBo)
        {
            try { return Ok(await _service.LayBaoCaoAsync(tuNgay, denNgay, idLoCao, loaiPhanBo)); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}

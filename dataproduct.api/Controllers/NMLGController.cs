using dataproduct.api.DTOs.NMLG_Dto;
using dataproduct.api.Models;
using dataproduct.api.Services;
using Microsoft.AspNetCore.Mvc;

namespace dataproduct.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NMLGController : ControllerBase
    {
        private readonly NMLGService _service;

        public NMLGController(NMLGService service)
        {
            _service = service;
        }


        [HttpGet("silowithlocao")]
        public async Task<IActionResult> GetSiLoWithLoCao(int? idLoCao)
        {
            var data = await _service.GetSiLoWithLoCaAsync(idLoCao);
            return Ok(data);
        }

        [HttpPost("silo")]
        public async Task<IActionResult> AddSiLo([FromBody] AddSiLoLGDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest(new { message = "Dữ liệu không hợp lệ" });

                var siLo = new SiLo_LG
                {
                    ID_LoCao = dto.ID_LoCao,
                    TenSiLo = dto.TenSiLo,
                    ThuTu = dto.ThuTu,
                    TenNL = dto.TenNL,
                    TenNL_DieuChinh = dto.TenNL_DieuChinh
                };

                var result = await _service.AddSiLoAsync(siLo);

                return CreatedAtAction(nameof(AddSiLo), new { id = result.ID }, new SiLoLGResponseDto
                {
                    ID = result.ID,
                    ID_LoCao = result.ID_LoCao,
                    TenSiLo = result.TenSiLo,
                    ThuTu = result.ThuTu,
                    TenNL = result.TenNL,
                    TenNL_DieuChinh = result.TenNL_DieuChinh
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi thêm SiLo", error = ex.Message });
            }
        }

        [HttpPut("silo/{id}")]
        public async Task<IActionResult> UpdateSiLo(int id, [FromBody] UpdateSiLoLGDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest(new { message = "Dữ liệu không hợp lệ" });

                var siLo = new SiLo_LG
                {
                    ID_LoCao = dto.ID_LoCao,
                    TenSiLo = dto.TenSiLo,
                    ThuTu = dto.ThuTu,
                    TenNL = dto.TenNL,
                    TenNL_DieuChinh = dto.TenNL_DieuChinh
                };

                var result = await _service.UpdateSiLoAsync(id, siLo);

                if (result == null)
                    return NotFound(new { message = $"Không tìm thấy SiLo với ID = {id}" });

                return Ok(new SiLoLGResponseDto
                {
                    ID = result.ID,
                    ID_LoCao = result.ID_LoCao,
                    TenSiLo = result.TenSiLo,
                    ThuTu = result.ThuTu,
                    TenNL = result.TenNL,
                    TenNL_DieuChinh = result.TenNL_DieuChinh
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi cập nhật SiLo", error = ex.Message });
            }
        }

        [HttpGet("getkltonsilolocao")]
        public async Task<IActionResult> GetSiLoTonByLoCao(int? idLoCao, int? idCa, DateTime? ngay)
        {
            try
            {
                if (idCa != 1 && idCa != 2)
                    return BadRequest(new { message = "IdCa chỉ nhận giá trị 1 (ca ngày) hoặc 2 (ca đêm)." });

                var result = await _service.GetSiLoTonAsync(idLoCao, idCa, ngay);

                if (result == null || !result.Any())
                    return NotFound(new { message = "Không có dữ liệu." });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}

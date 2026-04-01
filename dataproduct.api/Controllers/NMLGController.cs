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

        // ca=1: 07:30→19:30 | ca=2: 19:30→07:30 hôm sau

        [HttpGet("naplieu")]
        public async Task<IActionResult> GetNapLieu([FromQuery] int loCao, [FromQuery] DateTime ngay, [FromQuery] int ca)
        {
            try
            {
                if (ca != 1 && ca != 2) return BadRequest(new { message = "ca chỉ nhận giá trị 1 hoặc 2" });
                var (bd, ed) = NMLGService.GetCaRange(ngay, ca);
                var result = await _service.GetNapLieuAsync(loCao, bd, ed);
                return Ok(result);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("naplieu/mapped")]
        public async Task<IActionResult> GetNapLieuMapped([FromQuery] int loCao, [FromQuery] DateTime ngay, [FromQuery] int ca)
        {
            try
            {
                if (ca != 1 && ca != 2) return BadRequest(new { message = "ca chỉ nhận giá trị 1 hoặc 2" });
                var (bd, ed) = NMLGService.GetCaRange(ngay, ca);
                var result = await _service.GetNapLieuMappedAsync(loCao, bd, ed);
                return Ok(result);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("naplieu/pivot")]
        public async Task<IActionResult> GetNapLieuPivot([FromQuery] int loCao, [FromQuery] DateTime ngay, [FromQuery] int ca)
        {
            try
            {
                if (ca != 1 && ca != 2) return BadRequest(new { message = "ca chỉ nhận giá trị 1 hoặc 2" });
                var (bd, ed) = NMLGService.GetCaRange(ngay, ca);
                var result = await _service.GetNapLieuPivotAsync(loCao, bd, ed);
                return Ok(result);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
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

        [HttpGet("quykho")]
        public async Task<IActionResult> GetQuyKho([FromQuery] DateOnly ngay, [FromQuery] int idCa, [FromQuery] int idLoCao)
        {
            try
            {
                if (idCa != 1 && idCa != 2)
                    return BadRequest(new { message = "idCa chỉ nhận giá trị 1 hoặc 2." });

                var result = await _service.GetQuyKhoAsync(ngay, idCa, idLoCao);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("quykho")]
        public async Task<IActionResult> UpsertQuyKho([FromBody] UpsertQuyKhoRequest request)
        {
            try
            {
                if (request.IDCa != 1 && request.IDCa != 2)
                    return BadRequest(new { message = "IDCa chỉ nhận giá trị 1 hoặc 2." });

                if (request.Items == null || request.Items.Count == 0)
                    return BadRequest(new { message = "Danh sách Items không được rỗng." });

                await _service.UpsertQuyKhoAsync(request);
                return Ok(new { message = "Lưu quy khô thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}

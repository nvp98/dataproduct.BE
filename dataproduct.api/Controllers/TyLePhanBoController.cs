using dataproduct.api.DTOs.NMLG_Dto;
using dataproduct.api.Services;
using Microsoft.AspNetCore.Mvc;

namespace dataproduct.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TyLePhanBoController : ControllerBase
    {
        private readonly TyLePhanBoService _service;

        public TyLePhanBoController(TyLePhanBoService service)
        {
            _service = service;
        }

        [HttpGet("get-history")]
        public async Task<IActionResult> GetHistory(
            [FromQuery] int idNvl,
            [FromQuery] DateTime? tuNgay,
            [FromQuery] DateTime? denNgay)
        {
            try { return Ok(await _service.GetHistoryAsync(idNvl, tuNgay, denNgay)); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateTyLePhanBoDto dto)
        {
            try { return Ok(await _service.CreateAsync(dto)); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}

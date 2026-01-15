using dataproduct.api.DTOs;
using dataproduct.api.Models;
using dataproduct.api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace dataproduct.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class STD_NXT_HRC2Controller : ControllerBase
    {
        private readonly STD_NXT_HRC2Service _service;
        public STD_NXT_HRC2Controller(STD_NXT_HRC2Service service)
        {
            _service = service;
        }

        [HttpPost("upsert")]
        public async Task<IActionResult> Upsert([FromBody] STD_NXT_HRC2_UpsertDto entity)
        {
            try
            {
                var result = await _service.UpsertAsync(entity);
                return Ok(result);

            }
            catch (ApplicationException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }

        }

        [HttpGet("get-by-phieu-id/{phieuId}")]
        public async Task<IActionResult> GetByBm(Guid phieuId)
        {
            try
            {
                var result = await _service.GetByPhieuIdAsync(phieuId);
                return Ok(new { data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPost("phan-bo")]
        public async Task<IActionResult> PhanBo([FromBody] STD_NXT_HRC2_PhanBoDto entity)
        {
            try
            {
                var result = await _service.PhanBoAsync(entity);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
    }
}

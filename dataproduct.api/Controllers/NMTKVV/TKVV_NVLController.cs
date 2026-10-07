using dataproduct.api.DTOs.NMTKVV_Dto;
using dataproduct.api.Services.NMTKVV;
using Microsoft.AspNetCore.Mvc;

namespace dataproduct.api.Controllers.NMTKVV
{
    [ApiController]
    [Route("api/[controller]")]
    public class TKVV_NVLController : ControllerBase
    {
        private readonly TKVV_SiloService _service;

        public TKVV_NVLController(TKVV_SiloService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetNvlList([FromQuery] string? maBM, [FromQuery] string? scope)
        {
            var result = await _service.GetNvlListAsync(maBM, scope);
            return Ok(result);
        }
    }
}

using dataproduct.api.Services;
using Microsoft.AspNetCore.Mvc;

namespace dataproduct.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BKKcscanBbxlSanxuatController : ControllerBase
    {
        private readonly BKKcscanBbxlSanxuatService _service;

        public BKKcscanBbxlSanxuatController(BKKcscanBbxlSanxuatService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(DateOnly? ngaySX, string? caSX, DateOnly? ngayXL, string? caXL, string? order, int? xuongCan)
        {
            return Ok(await _service.GetAllAsync(ngaySX, caSX, ngayXL, caXL, order, xuongCan));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _service.GetByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }
    }
}

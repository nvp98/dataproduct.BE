using dataproduct.api.Services;
using Microsoft.AspNetCore.Mvc;

namespace dataproduct.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BkKcsBbxnSanLuongController : ControllerBase
    {
        private readonly BkKcsBbxnSanLuongService _service;

        public BkKcsBbxnSanLuongController(BkKcsBbxnSanLuongService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(DateOnly? ngaySX, string? ca, string? sanPham, string? macThep, string? idXuongCan)
        {
            return Ok(await _service.GetAllAsync(ngaySX, ca, sanPham, macThep, idXuongCan));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _service.GetByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }
    }
}

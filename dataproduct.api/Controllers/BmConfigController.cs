using dataproduct.api.Services;
using Microsoft.AspNetCore.Mvc;

namespace dataproduct.api.Controllers
{
    [ApiController]
    [Route("api/bm-config")]
    public class BmConfigController : ControllerBase
    {
        private readonly BmConfigService _service;

        public BmConfigController(BmConfigService service)
        {
            _service = service;
        }

        /// <summary>GET /api/bm-config — Lấy toàn bộ cấu hình mã BM</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var all = await _service.GetAllAsync();
            return Ok(all);
        }

        /// <summary>GET /api/bm-config/{key} — Lấy một mục theo key (tên file không có .html)</summary>
        [HttpGet("{key}")]
        public async Task<IActionResult> GetOne(string key)
        {
            var entry = await _service.GetAsync(key);
            if (entry == null) return NotFound($"Không tìm thấy key: {key}");
            return Ok(entry);
        }

        /// <summary>PUT /api/bm-config/{key} — Cập nhật hoặc thêm mới một mục</summary>
        [HttpPut("{key}")]
        public async Task<IActionResult> Update(string key, [FromBody] BmConfigEntry entry)
        {
            if (string.IsNullOrWhiteSpace(entry.MaBM))
                return BadRequest("MaBM không được để trống.");
            await _service.SaveAsync(key, entry);
            return Ok(entry);
        }
    }
}

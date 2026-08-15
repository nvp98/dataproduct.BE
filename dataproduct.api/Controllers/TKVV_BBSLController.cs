using dataproduct.api.DTOs.NMTKVV_Dto;
using dataproduct.api.Services;
using Microsoft.AspNetCore.Mvc;

namespace dataproduct.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TKVV_BBSLController : ControllerBase
    {
        private readonly TKVV_BBSLService _service;

        public TKVV_BBSLController(TKVV_BBSLService service)
        {
            _service = service;
        }

        // ─── Danh mục NVL ────────────────────────────────────────────────────

        [HttpGet("get-nvl")]
        public async Task<IActionResult> GetNvlList([FromQuery] string? maBM)
        {
            try { return Ok(await _service.GetNvlListAsync(maBM)); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("get-nvl/{id}")]
        public async Task<IActionResult> GetNvlById(int id)
        {
            try
            {
                var r = await _service.GetNvlByIdAsync(id);
                return r == null ? NotFound(new { message = $"Không tìm thấy NVL ID={id}" }) : Ok(r);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPost("create-nvl")]
        public async Task<IActionResult> CreateNvl([FromBody] CreateTKVVNguyenVatLieuDto dto)
        {
            try
            {
                var r = await _service.AddNvlAsync(dto);
                return CreatedAtAction(nameof(GetNvlById), new { id = r.Id }, r);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPut("update-nvl/{id}")]
        public async Task<IActionResult> UpdateNvl(int id, [FromBody] UpdateTKVVNguyenVatLieuDto dto)
        {
            try
            {
                var r = await _service.UpdateNvlAsync(id, dto);
                return r == null ? NotFound(new { message = $"Không tìm thấy NVL ID={id}" }) : Ok(r);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpDelete("delete-nvl/{id}")]
        public async Task<IActionResult> DeleteNvl(int id)
        {
            try
            {
                var ok = await _service.DeleteNvlAsync(id);
                return ok ? Ok(new { message = "Đã xóa thành công." }) : NotFound(new { message = $"Không tìm thấy NVL ID={id}" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // ─── Mapping ─────────────────────────────────────────────────────────

        [HttpGet("get-mapping")]
        public async Task<IActionResult> GetMappingList(
            [FromQuery] string? scope, [FromQuery] string? tagID, [FromQuery] int? ca)
        {
            try { return Ok(await _service.GetMappingListAsync(scope, tagID, ca)); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("get-mapping/{id}")]
        public async Task<IActionResult> GetMappingById(long id)
        {
            try
            {
                var r = await _service.GetMappingByIdAsync(id);
                return r == null ? NotFound(new { message = $"Không tìm thấy Mapping ID={id}" }) : Ok(r);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPost("create-mapping")]
        public async Task<IActionResult> CreateMapping([FromBody] CreateTKVVMappingDto dto)
        {
            try
            {
                var r = await _service.AddMappingAsync(dto);
                return CreatedAtAction(nameof(GetMappingById), new { id = r.Id }, r);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPut("update-mapping/{id}")]
        public async Task<IActionResult> UpdateMapping(long id, [FromBody] UpdateTKVVMappingDto dto)
        {
            try
            {
                var r = await _service.UpdateMappingAsync(id, dto);
                return r == null ? NotFound(new { message = $"Không tìm thấy Mapping ID={id}" }) : Ok(r);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpDelete("delete-mapping/{id}")]
        public async Task<IActionResult> DeleteMapping(long id)
        {
            try
            {
                var ok = await _service.DeleteMappingAsync(id);
                return ok ? Ok(new { message = "Đã xóa thành công." }) : NotFound(new { message = $"Không tìm thấy Mapping ID={id}" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // Danh sách Tag PLC từ EMS (dbo.EMS_GetMappingTag) — cho admin chọn khi tạo Mapping,
        // thay vì gõ tay TagIDEMS/TagName. Trả mọi loại Tag (không lọc Loai) vì dùng chung
        // cho toàn NM.TKVV, không riêng biên bản sản lượng.
        [HttpGet("get-ems-tags")]
        public async Task<IActionResult> GetEmsTagList([FromQuery] string? xuong, [FromQuery] string? tagName)
        {
            try { return Ok(await _service.GetEmsTagListAsync(xuong, tagName)); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // ─── Dữ liệu sản lượng ────────────────────────────────────────────────

        [HttpGet("get-datasanluong-filter")]
        public async Task<IActionResult> GetDataSanLuongFilter(
            [FromQuery] string? scope,
            [FromQuery] DateTime? ngayBatDau,
            [FromQuery] DateTime? ngayKetThuc)
        {
            try { return Ok(await _service.GetDataByFilterAsync(scope, ngayBatDau, ngayKetThuc)); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // Tổng tự động (PLC) theo Ngay/Ca/Scope — 1 Tag = 1 BM/xưởng nên chỉ có 1 số
        // tổng duy nhất (không tách theo sản phẩm), chỉ để đối chiếu, dùng chung cho
        // cả phiếu mới (chưa lưu) lẫn phiếu đã lưu (chỉ cần ngay/ca/scope từ form).
        [HttpGet("get-tong-tudong")]
        public async Task<IActionResult> GetTongTuDong(
            [FromQuery] DateTime ngay,
            [FromQuery] int ca,
            [FromQuery] int scope)
        {
            try
            {
                if (ca != 1 && ca != 2)
                    return BadRequest(new { message = "ca chỉ nhận giá trị 1 hoặc 2." });
                return Ok(await _service.GetTongTuDongAsync(ngay, ca, scope));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("get-chitiet/{idPhieu}")]
        public async Task<IActionResult> GetChiTietByPhieu(Guid idPhieu)
        {
            try { return Ok(await _service.GetChiTietByPhieuAsync(idPhieu)); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}

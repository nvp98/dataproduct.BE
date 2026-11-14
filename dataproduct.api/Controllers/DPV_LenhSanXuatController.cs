using dataproduct.api.Services;
using Microsoft.AspNetCore.Mvc;

namespace dataproduct.api.Controllers
{
    /// <summary>Quản trị Lệnh sản xuất — Xưởng Đúc phôi vuông NM.HRC1.</summary>
    [Route("api/[controller]")]
    [ApiController]
    public class DPV_LenhSanXuatController : ControllerBase
    {
        private readonly DPV_LenhSanXuatService _service;

        public DPV_LenhSanXuatController(DPV_LenhSanXuatService service)
        {
            _service = service;
        }

        private string? NguoiThaoTac => User.FindFirst("username")?.Value;

        [HttpGet("search")]
        public async Task<IActionResult> Search(string? mac, string? kichThuoc, int? mayDuc, int? idXuong, string? searchKey, bool chiCanhBao = false, int page = 1, int pageSize = 50)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;
            if (pageSize > 500) pageSize = 500;

            var rows = await _service.SearchAsync(mac, kichThuoc, mayDuc, idXuong, searchKey);
            if (chiCanhBao)
                rows = rows.Where(x => x.TrungLSX || x.ChuaCoMaLenh).ToList();

            var totalCount = rows.Count;
            return Ok(new
            {
                data = rows.Skip((page - 1) * pageSize).Take(pageSize),
                totalRecords = totalCount,
                soTrungLSX = rows.Count(x => x.TrungLSX),
                soChuaCoMaLenh = rows.Count(x => x.ChuaCoMaLenh),
                page,
                pageSize,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DPV_LenhSanXuatPayload payload)
        {
            try
            {
                var created = await _service.CreateAsync(payload, NguoiThaoTac);
                return Ok(created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] DPV_LenhSanXuatPayload payload)
        {
            try
            {
                var ok = await _service.UpdateAsync(id, payload, NguoiThaoTac);
                return ok ? NoContent() : NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Xóa mềm (IsDelete = 1)
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _service.DeleteAsync(id, NguoiThaoTac);
            return ok ? NoContent() : NotFound();
        }

        // Thêm các tổ hợp (Mác, KT, Máy đúc) có trong dữ liệu mẻ của khoảng ngày mà chưa có trong danh mục
        [HttpPost("sinh-tu-du-lieu-me")]
        public async Task<IActionResult> SinhTuDuLieuMe(DateOnly tuNgay, DateOnly denNgay, int? idPB, int? idXuong)
        {
            try
            {
                return Ok(await _service.SinhTuDuLieuMeAsync(tuNgay, denNgay, idPB, idXuong, NguoiThaoTac));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}

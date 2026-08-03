using dataproduct.api.DTOs.NMLG_Dto;
using dataproduct.api.Services;
using Microsoft.AspNetCore.Mvc;

namespace dataproduct.api.Controllers
{
    [Route("api/LG_PhanBo")]
    [ApiController]
    public class LG_PhanBoController : ControllerBase
    {
        private readonly PhanBoService _phanBoService;
        private readonly NhomPhanBoService _nhomPhanBoService;
        private readonly TyLePhanBoService _tyLePhanBoService;

        public LG_PhanBoController(
            PhanBoService phanBoService,
            NhomPhanBoService nhomPhanBoService,
            TyLePhanBoService tyLePhanBoService)
        {
            _phanBoService = phanBoService;
            _nhomPhanBoService = nhomPhanBoService;
            _tyLePhanBoService = tyLePhanBoService;
        }

        // ─── Phân bổ (api/LG_PhanBo) ─────────────────────────────────────────

        [HttpPost("tinh")]
        public async Task<IActionResult> Tinh([FromBody] TinhPhanBoRequestDto dto)
        {
            try
            {
                await _phanBoService.TinhPhanBoAsync(dto.Ngay, dto.IdNguoiThucThi);
                return Ok(new { message = "Đã tính lại phân bổ." });
            }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("get-ket-qua")]
        public async Task<IActionResult> GetKetQua(
            [FromQuery] DateTime ngay,
            [FromQuery] byte loaiPhanBo,
            [FromQuery] int idLoCao,
            [FromQuery] byte? ca)
        {
            try { return Ok(await _phanBoService.LayKetQuaAsync(ngay, loaiPhanBo, idLoCao, ca)); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("get-ket-qua-than-coc")]
        public async Task<IActionResult> GetKetQuaThanCoc(
            [FromQuery] DateTime ngay,
            [FromQuery] int idLoCao,
            [FromQuery] byte? ca)
        {
            try { return Ok(await _phanBoService.LayKetQuaThanCocAsync(ngay, idLoCao, ca)); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPost("chot")]
        public async Task<IActionResult> Chot([FromBody] ChotPhanBoRequestDto dto)
        {
            try
            {
                await _phanBoService.ChotPhanBoAsync(dto.Ngay, dto.IdNguoiXacNhan);
                return Ok(new { message = "Đã chốt phân bổ." });
            }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("bao-cao")]
        public async Task<IActionResult> BaoCao(
            [FromQuery] DateTime tuNgay,
            [FromQuery] DateTime denNgay,
            [FromQuery] int? idLoCao,
            [FromQuery] byte? loaiPhanBo)
        {
            try { return Ok(await _phanBoService.LayBaoCaoAsync(tuNgay, denNgay, idLoCao, loaiPhanBo)); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPut("ket-qua/ma-cong-doan-chi-phi")]
        public async Task<IActionResult> UpdateMaCongDoanChiPhi([FromBody] UpdateMaCongDoanChiPhiRequestDto dto)
        {
            try
            {
                await _phanBoService.UpdateMaCongDoanChiPhiAsync(dto.Ngay, dto.IdNvl, dto.MaCongDoanChiPhi);
                return Ok(new { message = "Đã cập nhật mã công đoạn chi phí." });
            }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // ─── Nhóm phân bổ (api/LG_PhanBo/nhom) ───────────────────────────────

        [HttpGet("nhom/get-list")]
        public async Task<IActionResult> GetNhomList([FromQuery] byte? loaiPhanBo, [FromQuery] int? idLoCao)
        {
            try { return Ok(await _nhomPhanBoService.GetListAsync(loaiPhanBo, idLoCao)); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPost("nhom/create")]
        public async Task<IActionResult> CreateNhom([FromBody] CreateNhomPhanBoDto dto)
        {
            try { return Ok(await _nhomPhanBoService.CreateAsync(dto)); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPut("nhom/update/{id}")]
        public async Task<IActionResult> UpdateNhom(int id, [FromBody] UpdateNhomPhanBoDto dto)
        {
            try { return Ok(await _nhomPhanBoService.UpdateAsync(id, dto)); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpDelete("nhom/delete/{id}")]
        public async Task<IActionResult> DeleteNhom(int id)
        {
            try
            {
                var ok = await _nhomPhanBoService.DeleteAsync(id);
                return ok ? Ok(new { message = "Đã xóa thành công." }) : NotFound(new { message = $"Không tìm thấy nhóm phân bổ ID={id}" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("nhom/{id}/get-nvl")]
        public async Task<IActionResult> GetNvl(int id, [FromQuery] DateTime ngay, [FromQuery] byte ca)
        {
            try { return Ok(await _nhomPhanBoService.GetNvlByNhomAsync(id, ngay, ca)); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPost("nhom/{id}/add-nvl")]
        public async Task<IActionResult> AddNvl(int id, [FromBody] AddNvlNhomPhanBoDto dto)
        {
            try { return Ok(await _nhomPhanBoService.AddNvlAsync(id, dto)); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpDelete("nhom/{id}/remove-nvl/{idNvl}")]
        public async Task<IActionResult> RemoveNvl(int id, int idNvl, [FromQuery] DateTime ngay, [FromQuery] byte ca)
        {
            try
            {
                var ok = await _nhomPhanBoService.RemoveNvlAsync(id, idNvl, ngay, ca);
                return ok ? Ok(new { message = "Đã xóa thành công." }) : NotFound(new { message = "Không tìm thấy NVL trong nhóm." });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("nhom/ty-le")]
        public async Task<IActionResult> GetTyLeNhom(
            [FromQuery] int idNhomPhanBo, [FromQuery] DateTime ngay, [FromQuery] byte ca, [FromQuery] int idLoCao)
        {
            try { return Ok(await _tyLePhanBoService.GetTyLeNhomAsync(idNhomPhanBo, ngay, ca, idLoCao)); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPost("nhom/ty-le")]
        public async Task<IActionResult> CreateTyLeNhom([FromBody] CreateTyLeNhomDto dto)
        {
            try
            {
                var soLuong = await _tyLePhanBoService.CreateForNhomAsync(dto);
                return Ok(new { message = $"Đã áp dụng % cho {soLuong} NVL trong nhóm." });
            }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPost("nhom/sao-chep")]
        public async Task<IActionResult> SaoChepNhom([FromBody] SaoChepNhomPhanBoRequestDto dto)
        {
            try { return Ok(await _nhomPhanBoService.SaoChepAsync(dto)); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // ─── Tỷ lệ phân bổ (api/LG_PhanBo/ty-le) ─────────────────────────────

        [HttpGet("ty-le/get-history")]
        public async Task<IActionResult> GetTyLeHistory(
            [FromQuery] int idNvl,
            [FromQuery] DateTime? tuNgay,
            [FromQuery] DateTime? denNgay)
        {
            try { return Ok(await _tyLePhanBoService.GetHistoryAsync(idNvl, tuNgay, denNgay)); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPost("ty-le/create")]
        public async Task<IActionResult> CreateTyLe([FromBody] CreateTyLePhanBoDto dto)
        {
            try { return Ok(await _tyLePhanBoService.CreateAsync(dto)); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}

using dataproduct.api.DTOs.NMLG_Dto;
using dataproduct.api.Models;
using dataproduct.api.Services;
using Microsoft.AspNetCore.Mvc;

namespace dataproduct.api.Controllers
{

    [Route("api/PhieuDieuChinh")]
    [ApiController]
    public class LG_PhieuDieuChinhController : ControllerBase
    {
        private readonly LG_PhieuDieuChinhService _service;

        public LG_PhieuDieuChinhController(LG_PhieuDieuChinhService service)
        {
            _service = service;
        }

        // ─── Danh mục ────────────────────────────────────────────────────────

        // Các xưởng của 1 phòng ban — mỗi xưởng 1 tab trong phiếu theo phòng ban (vd NMTKVV)
        [HttpGet("danh-muc-xuong")]
        public async Task<IActionResult> GetDanhMucXuong([FromQuery] int idPhongBan)
        {
            try
            {
                return Ok(await _service.GetDanhMucXuongAsync(idPhongBan));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // ─── Nhập / Xuất từ BBGN ─────────────────────────────────────────────
        // loai = loại điều chỉnh: 1 = Nhập (bên nhận), 3 = Xuất (bên giao) — cùng giá trị LoaiDieuChinh (2 = Nội bộ).
        [HttpGet("bbgn/{loai:int}")]
        public async Task<IActionResult> GetBBGN(
            int loai,
            [FromQuery] DateTime ngay,
            [FromQuery] int? idPhongBan,
            [FromQuery] Guid? idPhieu)
        {
            try
            {
                // idPhieu (phiếu đã lưu): đọc các dòng phiếu đang giữ trong database; bỏ trống: xem trước từ nguồn
                return Ok(await _service.GetBBGNAsync(loai, ngay, idPhongBan, idPhieu));
            }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // "Tải dữ liệu nguồn" của phiếu ĐÃ LƯU: ghi các dòng BBGN Nhập/Xuất vào database cho phiếu (chưa có số điều chỉnh);
        // người dùng điều chỉnh rồi Lưu để cập nhật. Chạy lại thì thêm dòng mới và làm mới mô tả, giữ nguyên số đã điều chỉnh.
        [HttpPost("bbgn/dong-bo/{idPhieu:guid}")]
        public async Task<IActionResult> DongBoBBGN(Guid idPhieu, [FromBody] DongBoLG_PhieuDieuChinhBBGNRequest request)
        {
            try
            {
                return Ok(await _service.DongBoBBGNAsync(idPhieu, request.IdPhongBan, request.Ngay, request.IdNguoiThucHien));
            }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // Lưu số điều chỉnh của 1 bên cho các dòng BBGN vào phiếu. Bên kia đã nhập mà khối lượng khác → 400, không lưu.
        [HttpPut("bbgn/{loai:int}/{idPhieu:guid}")]
        public async Task<IActionResult> SaveBBGN(int loai, Guid idPhieu, [FromBody] SaveLG_PhieuDieuChinhBBGNRequest request)
        {
            try
            {
                await _service.SaveBBGNAsync(loai, idPhieu, request);
                return Ok(new { success = true });
            }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // Bên {loai} reset số điều chỉnh của bên kia cho 1 dòng BBGN (bắt buộc có ghi chú) để tự điều chỉnh lại.
        [HttpPost("bbgn/{loai:int}/{idPhieu:guid}/reset")]
        public async Task<IActionResult> ResetBBGN(int loai, Guid idPhieu, [FromBody] ResetLG_PhieuDieuChinhBBGNRequest request)
        {
            try
            {
                await _service.ResetBBGNAsync(loai, idPhieu, request);
                return Ok(new { success = true });
            }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // ─── Nội bộ - Xuất vào sản xuất (Nạp liệu lò cao) ────────────────────

        // Nguồn Nạp liệu lò cao theo Ngày, chia theo lò cao — dùng cho phiếu MỚI (chưa lưu).
        [HttpGet("nguondulieu/nap-lieu-lo-cao")]
        public async Task<IActionResult> GetNapLieuLoCao(
            [FromQuery] DateTime ngay,
            [FromQuery] int? idPhongBan)
        {
            try
            {
                return Ok(await _service.GetNapLieuLoCaoAsync(ngay, idPhongBan));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // Danh sách chi tiết đã lưu (phần Nội bộ) của 1 Phiếu điều chỉnh (LG_PhieuDieuChinh_ChiTiet).
        // Trả toàn bộ dòng Nội bộ của phiếu; FE chia vào tab xưởng theo IdLoCao (= ID xưởng nhận).
        [HttpGet("chi-tiet/{idPhieu:guid}")]
        public async Task<IActionResult> GetChiTiet(Guid idPhieu)
        {
            try
            {
                return Ok(await _service.GetChiTietByPhieuAsync(idPhieu));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // Lưu từng dòng chi tiết của phiếu (upsert theo Id), xóa mềm các dòng trong deletedIds — gọi mỗi lần Lưu
        [HttpPut("chi-tiet/{idPhieu:guid}")]
        public async Task<IActionResult> ReplaceChiTiet(Guid idPhieu, [FromBody] ReplaceLG_PhieuDieuChinhChiTietRequest request)
        {
            try
            {
                await _service.ReplaceChiTietAsync(idPhieu, request.Items, request.DeletedIds, request.NguoiSua);
                return Ok(await _service.GetChiTietByPhieuAsync(idPhieu));
            }
            catch (ChiTietConflictException ex) { return Conflict(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // Tải dữ liệu nguồn Nạp liệu lò cao vào LG_PhieuDieuChinh_ChiTiet cho phiếu ĐÃ LƯU (LoaiDieuChinh = 2),
        // chia theo lò cao của nhà máy (scope của phiếu).
        [HttpPost("dong-bo/nap-lieu-lo-cao/{idPhieu:guid}")]
        public async Task<IActionResult> SyncTuNapLieuLoCao(Guid idPhieu, [FromBody] SyncLG_PhieuDieuChinhTuBBGNRequest? request)
        {
            try
            {
                var result = await _service.SyncChiTietFromNapLieuLoCaoAsync(idPhieu, request?.NguoiThucHien, request?.IdPhongBan);
                return Ok(result);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}

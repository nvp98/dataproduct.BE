using dataproduct.api.DTOs.NMLG_Dto;
using dataproduct.api.Services;
using Microsoft.AspNetCore.Mvc;

namespace dataproduct.api.Controllers
{
    /// <summary>
    /// Phiếu điều chỉnh số liệu NM.LG.
    /// Base route: /api/PhieuDieuChinh
    /// </summary>
    [Route("api/PhieuDieuChinh")]
    [ApiController]
    public class LG_PhieuDieuChinhController : ControllerBase
    {
        private readonly LG_PhieuDieuChinhService _service;

        public LG_PhieuDieuChinhController(LG_PhieuDieuChinhService service)
        {
            _service = service;
        }

        // Danh sách chi tiết BBGN (gồm Tên NVL) theo Ngày/Ca/Kíp — nguồn dữ liệu cho Phiếu điều chỉnh.
        [HttpGet("get-bbgn")]
        public async Task<IActionResult> GetBBGN(
            [FromQuery] DateTime ngay,
            [FromQuery] int? ca,
            [FromQuery] string? kip)
        {
            try
            {
                if (ca.HasValue && ca != 1 && ca != 2)
                    return BadRequest(new { message = "ca chỉ nhận giá trị 1 hoặc 2." });

                return Ok(await _service.GetBBGNAsync(ngay, ca, kip));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // Danh sách chi tiết Nạp liệu lò cao (gồm Tên NVL) theo Ngày/Ca/Kíp — nguồn "Nội bộ -
        // Xuất SX" cho Phiếu điều chỉnh.
        [HttpGet("get-naplieulocao")]
        public async Task<IActionResult> GetNapLieuLoCao(
            [FromQuery] DateTime ngay,
            [FromQuery] int? ca,
            [FromQuery] string? kip)
        {
            try
            {
                if (ca.HasValue && ca != 1 && ca != 2)
                    return BadRequest(new { message = "ca chỉ nhận giá trị 1 hoặc 2." });

                return Ok(await _service.GetNapLieuLoCaoAsync(ngay, ca, kip));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // Gộp cả 2 nguồn (SP_Get_BBGN + Sp_GetNVLNapLieuLoCao) theo Ngày/Ca/Kíp — dùng cho phiếu
        // MỚI (chưa lưu): 1 lần gọi nạp dữ liệu cho cả 2 Tab (Nhập - Xuất / Nội bộ - Xuất SX).
        [HttpGet("get-nguon")]
        public async Task<IActionResult> GetNguon(
            [FromQuery] DateTime ngay,
            [FromQuery] int? ca,
            [FromQuery] string? kip)
        {
            try
            {
                if (ca.HasValue && ca != 1 && ca != 2)
                    return BadRequest(new { message = "ca chỉ nhận giá trị 1 hoặc 2." });

                return Ok(await _service.GetNguonAsync(ngay, ca, kip));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // Danh sách chi tiết đã lưu của 1 Phiếu điều chỉnh (LG_PhieuDieuChinh_ChiTiet)
        [HttpGet("{idPhieu}/chi-tiet")]
        public async Task<IActionResult> GetChiTiet(Guid idPhieu)
        {
            try
            {
                return Ok(await _service.GetChiTietByPhieuAsync(idPhieu));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // Ghi đè toàn bộ chi tiết của phiếu (xóa cũ, ghi lại theo danh sách mới) — gọi mỗi lần Lưu
        [HttpPut("{idPhieu}/chi-tiet")]
        public async Task<IActionResult> ReplaceChiTiet(Guid idPhieu, [FromBody] ReplaceLG_PhieuDieuChinhChiTietRequest request)
        {
            try
            {
                await _service.ReplaceChiTietAsync(idPhieu, request.Items, request.NguoiSua);
                return Ok(await _service.GetChiTietByPhieuAsync(idPhieu));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // Tải dữ liệu nguồn từ BBGN và insert thẳng vào LG_PhieuDieuChinh_ChiTiet cho phiếu ĐÃ LƯU
        // (giống luồng "Tải dữ liệu" của Nạp liệu lò cao — xem SyncChiTietFromBBGNAsync).
        [HttpPost("{idPhieu}/sync-tu-bbgn")]
        public async Task<IActionResult> SyncTuBBGN(Guid idPhieu, [FromBody] SyncLG_PhieuDieuChinhTuBBGNRequest? request)
        {
            try
            {
                var result = await _service.SyncChiTietFromBBGNAsync(idPhieu, request?.NguoiThucHien);
                return Ok(result);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // Tải dữ liệu nguồn từ Nạp liệu lò cao và insert thẳng vào LG_PhieuDieuChinh_ChiTiet cho
        // phiếu ĐÃ LƯU (LoaiDieuChinh = 2, "Nội bộ - Xuất SX") — song song với sync-tu-bbgn.
        [HttpPost("{idPhieu}/sync-tu-naplieulocao")]
        public async Task<IActionResult> SyncTuNapLieuLoCao(Guid idPhieu, [FromBody] SyncLG_PhieuDieuChinhTuBBGNRequest? request)
        {
            try
            {
                var result = await _service.SyncChiTietFromNapLieuLoCaoAsync(idPhieu, request?.NguoiThucHien);
                return Ok(result);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // Gộp cả 2 nguồn cho phiếu ĐÃ LƯU — 1 lần bấm "Tải dữ liệu nguồn" đồng bộ cả BBGN lẫn
        // Nạp liệu lò cao vào LG_PhieuDieuChinh_ChiTiet (mỗi nguồn chỉ chạm đúng dòng của nó).
        [HttpPost("{idPhieu}/sync-tu-nguon")]
        public async Task<IActionResult> SyncTuNguon(Guid idPhieu, [FromBody] SyncLG_PhieuDieuChinhTuBBGNRequest? request)
        {
            try
            {
                var result = await _service.SyncChiTietFromNguonAsync(idPhieu, request?.NguoiThucHien);
                return Ok(result);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // Tích/hủy tích xác nhận "Người điều chỉnh giao" cho 1 dòng chi tiết — lưu ngay, không
        // cần chờ Lưu cả phiếu. Chỉ gọi được với dòng đã có Id thật (đã Lưu hoặc đã sync BBGN).
        [HttpPut("chi-tiet/xac-nhan-giao/{id}")]
        public async Task<IActionResult> XacNhanGiao(long id, [FromBody] XacNhanChiTietDto dto)
        {
            try
            {
                var result = await _service.XacNhanGiaoAsync(id, dto);
                if (result == null) return NotFound(new { message = "Không tìm thấy dòng chi tiết." });
                return Ok(result);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // ─── Danh mục NVL (LG_PhieuDieuChinh_NVL) ──────────────────────────────

        [HttpGet("nvl")]
        public async Task<IActionResult> GetNvlList([FromQuery] bool onlyActive = true)
        {
            try
            {
                return Ok(await _service.GetNvlListAsync(onlyActive));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPost("nvl")]
        public async Task<IActionResult> CreateNvl([FromBody] CreateLGPhieuDieuChinhNvlDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.TenNVL))
                    return BadRequest(new { message = "Tên NVL không được để trống." });

                return Ok(await _service.AddNvlAsync(dto));
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPut("nvl/{id}")]
        public async Task<IActionResult> UpdateNvl(int id, [FromBody] UpdateLGPhieuDieuChinhNvlDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.TenNVL))
                    return BadRequest(new { message = "Tên NVL không được để trống." });

                var result = await _service.UpdateNvlAsync(id, dto);
                if (result == null) return NotFound(new { message = "Không tìm thấy NVL." });
                return Ok(result);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpDelete("nvl/{id}")]
        public async Task<IActionResult> DeleteNvl(int id)
        {
            try
            {
                var ok = await _service.DeleteNvlAsync(id);
                if (!ok) return NotFound(new { message = "Không tìm thấy NVL." });
                return Ok(new { success = true });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}

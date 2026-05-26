using dataproduct.api.DTOs;
using dataproduct.api.Services;
using Microsoft.AspNetCore.Mvc;

namespace dataproduct.api.Controllers
{
    /// <summary>
    /// HRC1 - Biên bản giao nhận thép lỏng
    /// Base route: /api/hrc1
    /// userId lấy từ header X-User-Id (int)
    /// </summary>
    [Route("api/hrc1")]
    [ApiController]
    public class HRC1_BBGNController : ControllerBase
    {
        private readonly HRC1_BBGNService _svc;
        private readonly BBGN_ThepLongService _bbgnSvc;

        public HRC1_BBGNController(HRC1_BBGNService svc, BBGN_ThepLongService bbgnSvc)
        {
            _svc = svc;
            _bbgnSvc = bbgnSvc;
        }

        // -------------------------------------------------------
        // Phiếu — dùng chung cho cả 3 công đoạn
        // -------------------------------------------------------

        /// <summary>GET /api/hrc1/phieu/{idPhieu}</summary>
        [HttpGet("phieu/{idPhieu:guid}")]
        public async Task<IActionResult> GetPhieu(Guid idPhieu)
        {
            try
            {
                var result = await _svc.GetPhieuAsync(idPhieu);
                return Ok(result);
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (Exception ex)            { return StatusCode(500, ex.Message); }
        }

        // -------------------------------------------------------
        // Lò thổi
        // -------------------------------------------------------

        /// <summary>POST /api/hrc1/phieu/{idPhieu}/sync-lo-thoi — đồng bộ mẻ thổi từ gang lỏng, trả về phiếu cập nhật</summary>
        [HttpPost("phieu/{idPhieu:guid}/sync-lo-thoi")]
        public async Task<IActionResult> SyncLoThoi(Guid idPhieu)
        {
            try
            {
                var result = await _svc.SyncMeThoiLoThoiAsync(idPhieu);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)      { return NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
            catch (Exception ex)                 { return StatusCode(500, ex.Message); }
        }

        /// <summary>DELETE /api/hrc1/lo-thoi/me/{meId} — xóa cứng mẻ ghost (user xác nhận thủ công)</summary>
        [HttpDelete("lo-thoi/me/{meId:int}")]
        public async Task<IActionResult> XoaMeGhost(int meId)
        {
            try
            {
                await _svc.XoaMeGhostAsync(meId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)      { return NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
            catch (Exception ex)                 { return StatusCode(500, ex.Message); }
        }

        /// <summary>POST /api/hrc1/lo-thoi/fetch-me — load mẻ thổi từ gang lỏng theo ngày/ca/lò</summary>
        [HttpPost("lo-thoi/fetch-me")]
        public async Task<IActionResult> FetchMeThoi([FromBody] HRC1_FetchMeThoiRequest req)
        {
            try
            {
                var result = await _bbgnSvc.FetchMeThoiHRC1Async(req);
                return Ok(result);
            }
            catch (InvalidOperationException ex) { return StatusCode(502, ex.Message); }
            catch (Exception ex)                 { return StatusCode(500, ex.Message); }
        }

        /// <summary>PUT /api/hrc1/lo-thoi/{meId}</summary>
        [HttpPut("lo-thoi/{meId:int}")]
        public async Task<IActionResult> UpdateLoThoi(int meId, [FromBody] HRC1_LoThoiUpdateRequest req)
        {
            try
            {
                await _svc.UpdateMeAsync(meId, req, LayUserId());
                return NoContent();
            }
            catch (KeyNotFoundException ex)      { return NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
            catch (Exception ex)                 { return StatusCode(500, ex.Message); }
        }

        /// <summary>POST /api/hrc1/lo-thoi/{meId}/xac-nhan</summary>
        [HttpPost("lo-thoi/{meId:int}/xac-nhan")]
        public async Task<IActionResult> XacNhanLoThoi(int meId)
        {
            try
            {
                await _svc.XacNhanLoThoiAsync(meId, LayUserId());
                return NoContent();
            }
            catch (KeyNotFoundException ex)      { return NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
            catch (Exception ex)                 { return StatusCode(500, ex.Message); }
        }

        /// <summary>POST /api/hrc1/lo-thoi/{meId}/bo-xac-nhan</summary>
        [HttpPost("lo-thoi/{meId:int}/bo-xac-nhan")]
        public async Task<IActionResult> BoXacNhanLoThoi(int meId)
        {
            try
            {
                await _svc.BoXacNhanLoThoiAsync(meId, LayUserId());
                return NoContent();
            }
            catch (KeyNotFoundException ex)      { return NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
            catch (Exception ex)                 { return StatusCode(500, ex.Message); }
        }

        /// <summary>POST /api/hrc1/lo-thoi/{meId}/lam-moi</summary>
        [HttpPost("lo-thoi/{meId:int}/lam-moi")]
        public async Task<IActionResult> LamMoiLoThoi(int meId)
        {
            try
            {
                await _svc.LamMoiAsync(meId, LayUserId());
                return NoContent();
            }
            catch (KeyNotFoundException ex)      { return NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
            catch (Exception ex)                 { return StatusCode(500, ex.Message); }
        }

        // -------------------------------------------------------
        // Tinh luyện
        // -------------------------------------------------------

        /// <summary>GET /api/hrc1/tinh-luyen/cho-nhan</summary>
        [HttpGet("tinh-luyen/cho-nhan")]
        public async Task<IActionResult> GetChoNhan()
        {
            try
            {
                var result = await _svc.GetChoNhanAsync();
                return Ok(result);
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        /// <summary>GET /api/hrc1/tinh-luyen/me-cho-nhan — paged + filter theo ngày/ca/mẻ</summary>
        [HttpGet("tinh-luyen/me-cho-nhan")]
        public async Task<IActionResult> GetMeChoNhan([FromQuery] HRC1_GetMeChoNhanQuery q)
        {
            try
            {
                var result = await _svc.GetMeChoNhanPagedAsync(q);
                return Ok(result);
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        /// <summary>POST /api/hrc1/tinh-luyen/nhan-me</summary>
        [HttpPost("tinh-luyen/nhan-me")]
        public async Task<IActionResult> NhanMe([FromBody] HRC1_NhanMeRequest req)
        {
            try
            {
                await _svc.NhanMeAsync(req, LayUserId());
                return NoContent();
            }
            catch (KeyNotFoundException ex)      { return NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
            catch (Exception ex)                 { return StatusCode(500, ex.Message); }
        }

        /// <summary>PUT /api/hrc1/tinh-luyen/{mePhanCongId}</summary>
        [HttpPut("tinh-luyen/{mePhanCongId:int}")]
        public async Task<IActionResult> UpdateTinhLuyen(int mePhanCongId, [FromBody] HRC1_TinhLuyenUpdateRequest req)
        {
            try
            {
                await _svc.UpdateMePhanCongAsync(mePhanCongId, req, LayUserId());
                return NoContent();
            }
            catch (KeyNotFoundException ex)      { return NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
            catch (Exception ex)                 { return StatusCode(500, ex.Message); }
        }

        /// <summary>POST /api/hrc1/tinh-luyen/huy-nhan-me</summary>
        [HttpPost("tinh-luyen/huy-nhan-me")]
        public async Task<IActionResult> HuyNhanMe([FromBody] HRC1_HuyNhanMeRequest req)
        {
            try
            {
                await _svc.HuyNhanMeAsync(req, LayUserId());
                return NoContent();
            }
            catch (KeyNotFoundException ex)      { return NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
            catch (Exception ex)                 { return StatusCode(500, ex.Message); }
        }

        /// <summary>POST /api/hrc1/tinh-luyen/them-dong</summary>
        [HttpPost("tinh-luyen/them-dong")]
        public async Task<IActionResult> ThemDong([FromBody] HRC1_ThemDongTLRequest req)
        {
            try
            {
                await _svc.ThemDongAsync(req, LayUserId());
                return NoContent();
            }
            catch (KeyNotFoundException ex)      { return NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
            catch (Exception ex)                 { return StatusCode(500, ex.Message); }
        }

        // -------------------------------------------------------
        // Máy đúc
        // -------------------------------------------------------

        /// <summary>POST /api/hrc1/duc/xac-nhan</summary>
        [HttpPost("duc/xac-nhan")]
        public async Task<IActionResult> XacNhanDuc([FromBody] HRC1_DucXacNhanRequest req)
        {
            try
            {
                await _svc.XacNhanDucAsync(req, LayUserId());
                return NoContent();
            }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
            catch (Exception ex)                 { return StatusCode(500, ex.Message); }
        }

        /// <summary>POST /api/hrc1/duc/bo-xac-nhan</summary>
        [HttpPost("duc/bo-xac-nhan")]
        public async Task<IActionResult> BoXacNhanDuc([FromBody] HRC1_DucBoXacNhanRequest req)
        {
            try
            {
                await _svc.BoXacNhanDucAsync(req, LayUserId());
                return NoContent();
            }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
            catch (Exception ex)                 { return StatusCode(500, ex.Message); }
        }

        // -------------------------------------------------------
        private int LayUserId()
        {
            if (Request.Headers.TryGetValue("X-User-Id", out var val)
                && int.TryParse(val, out var id))
                return id;
            return 0;
        }
    }
}

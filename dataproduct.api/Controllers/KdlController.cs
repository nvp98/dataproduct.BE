using System.Text.Json;
using dataproduct.api.Repositories;
using dataproduct.api.Services.Kdl;
using Microsoft.AspNetCore.Mvc;

namespace dataproduct.api.Controllers
{
    /// <summary>
    /// Kho dữ liệu động (KDL): xem dữ liệu theo cấu hình form (KDL_Form.CotJson), tổng hợp, xuất Excel,
    /// nhập tay, đồng bộ nguồn theo module. Thiết kế: docs/04-Thiet-Ke-Kho-Du-Lieu-Dong.md
    /// </summary>
    [Route("api/Kdl")]
    [ApiController]
    public class KdlController : ControllerBase
    {
        private readonly KdlXemService _xem;
        private readonly KdlSyncService _sync;
        private readonly IKdlRepository _logs;

        public KdlController(KdlXemService xem, KdlSyncService sync, IKdlRepository logs)
        {
            _xem = xem;
            _sync = sync;
            _logs = logs;
        }

        private string? NguoiThaoTac => User.FindFirst("username")?.Value;

        // ── Cấu hình (chỉ đọc) ─────────────────────────────────────────────────

        [HttpGet("form")]
        public async Task<IActionResult> GetForms(string? maModule)
            => Ok(await _xem.GetFormsAsync(maModule));

        [HttpGet("{maModule}/keys")]
        public async Task<IActionResult> GetKeys(string maModule)
            => Ok(await _xem.GetKeysAsync(maModule));

        [HttpGet("form/{maForm}/cau-truc")]
        public async Task<IActionResult> GetCauTruc(string maForm)
        {
            var ct = await _xem.GetCauTrucAsync(maForm);
            return ct == null ? NotFound(new { message = $"Không tìm thấy form {maForm}." }) : Ok(ct);
        }

        // ── Dữ liệu ────────────────────────────────────────────────────────────

        [HttpGet("form/{maForm}/du-lieu")]
        public async Task<IActionResult> GetDuLieu(string maForm, [FromQuery] KdlLocQuery q, int page = 1, int pageSize = 50)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;
            if (pageSize > 1000) pageSize = 1000;

            var kq = await _xem.GetDuLieuAsync(maForm, q.ToLoc(), page, pageSize);
            if (kq == null) return NotFound(new { message = $"Không tìm thấy form {maForm}." });

            var (rows, total, tong) = kq.Value;
            return Ok(new
            {
                data = rows,
                tong,
                totalRecords = total,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling((double)total / pageSize)
            });
        }

        /// <summary>nhom: các chiều nhóm cách nhau dấu phẩy — NgaySX, CaSX, Kip, MayDuc, Mac, KichThuoc, LenhSanXuat.</summary>
        [HttpGet("form/{maForm}/tong-hop")]
        public async Task<IActionResult> GetTongHop(string maForm, [FromQuery] KdlLocQuery q, string? nhom)
        {
            var cotNhom = (nhom ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            try
            {
                var kq = await _xem.GetTongHopAsync(maForm, q.ToLoc(), cotNhom);
                return kq == null ? NotFound(new { message = $"Không tìm thấy form {maForm}." }) : Ok(kq);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("form/{maForm}/export-excel")]
        public async Task<IActionResult> ExportExcel(string maForm, [FromQuery] KdlLocQuery q)
        {
            var loc = q.ToLoc();
            var kq = await _xem.ExportExcelAsync(maForm, loc);
            if (kq == null) return NotFound(new { message = $"Không tìm thấy form {maForm}." });

            var fileName = $"{maForm}_{loc.TuNgay:yyyyMMdd}_{loc.DenNgay:yyyyMMdd}.xlsx";
            return File(kq.Value.Bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        /// <summary>
        /// Tổng hợp chi tiết: dòng đối tượng xen dòng Tổng ca / ngày / tháng / năm.
        /// loaiDuLieu: tatCa (mặc định) | ca | ngay | thang | nam — cấp thấp nhất được hiển thị.
        /// </summary>
        [HttpGet("form/{maForm}/tong-hop-chi-tiet")]
        public async Task<IActionResult> GetTongHopChiTiet(string maForm, [FromQuery] KdlLocQuery q, string? loaiDuLieu)
        {
            var kq = await _xem.GetTongHopChiTietAsync(maForm, q.ToLoc(), loaiDuLieu);
            if (kq == null) return NotFound(new { message = $"Không tìm thấy form {maForm}." });
            return Ok(new { data = kq.Value.Rows, vuotGioiHan = kq.Value.VuotGioiHan });
        }

        [HttpGet("form/{maForm}/tong-hop-chi-tiet/export-excel")]
        public async Task<IActionResult> ExportTongHopChiTiet(string maForm, [FromQuery] KdlLocQuery q, string? loaiDuLieu)
        {
            var loc = q.ToLoc();
            var kq = await _xem.ExportTongHopChiTietExcelAsync(maForm, loc, loaiDuLieu);
            if (kq == null) return NotFound(new { message = $"Không tìm thấy form {maForm}." });

            var fileName = $"{maForm}_THCT_{loc.TuNgay:yyyyMMdd}_{loc.DenNgay:yyyyMMdd}.xlsx";
            return File(kq.Value.Bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        /// <summary>
        /// Tổng hợp theo chiều: mỗi cấp ca / ngày / tháng / năm có dòng "Theo …" từng giá trị chiều + dòng "Tổng …".
        /// chieu: Mac | LenhSanXuat | MayDuc | KichThuoc. loaiDuLieu: tatCa | ca | ngay | thang | nam.
        /// </summary>
        [HttpGet("form/{maForm}/tong-hop-theo-chieu")]
        public async Task<IActionResult> GetTongHopTheoChieu(string maForm, [FromQuery] KdlLocQuery q, string? chieu, string? loaiDuLieu)
        {
            try
            {
                var kq = await _xem.GetTongHopTheoChieuAsync(maForm, q.ToLoc(), chieu, loaiDuLieu);
                if (kq == null) return NotFound(new { message = $"Không tìm thấy form {maForm}." });
                return Ok(new { data = kq.Value.Rows, vuotGioiHan = kq.Value.VuotGioiHan });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("form/{maForm}/tong-hop-theo-chieu/export-excel")]
        public async Task<IActionResult> ExportTongHopTheoChieu(string maForm, [FromQuery] KdlLocQuery q, string? chieu, string? loaiDuLieu)
        {
            var loc = q.ToLoc();
            try
            {
                var kq = await _xem.ExportTongHopTheoChieuExcelAsync(maForm, loc, chieu, loaiDuLieu);
                if (kq == null) return NotFound(new { message = $"Không tìm thấy form {maForm}." });

                var fileName = $"{maForm}_TH_{chieu}_{loc.TuNgay:yyyyMMdd}_{loc.DenNgay:yyyyMMdd}.xlsx";
                return File(kq.Value.Bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>Body: { "MaKey": giá trị | null }. null / "" = xóa nhập tay, quay về số tự động.</summary>
        [HttpPut("form/{maForm}/doi-tuong/{idDoiTuong}/nhap-tay")]
        public async Task<IActionResult> NhapTay(string maForm, int idDoiTuong, [FromBody] Dictionary<string, JsonElement> giaTri)
        {
            try
            {
                var dong = await _xem.NhapTayAsync(maForm, idDoiTuong, giaTri, NguoiThaoTac, HttpContext.RequestAborted);
                return dong == null ? NotFound(new { message = "Không tìm thấy form hoặc đối tượng." }) : Ok(dong);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ── Đồng bộ ────────────────────────────────────────────────────────────

        [HttpPost("{maModule}/dong-bo")]
        public async Task<IActionResult> DongBo(string maModule, DateOnly tuNgay, DateOnly denNgay)
        {
            try
            {
                var log = await _sync.SyncAsync(maModule, tuNgay, denNgay, NguoiThaoTac, HttpContext.RequestAborted);
                return log.ThanhCong ? Ok(log) : BadRequest(new { message = $"Đồng bộ lỗi: {log.LoiChiTiet}", log });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{maModule}/dong-bo/lan-gan-nhat")]
        public async Task<IActionResult> LanGanNhat(string maModule)
            => Ok(await _logs.GetSyncLogGanNhatAsync(maModule));
    }

    public class KdlLocQuery
    {
        public DateOnly TuNgay { get; set; }
        public DateOnly DenNgay { get; set; }
        public byte? CaSX { get; set; }
        public string? Kip { get; set; }
        public string? MayDuc { get; set; }
        public string? Mac { get; set; }
        public string? KichThuoc { get; set; }
        public string? LenhSanXuat { get; set; }
        public string? SearchKey { get; set; }

        public KdlLoc ToLoc() => new()
        {
            TuNgay = TuNgay,
            DenNgay = DenNgay < TuNgay ? TuNgay : DenNgay,
            CaSX = CaSX,
            Kip = Kip?.Trim(),
            MayDuc = MayDuc?.Trim(),
            Mac = Mac?.Trim(),
            KichThuoc = KichThuoc?.Trim(),
            LenhSanXuat = LenhSanXuat?.Trim(),
            SearchKey = SearchKey?.Trim(),
        };
    }
}

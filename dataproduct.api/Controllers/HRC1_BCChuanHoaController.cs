using dataproduct.api.Services;
using Microsoft.AspNetCore.Mvc;

namespace dataproduct.api.Controllers
{
    /// <summary>
    /// 2 bảng tra cứu của form Giao nhận Xưởng Đúc phôi vuông (KDL HRC1_DPV_GN): mapping xưởng → viết tắt, vật tư → Nội dung 1.
    /// Quyền sửa (Xử lý trên KDL_HRC1_DPV_GN) kiểm ở FE như các màn hình khác.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class HRC1_BCChuanHoaController : ControllerBase
    {
        private readonly HRC1_BCChuanHoaService _service;
        private readonly HRC1_GNTongHopNhomService _tongHopNhom;
        private readonly HRC1_GNPhanBoService _phanBo;

        public HRC1_BCChuanHoaController(HRC1_BCChuanHoaService service, HRC1_GNTongHopNhomService tongHopNhom, HRC1_GNPhanBoService phanBo)
        {
            _service = service;
            _tongHopNhom = tongHopNhom;
            _phanBo = phanBo;
        }

        /// <summary>Phân bổ BBGN: chia NVL có cờ phân bổ của ca cho các LSX (+ điều chỉnh tay đã lưu).</summary>
        [HttpGet("phan-bo")]
        public async Task<IActionResult> GetPhanBo(DateOnly tuNgay, DateOnly denNgay)
        {
            if (denNgay < tuNgay) return BadRequest(new { message = "Đến ngày phải sau Từ ngày." });
            return Ok(await _phanBo.GetAsync(tuNgay, denNgay));
        }

        [HttpPut("phan-bo/dieu-chinh")]
        public Task<IActionResult> LuuDieuChinhPhanBo([FromBody] HRC1_PhanBoDieuChinhPayload payload)
            => Chay(async () =>
            {
                await _phanBo.LuuDieuChinhAsync(payload, NguoiThaoTac);
                return Ok();
            });

        [HttpGet("phan-bo/export-excel")]
        public async Task<IActionResult> ExportPhanBo(DateOnly tuNgay, DateOnly denNgay)
        {
            if (denNgay < tuNgay) return BadRequest(new { message = "Đến ngày phải sau Từ ngày." });
            var bytes = await _phanBo.ExportExcelAsync(tuNgay, denNgay);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"PhanBo_BBGN_DPV_{tuNgay:yyyyMMdd}_{denNgay:yyyyMMdd}.xlsx");
        }

        /// <summary>Bảng tổng hợp theo nhóm (Ngày, Ca, Kíp × Chiều / Xưởng / Nhóm 1) từ dữ liệu form Giao nhận.</summary>
        [HttpGet("tong-hop-nhom")]
        public async Task<IActionResult> GetTongHopNhom(DateOnly tuNgay, DateOnly denNgay)
        {
            if (denNgay < tuNgay) return BadRequest(new { message = "Đến ngày phải sau Từ ngày." });
            return Ok(await _tongHopNhom.GetAsync(tuNgay, denNgay));
        }

        [HttpGet("tong-hop-nhom/export-excel")]
        public async Task<IActionResult> ExportTongHopNhom(DateOnly tuNgay, DateOnly denNgay)
        {
            if (denNgay < tuNgay) return BadRequest(new { message = "Đến ngày phải sau Từ ngày." });
            var bytes = await _tongHopNhom.ExportExcelAsync(tuNgay, denNgay);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"TongHopNhom_GiaoNhan_DPV_{tuNgay:yyyyMMdd}_{denNgay:yyyyMMdd}.xlsx");
        }

        private string? NguoiThaoTac => User.FindFirst("username")?.Value;

        [HttpGet("xuong")]
        public async Task<IActionResult> GetXuong() => Ok(await _service.GetXuongAsync());

        [HttpPost("xuong")]
        public Task<IActionResult> CreateXuong([FromBody] HRC1_XuongChuanHoaPayload payload)
            => Chay(async () => Ok(await _service.CreateXuongAsync(payload, NguoiThaoTac)));

        [HttpPut("xuong/{id:int}")]
        public Task<IActionResult> UpdateXuong(int id, [FromBody] HRC1_XuongChuanHoaPayload payload)
            => Chay(async () => await _service.UpdateXuongAsync(id, payload, NguoiThaoTac) is { } kq ? Ok(kq) : NotFound());

        [HttpGet("nvl")]
        public async Task<IActionResult> GetNvl() => Ok(await _service.GetNvlAsync());

        [HttpPost("nvl")]
        public Task<IActionResult> CreateNvl([FromBody] HRC1_NVLChuanHoaPayload payload)
            => Chay(async () => Ok(await _service.CreateNvlAsync(payload, NguoiThaoTac)));

        [HttpPut("nvl/{id:int}")]
        public Task<IActionResult> UpdateNvl(int id, [FromBody] HRC1_NVLChuanHoaPayload payload)
            => Chay(async () => await _service.UpdateNvlAsync(id, payload, NguoiThaoTac) is { } kq ? Ok(kq) : NotFound());

        /// <summary>Xưởng / vật tư đang ra N/A trên form Giao nhận trong khoảng ngày.</summary>
        [HttpGet("chua-map")]
        public async Task<IActionResult> GetChuaMap(DateOnly tuNgay, DateOnly denNgay)
        {
            if (denNgay < tuNgay) return BadRequest(new { message = "Đến ngày phải sau Từ ngày." });
            return Ok(await _service.GetChuaMapAsync(tuNgay, denNgay));
        }

        private static async Task<IActionResult> Chay(Func<Task<IActionResult>> f)
        {
            try { return await f(); }
            catch (InvalidOperationException ex) { return new BadRequestObjectResult(new { message = ex.Message }); }
        }
    }
}

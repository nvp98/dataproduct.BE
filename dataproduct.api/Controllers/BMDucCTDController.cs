using dataproduct.api.DTOs.CTD_Dto;
using dataproduct.api.Services;
using Humanizer;
using Microsoft.AspNetCore.Mvc;
using static dataproduct.api.DTOs.CTD_Dto.PhoinhapkhoDto;

namespace dataproduct.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BMDucCTDController : ControllerBase
    {
        private readonly BMDucCTDService _service;
        private readonly PheDuyetService _pdservice;

        public BMDucCTDController(BMDucCTDService service, PheDuyetService pdservice)
        {
            _service = service;
            _pdservice = pdservice;


        }
        [HttpGet("sanluongphoithep")]
        public async Task<IActionResult> GetSanLuongPhoi([FromQuery] string ca,[FromQuery] string kip,[FromQuery] DateTime ngaySX)
        {
            var data = await _service.GetByKipNgayAsync(ca,kip, ngaySX);
            return Ok(data);
        }
        [HttpGet("Getphoinhapkho")]
        public async Task<IActionResult> GetPhoiNhapKho([FromQuery] string ca, [FromQuery] string kip, [FromQuery] DateTime ngaySX, [FromQuery] int mayduc)
        {
            var data = await _service.GetPhoiNhapKhoAsync(ca, kip, ngaySX,mayduc);
            return Ok(data);
        }
        [HttpGet("export-sanluong-pdf")]
        public async Task<IActionResult> ExportSanLuongPdf(DateOnly? NgaySX,int? Ca, string? Kip, Guid? idPhieu)
        {
            if (idPhieu == null)
                return BadRequest("Thiếu idPhiếu");

            // 1. Lấy danh sách phê duyệt + chữ ký
            var pheDuyets = await _pdservice.GetPheDuyetPhieuAsync(idPhieu.Value);

            // 2. Export PDF (truyền luôn chữ ký vào)
            var file = await _service.ExportPdfSanLuongAsync(
                NgaySX,
                Ca,
                Kip,
                idPhieu.Value,
                pheDuyets
            );

            return File(file.Content, file.ContentType, file.FileName);
        }

        [HttpPost("InsertSanLuongPhoi")]
        public async Task<IActionResult> InsertSanLuongPhoi([FromBody] SaveSanLuongPhoiDto dto)
        {
            await _service.InsertSanLuongPhoiAsync(dto);
            return Ok();
        }

        [HttpDelete("DeleteSanLuongPhoi/{idPhieu}")]
        public async Task<IActionResult> DeleteSaLuongPhoiByPhieu(Guid idPhieu)
        {
            if (idPhieu == Guid.Empty)
                return BadRequest("IdPhieu không hợp lệ");

            await _service.DeleteSanLuongPhoiByPhieuAsync(idPhieu);

            return Ok(new
            {
                success = true,
                message = "Xóa dữ liệu theo phiếu thành công"
            });
        }
        [HttpPost("InsertPhoiNhapKho")]
        public async Task<IActionResult> InsertPhoiNhapKho([FromBody] SavePhoiNhapKhoDto dto)
        {
            await _service.InsertPhoiNhapKhoAsync(dto);
            return Ok();
        }
        [HttpDelete("DeletePhoiNhapKho/{idPhieu}")]

        public async Task<IActionResult> DeletePhoiNhapKhoByPhieu(Guid idPhieu)
        {
            if (idPhieu == Guid.Empty)
                return BadRequest("IdPhieu không hợp lệ");

            await _service.DeletePhoiNhapKhoByPhieuAsync(idPhieu);

            return Ok(new
            {
                success = true,
                message = "Xóa dữ liệu theo phiếu thành công"
            });
        }

        [HttpGet("export-phoinhapkho-pdf")]
        public async Task<IActionResult> ExportPhoiNhapKhoPdf(DateOnly? NgaySX, int? Ca, string? Kip, Guid? idPhieu)
        {
            if (idPhieu == null)
                return BadRequest("Thiếu idPhiếu");

            // 1. Lấy danh sách phê duyệt + chữ ký
            var pheDuyets = await _pdservice.GetPheDuyetPhieuAsync(idPhieu.Value);

            // 2. Export PDF (truyền luôn chữ ký vào)
            var file = await _service.ExportPdfPhoiNhapKhoAsync(
                NgaySX,
                Ca,
                Kip,
                idPhieu.Value,
                pheDuyets
            );

            return File(file.Content, file.ContentType, file.FileName);
        }
    }
}

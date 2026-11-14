using dataproduct.api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace dataproduct.api.Controllers
{
    /// <summary>Danh mục phòng ban / xưởng từ DB master — dùng cho các dropdown chọn BP/NM, Xưởng.</summary>
    [Route("api/[controller]")]
    [ApiController]
    public class DanhMucMasterController : ControllerBase
    {
        private readonly IDanhMucMasterRepository _repo;

        public DanhMucMasterController(IDanhMucMasterRepository repo)
        {
            _repo = repo;
        }

        // Trả cả bản ghi ngưng hoạt động (hoatDong = false) để hiển thị tên cho dữ liệu cũ;
        // dropdown chỉ lấy các dòng hoatDong = true
        [HttpGet("phong-ban")]
        public async Task<IActionResult> GetPhongBan()
            => Ok((await _repo.GetPhongBanAsync())
                .Select(x => new { id = x.ID_PhongBan, x.TenNgan, x.TenPhongBan, hoatDong = x.ID_TrangThai == 1 }));

        [HttpGet("xuong")]
        public async Task<IActionResult> GetXuong()
            => Ok((await _repo.GetXuongAsync())
                .Select(x => new { id = x.ID_Xuong, x.TenXuong, idPhongBan = x.ID_PhongBan, hoatDong = x.ID_TrangThai == 1 }));
    }
}

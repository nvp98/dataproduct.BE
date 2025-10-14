using dataproduct.api.Models.MasterData;
using dataproduct.api.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TaiKhoanController : ControllerBase
    {
        private readonly ProductDataMasterDbContext _context;

        public TaiKhoanController(ProductDataMasterDbContext context)
        {
            _context = context;
        }

        [HttpGet("nguoiky")]
        public async Task<IActionResult> GetNguoiKy([FromQuery] string? maphongBan)
        {
            // viTri có thể là: "NM.CTD", "P.QLCL", "NM.HRC1"

            var query = _context.Tbl_TaiKhoan
                               .Include(x => x.PhongBan) // <-- để join sang bảng phòng ban
                               .AsQueryable();

            //if (!string.IsNullOrEmpty(viTri))
            //{
            //    if (viTri == "NM.CTD")
            //        query = query.Where(x => x.Xuong_API == "NM.CTD");
            //    else if (viTri == "P.QLCL")
            //        query = query.Where(x => x.PhongBan_API == "P.QLCL");
            //    else if (viTri == "NM.HRC1")
            //        query = query.Where(x => x.Xuong_API == "NM.HRC1");
            //}
            if (!string.IsNullOrEmpty(maphongBan))
            {
                query = query.Where(x => x.PhongBan.TenNgan.Contains(maphongBan));
            }

            var list = await query
                            .Select(x => new
                            {
                                x.ID_TaiKhoan,
                                x.HoVaTen,
                                x.TenTaiKhoan,
                                x.PhongBan_API,
                                x.Xuong_API,
                                x.PhongBan.TenNgan,
                                x.ChuKy,
                                TenPhongBan = x.PhongBan != null ? x.PhongBan.TenPhongBan : null
                            })
                            .ToListAsync();

            return Ok(list);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.username) || string.IsNullOrEmpty(request.password))
                return BadRequest(new { message = "Thiếu tên tài khoản hoặc mật khẩu" });

            // 🔒 Mã hóa mật khẩu bằng MD5
            string hashedPassword = SecurityHelper.ToMD5(request.password);

            var user = await _context.Tbl_TaiKhoan
                .Include(x => x.PhongBan)
                .FirstOrDefaultAsync(x =>
                    x.TenTaiKhoan == request.username &&
                    x.MatKhau == hashedPassword);

            if (user == null)
                return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu" });

            // Lưu session (nếu dùng session trên server)
            //HttpContext.Session.SetString("UserID", user.ID_TaiKhoan.ToString());
            //HttpContext.Session.SetString("UserName", user.HoVaTen ?? "");
            //HttpContext.Session.SetString("PhongBan", user.PhongBan?.TenPhongBan ?? "");
            //HttpContext.Session.SetString("Xuong", user.Xuong_API ?? "");

            var result = new
            {
                user.ID_TaiKhoan,
                user.TenTaiKhoan,
                user.HoVaTen,
                user.ChuKy,
                user.PhongBan_API,
                user.PhongBan.TenNgan,
                user.ID_PhanXuong,
                user.ID_PhongBan,
                user.Xuong_API,
                TenPhongBan = user.PhongBan?.TenPhongBan
            };

            return Ok(result);
        }
    }

}

public class LoginRequest
{
    public string username { get; set; } = "";
    public string password { get; set; } = "";
}


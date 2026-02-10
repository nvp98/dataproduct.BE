using dataproduct.api.DTOs.CTD_Dto;
using dataproduct.api.Repositories;

namespace dataproduct.api.Services
{
    public class PheDuyetService
    {
        private readonly IPheDuyetRepository _pheDuyetRepo;
        private readonly IConfiguration _configuration;
        public PheDuyetService(IPheDuyetRepository pheDuyetRepo, IConfiguration configuration)
        {
            _pheDuyetRepo = pheDuyetRepo;
            _configuration = configuration;
        }
        public async Task<List<PheDuyetDto>> GetPheDuyetPhieuAsync(Guid phieuId)
        {
            var pheDuyet = await _pheDuyetRepo.GetBmPheDuyetByPhieuIdAsync(phieuId); ;
            if (!pheDuyet.Any())
            {
                return new List<PheDuyetDto>();
            }
            var nguoiId = pheDuyet
               .Where(x => x.NguoiDuyetId.HasValue)
               .Select(x => x.NguoiDuyetId.Value)
               .Distinct()
               .ToList();

            var taiKhoans = await _pheDuyetRepo.GetTaiKhoanByIdsAsync(nguoiId);
            var phongBans = await _pheDuyetRepo.GetAllPhongBanAsync();
            var viTris = await _pheDuyetRepo.GetAllViTriAsync();
            var ds = pheDuyet.Select(p =>
            {
                var tk = taiKhoans.FirstOrDefault(x => x.ID_TaiKhoan == p.NguoiDuyetId);
                var pb = phongBans.FirstOrDefault(x => x.ID_PhongBan == tk.ID_PhongBan);
                var vt = viTris.FirstOrDefault(x => x.ID_ViTri == tk.ID_ChucVu);
                return new PheDuyetDto
                {
                    CapDuyet = p.CapDuyet,
                    NguoiDuyetID = p.NguoiDuyetId,
                    HoVaTen = tk.HoVaTen,
                    ChuKy =  tk.ChuKy,

                    TenPhongBan = pb?.TenPhongBan,
                    TenViTri = vt?.TenViTri,

                    NgayDuyet = p.NgayDuyet,
                    TinhTrang = p.TinhTrang,
                    GhiChu = p.GhiChu


                };
            }).ToList();
            return ds;
        }

        public string FormatChuKy(string? chuKy)
        {
            if (string.IsNullOrWhiteSpace(chuKy))
                return "";

            // Nếu là base64 image (bắt đầu bằng data:image)
            if (chuKy.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
            {
                return $"<img src=\"{chuKy}\" style=\"max-width: 150px; max-height: 80px;\" />";
            }

            // Nếu là URL (http/https)
            if (chuKy.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return $"<img src=\"{chuKy}\" style=\"max-width: 150px; max-height: 80px;\" />";
            }

            // Nếu là đường dẫn relative (ví dụ: /uploads/chuky/xxx.png)
            if (chuKy.StartsWith("/"))
            {
                // Lấy domain từ config
                var domain = _configuration.GetValue<string>("AppSettings:Domain") ?? "https://report.hoaphatdungquat.vn";

                // Ghép domain với relative path
                var fullUrl = domain.TrimEnd('/') + chuKy;

                return $"<img src=\"{fullUrl}\" style=\"max-width: 150px; max-height: 80px;\" />";
            }

            // Nếu không phải là link/base64, trả về text gốc
            return chuKy;
        }
    }
}

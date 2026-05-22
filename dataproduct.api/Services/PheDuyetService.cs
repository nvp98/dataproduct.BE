using dataproduct.api.DTOs.CTD_Dto;
using dataproduct.api.Models.MasterData;
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

                var pb = phongBans.FirstOrDefault(x => x.ID_PhongBan == tk?.ID_PhongBan);
                var vt = viTris.FirstOrDefault(x => x.ID_ViTri == tk?.ID_ChucVu);

                return new PheDuyetDto
                {
                    CapDuyet = p.CapDuyet,
                    NguoiDuyetID = p.NguoiDuyetId,
                    HoVaTen = tk?.HoVaTen,
                    ChuKy = tk?.ChuKy,
                    TenPhongBan = pb?.TenPhongBan,
                    TenViTri = vt?.TenViTri,
                    NgayDuyet = p.NgayDuyet,
                    TinhTrang = p.TinhTrang,
                    GhiChu = p.GhiChu
                };
            }).ToList();
            return ds;
        }

        public async Task<TaiKhoan> GetNguoiDuyet(int IDNguoiDuyet)
        {
            // Lấy thông tin người duyệt từ repository
            var nguoiDuyet = await _pheDuyetRepo.GetTaiKhoanByIdsAsync([IDNguoiDuyet]);
            if (nguoiDuyet == null || !nguoiDuyet.Any())
            {
                return null;
            }
            return nguoiDuyet[0];
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

        /// <summary>
        /// Khởi tạo luồng duyệt cho một phiếu ở một cấp duyệt cụ thể
        /// </summary>
        /// <param name="phieuId">ID phiếu</param>
        /// <param name="capDuyet">Cấp duyệt (0=Xuống/Factory, 1=QLCL/QC, 2=Đúc/Casting)</param>
        /// <param name="idNguoiDuyet">ID người duyệt</param>
        /// <returns>Thông tin phê duyệt vừa tạo</returns>
        public async Task<PheDuyetDto> InitializePheDuyetAsync(Guid phieuId, int capDuyet, int idNguoiDuyet)
        {
            // Validate đầu vào
            if (phieuId == Guid.Empty)
                throw new ArgumentException("ID phiếu không hợp lệ");

            // if (capDuyet < 0 || capDuyet > 2)
            //     throw new ArgumentException("Cấp duyệt phải từ 0 đến 2");

            if (idNguoiDuyet <= 0)
                throw new ArgumentException("ID người duyệt không hợp lệ");

            // Khởi tạo phê duyệt
            var pheDuyet = await _pheDuyetRepo.InitializePheDuyetAsync(phieuId, capDuyet, idNguoiDuyet);

            // Lấy thông tin người duyệt
            var taiKhoan = await _pheDuyetRepo.GetTaiKhoanByIdsAsync(new List<int> { idNguoiDuyet });
            var nguoiDuyet = taiKhoan.FirstOrDefault();

            if (nguoiDuyet == null)
                throw new Exception($"Không tìm thấy người duyệt với ID: {idNguoiDuyet}");

            // Lấy thông tin phòng ban và vị trí
            var phongBans = await _pheDuyetRepo.GetAllPhongBanAsync();
            var viTris = await _pheDuyetRepo.GetAllViTriAsync();

            var pb = phongBans.FirstOrDefault(x => x.ID_PhongBan == nguoiDuyet.ID_PhongBan);
            var vt = viTris.FirstOrDefault(x => x.ID_ViTri == nguoiDuyet.ID_ChucVu);

            return new PheDuyetDto
            {
                CapDuyet = pheDuyet.CapDuyet,
                NguoiDuyetID = pheDuyet.NguoiDuyetId,
                HoVaTen = nguoiDuyet.HoVaTen,
                ChuKy = nguoiDuyet.ChuKy,
                TenPhongBan = pb?.TenPhongBan,
                TenViTri = vt?.TenViTri,
                NgayDuyet = pheDuyet.NgayDuyet,
                TinhTrang = pheDuyet.TinhTrang,
                GhiChu = pheDuyet.GhiChu
            };
        }
    }
}

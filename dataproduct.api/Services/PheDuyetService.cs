using dataproduct.api.DTOs.CTD_Dto;
using dataproduct.api.Repositories;

namespace dataproduct.api.Services
{
    public class PheDuyetService
    {
        private readonly IPheDuyetRepository _pheDuyetRepo;
        public PheDuyetService(IPheDuyetRepository pheDuyetRepo)
        {
            _pheDuyetRepo = pheDuyetRepo;
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
    }
}

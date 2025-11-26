using dataproduct.api.DTOs;
using dataproduct.api.Models;
using dataproduct.api.Models.MasterData;
using dataproduct.api.Repositories;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace dataproduct.api.Services
{
    public class BmPheDuyetService
    {
        private readonly IBMPheDuyetRepository _repo;
        private readonly ProductDataMasterDbContext _contextMaster;
        private readonly IPhieuRepository _phieuRepo;

        public BmPheDuyetService(IBMPheDuyetRepository repo, ProductDataMasterDbContext Mastercontext, IPhieuRepository phieuRepo)
        {
            _repo = repo;
            _contextMaster = Mastercontext;
            _phieuRepo = phieuRepo;
        }

        public async Task<IEnumerable<BM_PheDuyetDto>> GetAllAsync(int? NguoiDuyetID,int? isCheckDuyet)
        {
            var data = await _repo.GetAllAsync(NguoiDuyetID,isCheckDuyet);
            return data.Select(x => new BM_PheDuyetDto
            {
                Id = x.Id,
                PhieuId = x.PhieuId,
                CapDuyet = x.CapDuyet,
                NguoiDuyetId = x.NguoiDuyetId,
                NgayDuyet = x.NgayDuyet,
                GhiChu = x.GhiChu,
                TinhTrang = x.TinhTrang
            });
        }

        public async Task<BM_PheDuyetDto?> GetByIdAsync(int? id)
        {
            var x = await _repo.GetByIdAsync(id);
            if (x == null) return null;

            var tk = await _contextMaster.Tbl_TaiKhoan
                            .FirstOrDefaultAsync(u => u.ID_TaiKhoan == x.NguoiDuyetId);

            return new BM_PheDuyetDto
            {
                Id = x.Id,
                PhieuId = x.PhieuId,
                CapDuyet = x.CapDuyet,
                NguoiDuyetId = x.NguoiDuyetId,
                TenNguoiDuyet = tk?.HoVaTen ?? "",
                NgayDuyet = x.NgayDuyet,
                GhiChu = x.GhiChu,
                TinhTrang = x.TinhTrang
            };
        }

        public async Task<IEnumerable<BM_PheDuyetDto>?> GetByIdPhieuAsync(Guid id)
        {
            var x = await _repo.GetByIdPhieuAsync(id);
            if (x == null) return null;

            var result = (from pd in x
                          join tk in _contextMaster.Tbl_TaiKhoan
                              on pd.NguoiDuyetId equals tk.ID_TaiKhoan into joined
                          from tk in joined.DefaultIfEmpty()
                              //where pd.NguoiDuyetId == id
                          select new BM_PheDuyetDto
                          {
                              Id = pd.Id,
                              PhieuId = pd.PhieuId,
                              CapDuyet = pd.CapDuyet,
                              NguoiDuyetId = pd.NguoiDuyetId,
                              TenNguoiDuyet = tk != null ? tk.HoVaTen : "",
                              NgayDuyet = pd.NgayDuyet,
                              GhiChu = pd.GhiChu,
                              TinhTrang = pd.TinhTrang
                          });
            return result;
            //return x.Select(x => new BM_PheDuyetDto
            //{
            //    Id = x.Id,
            //    PhieuId = x.PhieuId,
            //    CapDuyet = x.CapDuyet,
            //    NguoiDuyetId = x.NguoiDuyetId,
            //    TenNguoiDuyet = x.,
            //    NgayDuyet = x.NgayDuyet,
            //    GhiChu = x.GhiChu,
            //    TinhTrang = x.TinhTrang,

            //});        
        }


        public async Task<BmPheDuyet> CreateAsync(BmPheDuyet model)
        {
            await _repo.AddAsync(model);
            return model;
        }

        public async Task<bool> UpdateAsync(int? id, BmPheDuyet model)
        {
            var item = await _repo.GetByIdAsync(id);
            if (item == null) return false;
            model.NgayDuyet = DateTime.Now;
            await _repo.UpdateAsync(model);
            return true;
        }

        public async Task<bool> DeleteAsync(int? id)
        {
            var item = await _repo.GetByIdAsync(id);
            if (item == null) return false;
            //await _repo.DeleteAsync(id);
            return true;
        }

        /// <summary>
        /// Cập nhật trạng thái phê duyệt của một user và kiểm tra để cập nhật trạng thái phiếu
        /// </summary>
        /// <param name="phieuId">ID của phiếu</param>
        /// <param name="nguoiDuyetId">ID của người duyệt</param>
        /// <param name="tinhTrang">Trạng thái: 0 = chờ xử lý, 1 = xác nhận, 2 = không xác nhận</param>
        /// <returns>True nếu thành công</returns>
        public async Task<bool> UpdateTinhTrangAsync(Guid phieuId, int nguoiDuyetId, int tinhTrang)
        {
            // 1. Cập nhật tinhTrang của record BM_PheDuyet
            var updated = await _repo.UpdateTinhTrangAsync(phieuId, nguoiDuyetId, tinhTrang);
            if (!updated) return false;

            // 2. Lấy tất cả records BM_PheDuyet của phiếu này
            var allPheDuyet = await _repo.GetByIdPhieuAsync(phieuId);
            if (allPheDuyet == null || !allPheDuyet.Any()) return true;

            // Loại bỏ các cấp duyệt = 0 (người lập phiếu, không tham gia phê duyệt)
            var approverPheDuyet = allPheDuyet
                .Where(x => (x.CapDuyet ?? 0) != 0)
                .ToList();

            if (!approverPheDuyet.Any())
            {
                // Không có ai cần phê duyệt → không cập nhật trạng thái
                return true;
            }

            // 3. Kiểm tra tất cả records cần phê duyệt
            const int PendingStatus = 0;
            const int ApprovedStatus = 1;
            const int RejectedStatus = 2;

            var allApproved = approverPheDuyet.All(x => x.TinhTrang == ApprovedStatus);
            var allRejected = approverPheDuyet.All(x => x.TinhTrang == RejectedStatus);
            var anyApproved = approverPheDuyet.Any(x => x.TinhTrang == ApprovedStatus);
            var allPending = approverPheDuyet.All(x => x.TinhTrang == PendingStatus);

            // 4. Cập nhật trạng thái phiếu nếu cần
            var phieu = await _phieuRepo.GetByIdAsync(phieuId);
            if (phieu == null) return true;

            if (allApproved)
            {
                // Tất cả đều xác nhận → chuyển sang Hoàn thành (2)
                phieu.TinhTrang = 2;
                await _phieuRepo.UpdateAsync(phieu);
            }
            else if (allRejected)
            {
                // Tất cả đều không xác nhận → chuyển sang Không xác nhận (4)
                phieu.TinhTrang = 4;
                await _phieuRepo.UpdateAsync(phieu);
            }
            else if (anyApproved && !allPending)
            {
                // Có người xác nhận nhưng chưa hoàn tất → Đang phê duyệt (6)
                phieu.TinhTrang = 6;
                await _phieuRepo.UpdateAsync(phieu);
            }
            // Nếu tất cả vẫn đang chờ xử lý thì giữ nguyên trạng thái hiện tại

            return true;
        }
    }
}

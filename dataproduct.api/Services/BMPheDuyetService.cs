using dataproduct.api.DTOs;
using dataproduct.api.Models;
using dataproduct.api.Models.MasterData;
using dataproduct.api.Repositories;
using dataproduct.api.Utils;
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
       
        public async Task<bool> UpdateTinhTrangAsync(Guid phieuId, int nguoiDuyetId, int tinhTrang)
        {
            const int PendingStatus = 0;
            const int ApprovedStatus = 1;
            const int RejectedStatus = 2;

            // 1. Lấy phiếu và danh sách phê duyệt hiện tại (trước khi update)
            var phieu = await _phieuRepo.GetByIdAsync(phieuId);
            var allPheDuyet = await _repo.GetByIdPhieuAsync(phieuId);

            if (phieu == null)
            {
                // Nếu không tìm thấy phiếu thì chỉ update BM_PheDuyet và return
                return await _repo.UpdateTinhTrangAsync(phieuId, nguoiDuyetId, tinhTrang);
            }

            EnsurePhieuOperable(phieu);

            // Trường hợp người duyệt chọn Reject (Không xác nhận)
            if (tinhTrang == RejectedStatus)
            {
                if (phieu.ID_PhieuGoc != null)
                {
                    // Phiếu này là bản clone: xóa hẳn khỏi table, mở khóa phiếu cha (ID_PhieuGoc) để hiển thị lại.
                    // Hỗ trợ chuỗi clone nhiều tầng: nếu reject A2 thì mở A1; reject A1 thì mở A...
                    var phieuCha = await _phieuRepo.GetByIdPhieuChaAsync(phieu.ID_PhieuGoc.Value);
                    if (phieuCha != null)
                    {
                        phieuCha.IsLock = 0;
                        await _phieuRepo.UpdateAsync(phieuCha);
                    }
                    await _repo.DeleteByPhieuIdAsync(phieuId);
                    await _phieuRepo.DeleteAsync(phieuId);
                    return true;
                }

                // Phiếu không phải clone: đánh dấu Không xác nhận như cũ
                phieu.IsDelete = 1;
                phieu.TinhTrang = 4;
                await _phieuRepo.UpdateAsync(phieu);
            }

            if (allPheDuyet == null || !allPheDuyet.Any())
            {
                // Nếu không có records thì chỉ update và return
                return await _repo.UpdateTinhTrangAsync(phieuId, nguoiDuyetId, tinhTrang);
            }

            // Loại bỏ các cấp duyệt = 0 (người lập phiếu, không tham gia phê duyệt)
            var approverPheDuyet = allPheDuyet
                .Where(x => (x.CapDuyet ?? 0) != 0)
                .ToList();

            if (!approverPheDuyet.Any())
            {
                // Không có ai cần phê duyệt → chỉ update và return
                return await _repo.UpdateTinhTrangAsync(phieuId, nguoiDuyetId, tinhTrang);
            }

            // 2. Tính toán trạng thái mới sau khi update (giả lập update)

            // Tạo danh sách giả lập sau khi update - tính toán TinhTrang sau khi update
            var simulatedTinhTrangList = approverPheDuyet.Select(x =>
                x.NguoiDuyetId == nguoiDuyetId ? tinhTrang : (x.TinhTrang ?? PendingStatus)
            ).ToList();

            var allApproved = simulatedTinhTrangList.All(x => x == ApprovedStatus);
            var allRejected = simulatedTinhTrangList.All(x => x == RejectedStatus);
            var anyApproved = simulatedTinhTrangList.Any(x => x == ApprovedStatus);
            var allPending = simulatedTinhTrangList.All(x => x == PendingStatus);

            // 4. Check trước khi update BmPheDuyet
            int? newPhieuStatus = null;
            if (allApproved)
            {
                // Tất cả đều xác nhận → chuyển sang Hoàn thành (2)
                PhieuStatusHelper.CheckAllowStatusChange(phieu.TinhTrang ?? 0, 2);
                newPhieuStatus = 2;
            }
            else if (allRejected)
            {
                // Tất cả đều không xác nhận → chuyển sang Không xác nhận (4)
                PhieuStatusHelper.CheckAllowStatusChange(phieu.TinhTrang ?? 0, 4);
                newPhieuStatus = 4;
            }
            else if (anyApproved && !allPending)
            {
                // Có người xác nhận nhưng chưa hoàn tất → Đang phê duyệt (6)
                PhieuStatusHelper.CheckAllowStatusChange(phieu.TinhTrang ?? 0, 6);
                newPhieuStatus = 6;
            }
            // Nếu tất cả vẫn đang chờ xử lý thì giữ nguyên trạng thái hiện tại (không cần check)

            // 5. Nếu check pass thì mới update BmPheDuyet
            var updateResult = await _repo.UpdateTinhTrangAsync(phieuId, nguoiDuyetId, tinhTrang);
            if (!updateResult) return false;

            // 6. Cập nhật trạng thái phiếu nếu cần
            if (newPhieuStatus.HasValue)
            {
                phieu.TinhTrang = newPhieuStatus.Value;
                await _phieuRepo.UpdateAsync(phieu);
            }

            return true;
        }

        private static void EnsurePhieuOperable(BmPhieu phieu)
        {
            if (phieu.IsDelete == 1)
            {
                throw new InvalidOperationException("Phiếu đã bị xóa hoặc từ chối. Vui lòng quay về danh sách để tải lại dữ liệu mới nhất.");
            }

            if (phieu.IsLock == 1)
            {
                throw new InvalidOperationException("Phiếu đã bị khóa do đang có bản hiệu chỉnh. Vui lòng quay về danh sách để mở phiếu hợp lệ.");
            }
        }
    }
}

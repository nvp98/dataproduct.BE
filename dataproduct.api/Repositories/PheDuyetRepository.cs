using dataproduct.api.Models;
using dataproduct.api.Models.MasterData;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public class PheDuyetRepository : IPheDuyetRepository
    {
        private readonly ProductFormContext _context;
        private readonly ProductDataMasterDbContext _dbcontext;
        public PheDuyetRepository(ProductFormContext context, ProductDataMasterDbContext dbcontext)
        {
            _context = context;
            _dbcontext = dbcontext;
        }
        public async Task<List<TaiKhoan>> GetTaiKhoanByIdsAsync(List<int> ids)
        {
            return await _dbcontext.Tbl_TaiKhoan
                .Where(x => ids.Contains(x.ID_TaiKhoan))
                .ToListAsync();
        }

        public async Task<List<PhongBan>> GetAllPhongBanAsync()
        {
            return await _dbcontext.Tbl_PhongBan.ToListAsync();
        }

        public async Task<List<ViTri>> GetAllViTriAsync()
        {
            return await _dbcontext.Tbl_ViTri.ToListAsync();
        }
        public async Task<List<BmPheDuyet>> GetBmPheDuyetByPhieuIdAsync(Guid phieuId)
        {
            return await _context.BmPheDuyets
                .Where(x => x.PhieuId == phieuId)
                .OrderBy(x => x.CapDuyet)
                .ToListAsync();
        }

        public async Task<BmPheDuyet> InitializePheDuyetAsync(Guid phieuId, int capDuyet, int idNguoiDuyet)
        {
            // Kiểm tra xem đã có record phê duyệt cho cấp này chưa
            var existingPheDuyet = await _context.BmPheDuyets
                .FirstOrDefaultAsync(x => x.PhieuId == phieuId && x.CapDuyet == capDuyet && x.NguoiDuyetId == idNguoiDuyet);

            if (existingPheDuyet != null)
            {
                // Cập nhật nếu đã tồn tại
                existingPheDuyet.NguoiDuyetId = idNguoiDuyet;
                existingPheDuyet.TinhTrang = 0; // 0 = Chờ duyệt
                existingPheDuyet.NgayDuyet = null; // Reset ngày duyệt
                _context.BmPheDuyets.Update(existingPheDuyet);
                await _context.SaveChangesAsync();
                return existingPheDuyet;
            }

            // Tạo mới nếu chưa tồn tại
            var newPheDuyet = new BmPheDuyet
            {
                PhieuId = phieuId,
                CapDuyet = capDuyet,
                NguoiDuyetId = idNguoiDuyet,
                TinhTrang = 0, // 0 = Chờ duyệt
                NgayDuyet = null,
                GhiChu = null
            };

            _context.BmPheDuyets.Add(newPheDuyet);
            await _context.SaveChangesAsync();
            return newPheDuyet;
        }
    }
}

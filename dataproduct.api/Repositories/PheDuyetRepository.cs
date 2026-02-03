using dataproduct.api.Models;
using dataproduct.api.Models.MasterData;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public class PheDuyetRepository : IPheDuyetRepository
    {
        private readonly ProductFormContext _context;
        private readonly ProductDataMasterDbContext _dbcontext;
        public PheDuyetRepository(ProductFormContext context)
        {
            _context = context;
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
    }
}

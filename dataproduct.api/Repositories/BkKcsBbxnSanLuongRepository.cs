using dataproduct.api.Models;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public class BkKcsBbxnSanLuongRepository : IBkKcsBbxnSanLuongRepository
    {
        private readonly ProductFormContext _context;

        public BkKcsBbxnSanLuongRepository(ProductFormContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<BkKcsBbxnSanLuong>> GetAllAsync(DateOnly? ngaySX, string? ca, string? sanPham, string? macThep, string? idXuongCan)
        {
            var query = _context.BkKcsBbxnSanLuongs.AsNoTracking().AsQueryable();

            if (ngaySX.HasValue)
                query = query.Where(x => x.NgaySX == ngaySX.Value);

            if (!string.IsNullOrWhiteSpace(ca))
                query = query.Where(x => (x.TenCa != null && x.TenCa.Contains(ca)) || x.Ca == null);

            if (!string.IsNullOrWhiteSpace(sanPham))
                query = query.Where(x => x.SanPham == sanPham);

            if (!string.IsNullOrWhiteSpace(macThep))
                query = query.Where(x => x.MacThep == macThep);

            if (!string.IsNullOrWhiteSpace(idXuongCan))
                query = query.Where(x => x.IDXuongCan == idXuongCan);

            return await query
                .OrderByDescending(x => x.NgaySX)
                .ThenByDescending(x => x.Id)
                .ToListAsync();
        }

        public async Task<BkKcsBbxnSanLuong?> GetByIdAsync(long id)
        {
            return await _context.BkKcsBbxnSanLuongs
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<IEnumerable<BkKcsBbxnSanLuong>> GetByIdPhieuAsync(Guid idPhieu)
        {
            return await _context.BkKcsBbxnSanLuongs
                .Where(x => x.IDPhieu == idPhieu)
                .ToListAsync();
        }

        public async Task UpdateAsync(BkKcsBbxnSanLuong entity)
        {
            _context.BkKcsBbxnSanLuongs.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateRangeAsync(IEnumerable<BkKcsBbxnSanLuong> entities)
        {
            _context.BkKcsBbxnSanLuongs.UpdateRange(entities);
            await _context.SaveChangesAsync();
        }

        public async Task UpdatePhieuInfoAsync(IEnumerable<long> ids, Guid idPhieu, int tinhTrang)
        {
            var entities = await _context.BkKcsBbxnSanLuongs
                .Where(x => ids.Contains(x.Id))
                .ToListAsync();

            foreach (var entity in entities)
            {
                entity.IDPhieu = idPhieu;
                entity.TinhTrang = tinhTrang;
            }

            _context.BkKcsBbxnSanLuongs.UpdateRange(entities);
            await _context.SaveChangesAsync();
        }
    }
}

using dataproduct.api.Models;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public class MacThepRepository : IMacThepRepository
    {
        private readonly ProductFormContext _context;

        public MacThepRepository(ProductFormContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<MacThep>> GetAllAsync(byte? nhaMay, bool? isLock, string? tenMacThep)
        {
            var query = _context.MacTheps.AsQueryable();

            if (nhaMay.HasValue)
                query = query.Where(x => x.NhaMay == nhaMay.Value);
            if (isLock.HasValue)
                query = query.Where(x => x.IsLock == isLock.Value);
            if (!string.IsNullOrWhiteSpace(tenMacThep))
                query = query.Where(x => x.TenMacThep.Contains(tenMacThep));

            return await query.OrderBy(x => x.NhaMay).ThenBy(x => x.TenMacThep).ToListAsync();
        }

        public Task<MacThep?> GetByIdAsync(int id)
            => _context.MacTheps.FirstOrDefaultAsync(x => x.Id == id);

        public async Task AddAsync(MacThep entity)
        {
            _context.MacTheps.Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(MacThep entity)
        {
            _context.MacTheps.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var item = await _context.MacTheps.FirstOrDefaultAsync(x => x.Id == id);
            if (item == null) return;
            _context.MacTheps.Remove(item);
            await _context.SaveChangesAsync();
        }

        public Task<bool> ExistsByTenAsync(string tenMacThep, byte nhaMay, int? excludeId = null)
        {
            var query = _context.MacTheps.Where(x => x.TenMacThep == tenMacThep && x.NhaMay == nhaMay);
            if (excludeId.HasValue)
                query = query.Where(x => x.Id != excludeId.Value);
            return query.AnyAsync();
        }
    }
}


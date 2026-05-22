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

        public async Task<IEnumerable<MacThep>> GetAllAsync(byte? nhaMay, bool? isLock, string? tenMacThep, List<int>? idMayDucs = null)
        {
            var query = _context.MacTheps.AsQueryable();

            if (nhaMay.HasValue)
                query = query.Where(x => x.NhaMay == nhaMay.Value);
            if (isLock.HasValue)
                query = query.Where(x => x.IsLock == isLock.Value);
            if (!string.IsNullOrWhiteSpace(tenMacThep))
                query = query.Where(x => x.TenMacThep.Contains(tenMacThep));
            if (idMayDucs != null && idMayDucs.Count > 0)
                query = query.Where(x => _context.MacThep_MayDucs.Any(m => m.IdMacThep == x.Id && idMayDucs.Contains(m.IdMayDuc)));

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

        public Task<bool> ExistsByTenAndMayDucAsync(string tenMacThep, List<int> idMayDucs, int? excludeId = null)
        {
            if (idMayDucs == null || idMayDucs.Count == 0) return Task.FromResult(false);

            var query = _context.MacThep_MayDucs
                .Where(m => idMayDucs.Contains(m.IdMayDuc) &&
                            _context.MacTheps.Any(mt => mt.Id == m.IdMacThep && mt.TenMacThep == tenMacThep));
            if (excludeId.HasValue)
                query = query.Where(m => m.IdMacThep != excludeId.Value);
            return query.AnyAsync();
        }

    }
}


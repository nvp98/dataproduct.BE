using dataproduct.api.Models;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public interface INL_BTDBenPheRepository
    {
        Task AddRangeAsync(List<NL_BTDBenPhe> entities);
        Task DeleteByPhieuIdAsync(Guid idPhieu);
        Task<List<NL_BTDBenPhe>> GetByPhieuIdAsync(Guid idPhieu);
        Task<List<NL_BTDBenPhe>> GetAllAsync();
    }

    public class NL_BTDBenPheRepository : INL_BTDBenPheRepository
    {
        private readonly ProductFormContext _context;

        public NL_BTDBenPheRepository(ProductFormContext context)
        {
            _context = context;
        }

        public async Task AddRangeAsync(List<NL_BTDBenPhe> entities)
        {
            if (entities == null || entities.Count == 0)
                return;

            await _context.NL_BTDBenPhes.AddRangeAsync(entities);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteByPhieuIdAsync(Guid idPhieu)
        {
            var records = await _context.NL_BTDBenPhes
                .Where(x => x.IDPhieu == idPhieu)
                .ToListAsync();

            if (records.Count > 0)
            {
                _context.NL_BTDBenPhes.RemoveRange(records);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<NL_BTDBenPhe>> GetByPhieuIdAsync(Guid idPhieu)
        {
            return await _context.NL_BTDBenPhes
                .Where(x => x.IDPhieu == idPhieu)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<NL_BTDBenPhe>> GetAllAsync()
        {
            return await _context.NL_BTDBenPhes
                .AsNoTracking()
                .ToListAsync();
        }
    }
}

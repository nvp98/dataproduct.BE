using dataproduct.api.Models;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public class CtdPhoiNguoiRepository : ICtdPhoiNguoiRepository
    {
        private readonly ProductFormContext _context;

        public CtdPhoiNguoiRepository(ProductFormContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CtdPhoiNguoi>> GetByPhieuIdAsync(Guid phieuId)
        {
            return await _context.CtdPhoiNguois
                .Where(x => x.PhieuId == phieuId)
                .ToListAsync();
        }

        public async Task AddAsync(CtdPhoiNguoi entity)
        {
            _context.CtdPhoiNguois.Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task AddListAsync(List<CtdPhoiNguoi> entities)
        {
            await _context.CtdPhoiNguois.AddRangeAsync(entities);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteByPhieuIdAsync(Guid phieuId)
        {
            var entities = await _context.CtdPhoiNguois
                .Where(x => x.PhieuId == phieuId)
                .ToListAsync();
            _context.CtdPhoiNguois.RemoveRange(entities);
            await _context.SaveChangesAsync();
        }
    }
}

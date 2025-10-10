using dataproduct.api.Models;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public class PhieuRepository : IPhieuRepository
    {
        private readonly ProductFormContext _context;

        public PhieuRepository(ProductFormContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<BmPhieu>> GetAllAsync()
        {
            return await _context.BmPhieus.Where(x => x.IsDelete != 1).ToListAsync();
        }

        public async Task<BmPhieu?> GetByIdAsync(Guid id)
        {
            return await _context.BmPhieus.FirstOrDefaultAsync(x => x.Idphieu == id);
        }

        public async Task AddAsync(BmPhieu entity)
        {
            _context.BmPhieus.Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(BmPhieu entity)
        {
            _context.BmPhieus.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var item = await _context.BmPhieus.FindAsync(id);
            if (item != null)
            {
                _context.BmPhieus.Remove(item);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.BmPhieus.AnyAsync(e => e.Idphieu == id);
        }
    }
}

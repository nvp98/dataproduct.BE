using dataproduct.api.Models;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public class BKPhoiThepRepository : IBKPhoiThepRepository
    {
        private readonly ProductFormContext _context;

        public BKPhoiThepRepository(ProductFormContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<BkPhoiThep>> GetAllAsync()
        {
            return await _context.BkPhoiThep.ToListAsync();
        }

        public async Task<BkPhoiThep?> GetByIdAsync(int id)
        {
            return await _context.BkPhoiThep.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task AddAsync(BkPhoiThep entity)
        {
            _context.BkPhoiThep.Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(BkPhoiThep entity)
        {
            _context.BkPhoiThep.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var item = await _context.BmPhieus.FindAsync(id);
            if (item != null)
            {
                _context.BmPhieus.Remove(item);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.BkPhoiThep.AnyAsync(e => e.Id == id);
        }
    }
}

using dataproduct.api.Models;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public class BMPheDuyetRepository : IBMPheDuyetRepository
    {
        private readonly ProductFormContext _context;

        public BMPheDuyetRepository(ProductFormContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<BmPheDuyet>> GetAllAsync()
        {
            return await _context.BmPheDuyets.ToListAsync();
        }

        public async Task<BmPheDuyet?> GetByIdAsync(int id)
        {
            return await _context.BmPheDuyets.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task AddAsync(BmPheDuyet entity)
        {
            _context.BmPheDuyets.Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(BmPheDuyet entity)
        {
            _context.BmPheDuyets.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var item = await _context.BmPheDuyets.FindAsync(id);
            if (item != null)
            {
                _context.BmPheDuyets.Remove(item);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.BmPheDuyets.AnyAsync(e => e.Id == id);
        }

    }
}

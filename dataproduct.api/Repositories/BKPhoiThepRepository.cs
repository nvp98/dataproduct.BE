using dataproduct.api.Models;
using Microsoft.AspNetCore.Mvc;
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

        public async Task<IEnumerable<BkPhoiThep>> GetAllAsync(DateOnly? NgaySX,int? Ca, string? Kip)
        {
            var query = _context.BkPhoiThep.AsQueryable();

            if (NgaySX.HasValue)
                query = query.Where(x => x.NgaySx == NgaySX);

            if (Ca.HasValue)
                query = query.Where(x => x.Ca == Ca.Value);

            if (!string.IsNullOrEmpty(Kip))
                query = query.Where(x => x.Kip == Kip);

            return await query.ToListAsync();
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

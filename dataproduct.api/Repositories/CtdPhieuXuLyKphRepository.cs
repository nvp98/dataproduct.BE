using dataproduct.api.Models;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public class CtdPhieuXuLyKphRepository : ICtdPhieuXuLyKphRepository
    {
        private readonly ProductFormContext _context;

        public CtdPhieuXuLyKphRepository(ProductFormContext context)
        {
            _context = context;
        }

        public async Task AddRangeAsync(List<CtdPhieuXuLyKph> entities)
        {
            if (entities.Count == 0)
                return;

            await _context.CtdPhieuXuLyKphs.AddRangeAsync(entities);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteByIdPhieuAsync(Guid idPhieu)
        {
            var entitiesToDelete = _context.CtdPhieuXuLyKphs
                .Where(x => x.IdPhieu == idPhieu)
                .ToList();

            if (entitiesToDelete.Count > 0)
            {
                _context.CtdPhieuXuLyKphs.RemoveRange(entitiesToDelete);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<CtdPhieuXuLyKph>> GetByIdPhieuAsync(Guid idPhieu)
        {
            return await _context.CtdPhieuXuLyKphs
                .Where(x => x.IdPhieu == idPhieu)
                .OrderBy(x => x.Id)
                .ToListAsync();
        }
    }
}

using dataproduct.api.Models;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public class Hrc1PhuLieuNmRepository : IHrc1PhuLieuNmRepository
    {
        private readonly ProductFormContext _context;

        public Hrc1PhuLieuNmRepository(ProductFormContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Hrc1PhuLieuNm>> GetAllAsync(bool? dangSuDung, string? searchKey)
        {
            var query = _context.Hrc1PhuLieuNms.AsQueryable();

            if (dangSuDung.HasValue)
                query = query.Where(x => x.DangSuDung == dangSuDung.Value);
            if (!string.IsNullOrWhiteSpace(searchKey))
                query = query.Where(x => x.TenPhuLieu.Contains(searchKey));

            return await query
                .OrderBy(x => x.ThuTu ?? int.MaxValue)
                .ThenBy(x => x.ID)
                .ToListAsync();
        }

        public Task<Hrc1PhuLieuNm?> GetByIdAsync(int id)
            => _context.Hrc1PhuLieuNms.FirstOrDefaultAsync(x => x.ID == id);

        public async Task AddAsync(Hrc1PhuLieuNm entity)
        {
            _context.Hrc1PhuLieuNms.Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Hrc1PhuLieuNm entity)
        {
            _context.Hrc1PhuLieuNms.Update(entity);
            await _context.SaveChangesAsync();
        }

        public Task<bool> ExistsByTenPhuLieuAsync(string tenPhuLieu, int? excludeId = null)
        {
            var query = _context.Hrc1PhuLieuNms.Where(x => x.TenPhuLieu == tenPhuLieu);
            if (excludeId.HasValue)
                query = query.Where(x => x.ID != excludeId.Value);
            return query.AnyAsync();
        }
    }
}

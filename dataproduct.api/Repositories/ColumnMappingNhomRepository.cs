using dataproduct.api.Models;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public class ColumnMappingNhomRepository : IColumnMappingNhomRepository
    {
        private readonly ProductFormContext _context;

        public ColumnMappingNhomRepository(ProductFormContext context)
        {
            _context = context;
        }

        public async Task<List<BM_ColumnMappingNhom>> GetAllAsync(int? loCao)
        {
            var query = _context.BM_ColumnMappingNhom.AsQueryable();

            if (loCao.HasValue)
                query = query.Where(x => x.LoCao == loCao.Value);

            return await query.OrderBy(x => x.ThuTu).ToListAsync();
        }

        public async Task<List<BM_ColumnMappingNhom>> GetAllWithColumnsAsync(int loCao)
        {
            return await _context.BM_ColumnMappingNhom
                .Include(n => n.Columns.Where(c => c.IsVisible).OrderBy(c => c.ThuTu))
                .Where(n => n.LoCao == loCao && n.IsVisible)
                .OrderBy(n => n.ThuTu)
                .ToListAsync();
        }

        public async Task<BM_ColumnMappingNhom?> GetByIdAsync(int id)
        {
            return await _context.BM_ColumnMappingNhom.FindAsync(id);
        }

        public async Task AddAsync(BM_ColumnMappingNhom entity)
        {
            await _context.BM_ColumnMappingNhom.AddAsync(entity);
        }

        public Task UpdateAsync(BM_ColumnMappingNhom entity)
        {
            _context.BM_ColumnMappingNhom.Update(entity);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(BM_ColumnMappingNhom entity)
        {
            _context.BM_ColumnMappingNhom.Remove(entity);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}

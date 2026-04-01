using dataproduct.api.Models;
using dataproduct.api.Models.MasterData;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public class ColumnMappingRepository : IColumnMappingRepository
    {
        private readonly ProductFormContext _context;

        public ColumnMappingRepository(ProductFormContext context)
        {
            _context = context;
        }

        public async Task<List<BM_ColumnMapping>> GetAllAsync(int? loCao)
        {
            var query = _context.BM_ColumnMapping
                .Include(x => x.Nhom)
                .AsQueryable();

            if (loCao.HasValue)
                query = query.Where(x => x.Nhom.LoCao == loCao.Value);

            return await query
                .OrderBy(x => x.Nhom.ThuTu)
                .ThenBy(x => x.ThuTu)
                .ToListAsync();
        }

        public async Task<BM_ColumnMapping?> GetByIdAsync(int id)
        {
            return await _context.BM_ColumnMapping
                .Include(x => x.Nhom)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<BM_ColumnMapping>> GetColumnsAsync(int loCao)
        {
            return await _context.BM_ColumnMapping
                .Include(x => x.Nhom)
                .Where(x => x.Nhom.LoCao == loCao && x.IsVisible)
                .OrderBy(x => x.Nhom.ThuTu)
                .ThenBy(x => x.ThuTu)
                .ToListAsync();
        }

        public async Task<bool> HasChildrenAsync(int nhomId)
        {
            return await _context.BM_ColumnMapping.AnyAsync(x => x.NhomId == nhomId);
        }

        public async Task<bool> ExistsDataIndexAsync(int nhomId, string dataIndex, int? excludeId = null)
        {
            return await _context.BM_ColumnMapping.AnyAsync(x =>
                x.NhomId == nhomId &&
                x.DataIndex == dataIndex &&
                (!excludeId.HasValue || x.Id != excludeId.Value)
            );
        }

        public async Task AddAsync(BM_ColumnMapping entity)
        {
            await _context.BM_ColumnMapping.AddAsync(entity);
        }

        public Task UpdateAsync(BM_ColumnMapping entity)
        {
            _context.BM_ColumnMapping.Update(entity);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(BM_ColumnMapping entity)
        {
            _context.BM_ColumnMapping.Remove(entity);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}

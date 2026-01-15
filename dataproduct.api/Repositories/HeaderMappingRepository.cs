using dataproduct.api.Models;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public class HeaderMappingRepository : IHeaderMappingRepository
    {
        private readonly ProductFormContext _context;

        public HeaderMappingRepository(ProductFormContext context)
        {
            _context = context;
        }

        public async Task<Header_Mapping?> GetByIdAsync(int id)
        {
            return await _context.Header_Mappings.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Header_Mapping> AddAsync(Header_Mapping entity)
        {
            _context.Header_Mappings.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task UpdateAsync(Header_Mapping entity)
        {
            _context.Header_Mappings.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _context.Header_Mappings.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
            {
                return;
            }

            _context.Header_Mappings.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteByHeaderKeyAsync(int headerKeyId)
        {
            var entities = await _context.Header_Mappings
                .Where(x => x.ID_HeaderKey == headerKeyId)
                .ToListAsync();

            if (!entities.Any())
            {
                return;
            }

            _context.Header_Mappings.RemoveRange(entities);
            await _context.SaveChangesAsync();
        }

        public Task<bool> ExistsAsync(int phuLieuId, int headerKeyId, int? excludeId = null)
        {
            var query = _context.Header_Mappings
                .Where(x => x.ID_PhuLieu == phuLieuId && x.ID_HeaderKey == headerKeyId);

            if (excludeId.HasValue)
            {
                query = query.Where(x => x.Id != excludeId.Value);
            }

            return query.AnyAsync();
        }
    }
}


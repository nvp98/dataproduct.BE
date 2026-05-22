using dataproduct.api.Models;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Services
{
    public class HeaderNhomService
    {
        private readonly ProductFormContext _context;

        public HeaderNhomService(ProductFormContext context)
        {
            _context = context;
        }

        public async Task<(IEnumerable<Header_Nhom> Data, int TotalCount)> SearchAsync(
            string? searchKey, int page, int pageSize)
        {
            var query = _context.Header_Nhoms.AsQueryable();
            if (!string.IsNullOrEmpty(searchKey))
                query = query.Where(x => x.TenHienThi.Contains(searchKey));

            var total = await query.CountAsync();
            var data = await query.OrderBy(x => x.Id)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync();
            return (data, total);
        }

        public async Task<Header_Nhom?> GetByIdAsync(int id) =>
            await _context.Header_Nhoms.FindAsync(id);

        public async Task<Header_Nhom> CreateAsync(Header_Nhom entity)
        {
            entity.Id = 0;
            _context.Header_Nhoms.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> UpdateAsync(int id, Header_Nhom entity)
        {
            var existing = await _context.Header_Nhoms.FindAsync(id);
            if (existing == null) return false;
            existing.TenHienThi = entity.TenHienThi;
            existing.TenNhom = entity.TenNhom;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _context.Header_Nhoms.FindAsync(id);
            if (existing == null) return false;
            _context.Header_Nhoms.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}

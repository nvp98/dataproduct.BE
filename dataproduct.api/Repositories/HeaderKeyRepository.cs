using System;
using dataproduct.api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public class HeaderKeyRepository : IHeaderKeyRepository
    {
        private readonly ProductFormContext _context;

        public HeaderKeyRepository(ProductFormContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Header_Key>> GetAllAsync()
        {
            var query = _context.Header_Keys.AsQueryable();

            return await query.ToListAsync();
        }

        public async Task<Header_Key?> GetByIdAsync(int id)
        {
            return await _context.Header_Keys.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task AddAsync(Header_Key entity)
        {
            if (entity.KeyGuid == Guid.Empty)
            {
                entity.KeyGuid = Guid.NewGuid();
            }
            entity.NgayTao ??= DateTime.Now;
            _context.Header_Keys.Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Header_Key entity)
        {
            _context.Header_Keys.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
                var item = await _context.Header_Keys.FindAsync(id);
            if (item != null)
            {
                _context.Header_Keys.Remove(item);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<(IEnumerable<Header_Key> Data, int TotalCount)> SearchWithPagingAsync(string? searchKey, string? LoaiPhieu, int page, int pageSize)
        {
            var query = _context.Header_Keys.AsQueryable();
            if (!string.IsNullOrEmpty(searchKey))
                query = query.Where(x => x.TenHienThi.ToString().Contains(searchKey));
            if (!string.IsNullOrEmpty(LoaiPhieu))
                query = query.Where(x => x.LoaiPhieu == LoaiPhieu);
            var totalCount = await query.CountAsync();
            var data = await query.OrderByDescending(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (data.ToList(), totalCount);
        }

        public async Task<bool> ExistsByTenHienThiAsync(string tenHienThi, int? excludeId = null)
        {
            var query = _context.Header_Keys.AsQueryable()
                .Where(x => x.TenHienThi == tenHienThi);
            if (excludeId.HasValue)
            {
                query = query.Where(x => x.Id != excludeId.Value);
            }
            return await query.AnyAsync();
        }

        public async Task<bool> IsInUseAsync(int id)
        {
            return await _context.Header_Mappings.AnyAsync(x => x.ID_HeaderKey == id);
        }
    }
}

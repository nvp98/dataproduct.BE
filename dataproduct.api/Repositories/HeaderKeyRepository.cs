using System;
using dataproduct.api.Models;
using dataproduct.api.ResponseModels;
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

        public async Task<(IEnumerable<HeaderKey_ResponseModel> Data, int TotalCount)> SearchWithPagingAsync(string? searchKey, string? LoaiPhieu, int page, int pageSize)
        {
            var query = _context.Header_Keys.AsQueryable();

            if (!string.IsNullOrEmpty(searchKey))
            {
                query = query.Where(x => x.TenHienThi.Contains(searchKey));
            }

            if (!string.IsNullOrEmpty(LoaiPhieu))
            {
                query = query.Where(x => x.LoaiPhieu == LoaiPhieu);
            }

            var totalCount = await query.CountAsync();

            var headerKeys = await query
                .OrderBy(x => x.ThuTu.HasValue ? 0 : 1) // Có ThuTu = 0, null = 1 (đặt null ở sau)
                .ThenBy(x => x.ThuTu) // Sau đó sắp xếp theo giá trị ThuTu tăng dần
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var headerIds = headerKeys.Select(x => x.Id).ToList();

            var mappings = await _context.Header_Mappings
                .Where(m => headerIds.Contains(m.ID_HeaderKey))
                .GroupBy(m => m.ID_HeaderKey)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => g.Select(m => new HeaderMapping_ResponseModel
                    {
                        Id = m.Id,
                        ID_PhuLieu = m.ID_PhuLieu,
                        TenNguonDuLieu = m.TenNguonDuLieu
                    }).ToList());

            var result = headerKeys.Select(h => new HeaderKey_ResponseModel
            {
                Id = h.Id,
                KeyGuid = h.KeyGuid,
                TenHienThi = h.TenHienThi,
                Mota = h.Mota,
                LoaiPhieu = h.LoaiPhieu,
                IsActive = h.IsActive,
                NgayTao = h.NgayTao,
                ThuTu = h.ThuTu.HasValue ? (int?)h.ThuTu.Value : null,
                HeaderMappings = mappings.TryGetValue(h.Id, out var list)
                    ? list
                    : new List<HeaderMapping_ResponseModel>()
            }).ToList();

            return (result, totalCount);
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

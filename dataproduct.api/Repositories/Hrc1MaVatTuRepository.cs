using dataproduct.api.DTOs;
using dataproduct.api.Models;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public class Hrc1MaVatTuRepository : IHrc1MaVatTuRepository
    {
        private readonly ProductFormContext _context;

        public Hrc1MaVatTuRepository(ProductFormContext context)
        {
            _context = context;
        }

        public async Task<(IEnumerable<Hrc1MaVatTuItem> Data, int TotalCount)> SearchAsync(Hrc1MaVatTuSearchRequest req)
        {
            var query = _context.Hrc1MaVatTus.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(req.SearchKey))
                query = query.Where(x => x.MaVatTu.Contains(req.SearchKey));

            var total = await query.CountAsync();
            var data = await query
                .OrderBy(x => x.MaVatTu)
                .Skip((req.Page - 1) * req.PageSize)
                .Take(req.PageSize)
                .Select(x => new Hrc1MaVatTuItem { Id = x.Id, MaVatTu = x.MaVatTu })
                .ToListAsync();

            return (data, total);
        }

        public async Task<Hrc1MaVatTuItem?> GetByIdAsync(int id)
        {
            var x = await _context.Hrc1MaVatTus.FindAsync(id);
            return x == null ? null : new Hrc1MaVatTuItem { Id = x.Id, MaVatTu = x.MaVatTu };
        }

        public async Task<Hrc1MaVatTuItem> CreateAsync(Hrc1MaVatTuUpsertDto dto)
        {
            if (await _context.Hrc1MaVatTus.AnyAsync(x => x.MaVatTu == dto.MaVatTu))
                throw new InvalidOperationException($"Mã vật tư '{dto.MaVatTu}' đã tồn tại.");

            var entity = new Hrc1MaVatTu { MaVatTu = dto.MaVatTu.Trim(), NgayTao = DateTime.Now };
            _context.Hrc1MaVatTus.Add(entity);
            await _context.SaveChangesAsync();
            return new Hrc1MaVatTuItem { Id = entity.Id, MaVatTu = entity.MaVatTu };
        }

        public async Task<Hrc1MaVatTuBulkCreateResult> BulkCreateAsync(List<Hrc1MaVatTuUpsertDto> items)
        {
            var existingList = await _context.Hrc1MaVatTus.Select(x => x.MaVatTu).ToListAsync();
            var existing = new HashSet<string>(existingList, StringComparer.OrdinalIgnoreCase);

            var result = new Hrc1MaVatTuBulkCreateResult();
            var toAdd = new List<Hrc1MaVatTu>();

            foreach (var dto in items)
            {
                var ma = dto.MaVatTu?.Trim();
                if (string.IsNullOrEmpty(ma)) continue;

                if (existing.Contains(ma))
                {
                    result.Skipped++;
                    result.SkippedItems.Add(ma);
                    continue;
                }

                toAdd.Add(new Hrc1MaVatTu { MaVatTu = ma, NgayTao = DateTime.Now });
                existing.Add(ma);
            }

            if (toAdd.Count > 0)
            {
                _context.Hrc1MaVatTus.AddRange(toAdd);
                await _context.SaveChangesAsync();
                result.Created = toAdd.Count;
            }

            return result;
        }

        public async Task<bool> UpdateAsync(int id, Hrc1MaVatTuUpsertDto dto)
        {
            var entity = await _context.Hrc1MaVatTus.FindAsync(id);
            if (entity == null) return false;

            if (await _context.Hrc1MaVatTus.AnyAsync(x => x.MaVatTu == dto.MaVatTu && x.Id != id))
                throw new InvalidOperationException($"Mã vật tư '{dto.MaVatTu}' đã tồn tại.");

            entity.MaVatTu = dto.MaVatTu.Trim();
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.Hrc1MaVatTus.FindAsync(id);
            if (entity == null) return false;
            _context.Hrc1MaVatTus.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}

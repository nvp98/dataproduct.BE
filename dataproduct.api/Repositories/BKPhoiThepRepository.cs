using dataproduct.api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using dataproduct.api.DTOs;

namespace dataproduct.api.Repositories
{
    public class BKPhoiThepRepository : IBKPhoiThepRepository
    {
        private readonly ProductFormContext _context;

        public BKPhoiThepRepository(ProductFormContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<BkPhoiThep>> GetAllAsync(DateOnly? NgaySX, DateOnly? TuNgay, DateOnly? DenNgay, int? Ca, string? Kip, int? LoaiPhoi, int? MayDuc)
        {
            var query = _context.BkPhoiThep.AsQueryable();

            if (NgaySX.HasValue)
                query = query.Where(x => x.NgaySx == NgaySX);

            if (TuNgay.HasValue)
                query = query.Where(x => x.NgaySx >= TuNgay);

            if (DenNgay.HasValue)
                query = query.Where(x => x.NgaySx <= DenNgay);

            if (Ca.HasValue)
                query = query.Where(x => x.Ca == Ca.Value);

            if (!string.IsNullOrEmpty(Kip))
                query = query.Where(x => x.Kip == Kip);
            if (LoaiPhoi.HasValue)
                query = query.Where(x => x.PhanLoaiPhoi == LoaiPhoi);
            if (MayDuc.HasValue)
                query = query.Where(x => x.MayDuc == MayDuc.Value);

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

        public async Task<bool> UpdateStDaChuyenAsync(int id, int stDaChuyen)
        {
            try
            {
                var affected = await _context.BkPhoiThep
                    .Where(x => x.Id == id)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(e => e.StDaChuyen, stDaChuyen));
                return affected > 0;
            }
            catch
            {
                return false;
            }
        }

        public async Task<int> UpdateStDaChuyenRangeAsync(List<BKPhoiThepStUpdate> items)
        {
            if (items == null || items.Count == 0) return 0;
            using var tx = await _context.Database.BeginTransactionAsync();
            var total = 0;
            foreach (var item in items)
            {
                var bkphoi = await _context.BkPhoiThep.FirstOrDefaultAsync(x => x.Id == item.Id);
                if (bkphoi == null) continue;
                int stDaChuyen = bkphoi.StDaChuyen != null ? (int)bkphoi.StDaChuyen : 0;
                stDaChuyen = stDaChuyen + item.ST_DaChuyen;
                var affected = await _context.BkPhoiThep
                    .Where(x => x.Id == item.Id)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(e => e.StDaChuyen, stDaChuyen));
                total += affected;
            }
            await tx.CommitAsync();
            return total;
        }

        public async Task<int?> RevokeStDaChuyenAsync(int id, int soThuHoi)
        {
            var entity = await _context.BkPhoiThep.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null) return null;
            var current = entity.StDaChuyen ?? 0;
            var newVal = current - soThuHoi;
            if (newVal < 0) newVal = 0;
            var affected = await _context.BkPhoiThep
                .Where(x => x.Id == id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(e => e.StDaChuyen, newVal));
            return affected > 0 ? newVal : null;
        }

        public async Task<int> RevokeStDaChuyenRangeAsync(List<BKPhoiThepStRevoke> items)
        {
            if (items == null || items.Count == 0) return 0;
            using var tx = await _context.Database.BeginTransactionAsync();
            var total = 0;
            var ids = items.Select(i => i.Id).ToList();
            var map = items.ToDictionary(i => i.Id, i => i.SoThuHoi);
            var entities = await _context.BkPhoiThep.Where(x => ids.Contains(x.Id)).ToListAsync();
            foreach (var e in entities)
            {
                var soThuHoi = map[e.Id];
                var current = e.StDaChuyen ?? 0;
                var newVal = current - soThuHoi;
                if (newVal < 0) newVal = 0;
                var affected = await _context.BkPhoiThep
                    .Where(x => x.Id == e.Id)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.StDaChuyen, newVal));
                total += affected;
            }
            await tx.CommitAsync();
            return total;
        }

        public async Task DeleteAsync(int id)
        {
            var item = await _context.BkPhoiThep.FindAsync(id);
            if (item != null)
            {
                _context.BkPhoiThep.Remove(item);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.BkPhoiThep.AnyAsync(e => e.Id == id);
        }
    }
}

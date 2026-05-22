using dataproduct.api.Models;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public class BmQuyenXlRepository : IBmQuyenXlRepository
    {
        private readonly ProductFormContext _context;

        public BmQuyenXlRepository(ProductFormContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<BmQuyenXl>> GetAllAsync(int? idTaiKhoan, string? maBm, string? maKhuVuc)
        {
            var query = _context.BmQuyenXls.AsQueryable();

            if (idTaiKhoan.HasValue)
                query = query.Where(x => x.IdTaiKhoan == idTaiKhoan.Value);

            if (!string.IsNullOrEmpty(maBm))
                query = query.Where(x => x.MaBm == maBm);

            if (!string.IsNullOrEmpty(maKhuVuc))
                query = query.Where(x => x.MaKhuVuc == maKhuVuc);

            return await query.ToListAsync();
        }

        public async Task<BmQuyenXl?> GetByIdAsync(int id)
        {
            return await _context.BmQuyenXls.FindAsync(id);
        }

        public async Task<IEnumerable<BmQuyenXl>> GetByTaiKhoanIdAsync(int idTaiKhoan)
        {
            return await _context.BmQuyenXls
                .Where(x => x.IdTaiKhoan == idTaiKhoan)
                .ToListAsync();
        }

        public async Task AddAsync(BmQuyenXl entity)
        {
            await _context.BmQuyenXls.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task AddRangeAsync(List<BmQuyenXl> entities)
        {
            await _context.BmQuyenXls.AddRangeAsync(entities);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(BmQuyenXl entity)
        {
            _context.BmQuyenXls.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                _context.BmQuyenXls.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteRangeAsync(List<int> ids)
        {
            if (ids.Count == 0) return;
            var entities = await _context.BmQuyenXls
                .Where(x => ids.Contains(x.Id))
                .ToListAsync();
            if (entities.Count > 0)
            {
                _context.BmQuyenXls.RemoveRange(entities);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteByTaiKhoanAsync(int idTaiKhoan)
        {
            var entities = await _context.BmQuyenXls
                .Where(x => x.IdTaiKhoan == idTaiKhoan)
                .ToListAsync();
            if (entities.Count > 0)
            {
                _context.BmQuyenXls.RemoveRange(entities);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.BmQuyenXls.AnyAsync(e => e.Id == id);
        }

        public async Task<bool> CheckDuplicateAsync(int? idTaiKhoan, string? maBm, string? maKhuVuc, byte? quyenChucNang, string? khuVucPhu = null, int? excludeId = null)
        {
            var query = _context.BmQuyenXls.AsQueryable();

            if (idTaiKhoan.HasValue)
                query = query.Where(x => x.IdTaiKhoan == idTaiKhoan.Value);
            else
                query = query.Where(x => x.IdTaiKhoan == null);

            if (!string.IsNullOrEmpty(maBm))
                query = query.Where(x => x.MaBm == maBm);
            else
                query = query.Where(x => x.MaBm == null || x.MaBm == "");

            if (!string.IsNullOrEmpty(maKhuVuc))
                query = query.Where(x => x.MaKhuVuc == maKhuVuc);
            else
                query = query.Where(x => x.MaKhuVuc == null || x.MaKhuVuc == "");

            if (quyenChucNang.HasValue)
                query = query.Where(x => x.QuyenChucNang == quyenChucNang.Value);
            else
                query = query.Where(x => x.QuyenChucNang == null);

            if (!string.IsNullOrEmpty(khuVucPhu))
                query = query.Where(x => x.KhuVucPhu == khuVucPhu);
            else
                query = query.Where(x => x.KhuVucPhu == null || x.KhuVucPhu == "");

            if (excludeId.HasValue)
                query = query.Where(x => x.Id != excludeId.Value);

            return await query.AnyAsync();
        }
    }
}

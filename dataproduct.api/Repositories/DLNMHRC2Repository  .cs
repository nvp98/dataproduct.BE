using dataproduct.api.DTOs;
using dataproduct.api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public class DLNMHRC2Repository   : IDLNMHRC2Repository   
    {
        private readonly ProductFormContext _context;
        public DLNMHRC2Repository (ProductFormContext context)
        {
            _context = context;
        }
        public async Task<IEnumerable<DLNM_HRC2>> GetAllAsync(DateTime? Ngay,int? Ca, string? BieuMau, int? Scope)
        {
            var query = _context.DLNM_HRC2s.AsQueryable();

            if (Ngay.HasValue)
                query = query.Where(x => x.Ngay == Ngay.Value.Date);

            if (Ca.HasValue)
                query = query.Where(x => x.Ca == Ca.Value);

            if (!string.IsNullOrEmpty(BieuMau))
                query = query.Where(x => x.BieuMau == BieuMau);

            if (Scope.HasValue)
                query = query.Where(x => x.Scope == Scope.Value);

            return await query.ToListAsync();
        }

        public async Task<DLNM_HRC2?> GetByIdAsync(int id)
        {
            return await _context.DLNM_HRC2s.FirstOrDefaultAsync(x => x.REPORT_NO == id);
        }

        public async Task<IEnumerable<DLNM_HRC2>> GetByReportNoAsync(int reportNo)
        {
            return await _context.DLNM_HRC2s
                .Where(x => x.REPORT_NO == reportNo)
                .OrderBy(x => x.Id)
                .ToListAsync();
        }

        public async Task AddAsync(DLNM_HRC2 entity)
        {
            _context.DLNM_HRC2s.Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(DLNM_HRC2 entity)
        {
            _context.DLNM_HRC2s.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var item = await _context.DLNM_HRC2s.FindAsync(id);
            if (item != null)
            {
                _context.DLNM_HRC2s.Remove(item);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.DLNM_HRC2s.AnyAsync(e => e.REPORT_NO == id);
        }

        public async Task<(IEnumerable<DLNM_HRC2> Data, int TotalCount)> SearchWithPagingAsync(DateTime? NgaySX, int? Ca, string? LoaiBM, int? Scope, int page, int pageSize)
        {
            var query = _context.DLNM_HRC2s.AsQueryable();

            if (NgaySX.HasValue)
                query = query.Where(x => x.Ngay.HasValue && x.Ngay.Value.Date == NgaySX.Value.Date);

            if (Ca.HasValue)
                query = query.Where(x => x.Ca == Ca.Value);

            if (!string.IsNullOrEmpty(LoaiBM))
                query = query.Where(x => x.BieuMau == LoaiBM);

            if (Scope.HasValue)
                query = query.Where(x => x.Scope == Scope.Value);

            // Đếm số lượng REPORT_NO duy nhất
            var totalCount = await query.Select(x => x.REPORT_NO).Distinct().CountAsync();

            // Load tất cả dữ liệu vào memory và group ở đó để tránh lỗi EF Core
            var allData = await query
                .OrderBy(x => x.Id)
                .ToListAsync();

            // Group by REPORT_NO và lấy record đầu tiên của mỗi group
            var groupedData = allData
                .GroupBy(x => x.REPORT_NO)
                .Select(g => g.First())
                .OrderByDescending(x => x.Ngay)
                .ThenByDescending(x => x.REPORT_NO)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return (groupedData, totalCount);
        }
    }
}

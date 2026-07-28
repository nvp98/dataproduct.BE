using dataproduct.api.Models;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public class Hrc1PhuLieuNmRepository : IHrc1PhuLieuNmRepository
    {
        private readonly ProductFormContext _context;

        public Hrc1PhuLieuNmRepository(ProductFormContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Hrc1PhuLieuNm>> GetAllAsync(bool? dangSuDung, string? searchKey)
        {
            var query = _context.Hrc1PhuLieuNms.AsQueryable();

            if (dangSuDung.HasValue)
                query = query.Where(x => x.DangSuDung == dangSuDung.Value);
            if (!string.IsNullOrWhiteSpace(searchKey))
                query = query.Where(x => x.TenPhuLieu.Contains(searchKey));

            return await query
                .OrderBy(x => x.ThuTu ?? int.MaxValue)
                .ThenBy(x => x.ID)
                .ToListAsync();
        }

        public Task<Hrc1PhuLieuNm?> GetByIdAsync(int id)
            => _context.Hrc1PhuLieuNms.FirstOrDefaultAsync(x => x.ID == id);

        public async Task AddAsync(Hrc1PhuLieuNm entity)
        {
            _context.Hrc1PhuLieuNms.Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Hrc1PhuLieuNm entity)
        {
            _context.Hrc1PhuLieuNms.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Hrc1PhuLieuNm entity)
        {
            _context.Hrc1PhuLieuNms.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public Task<bool> ExistsByTenPhuLieuAsync(string tenPhuLieu, int? excludeId = null)
        {
            var query = _context.Hrc1PhuLieuNms.Where(x => x.TenPhuLieu == tenPhuLieu);
            if (excludeId.HasValue)
                query = query.Where(x => x.ID != excludeId.Value);
            return query.AnyAsync();
        }

        public async Task<string?> GetInUseReasonAsync(int id)
        {
            // (1) Phiếu tiêu hao BOF/LF — mẻ đã đo/nhập thực tế dùng phụ liệu này (kể cả mẻ đồng bộ
            // tự động từ NM lẫn nhập tay LF). Đây là nơi phổ biến nhất nên kiểm tra trước.
            if (await _context.Hrc1PhuLieus.AnyAsync(x => x.PhuLieuID == id && x.IsDeleted == false))
                return "đã có mẻ tiêu hao BOF/LF sử dụng phụ liệu này";

            // (2) Sổ Xuất-Nhập-Tồn — chi tiết theo Scope/Silo
            if (await _context.STD_XUAT_NHAP_TON_HRC1s.AnyAsync(x => x.PhuLieuID == id))
                return "đã được theo dõi trong Sổ Xuất-Nhập-Tồn (chi tiết)";

            // (3) Sổ Xuất-Nhập-Tồn — tổng hợp/chênh lệch/phân bổ
            if (await _context.STD_NXT_TOTAL_HRC1s.AnyAsync(x => x.PhuLieuID == id))
                return "đã được theo dõi trong Sổ Xuất-Nhập-Tồn (tổng hợp)";

            return null;
        }
    }
}

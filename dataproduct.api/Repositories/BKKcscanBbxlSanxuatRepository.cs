using dataproduct.api.Models;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public class BKKcscanBbxlSanxuatRepository : IBKKcscanBbxlSanxuatRepository
    {
        private readonly ProductFormContext _context;

        public BKKcscanBbxlSanxuatRepository(ProductFormContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<BkKcscanBbxlSanxuat>> GetAllAsync(DateOnly? ngaySX, string? ca, DateOnly? ngayXL, string? caXL, string? order, int? xuongCan)
        {
            var query = _context.BkKcscanBbxlSanxuats.AsNoTracking().AsQueryable();

            if (ngaySX.HasValue)
                query = query.Where(x => x.ProcessProductionDate == ngaySX.Value);

            if (!string.IsNullOrWhiteSpace(ca))
                query = query.Where(x => x.ProcessShiftName != null && x.ProcessShiftName.Contains(ca));

            if (ngayXL.HasValue)
                query = query.Where(x => x.NgayXL == ngayXL.Value || x.NgayXL == null);

            if (!string.IsNullOrWhiteSpace(caXL))
                query = query.Where(x => (x.CaXL != null && x.CaXL.Contains(caXL)) || x.CaXL == null);

            if (!string.IsNullOrWhiteSpace(order))
                query = query.Where(x => x.Order == order);

            if (xuongCan.HasValue)
                query = query.Where(x => x.XuongCan == xuongCan.Value);

            return await query
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .ToListAsync();
        }

        public async Task<BkKcscanBbxlSanxuat?> GetByIdAsync(long id)
        {
            return await _context.BkKcscanBbxlSanxuats
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
        }
    }
}

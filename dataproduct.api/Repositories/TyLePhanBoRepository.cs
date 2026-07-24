using dataproduct.api.Models;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public class TyLePhanBoRepository : ITyLePhanBoRepository
    {
        private readonly ProductFormContext _context;

        public TyLePhanBoRepository(ProductFormContext context)
        {
            _context = context;
        }

        public async Task<List<LG_PB_TyLePhanBo>> GetHistoryAsync(int idNvl, DateTime? tuNgay, DateTime? denNgay)
        {
            return await _context.LG_PB_TyLePhanBo
                .Where(x => !x.IsDelete && x.IDNVL == idNvl
                    && (tuNgay == null || x.Ngay >= tuNgay)
                    && (denNgay == null || x.Ngay <= denNgay))
                .OrderByDescending(x => x.Ngay)
                .ThenByDescending(x => x.NgayNhap)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Dictionary<int, decimal>> GetHieuLucMapAsync(IEnumerable<int> idNvlList, DateTime ngay, byte? ca)
        {
            var ids = idNvlList.ToList();
            var candidates = await _context.LG_PB_TyLePhanBo
                .Where(x => !x.IsDelete && ids.Contains(x.IDNVL) && x.Ngay == ngay.Date
                    && (x.Ca == ca || x.Ca == null))
                .AsNoTracking()
                .ToListAsync();

            var result = new Dictionary<int, decimal>();
            foreach (var idNvl in ids)
            {
                var rows = candidates.Where(x => x.IDNVL == idNvl).ToList();
                // Ưu tiên bản ghi đúng Ca cụ thể, fallback bản ghi Ca=NULL (áp dụng chung cả ngày)
                var chosen = rows.FirstOrDefault(x => x.Ca == ca) ?? rows.FirstOrDefault(x => x.Ca == null);
                if (chosen != null) result[idNvl] = chosen.TyLe;
            }
            return result;
        }

        public async Task<LG_PB_TyLePhanBo> UpsertAsync(LG_PB_TyLePhanBo entity)
        {
            var existing = await _context.LG_PB_TyLePhanBo.FirstOrDefaultAsync(x =>
                x.IDNVL == entity.IDNVL && x.Ngay == entity.Ngay && x.Ca == entity.Ca);

            if (existing == null)
            {
                entity.NgayNhap = DateTime.Now;
                await _context.LG_PB_TyLePhanBo.AddAsync(entity);
                await _context.SaveChangesAsync();
                return entity;
            }

            existing.TyLe = entity.TyLe;
            existing.GhiChu = entity.GhiChu;
            existing.IDNguoiNhap = entity.IDNguoiNhap;
            existing.NgayNhap = DateTime.Now;
            existing.IsDelete = false;
            await _context.SaveChangesAsync();
            return existing;
        }
    }
}

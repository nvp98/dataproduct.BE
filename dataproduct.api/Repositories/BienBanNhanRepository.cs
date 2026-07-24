using dataproduct.api.Models;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public class BienBanNhanRepository : IBienBanNhanRepository
    {
        private readonly ProductFormContext _context;

        public BienBanNhanRepository(ProductFormContext context)
        {
            _context = context;
        }

        public async Task<List<LG_PB_BienBanNhanQHLCCVH>> GetByNgayAsync(DateTime ngay, byte loaiPhanBo)
        {
            return await _context.LG_PB_BienBanNhanQHLCCVH
                .Where(x => !x.IsDelete && x.Ngay == ngay.Date && x.LoaiPhanBo == loaiPhanBo)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task UpsertAsync(LG_PB_BienBanNhanQHLCCVH entity)
        {
            var existing = await _context.LG_PB_BienBanNhanQHLCCVH.FirstOrDefaultAsync(x =>
                x.Ngay == entity.Ngay && x.Ca == entity.Ca && x.IDLoCao == entity.IDLoCao && x.LoaiPhanBo == entity.LoaiPhanBo);

            if (existing == null)
            {
                entity.NgayNhap = DateTime.Now;
                await _context.LG_PB_BienBanNhanQHLCCVH.AddAsync(entity);
            }
            else
            {
                existing.KhoiLuongNhanVe = entity.KhoiLuongNhanVe;
                existing.GhiChu = entity.GhiChu;
                existing.IDNguoiNhap = entity.IDNguoiNhap;
                existing.NgayNhap = DateTime.Now;
            }
            await _context.SaveChangesAsync();
        }

        public async Task<Dictionary<int, int>> GetMapXuongLoCaoAsync()
        {
            return await _context.LG_PB_Map_Xuong_LoCao
                .AsNoTracking()
                .ToDictionaryAsync(x => x.ID_Xuong, x => x.IDLoCao);
        }
    }
}

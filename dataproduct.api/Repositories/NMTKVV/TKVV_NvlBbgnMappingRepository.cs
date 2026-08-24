using dataproduct.api.DTOs.NMTKVV_Dto;
using dataproduct.api.Models;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories.NMTKVV
{
    public class TKVV_NvlBbgnMappingRepository : ITKVV_NvlBbgnMappingRepository
    {
        private readonly ProductFormContext _context;

        public TKVV_NvlBbgnMappingRepository(ProductFormContext context)
        {
            _context = context;
        }

        // TenVatTu/MaVatTuSap/... được TKVV_NvlBbgnMappingService enrich thêm sau,
        // vì Vật tư nằm ở PRODUCTDATA — DbContext khác, không join được ở đây.
        public async Task<List<TKVVNvlBbgnMappingDto>> GetListAsync(int? tkvvNvlId)
        {
            var query = from m in _context.TKVV_NVL_BBGN_Mapping
                        join nvl in _context.TKVV_NguyenVatLieu on m.TKVV_NVL_ID equals nvl.ID into nvlG
                        from nvl in nvlG.DefaultIfEmpty()
                        where tkvvNvlId == null || m.TKVV_NVL_ID == tkvvNvlId
                        orderby m.NgayTao descending
                        select new TKVVNvlBbgnMappingDto
                        {
                            Id = m.ID,
                            TkvvNvlId = m.TKVV_NVL_ID,
                            TenNVL = nvl != null ? nvl.TenNVL : null,
                            IdVatTuBBGN = m.ID_VatTu_BBGN,
                            TrangThai = m.TrangThai,
                            GhiChu = m.GhiChu,
                            NgayTao = m.NgayTao,
                        };
            return await query.AsNoTracking().ToListAsync();
        }

        public async Task<TKVV_NVL_BBGN_Mapping?> GetByIdAsync(int id)
            => await _context.TKVV_NVL_BBGN_Mapping.FindAsync(id);

        public async Task<TKVV_NVL_BBGN_Mapping> AddAsync(TKVV_NVL_BBGN_Mapping entity)
        {
            entity.NgayTao = DateTime.Now;
            _context.TKVV_NVL_BBGN_Mapping.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<TKVV_NVL_BBGN_Mapping?> UpdateAsync(int id, bool trangThai, string? ghiChu)
        {
            var existing = await _context.TKVV_NVL_BBGN_Mapping.FindAsync(id);
            if (existing == null) return null;

            existing.TrangThai = trangThai;
            existing.GhiChu = ghiChu;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.TKVV_NVL_BBGN_Mapping.FindAsync(id);
            if (entity == null) return false;
            _context.TKVV_NVL_BBGN_Mapping.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}

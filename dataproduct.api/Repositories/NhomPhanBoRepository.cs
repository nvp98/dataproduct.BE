using dataproduct.api.DTOs.NMLG_Dto;
using dataproduct.api.Models;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public class NhomPhanBoRepository : INhomPhanBoRepository
    {
        private readonly ProductFormContext _context;

        public NhomPhanBoRepository(ProductFormContext context)
        {
            _context = context;
        }

        public async Task<List<LG_NhomPhanBo>> GetListAsync(byte? loaiPhanBo)
        {
            return await _context.LG_NhomPhanBo
                .Where(x => !x.IsDelete && (loaiPhanBo == null || x.LoaiPhanBo == loaiPhanBo))
                .OrderBy(x => x.LoaiPhanBo)
                .ThenBy(x => x.ThuTu)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<LG_NhomPhanBo?> GetByIdAsync(int id)
            => await _context.LG_NhomPhanBo.FindAsync(id);

        public async Task<LG_NhomPhanBo> AddAsync(LG_NhomPhanBo entity)
        {
            await _context.LG_NhomPhanBo.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<LG_NhomPhanBo?> UpdateAsync(int id, LG_NhomPhanBo entity)
        {
            var existing = await _context.LG_NhomPhanBo.FindAsync(id);
            if (existing == null) return null;

            existing.TenNhom = entity.TenNhom;
            existing.LoaiPhanBo = entity.LoaiPhanBo;
            existing.PhuongThucPhanBo = entity.PhuongThucPhanBo;
            existing.ThuTu = entity.ThuTu;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _context.LG_NhomPhanBo.FindAsync(id);
            if (existing == null) return false;
            existing.IsDelete = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<NvlNhomPhanBoDto>> GetNvlByNhomAsync(int idNhomPhanBo)
        {
            var thanhVien = await _context.LG_NVL_NhomPhanBo
                .Where(x => !x.IsDelete && x.IDNhomPhanBo == idNhomPhanBo)
                .AsNoTracking()
                .ToListAsync();

            if (thanhVien.Count == 0) return new List<NvlNhomPhanBoDto>();

            var nvlIds = thanhVien.Select(x => x.IDNVL).ToList();
            var tenNvlMap = await _context.LG_NL_NVL
                .Where(x => nvlIds.Contains(x.ID))
                .AsNoTracking()
                .ToDictionaryAsync(x => x.ID, x => x.TenNVL_NM);

            return thanhVien
                .Select(x => new NvlNhomPhanBoDto
                {
                    Id = x.ID,
                    IdNvl = x.IDNVL,
                    TenNvl = tenNvlMap.GetValueOrDefault(x.IDNVL),
                    IdNhomPhanBo = x.IDNhomPhanBo
                })
                .ToList();
        }

        public async Task<LG_NVL_NhomPhanBo> AddNvlAsync(LG_NVL_NhomPhanBo entity)
        {
            await _context.LG_NVL_NhomPhanBo.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> RemoveNvlAsync(int idNhomPhanBo, int idNvl)
        {
            var existing = await _context.LG_NVL_NhomPhanBo
                .FirstOrDefaultAsync(x => x.IDNhomPhanBo == idNhomPhanBo && x.IDNVL == idNvl && !x.IsDelete);
            if (existing == null) return false;
            existing.IsDelete = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<(LG_NhomPhanBo Nhom, List<LG_NVL_NhomPhanBo> ThanhVien)>> GetNhomVaThanhVienAsync(byte loaiPhanBo)
        {
            var nhoms = await _context.LG_NhomPhanBo
                .Where(x => !x.IsDelete && x.LoaiPhanBo == loaiPhanBo)
                .OrderBy(x => x.ThuTu)
                .AsNoTracking()
                .ToListAsync();

            var nhomIds = nhoms.Select(x => x.ID).ToList();
            var thanhViens = await _context.LG_NVL_NhomPhanBo
                .Where(x => !x.IsDelete && nhomIds.Contains(x.IDNhomPhanBo))
                .AsNoTracking()
                .ToListAsync();

            return nhoms
                .Select(n => (n, thanhViens.Where(t => t.IDNhomPhanBo == n.ID).ToList()))
                .ToList();
        }

        public async Task<Dictionary<int, LG_NhomPhanBo>> GetByIdsAsync(IEnumerable<int> ids)
        {
            var idList = ids.Distinct().ToList();
            return await _context.LG_NhomPhanBo
                .Where(x => idList.Contains(x.ID))
                .AsNoTracking()
                .ToDictionaryAsync(x => x.ID, x => x);
        }

        public async Task<Dictionary<int, string?>> GetTenNvlMapAsync(IEnumerable<int> idNvlList)
        {
            var idList = idNvlList.Distinct().ToList();
            return await _context.LG_NL_NVL
                .Where(x => idList.Contains(x.ID))
                .AsNoTracking()
                .ToDictionaryAsync(x => x.ID, x => x.TenNVL_NM);
        }
    }
}

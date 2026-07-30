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

        public async Task<List<LG_PB_NhomPhanBo>> GetListAsync(byte? loaiPhanBo)
        {
            return await _context.LG_PB_NhomPhanBo
                .Where(x => !x.IsDelete && (loaiPhanBo == null || x.LoaiPhanBo == loaiPhanBo))
                .OrderBy(x => x.LoaiPhanBo)
                .ThenBy(x => x.ThuTu)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<LG_PB_NhomPhanBo?> GetByIdAsync(int id)
            => await _context.LG_PB_NhomPhanBo.FindAsync(id);

        public async Task<LG_PB_NhomPhanBo> AddAsync(LG_PB_NhomPhanBo entity)
        {
            await _context.LG_PB_NhomPhanBo.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<LG_PB_NhomPhanBo?> UpdateAsync(int id, LG_PB_NhomPhanBo entity)
        {
            var existing = await _context.LG_PB_NhomPhanBo.FindAsync(id);
            if (existing == null) return null;

            existing.TenNhom = entity.TenNhom;
            existing.LoaiPhanBo = entity.LoaiPhanBo;
            existing.PhuongThucPhanBo = entity.PhuongThucPhanBo;
            existing.MaVatTu = entity.MaVatTu;
            existing.ThuTu = entity.ThuTu;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _context.LG_PB_NhomPhanBo.FindAsync(id);
            if (existing == null) return false;
            existing.IsDelete = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<NvlNhomPhanBoDto>> GetNvlByNhomAsync(int idNhomPhanBo, DateTime ngay, byte ca)
        {
            var thanhVien = await _context.LG_PB_NVL_NhomPhanBo
                .Where(x => !x.IsDelete && x.IDNhomPhanBo == idNhomPhanBo && x.Ngay == ngay.Date && x.Ca == ca)
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
                    IdNhomPhanBo = x.IDNhomPhanBo,
                    Ngay = x.Ngay,
                    Ca = x.Ca,
                    IdLoCao = x.IDLoCao
                })
                .ToList();
        }

        public async Task<LG_PB_NVL_NhomPhanBo> AddNvlAsync(LG_PB_NVL_NhomPhanBo entity)
        {
            // UNIQUE (IDNVL, IDNhomPhanBo, Ngay, Ca) không phân biệt IsDelete — nếu NVL này đã từng được
            // thêm rồi xóa (IsDelete=true) đúng (Nhóm, Ngày, Ca) này thì phải khôi phục lại dòng cũ,
            // không được insert dòng mới (sẽ vi phạm unique constraint và crash).
            var existing = await _context.LG_PB_NVL_NhomPhanBo.FirstOrDefaultAsync(x =>
                x.IDNVL == entity.IDNVL && x.IDNhomPhanBo == entity.IDNhomPhanBo
                && x.Ngay == entity.Ngay && x.Ca == entity.Ca);

            if (existing == null)
            {
                await _context.LG_PB_NVL_NhomPhanBo.AddAsync(entity);
                await _context.SaveChangesAsync();
                return entity;
            }

            existing.IsDelete = false;
            existing.IDLoCao = entity.IDLoCao;
            existing.ThuTuUuTien = entity.ThuTuUuTien;
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> RemoveNvlAsync(int idNhomPhanBo, int idNvl, DateTime ngay, byte ca)
        {
            var existing = await _context.LG_PB_NVL_NhomPhanBo
                .FirstOrDefaultAsync(x => x.IDNhomPhanBo == idNhomPhanBo && x.IDNVL == idNvl
                    && x.Ngay == ngay.Date && x.Ca == ca && !x.IsDelete);
            if (existing == null) return false;
            existing.IsDelete = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<(LG_PB_NhomPhanBo Nhom, List<LG_PB_NVL_NhomPhanBo> ThanhVien)>> GetNhomVaThanhVienAsync(
            byte loaiPhanBo, DateTime ngay, byte ca, int idLoCao)
        {
            var nhoms = await _context.LG_PB_NhomPhanBo
                .Where(x => !x.IsDelete && x.LoaiPhanBo == loaiPhanBo)
                .OrderBy(x => x.ThuTu)
                .AsNoTracking()
                .ToListAsync();

            var nhomIds = nhoms.Select(x => x.ID).ToList();
            var thanhViens = await _context.LG_PB_NVL_NhomPhanBo
                .Where(x => !x.IsDelete && nhomIds.Contains(x.IDNhomPhanBo)
                    && x.Ngay == ngay.Date && x.Ca == ca && x.IDLoCao == idLoCao)
                .AsNoTracking()
                .ToListAsync();

            return nhoms
                .Select(n => (n, thanhViens.Where(t => t.IDNhomPhanBo == n.ID).ToList()))
                .ToList();
        }

        public async Task<Dictionary<int, LG_PB_NhomPhanBo>> GetByIdsAsync(IEnumerable<int> ids)
        {
            var idList = ids.Distinct().ToList();
            return await _context.LG_PB_NhomPhanBo
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
        public async Task<(DateTime Ngay, byte Ca)?> GetCaLienKe(int loaiPhanBo, int idLoCao, DateTime ngay, byte ca)
        {
            // Distinct() PHẢI đứng trước OrderBy() — nếu orderby nằm trong subquery bị Distinct() bọc
            // ngoài, SQL Server không đảm bảo giữ đúng thứ tự đó nên FirstOrDefaultAsync() có thể lấy
            // nhầm dòng không phải mới nhất.
            var result = await (
                from tv in _context.LG_PB_NVL_NhomPhanBo
                join nhom in _context.LG_PB_NhomPhanBo
                    on tv.IDNhomPhanBo equals nhom.ID
                where !tv.IsDelete
                   && nhom.LoaiPhanBo == loaiPhanBo
                   && tv.IDLoCao == idLoCao
                   && (
                            tv.Ngay < ngay
                         || (tv.Ngay == ngay && tv.Ca < ca)
                      )
                select new
                {
                    tv.Ngay,
                    tv.Ca
                })
                .Distinct()
                .OrderByDescending(x => x.Ngay)
                .ThenByDescending(x => x.Ca)
                .FirstOrDefaultAsync();

            if (result == null)
                return null;

            return (result.Ngay, result.Ca);
        }
    }
}

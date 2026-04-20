using dataproduct.api.DTOs.LGNL_Dto;
using dataproduct.api.Models;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public class LGNLRepository : ILGNLRepository
    {
        private readonly ProductFormContext _context;

        public LGNLRepository(ProductFormContext context)
        {
            _context = context;
        }

        // ─── TS Mapping lookup ───────────────────────────────────────────────

        public async Task<List<LGNLTsMappingDto>> GetTsMappingListAsync()
        {
            return await _context.LG1_NL_TS_Mapping
                .Where(x => x.IsActive == true)
                .AsNoTracking()
                .Select(x => new LGNLTsMappingDto
                {
                    ID       = x.ID,
                    TagKey   = x.TagKey,
                    IsActive = x.IsActive
                })
                .ToListAsync();
        }

        // ─── Dữ liệu thô LG1_DuLieuNL ───────────────────────────────────────

        public async Task<List<LG1_DuLieuNL>> GetDuLieuRawAsync(
            IEnumerable<string> tagKeys, int idLoCao, DateTime timeFrom, DateTime timeTo)
        {
            var keys = tagKeys.ToList();
            return await _context.LG1_DuLieuNL
                .Where(d => d.ID_LoCao == idLoCao
                         && d.Time >= timeFrom
                         && d.Time < timeTo
                         && keys.Contains(d.TagKey!))
                .OrderBy(d => d.Time)
                .AsNoTracking()
                .ToListAsync();
        }

        // ─── SiLo Master ─────────────────────────────────────────────────────

        public async Task<List<LG_NL_SiLo>> GetSiLoMasterListAsync(int? idLoCao)
        {
            return await _context.LG_NL_SiLo
                .Where(x => idLoCao == null || x.IDLoCao == idLoCao)
                .OrderBy(x => x.IDLoCao)
                .ThenBy(x => x.ThuTu)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<LG_NL_SiLo?> GetSiLoMasterByIdAsync(int id)
            => await _context.LG_NL_SiLo.FindAsync(id);

        public async Task<LG_NL_SiLo> AddSiLoMasterAsync(LG_NL_SiLo entity)
        {
            entity.NgayTao = DateTime.Now;
            await _context.LG_NL_SiLo.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<LG_NL_SiLo?> UpdateSiLoMasterAsync(int id, LG_NL_SiLo entity)
        {
            var existing = await _context.LG_NL_SiLo.FindAsync(id);
            if (existing == null) return null;

            existing.IDLoCao = entity.IDLoCao;
            existing.TenSiLo = entity.TenSiLo;
            existing.ThuTu   = entity.ThuTu;
            existing.TagKey  = entity.TagKey;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteSiLoMasterAsync(int id)
        {
            var existing = await _context.LG_NL_SiLo.FindAsync(id);
            if (existing == null) return false;
            _context.LG_NL_SiLo.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }

        // ─── Mapping (join SiLo + NVL) ───────────────────────────────────────

        public async Task<List<LGNLMappingDto>> GetMappingListAsync(DateOnly? ngay, int? idCa, int? idLoCao)
        {
            return await (
                from m in _context.LG_NL_Mapping
                join s in _context.LG_NL_SiLo
                    on m.IDSiLo equals s.ID into siloGroup
                from s in siloGroup.DefaultIfEmpty()
                join n in _context.LG_NL_NVL
                    on m.IDNVL equals n.ID into nvlGroup
                from n in nvlGroup.DefaultIfEmpty()
                where
                    (ngay == null || m.Ngay == ngay) &&
                    (idCa == null || m.IDCa == idCa) &&
                    (idLoCao == null || m.IDLoCao == idLoCao)
                orderby m.Ngay, m.IDCa, s.ThuTu
                select new LGNLMappingDto
                {
                    ID           = m.ID,
                    Ngay         = m.Ngay,
                    IDCa         = m.IDCa,
                    IDLoCao      = m.IDLoCao,
                    IDSiLo       = m.IDSiLo,
                    TenSiLo      = s != null ? s.TenSiLo      : null,
                    TagKey       = s != null ? s.TagKey        : null,
                    IDNVL        = m.IDNVL,
                    TenNVL       = n != null ? n.TenNVL        : null,
                    NhomHienThi  = n != null ? n.NhomHienThi   : null,
                    ThuTuNhom    = n != null ? n.ThuTuNhom     : null,
                    GhiChu       = m.GhiChu,
                    NgayTao      = m.NgayTao
                }
            ).AsNoTracking().ToListAsync();
        }

        public async Task<LG_NL_Mapping?> GetMappingByIdAsync(int id)
            => await _context.LG_NL_Mapping.FindAsync(id);

        public async Task<LG_NL_Mapping> AddMappingAsync(LG_NL_Mapping entity)
        {
            entity.NgayTao = DateTime.Now;
            await _context.LG_NL_Mapping.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<LG_NL_Mapping?> UpdateMappingAsync(int id, LG_NL_Mapping entity)
        {
            var existing = await _context.LG_NL_Mapping.FindAsync(id);
            if (existing == null) return null;

            existing.Ngay    = entity.Ngay;
            existing.IDCa    = entity.IDCa;
            existing.IDLoCao = entity.IDLoCao;
            existing.IDSiLo  = entity.IDSiLo;
            existing.IDNVL   = entity.IDNVL;
            existing.GhiChu  = entity.GhiChu;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteMappingAsync(int id)
        {
            var existing = await _context.LG_NL_Mapping.FindAsync(id);
            if (existing == null) return false;
            _context.LG_NL_Mapping.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }

        // ─── NVL ─────────────────────────────────────────────────────────────

        public async Task<List<LG_NL_NVL>> GetNvlListAsync(DateOnly? ngay, int? idCa, int? idLoCao)
        {
            return await _context.LG_NL_NVL
                .Where(x =>
                    (idLoCao == null || x.IDLoCao == idLoCao))
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<LG_NL_NVL?> GetNvlByIdAsync(int id)
            => await _context.LG_NL_NVL.FindAsync(id);

        public async Task<LG_NL_NVL> AddNvlAsync(LG_NL_NVL entity)
        {
            entity.NgayTao = DateTime.Now;
            await _context.LG_NL_NVL.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<LG_NL_NVL?> UpdateNvlAsync(int id, LG_NL_NVL entity)
        {
            var existing = await _context.LG_NL_NVL.FindAsync(id);
            if (existing == null) return null;

            existing.IDLoCao = entity.IDLoCao;
            existing.TenNVL  = entity.TenNVL;
            existing.DonVi   = entity.DonVi;
            existing.SoLuong = entity.SoLuong;
            existing.DoAm    = entity.DoAm;
            existing.GhiChu  = entity.GhiChu;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteNvlAsync(int id)
        {
            var existing = await _context.LG_NL_NVL.FindAsync(id);
            if (existing == null) return false;
            _context.LG_NL_NVL.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}

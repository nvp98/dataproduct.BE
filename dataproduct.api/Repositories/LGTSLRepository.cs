using dataproduct.api.DTOs.NMLG_Dto;
using dataproduct.api.Models;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public class LGTSLRepository : ILGTSLRepository
    {
        private readonly ProductFormContext _context;

        public LGTSLRepository(ProductFormContext context)
        {
            _context = context;
        }

        // ─── SiLo (LG_TSL_SiLo) ──────────────────────────────────────────────────

        //public async Task<List<LGTSSiLoDto>> GetSiLoListAsync(
        //    int? idLoCao,
        //    DateTime? ngay,
        //    int? ca)
        //{
        //    var ngayDate = ngay?.Date;

        //    return await (
        //        from m in _context.LG_TSL_SiLo_Mapping

        //        join s in _context.LG_TSL_SiLo
        //            on m.IDSiLo equals s.ID into siloGroup

        //        from s in siloGroup.DefaultIfEmpty()

        //        join nvlJoin in _context.LG_TSL_NVL
        //            on m.IDNVL equals nvlJoin.ID into nvlGroup

        //        from nvl in nvlGroup.DefaultIfEmpty()

        //        where
        //            (idLoCao == null || m.IDLoCao == idLoCao)
        //            && (ngayDate == null || m.Ngay.Date == ngayDate)
        //            && (ca == null || m.Ca == ca)

        //        orderby
        //            m.Ngay descending,
        //            m.Ca,
        //            s != null ? s.ThuTu : 999

        //        select new LGTSSiLoDto
        //        {
        //            ID = s != null ? s.ID : 0,

        //            IDLoCao = m.IDLoCao,

        //            TenSiLo = s != null
        //                ? s.TenSiLo
        //                : null,

        //            ThuTu = s != null
        //                ? s.ThuTu
        //                : null,

        //            TenNVL = nvl != null
        //                ? nvl.TenNVL
        //                : null
        //        }

        //    )
        //    .AsNoTracking()
        //    .ToListAsync();
        //}
        public async Task<List<LGTSSiLoDto>> GetSiLoListAsync(int? idLoCao)
        {
            return await _context.LG_TSL_SiLo
                .Where(s => idLoCao == null || s.ID_LoCao == idLoCao)
                .OrderBy(s => s.ID_LoCao)
                .ThenBy(s => s.ThuTu)
                .AsNoTracking()
                .Select(s => new LGTSSiLoDto
                {
                    ID = s.ID,
                    IDLoCao = s.ID_LoCao,
                    TenSiLo = s.TenSiLo,
                    ThuTu = s.ThuTu,
                })
                .ToListAsync();
        }
        public async Task<LG_TSL_SiLo?> GetSiLoByIdAsync(int id)
            => await _context.LG_TSL_SiLo.FindAsync(id);

        public async Task<LG_TSL_SiLo> AddSiLoAsync(LG_TSL_SiLo entity)
        {
            await _context.LG_TSL_SiLo.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<LG_TSL_SiLo?> UpdateSiLoAsync(int id, LG_TSL_SiLo entity)
        {
            var existing = await _context.LG_TSL_SiLo.FindAsync(id);
            if (existing == null) return null;

            existing.ID_LoCao = entity.ID_LoCao;
            existing.TenSiLo = entity.TenSiLo;
            existing.ThuTu = entity.ThuTu;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteSiLoAsync(int id)
        {
            var existing = await _context.LG_TSL_SiLo.FindAsync(id);
            if (existing == null) return false;
            _context.LG_TSL_SiLo.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }

        // ─── NVL (LG_TSL_NVL) ────────────────────────────────────────────────────

        public async Task<List<LGTSNvlDto>> GetNvlListAsync(int? idLoCao)
        {
            return await _context.LG_TSL_NVL
                .Where(n => idLoCao == null || n.IDLoCao == idLoCao)
                .OrderBy(n => n.IDLoCao)
                .ThenBy(n => n.TenNVL)
                .AsNoTracking()
                .Select(n => new LGTSNvlDto
                {
                    ID = n.ID,
                    IDLoCao = n.IDLoCao,
                    TenNVL = n.TenNVL,
                    TenNVL_TK = n.TenNVL_Tk,
                    GhiChu = n.GhiChu,
                    NgayTao = n.NgayTao,
                    XacNhan = n.XacNhan,
                    NgayXacNhan = n.NgayXacNhan,
                    IDNguoiXacNhan = n.IDNguoiXacNhan,
                })
                .ToListAsync();
        }

        public async Task<LG_TSL_NVL?> GetNvlByIdAsync(int id)
            => await _context.LG_TSL_NVL.FindAsync(id);

        public async Task<LG_TSL_NVL> AddNvlAsync(LG_TSL_NVL entity)
        {
            entity.NgayTao = DateTime.Now;
            await _context.LG_TSL_NVL.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<LG_TSL_NVL?> UpdateNvlAsync(int id, LG_TSL_NVL entity)
        {
            var existing = await _context.LG_TSL_NVL.FindAsync(id);
            if (existing == null) return null;

            existing.IDLoCao = entity.IDLoCao;
            existing.TenNVL = entity.TenNVL;
            existing.TenNVL_Tk = entity.TenNVL_Tk;
            existing.GhiChu = entity.GhiChu;
            existing.XacNhan = entity.XacNhan;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteNvlAsync(int id)
        {
            var existing = await _context.LG_TSL_NVL.FindAsync(id);
            if (existing == null) return false;
            _context.LG_TSL_NVL.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateXacNhanAsync(int id, bool xacNhan)
        {
            var existing = await _context.LG_TSL_NVL.FindAsync(id);
            if (existing == null) return false;

            existing.XacNhan = xacNhan;
            existing.NgayXacNhan = xacNhan ? DateTime.Now : null;

            await _context.SaveChangesAsync();
            return true;
        }

        // ─── Mapping (LG_TSL_SiLo_Mapping) ───────────────────────────────────────

        public async Task<List<LGTSMappingDto>> GetMappingListAsync(int? idLoCao, DateTime? ngay, int? ca)
        {
            return await (
                from m in _context.LG_TSL_SiLo_Mapping
                join s in _context.LG_TSL_SiLo
                    on m.IDSiLo equals s.ID into siloGroup
                from s in siloGroup.DefaultIfEmpty()
                join n in _context.LG_TSL_NVL
                    on m.IDNVL equals n.ID into nvlGroup
                from n in nvlGroup.DefaultIfEmpty()
                where
                    (idLoCao == null || m.IDLoCao == idLoCao) &&
                    (ngay == null || m.Ngay == ngay) &&
                    (ca == null || m.Ca == ca)
                orderby m.Ngay descending, m.Ca, s != null ? s.ThuTu : 999
                select new LGTSMappingDto
                {
                    ID = m.ID,
                    IDLoCao = m.IDLoCao,
                    IDSiLo = m.IDSiLo,
                    IDNVL = m.IDNVL,
                    Ngay = m.Ngay,
                    Ca = m.Ca,
                    GhiChu = m.GhiChu,
                    NgayTao = m.NgayTao,
                    NguoiTao = m.NguoiTao,
                    TenSiLo = s != null ? s.TenSiLo : null,
                    ThuTuSiLo = s != null ? s.ThuTu : null,
                    TenNVL = n != null ? n.TenNVL : null,
                    TenNVL_TK = n != null ? n.TenNVL_Tk : null,
                }
            ).AsNoTracking().ToListAsync();
        }

        public async Task<LG_TSL_SiLo_Mapping?> GetMappingByIdAsync(int id)
            => await _context.LG_TSL_SiLo_Mapping.FindAsync(id);

        public async Task<LG_TSL_SiLo_Mapping> AddMappingAsync(LG_TSL_SiLo_Mapping entity)
        {
            entity.NgayTao = DateTime.Now;
            await _context.LG_TSL_SiLo_Mapping.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<LG_TSL_SiLo_Mapping?> UpdateMappingAsync(int id, LG_TSL_SiLo_Mapping entity)
        {
            var existing = await _context.LG_TSL_SiLo_Mapping.FindAsync(id);
            if (existing == null) return null;

            existing.IDLoCao = entity.IDLoCao;
            existing.IDSiLo = entity.IDSiLo;
            existing.IDNVL = entity.IDNVL;
            existing.Ngay = entity.Ngay;
            existing.Ca = entity.Ca;
            existing.GhiChu = entity.GhiChu;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteMappingAsync(int id)
        {
            var existing = await _context.LG_TSL_SiLo_Mapping.FindAsync(id);
            if (existing == null) return false;
            _context.LG_TSL_SiLo_Mapping.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<LGTSSiLoMappingViewDto>> GetSiLoByMappingAsync(
            int? idLoCao,
            DateTime? ngay,
            int? ca)
        {
            var ngayDate = ngay?.Date;

            return await (
                from m in _context.LG_TSL_SiLo_Mapping

                    // =====================================================
                    // SILO
                    // =====================================================

                join s in _context.LG_TSL_SiLo
                    on m.IDSiLo equals s.ID into siloGroup

                from s in siloGroup.DefaultIfEmpty()

                    // =====================================================
                    // NVL
                    // =====================================================

                join nvl in _context.LG_TSL_NVL
                    on m.IDNVL equals nvl.ID into nvlGroup

                from nvl in nvlGroup.DefaultIfEmpty()

                    // =====================================================
                    // TON
                    // =====================================================

                join ton in _context.SiLoTon
                    on new
                    {
                        IDSiLo = m.IDSiLo,
                        IDLoCao = m.IDLoCao,
                        Ngay = m.Ngay.Date
                    }
                    equals new
                    {
                        IDSiLo = ton.IDSiLo,
                        IDLoCao = ton.IdLoCao,
                        Ngay = ton.Ngay.Date
                    }
                    into tonGroup

                from ton in tonGroup.DefaultIfEmpty()

                    // =====================================================
                    // FILTER
                    // =====================================================

                where
                    (idLoCao == null || m.IDLoCao == idLoCao)
                    && (ngayDate == null || m.Ngay.Date == ngayDate)
                    && (ca == null || m.Ca == ca)

                // =====================================================
                // ORDER
                // =====================================================

                orderby
                    m.Ngay descending,
                    m.Ca,
                    s != null ? s.ThuTu : 999

                // =====================================================
                // DTO
                // =====================================================

                select new LGTSSiLoMappingViewDto
                {
                    IDMapping = m.ID,

                    IDSiLo = m.IDSiLo,

                    IDLoCao = m.IDLoCao,

                    IDNVL = m.IDNVL,

                    TenSiLo = s != null
                        ? s.TenSiLo
                        : null,

                    ThuTu = s != null
                        ? s.ThuTu
                        : null,

                    TenNVL = nvl != null
                        ? nvl.TenNVL
                        : null,

                    TenNVL_TK = nvl != null
                        ? nvl.TenNVL_Tk
                        : null,

                    Ngay = m.Ngay,

                    Ca = m.Ca,

                    GhiChu = m.GhiChu,

                    Ton = ton != null
                        ? ton.Ton
                        : 0
                }

            )
            .AsNoTracking()
            .ToListAsync();
        }
    }

}

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

        public async Task DeleteByPhieuIdAsync(Guid idPhieu)
        {
            var existing = _context.LG_TSL_ChiTiet.Where(c => c.IDPhieu == idPhieu);
            _context.LG_TSL_ChiTiet.RemoveRange(existing);
            await _context.SaveChangesAsync();
        }

        public async Task UpsertChiTietAsync(UpsertLGTSChiTietDto dto)
        {
            var existing = _context.LG_TSL_ChiTiet
                .Where(c => c.IDPhieu == dto.IDPhieu);
            _context.LG_TSL_ChiTiet.RemoveRange(existing);

            var now = DateTime.Now;
            var newRecords = dto.Items.Select(item => new LG_TSL_ChiTiet
            {
                IDPhieu = dto.IDPhieu,
                IDLoCao = dto.IDLoCao,
                Ngay = dto.Ngay.Date,
                Ca = dto.Ca,
                IDSiLo = item.IDSiLo,
                IDMapping = item.IDMapping,
                IDNVL = item.IDNVL,
                TenSiLo = item.TenSiLo,
                TenNVL = item.TenNVL,
                KLTonCuoiKip = item.KLTonCuoiKip,
                GhiChu = item.GhiChu,
                ThuTu = item.ThuTu,
            });
            await _context.LG_TSL_ChiTiet.AddRangeAsync(newRecords);
            await _context.SaveChangesAsync();
        }

        public async Task<List<LGTSChiTietDto>> GetChiTietByPhieuAsync(Guid idPhieu)
        {
            return await _context.LG_TSL_ChiTiet
                .Where(c => c.IDPhieu == idPhieu)
                .OrderBy(c => c.ThuTu)
                .Select(c => new LGTSChiTietDto
                {
                    ID = c.ID,
                    IDPhieu = c.IDPhieu,
                    IDLoCao = c.IDLoCao,
                    Ngay = c.Ngay,
                    Ca = c.Ca,
                    IDSiLo = c.IDSiLo,
                    IDMapping = c.IDMapping,
                    IDNVL = c.IDNVL,
                    TenSiLo = c.TenSiLo,
                    TenNVL = c.TenNVL,
                    KLTonCuoiKip = c.KLTonCuoiKip,
                    GhiChu = c.GhiChu,
                    ThuTu = c.ThuTu,
                })
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<LGTSSiLoMappingViewDto>> GetSiLoByMappingAsync(
     int? idLoCao,
     DateTime? ngay,
     int? ca)
        {
            // =====================================================
            // BASE QUERY
            // =====================================================

            IQueryable<LG_TSL_SiLo_Mapping> mappingQuery =
                _context.LG_TSL_SiLo_Mapping
                    .AsNoTracking();

            // =====================================================
            // FILTER: LÒ CAO
            // =====================================================

            if (idLoCao.HasValue)
            {
                mappingQuery = mappingQuery
                    .Where(x => x.IDLoCao == idLoCao.Value);
            }

            // =====================================================
            // FILTER: CA
            // =====================================================

            if (ca.HasValue)
            {
                mappingQuery = mappingQuery
                    .Where(x => x.Ca == ca.Value);
            }

            // =====================================================
            // FILTER: NGÀY
            // =====================================================

            if (ngay.HasValue)
            {
                var fromDate = ngay.Value.Date;
                var toDate = fromDate.AddDays(1);

                mappingQuery = mappingQuery
                    .Where(x =>
                        x.Ngay >= fromDate &&
                        x.Ngay < toDate);
            }

            // =====================================================
            // QUERY
            // =====================================================

            var result = await (
                from m in mappingQuery

                    // ================================================
                    // SILO
                    // ================================================

                join s in _context.LG_TSL_SiLo.AsNoTracking()
                    on m.IDSiLo equals s.ID into siloGroup

                from s in siloGroup.DefaultIfEmpty()

                    // ================================================
                    // NVL
                    // ================================================

                join nvl in _context.LG_TSL_NVL.AsNoTracking()
                    on m.IDNVL equals nvl.ID into nvlGroup

                from nvl in nvlGroup.DefaultIfEmpty()

                    // ================================================
                    // DTO
                    // ================================================

                select new
                {
                    Mapping = m,
                    SiLo = s,
                    NVL = nvl,

                    Ton = _context.SiLoTon
                        .Where(t =>
                            t.IDSiLo == m.IDSiLo &&
                            t.IdLoCao == m.IDLoCao &&
                            t.Ngay >= m.Ngay.Date &&
                            t.Ngay < m.Ngay.Date.AddDays(1))
                        .Select(t => (decimal?)t.Ton)
                        .FirstOrDefault()
                }

            )
            .OrderByDescending(x => x.Mapping.Ngay)
            .ThenBy(x => x.Mapping.Ca)
            .ThenBy(x => x.SiLo != null ? x.SiLo.ThuTu : 999)

            .Select(x => new LGTSSiLoMappingViewDto
            {
                IDMapping = x.Mapping.ID,

                IDSiLo = x.Mapping.IDSiLo,

                IDLoCao = x.Mapping.IDLoCao,

                IDNVL = x.Mapping.IDNVL,

                TenSiLo = x.SiLo != null
                    ? x.SiLo.TenSiLo
                    : null,

                ThuTu = x.SiLo != null
                    ? x.SiLo.ThuTu
                    : null,

                TenNVL = x.NVL != null
                    ? x.NVL.TenNVL
                    : null,

                TenNVL_TK = x.NVL != null
                    ? x.NVL.TenNVL_Tk
                    : null,

                Ngay = x.Mapping.Ngay,

                Ca = x.Mapping.Ca,

                GhiChu = x.Mapping.GhiChu,

                Ton = x.Ton ?? 0
            })

            .ToListAsync();

            return result;
        }

    }
}
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
                .Where(s => s.IsDelete != true && (idLoCao == null || s.ID_LoCao == idLoCao))
                .OrderBy(s => s.ID_LoCao)
                .ThenBy(s => s.ThuTu)
                .AsNoTracking()
                .Select(s => new LGTSSiLoDto
                {
                    Id = s.ID,
                    IdLoCao = s.ID_LoCao,
                    TenSiLo = s.TenSiLo,
                    ThuTu = s.ThuTu,
                    ThuTuCoDinh = s.ThuTuCoDinh,
                    IsDelete = s.IsDelete
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
            existing.IsDelete = entity.IsDelete;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteSiLoAsync(int id)
        {
            var existing = await _context.LG_TSL_SiLo.FindAsync(id);
            if (existing == null) return false;
            existing.IsDelete = true;
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
                    Id = n.ID,
                    IdLoCao = n.IDLoCao,
                    TenNVL = n.TenNVL,
                    TenNVLTk = n.TenNVL_Tk,
                    TenHienThi = n.XacNhan && n.TenNVL_Tk != null ? n.TenNVL_Tk : n.TenNVL,
                    GhiChu = n.GhiChu,
                    NgayTao = n.NgayTao,
                    XacNhan = n.XacNhan,
                    NgayXacNhan = n.NgayXacNhan,
                    IdNguoiXacNhan = n.IDNguoiXacNhan,
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
                    // IDSiLo trong Mapping lưu ThuTuCoDinh → join qua ThuTuCoDinh + IDLoCao
                join s in _context.LG_TSL_SiLo
                    on new { ThuTuCoDinh = (int?)m.IDSiLo, LoCao = (int?)m.IDLoCao }
                    equals new { s.ThuTuCoDinh, LoCao = s.ID_LoCao } into siloGroup
                from s in siloGroup.DefaultIfEmpty()
                join n in _context.LG_TSL_NVL
                    on m.IDNVL equals n.ID into nvlGroup
                from n in nvlGroup.DefaultIfEmpty()
                where
                    (idLoCao == null || m.IDLoCao == idLoCao) &&
                    (ngay == null || (m.Ngay >= ngay.Value.Date && m.Ngay < ngay.Value.Date.AddDays(1))) &&
                    (ca == null || m.Ca == ca)
                orderby m.Ngay descending, m.Ca, s != null ? s.ThuTu : 999
                select new LGTSMappingDto
                {
                    Id = m.ID,
                    IdLoCao = m.IDLoCao,
                    IdSiLo = m.IDSiLo,   // = ThuTuCoDinh của silo
                    IdNVL = m.IDNVL,
                    Ngay = m.Ngay,
                    Ca = m.Ca,
                    GhiChu = m.GhiChu,
                    NgayTao = m.NgayTao,
                    NguoiTao = m.NguoiTao,
                    TenSiLo = s != null ? s.TenSiLo : null,
                    ThuTuSiLo = s != null ? s.ThuTu : (int?)null,
                    TenNVL = n != null ? n.TenNVL : null,
                    TenNVLTk = n != null ? n.TenNVL_Tk : null,
                }
            ).AsNoTracking().ToListAsync();
        }

        public async Task<LG_TSL_SiLo_Mapping?> GetExistingMappingAsync(int thuTuCoDinh, int idLoCao, DateTime ngay, int ca)
            => await _context.LG_TSL_SiLo_Mapping
                .Where(m =>
                    m.IDSiLo == thuTuCoDinh &&
                    m.IDLoCao == idLoCao &&
                    m.Ngay.Date == ngay.Date &&
                    m.Ca == ca)
                .FirstOrDefaultAsync();

        public async Task<int?> GetSiLoThuTuCoDinhAsync(int siloId, int idLoCao)
            => await _context.LG_TSL_SiLo
                .Where(s => s.ID == siloId && s.ID_LoCao == idLoCao)
                .Select(s => s.ThuTuCoDinh)
                .FirstOrDefaultAsync();

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
                .Where(c => c.IDPhieu == dto.IdPhieu);
            _context.LG_TSL_ChiTiet.RemoveRange(existing);

            var now = DateTime.Now;
            var newRecords = dto.Items.Select(item => new LG_TSL_ChiTiet
            {
                IDPhieu = dto.IdPhieu,
                IDLoCao = dto.IdLoCao,
                Ngay = dto.Ngay.Date,
                Ca = dto.Ca,
                IDSiLo = item.IdSiLo,
                IDMapping = item.IdMapping,
                IDNVL = item.IdNVL,
                TenSiLo = item.TenSiLo,
                TenNVL = item.TenNVL,
                KLTonCuoiKip = item.KLTonCuoiKip,
                ManualKL = item.ManualKL,
                KLGoc = item.KLGoc,
                GhiChu = item.GhiChu,
                ThuTu = item.ThuTu,
            });
            await _context.LG_TSL_ChiTiet.AddRangeAsync(newRecords);
            await _context.SaveChangesAsync();
        }

        public async Task<List<LGTSChiTietDto>> GetChiTietByPhieuAsync(Guid idPhieu)
        {
            return await (
                from c in _context.LG_TSL_ChiTiet.AsNoTracking()
                where c.IDPhieu == idPhieu
                join nvl in _context.LG_TSL_NVL.AsNoTracking()
                    on c.IDNVL equals nvl.ID into nvlGroup
                from nvl in nvlGroup.DefaultIfEmpty()
                orderby c.ThuTu
                select new LGTSChiTietDto
                {
                    Id = c.ID,
                    IdPhieu = c.IDPhieu,
                    IdLoCao = c.IDLoCao,
                    Ngay = c.Ngay,
                    Ca = c.Ca,
                    IdSiLo = c.IDSiLo,
                    IdMapping = c.IDMapping,
                    IdNVL = c.IDNVL,
                    TenSiLo = c.TenSiLo,
                    TenNVL = nvl != null
                        ? (nvl.XacNhan && nvl.TenNVL_Tk != null ? nvl.TenNVL_Tk : nvl.TenNVL)
                        : c.TenNVL,
                    TenNVLTk = nvl != null ? nvl.TenNVL_Tk : null,
                    KLTonCuoiKip = c.KLTonCuoiKip,
                    ManualKL = c.ManualKL,
                    KLGoc = c.KLGoc,
                    GhiChu = c.GhiChu,
                    ThuTu = c.ThuTu,
                }
            ).ToListAsync();
        }

        //public async Task<List<LGTSSiLoMappingViewDto>> GetSiLoByMappingAsync(
        //    int? idLoCao,
        //    DateTime? ngay,
        //    int? ca)
        //{
        //    var fromDate = ngay?.Date;
        //    var toDate = fromDate?.AddDays(1);

        //    // SiLoTon được ghi vào ~phút 25-35 của giờ kết ca:
        //    // Ca 1 (Ngày) kết lúc 19:30 → lấy khoảng 19:25 – 19:35 cùng ngày
        //    // Ca 2 (Đêm) kết lúc 07:30 → lấy khoảng 07:25 – 07:35 ngày hôm sau
        //    var tonFrom = ca == 1
        //        ? fromDate?.AddHours(19).AddMinutes(25)
        //        : fromDate?.AddDays(1).AddHours(7).AddMinutes(25);
        //    var tonTo = ca == 1
        //        ? fromDate?.AddHours(19).AddMinutes(35)
        //        : fromDate?.AddDays(1).AddHours(7).AddMinutes(35);

        //    // IDSiLo trong Mapping lưu ThuTuCoDinh → join qua ThuTuCoDinh + IDLoCao
        //    // để lấy đúng TenSiLo từ LG_TSL_SiLo và sắp xếp theo thứ tự vật lý (ThuTu).
        //    var result = await (
        //        from m in _context.LG_TSL_SiLo_Mapping.AsNoTracking()
        //        where
        //            (idLoCao == null || m.IDLoCao == idLoCao) &&
        //            (ca == null || m.Ca == ca) &&
        //            (fromDate == null || (m.Ngay >= fromDate && m.Ngay < toDate))

        //        join s in _context.LG_TSL_SiLo.AsNoTracking()
        //            on new { ThuTuCoDinh = (int?)m.IDSiLo, LoCao = (int?)m.IDLoCao }
        //            equals new { s.ThuTuCoDinh, LoCao = s.ID_LoCao } into siloGroup
        //        from s in siloGroup.DefaultIfEmpty()

        //        join nvl in _context.LG_TSL_NVL.AsNoTracking()
        //            on m.IDNVL equals nvl.ID into nvlGroup
        //        from nvl in nvlGroup.DefaultIfEmpty()

        //        orderby s != null ? s.ThuTu : m.IDSiLo

        //        select new LGTSSiLoMappingViewDto
        //        {
        //            IdMapping = m.ID,
        //            IdSiLo = m.IDSiLo,
        //            IdLoCao = m.IDLoCao,
        //            IdNVL = m.IDNVL,
        //            TenSiLo = s != null ? s.TenSiLo : null,
        //            ThuTu = s != null ? s.ThuTu : m.IDSiLo,
        //            TenNVL = nvl != null
        //                          ? (nvl.XacNhan && nvl.TenNVL_Tk != null ? nvl.TenNVL_Tk : nvl.TenNVL)
        //                          : null,
        //            TenNVLTk = nvl != null ? nvl.TenNVL_Tk : null,
        //            Ngay = m.Ngay,
        //            Ca = m.Ca,
        //            GhiChu = m.GhiChu,
        //            // Lấy bản ghi mới nhất trong khung giờ của ca
        //            Ton = _context.SiLoTon
        //                .Where(t =>
        //                    t.IDSiLo == m.IDSiLo &&
        //                    t.IdLoCao == m.IDLoCao &&
        //                    (tonFrom == null || t.Ngay >= tonFrom) &&
        //                    (tonTo == null || t.Ngay < tonTo))
        //                .OrderByDescending(t => t.Ngay)
        //                .Select(t => (decimal?)t.Ton)
        //                .FirstOrDefault() ?? 0
        //        }
        //    ).ToListAsync();

        //    return result;
        //}
        public async Task<List<LGTSSiLoMappingViewDto>> GetSiLoByMappingAsync(int? idLoCao,DateTime? ngay,int? ca)
        {
            var fromDate = ngay?.Date;
            var toDate = fromDate?.AddDays(1);

            var tonFrom = ca == 1
                ? fromDate?.AddHours(19).AddMinutes(25)
                : fromDate?.AddDays(1).AddHours(7).AddMinutes(25);
            var tonTo = ca == 1
                ? fromDate?.AddHours(19).AddMinutes(35)
                : fromDate?.AddDays(1).AddHours(7).AddMinutes(35);

            // ✅ Bước 1: Load mapping + join SiLo + NVL (giữ nguyên query này)
            var mappings = await (
                from m in _context.LG_TSL_SiLo_Mapping.AsNoTracking()
                where
                    (idLoCao == null || m.IDLoCao == idLoCao) &&
                    (ca == null || m.Ca == ca) &&
                    (fromDate == null || (m.Ngay >= fromDate && m.Ngay < toDate))
                join s in _context.LG_TSL_SiLo.AsNoTracking()
                    on new { ThuTuCoDinh = (int?)m.IDSiLo, LoCao = (int?)m.IDLoCao }
                    equals new { s.ThuTuCoDinh, LoCao = s.ID_LoCao } into siloGroup
                from s in siloGroup.DefaultIfEmpty()
                join nvl in _context.LG_TSL_NVL.AsNoTracking()
                    on m.IDNVL equals nvl.ID into nvlGroup
                from nvl in nvlGroup.DefaultIfEmpty()
                orderby s != null ? s.ThuTu : m.IDSiLo
                select new
                {
                    m.ID,
                    m.IDSiLo,
                    m.IDLoCao,
                    m.IDNVL,
                    m.Ngay,
                    m.Ca,
                    m.GhiChu,
                    TenSiLo = s != null ? s.TenSiLo : null,
                    ThuTu = s != null ? s.ThuTu : m.IDSiLo,
                    TenNVL = nvl != null
                                    ? (nvl.XacNhan && nvl.TenNVL_Tk != null ? nvl.TenNVL_Tk : nvl.TenNVL)
                                    : null,
                    TenNVLTk = nvl != null ? nvl.TenNVL_Tk : null,
                }
            ).ToListAsync();

            // ✅ Bước 2: Load TẤT CẢ SiLoTon liên quan trong 1 query duy nhất
            var siloIds = mappings.Select(m => m.IDSiLo).Distinct().ToList();
            var loCaoIds = mappings.Select(m => m.IDLoCao).Distinct().ToList();

            var tonQuery = _context.SiLoTon
                .AsNoTracking()
                .Where(t => siloIds.Contains(t.IDSiLo) && loCaoIds.Contains(t.IdLoCao));

            if (tonFrom.HasValue) tonQuery = tonQuery.Where(t => t.Ngay >= tonFrom.Value);
            if (tonTo.HasValue) tonQuery = tonQuery.Where(t => t.Ngay < tonTo.Value);

            // Lấy bản ghi mới nhất cho mỗi (IDSiLo, IdLoCao)
            var tonLookup = await tonQuery
                .GroupBy(t => new { t.IDSiLo, t.IdLoCao })
                .Select(g => new
                {
                    g.Key.IDSiLo,
                    g.Key.IdLoCao,
                    Ton = g.OrderByDescending(t => t.Ngay).Select(t => (decimal?)t.Ton).FirstOrDefault()
                })
                .ToDictionaryAsync(
                    t => (t.IDSiLo, t.IdLoCao),
                    t => t.Ton ?? 0m);

            // ✅ Bước 3: Map kết quả — lookup O(1), không có thêm query nào
            var result = mappings.Select(m => new LGTSSiLoMappingViewDto
            {
                IdMapping = m.ID,
                IdSiLo = m.IDSiLo,
                IdLoCao = m.IDLoCao,
                IdNVL = m.IDNVL,
                TenSiLo = m.TenSiLo,
                ThuTu = m.ThuTu,
                TenNVL = m.TenNVL,
                TenNVLTk = m.TenNVLTk,
                Ngay = m.Ngay,
                Ca = m.Ca,
                GhiChu = m.GhiChu,
                Ton = tonLookup.TryGetValue((m.IDSiLo, m.IDLoCao), out var ton) ? ton : 0m
            }).ToList();

            return result;
        }
        public async Task<BmPhieu?> GetPhieuByIdAsync(Guid idPhieu)
            => await _context.BmPhieus.AsNoTracking().FirstOrDefaultAsync(p => p.Idphieu == idPhieu);
    }
}
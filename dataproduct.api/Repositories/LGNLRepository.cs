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
                join nh in _context.LG_NL_NhomNVL
                    on n.IDNhomNVL equals nh.ID into nhomGroup
                from nh in nhomGroup.DefaultIfEmpty()
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
                    NhomHienThi  = nh != null ? nh.TenNhom     : null,
                    ThuTuNhom    = nh != null ? nh.ThuTu       : null,
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

        // ─── Nhóm NVL ────────────────────────────────────────────────────────

        public async Task<List<LG_NL_NhomNVL>> GetNhomNvlListAsync(int? idLoCao)
        {
            return await _context.LG_NL_NhomNVL
                .Where(x => idLoCao == null || x.IDLoCao == idLoCao)
                .OrderBy(x => x.IDLoCao)
                .ThenBy(x => x.ThuTu)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<LG_NL_NhomNVL?> GetNhomNvlByIdAsync(int id)
            => await _context.LG_NL_NhomNVL.FindAsync(id);

        public async Task<LG_NL_NhomNVL> AddNhomNvlAsync(LG_NL_NhomNVL entity)
        {
            entity.NgayTao = DateTime.Now;
            await _context.LG_NL_NhomNVL.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<LG_NL_NhomNVL?> UpdateNhomNvlAsync(int id, LG_NL_NhomNVL entity)
        {
            var existing = await _context.LG_NL_NhomNVL.FindAsync(id);
            if (existing == null) return null;

            existing.IDLoCao = entity.IDLoCao;
            existing.TenNhom = entity.TenNhom;
            existing.ThuTu   = entity.ThuTu;
            existing.GhiChu  = entity.GhiChu;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteNhomNvlAsync(int id)
        {
            var existing = await _context.LG_NL_NhomNVL.FindAsync(id);
            if (existing == null) return false;
            _context.LG_NL_NhomNVL.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }

        // ─── NVL ─────────────────────────────────────────────────────────────

        public async Task<List<LGNLNvlDto>> GetNvlListAsync(int? idLoCao)
        {
            return await (
                from n in _context.LG_NL_NVL
                join nh in _context.LG_NL_NhomNVL
                    on n.IDNhomNVL equals nh.ID into nhomGroup
                from nh in nhomGroup.DefaultIfEmpty()
                where idLoCao == null || n.IDLoCao == idLoCao
                orderby n.IDLoCao, nh.ThuTu
                select new LGNLNvlDto
                {
                    ID           = n.ID,
                    IDLoCao      = n.IDLoCao,
                    IDNhomNVL    = n.IDNhomNVL,
                    TenNVL       = n.TenNVL,
                    DonVi        = n.DonVi,
                    SoLuong      = n.SoLuong,
                    DoAm         = n.DoAm,
                    GhiChu       = n.GhiChu,
                    NgayTao      = n.NgayTao,
                    NhomHienThi  = nh != null ? nh.TenNhom : null,
                    ThuTuNhom    = nh != null ? nh.ThuTu   : null,
                }
            ).AsNoTracking().ToListAsync();
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

            existing.IDLoCao    = entity.IDLoCao;
            existing.IDNhomNVL  = entity.IDNhomNVL;
            existing.TenNVL     = entity.TenNVL;
            existing.DonVi      = entity.DonVi;
            existing.SoLuong    = entity.SoLuong;
            existing.DoAm       = entity.DoAm;
            existing.GhiChu     = entity.GhiChu;

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

        // ─── Pivot dữ liệu nạp liệu theo Silo mapping ───────────────

        public async Task<LGNLDuLieuSiLoResult> GetDuLieuSiloPivotAsync(
            DateOnly ngay, int idCa, int idLoCao)
        {
            // 1. Lấy danh sách silo-NVL mapping cho ca/ngày/lò cao
            var mappings = await (
                from m in _context.LG_NL_Mapping
                join s in _context.LG_NL_SiLo on m.IDSiLo equals s.ID into sg
                from s in sg.DefaultIfEmpty()
                join n in _context.LG_NL_NVL on m.IDNVL equals n.ID into ng
                from n in ng.DefaultIfEmpty()
                join nh in _context.LG_NL_NhomNVL on n.IDNhomNVL equals nh.ID into nhg
                from nh in nhg.DefaultIfEmpty()
                where m.IDLoCao == idLoCao && m.IDCa == idCa && m.Ngay == ngay
                orderby nh != null ? nh.ThuTu : 999, s != null ? s.ThuTu : 999
                select new
                {
                    TagKey    = s != null ? s.TagKey : null,
                    SiLoId    = s != null ? s.ID : 0,
                    TenNVL    = n != null ? n.TenNVL : (s != null ? s.TenSiLo : null),
                    DonVi     = n != null ? n.DonVi : null,
                    TenNhom   = nh != null ? nh.TenNhom : null,
                    ThuTuNhom = nh != null ? (nh.ThuTu ?? 999) : 999,
                }
            ).AsNoTracking().ToListAsync();

            var valid = mappings.Where(m => !string.IsNullOrEmpty(m.TagKey)).ToList();
            if (valid.Count == 0)
                return new LGNLDuLieuSiLoResult();

            // Gán dataIndex duy nhất cho mỗi silo
            var withIndex = valid.Select(m => new
            {
                m.TagKey,
                DataIndex = $"silo_{m.SiLoId}",
                m.TenNVL,
                m.DonVi,
                m.TenNhom,
                m.ThuTuNhom,
            }).ToList();

            // 2. Tính khung thời gian theo ca
            DateTime timeFrom, timeTo;
            if (idCa == 1) // Ca ngày: 07:00 → 19:00
            {
                timeFrom = ngay.ToDateTime(new TimeOnly(7, 0));
                timeTo   = ngay.ToDateTime(new TimeOnly(19, 0));
            }
            else // Ca đêm: 19:00 → 07:00 hôm sau
            {
                timeFrom = ngay.ToDateTime(new TimeOnly(19, 0));
                timeTo   = ngay.AddDays(1).ToDateTime(new TimeOnly(7, 0));
            }

            // 3. Truy vấn SCADA data — bao gồm TS0 (số mẻ) ngoài các silo tag
            var tagKeys = withIndex.Select(m => m.TagKey!).Distinct()
                .Append("TS0").Distinct().ToList();
            var rawData = await _context.LG1_DuLieuNL
                .Where(d => d.ID_LoCao == idLoCao
                         && d.Time >= timeFrom
                         && d.Time < timeTo
                         && tagKeys.Contains(d.TagKey!))
                .OrderBy(d => d.Time)
                .AsNoTracking()
                .ToListAsync();

            // 4. Xây dựng cấu trúc cột (nhóm theo NhomNVL)
            var tagToIndex = withIndex
                .GroupBy(m => m.TagKey!)
                .ToDictionary(g => g.Key, g => g.First().DataIndex);

            var groups = withIndex
                .GroupBy(m => m.TenNhom)
                .OrderBy(g => g.Min(m => m.ThuTuNhom));

            var columns = new List<LGNLColumnDto>();
            foreach (var grp in groups)
            {
                var leaves = grp
                    .GroupBy(m => m.DataIndex)
                    .Select(g => new LGNLColumnDto
                    {
                        Title     = g.First().TenNVL ?? g.Key,
                        DataIndex = g.Key,
                    })
                    .ToList();

                if (grp.Key == null || leaves.Count == 1)
                    columns.AddRange(leaves);
                else
                    columns.Add(new LGNLColumnDto { Title = grp.Key, Children = leaves });
            }

            // 5. Pivot theo timestamp (mỗi time = 1 mẻ nạp)
            var rows = rawData
                .GroupBy(d => d.Time)
                .OrderBy(g => g.Key)
                .Select((g, idx) =>
                {
                    var row = new Dictionary<string, object?>
                    {
                        ["id"]   = idx + 1,
                        ["time"] = g.Key?.ToString("yyyy-MM-ddTHH:mm:ss"),
                    };
                    foreach (var d in g)
                    {
                        if (d.TagKey == "TS0")
                        {
                            row["soMe"] = d.Value;
                            continue;
                        }

                        if (d.TagKey != null && tagToIndex.TryGetValue(d.TagKey, out var di))
                            row[di] = d.Value;
                    }
                    return row;
                })
                .ToList();

            return new LGNLDuLieuSiLoResult { Columns = columns, Rows = rows };
        }

        // ─── Dữ liệu theo LoCao, Ngày ───────────────────────────────

        public async Task<List<LGNLDuLieuScadaDto>> GetDataByFilterAsync(
             int? idLoCao, DateTime? ngayBatDau, DateTime? ngayKetThuc)
        {
            // Query dữ liệu thô từ LG1_DuLieuNL
            var query = from d in _context.LG1_DuLieuNL
                        where
                              (idLoCao == null || d.ID_LoCao == idLoCao) &&
                              (ngayBatDau == null || d.Time >= ngayBatDau) &&
                              (ngayKetThuc == null || d.Time < ngayKetThuc)
                        orderby d.Time
                        select new LGNLDuLieuScadaDto
                        {
                            ID = d.ID,
                            TagName = d.TagName,
                            TagKey = d.TagKey,
                            Time = d.Time,
                            Value = d.Value,
                            IDLoCao = d.ID_LoCao
                        };

            return await query.AsNoTracking().ToListAsync();
        }
    }
}

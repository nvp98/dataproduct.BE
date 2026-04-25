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
            entity.NgayTao    = DateTime.Now;
            entity.ThoiDiemBD = null; // config đầu ca

            // Đóng TẤT CẢ silo đang active của lò này (ngoại trừ đúng ngày/ca đang lưu)
            // Khi thêm config mới cho bất kỳ silo nào → toàn bộ lò chuyển sang batch mới
            var actives = await _context.LG_NL_Mapping
                .Where(m => m.IDLoCao    == entity.IDLoCao
                         && m.NgayHetHL  == null
                         && m.ThoiDiemBD == null
                         && !(m.Ngay == entity.Ngay && m.IDCa == entity.IDCa))
                .ToListAsync();

            foreach (var m in actives)
            {
                m.NgayHetHL = entity.Ngay;
                m.IDCaHetHL = entity.IDCa;
            }

            // Xóa config cũ của đúng silo này trong cùng (Ngay, IDCa) nếu đang ghi đè
            // Không xóa silo khác → nhiều silo có thể cùng 1 batch (Ngay, IDCa)
            var existing = await _context.LG_NL_Mapping
                .Where(m => m.IDLoCao    == entity.IDLoCao
                         && m.Ngay       == entity.Ngay
                         && m.IDCa       == entity.IDCa
                         && m.IDSiLo     == entity.IDSiLo
                         && m.ThoiDiemBD == null)
                .ToListAsync();
            _context.LG_NL_Mapping.RemoveRange(existing);

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

        public async Task<LG_NL_Mapping> ChangeSiLoNVLAsync(
            int idLoCao, DateOnly ngay, int idCa, int idSiLo, int idNVLMoi,
            DateTime thoiDiem, string? ghiChu)
        {
            // Không xóa row cũ — giữ lại để data trước thời điểm này vẫn map đúng NVL cũ
            var newRow = new LG_NL_Mapping
            {
                IDLoCao    = idLoCao,
                Ngay       = ngay,
                IDCa       = idCa,
                IDSiLo     = idSiLo,
                IDNVL      = idNVLMoi,
                ThoiDiemBD = thoiDiem,
                NgayHetHL  = null,
                IDCaHetHL  = null,
                GhiChu     = ghiChu,
                NgayTao    = DateTime.Now,
            };

            await _context.LG_NL_Mapping.AddAsync(newRow);
            await _context.SaveChangesAsync();
            return newRow;
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
                orderby n.IDLoCao, nh.ThuTu, n.ThuTu, n.TenNVL
                select new LGNLNvlDto
                {
                    ID           = n.ID,
                    IDLoCao      = n.IDLoCao,
                    IDNhomNVL    = n.IDNhomNVL,
                    TenNVL       = n.TenNVL,
                    ThuTu        = n.ThuTu,
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
            existing.ThuTu      = entity.ThuTu;
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

        //public async Task<LGNLDuLieuSiLoResult> GetDuLieuSiloPivotAsync(
        //    DateOnly ngay, int idCa, int idLoCao)
        //{
        //    // 1. Lấy danh sách silo-NVL mapping cho ca/ngày/lò cao
        //    var mappings = await (
        //        from m in _context.LG_NL_Mapping
        //        join s in _context.LG_NL_SiLo on m.IDSiLo equals s.ID into sg
        //        from s in sg.DefaultIfEmpty()
        //        join n in _context.LG_NL_NVL on m.IDNVL equals n.ID into ng
        //        from n in ng.DefaultIfEmpty()
        //        join nh in _context.LG_NL_NhomNVL on n.IDNhomNVL equals nh.ID into nhg
        //        from nh in nhg.DefaultIfEmpty()
        //        where m.IDLoCao == idLoCao && m.IDCa == idCa && m.Ngay == ngay
        //        orderby nh != null ? nh.ThuTu : 999, s != null ? s.ThuTu : 999
        //        select new
        //        {
        //            TagKey    = s != null ? s.TagKey : null,
        //            SiLoId    = s != null ? s.ID : 0,
        //            TenNVL    = n != null ? n.TenNVL : (s != null ? s.TenSiLo : null),
        //            DonVi     = n != null ? n.DonVi : null,
        //            TenNhom   = nh != null ? nh.TenNhom : null,
        //            ThuTuNhom = nh != null ? (nh.ThuTu ?? 999) : 999,
        //        }
        //    ).AsNoTracking().ToListAsync();

        //    var valid = mappings.Where(m => !string.IsNullOrEmpty(m.TagKey)).ToList();
        //    if (valid.Count == 0)
        //        return new LGNLDuLieuSiLoResult();

        //    // Gán dataIndex duy nhất cho mỗi silo
        //    var withIndex = valid.Select(m => new
        //    {
        //        m.TagKey,
        //        DataIndex = $"silo_{m.SiLoId}",
        //        m.TenNVL,
        //        m.DonVi,
        //        m.TenNhom,
        //        m.ThuTuNhom,
        //    }).ToList();

        //    // 2. Tính khung thời gian theo ca
        //    DateTime timeFrom, timeTo;
        //    if (idCa == 1) // Ca ngày: 07:00 → 19:00
        //    {
        //        timeFrom = ngay.ToDateTime(new TimeOnly(7, 0));
        //        timeTo   = ngay.ToDateTime(new TimeOnly(19, 0));
        //    }
        //    else // Ca đêm: 19:00 → 07:00 hôm sau
        //    {
        //        timeFrom = ngay.ToDateTime(new TimeOnly(19, 0));
        //        timeTo   = ngay.AddDays(1).ToDateTime(new TimeOnly(7, 0));
        //    }

        //    // 3. Truy vấn SCADA data — bao gồm TS0 (số mẻ) ngoài các silo tag
        //    var tagKeys = withIndex.Select(m => m.TagKey!).Distinct()
        //        .Append("TS0").Distinct().ToList();
        //    var rawData = await _context.LG1_DuLieuNL
        //        .Where(d => d.ID_LoCao == idLoCao
        //                 && d.Time >= timeFrom
        //                 && d.Time < timeTo
        //                 && tagKeys.Contains(d.TagKey!))
        //        .OrderBy(d => d.Time)
        //        .AsNoTracking()
        //        .ToListAsync();

        //    // 4. Xây dựng cấu trúc cột (nhóm theo NhomNVL)
        //    var tagToIndex = withIndex
        //        .GroupBy(m => m.TagKey!)
        //        .ToDictionary(g => g.Key, g => g.First().DataIndex);

        //    var groups = withIndex
        //        .GroupBy(m => m.TenNhom)
        //        .OrderBy(g => g.Min(m => m.ThuTuNhom));

        //    var columns = new List<LGNLColumnDto>();
        //    foreach (var grp in groups)
        //    {
        //        var leaves = grp
        //            .GroupBy(m => m.DataIndex)
        //            .Select(g => new LGNLColumnDto
        //            {
        //                Title     = g.First().TenNVL ?? g.Key,
        //                DataIndex = g.Key,
        //            })
        //            .ToList();

        //        if (grp.Key == null || leaves.Count == 1)
        //            columns.AddRange(leaves);
        //        else
        //            columns.Add(new LGNLColumnDto { Title = grp.Key, Children = leaves });
        //    }

        //    // 5. Pivot theo timestamp (mỗi time = 1 mẻ nạp)
        //    var rows = rawData
        //        .GroupBy(d => d.Time)
        //        .OrderBy(g => g.Key)
        //        .Select((g, idx) =>
        //        {
        //            var row = new Dictionary<string, object?>
        //            {
        //                ["id"]   = idx + 1,
        //                ["time"] = g.Key?.ToString("yyyy-MM-ddTHH:mm:ss"),
        //            };
        //            foreach (var d in g)
        //            {
        //                if (d.TagKey == "TS0")
        //                {
        //                    row["soMe"] = d.Value;
        //                    continue;
        //                }

        //                if (d.TagKey != null && tagToIndex.TryGetValue(d.TagKey, out var di))
        //                    row[di] = d.Value;
        //            }
        //            return row;
        //        })
        //        .ToList();

        //    return new LGNLDuLieuSiLoResult { Columns = columns, Rows = rows };
        //}

        // ─── Dữ liệu theo LoCao, Ngày ───────────────────────────────
        public async Task<LGNLDuLieuSiLoResult> GetDuLieuSiloPivotAsync(
            DateOnly ngay, int idCa, int idLoCao)
        {
            // 1. Tìm (Ngay, IDCa) có config hiệu lực tại thời điểm yêu cầu
            //    Hiệu lực khi: bắt đầu ≤ yêu cầu VÀ chưa kết thúc
            //    Chỉ xét row ThoiDiemBD=null (config đầu ca) để định vị phiên cấu hình
            var configRef = await _context.LG_NL_Mapping
                .Where(m => m.IDLoCao    == idLoCao
                         && m.ThoiDiemBD == null
                         && (m.Ngay < ngay || (m.Ngay == ngay && m.IDCa <= idCa))
                         && (m.NgayHetHL == null
                             || m.NgayHetHL > ngay
                             || (m.NgayHetHL == ngay && m.IDCaHetHL > idCa)))
                .OrderByDescending(m => m.Ngay)
                .ThenByDescending(m => m.IDCa)
                .Select(m => new { m.Ngay, m.IDCa })
                .FirstOrDefaultAsync();

            if (configRef == null)
                return new LGNLDuLieuSiLoResult();

            // 2. Tính khung thời gian SCADA theo ngay/idCa gốc (không đổi)
            DateTime timeFrom, timeTo;
            if (idCa == 1)
            {
                timeFrom = ngay.ToDateTime(new TimeOnly(7, 30));
                timeTo   = ngay.ToDateTime(new TimeOnly(19, 30));
            }
            else
            {
                timeFrom = ngay.ToDateTime(new TimeOnly(19, 30));
                timeTo   = ngay.AddDays(1).ToDateTime(new TimeOnly(7, 30));
            }

            // 3. Lấy TẤT CẢ mapping của config hiệu lực
            //    Gồm cả row ThoiDiemBD khác null (đổi NVL giữa ca)
            var mappings = await (
                from m in _context.LG_NL_Mapping
                join s  in _context.LG_NL_SiLo    on m.IDSiLo    equals s.ID  into sg
                from s  in sg.DefaultIfEmpty()
                join n  in _context.LG_NL_NVL      on m.IDNVL     equals n.ID  into ng
                from n  in ng.DefaultIfEmpty()
                join nh in _context.LG_NL_NhomNVL  on n.IDNhomNVL equals nh.ID into nhg
                from nh in nhg.DefaultIfEmpty()
                where m.IDLoCao == idLoCao
                   && m.Ngay    == configRef.Ngay
                   && m.IDCa    == configRef.IDCa
                   && s.TagKey  != null
                orderby nh != null ? nh.ThuTu : 999,
                        s  != null ? s.ThuTu  : 999,
                        m.ThoiDiemBD == null ? 0 : 1,
                        m.ThoiDiemBD
                select new
                {
                    TagKey     = s!.TagKey!,
                    IDNVL      = n != null ? n.ID : 0,
                    ThoiDiemBD = m.ThoiDiemBD,
                    TenNVL     = n != null ? n.TenNVL : s.TenSiLo,
                    TenNhom    = nh != null ? nh.TenNhom : null,
                    ThuTuNhom  = nh != null ? (nh.ThuTu ?? 999) : 999,
                }
            ).AsNoTracking().ToListAsync();

            if (mappings.Count == 0)
                return new LGNLDuLieuSiLoResult();

            // 4. Xây dựng timeline theo TagKey: [(ThoiDiemBD, DataIndex)]
            //    Mỗi đoạn thời gian → 1 NVL → 1 DataIndex
            //    DataIndex = nvl_{IDNVL} (gộp silo theo NVL, nhất quán với logic cũ)
            var tagTimeline = mappings
                .GroupBy(m => m.TagKey)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(m => m.ThoiDiemBD ?? DateTime.MinValue)
                           .Select(m => (
                               From      : m.ThoiDiemBD ?? timeFrom,
                               DataIndex : $"nvl_{m.IDNVL}",
                               m.TenNVL,
                               m.TenNhom,
                               m.ThuTuNhom,
                               m.IDNVL
                           ))
                           .ToList()
                );

            // Resolver: (tagKey, time) → DataIndex đang hiệu lực tại thời điểm đó
            string? ResolveDataIndex(string tagKey, DateTime time)
            {
                if (!tagTimeline.TryGetValue(tagKey, out var tl)) return null;
                var seg = tl.LastOrDefault(t => t.From <= time);
                return seg.DataIndex;
            }

            // 5. Query SCADA
            var tagKeys = tagTimeline.Keys.Append("TS0").Distinct().ToList();
            var rawData = await _context.LG1_DuLieuNL
                .Where(d => d.ID_LoCao == idLoCao
                         && d.Time >= timeFrom
                         && d.Time <  timeTo
                         && tagKeys.Contains(d.TagKey!))
                .OrderBy(d => d.Time)
                .AsNoTracking()
                .ToListAsync();

            // 6. Build columns — distinct (IDNVL) có thể xuất hiện từ nhiều silo/thời đoạn
            var allSegments = tagTimeline.Values
                .SelectMany(tl => tl)
                .GroupBy(seg => seg.DataIndex)
                .Select(g => g.First())
                .ToList();

            var colGroups = allSegments
                .GroupBy(seg => seg.TenNhom)
                .OrderBy(g => g.Min(seg => seg.ThuTuNhom));

            var columns = new List<LGNLColumnDto>();
            foreach (var grp in colGroups)
            {
                var leaves = grp
                    .GroupBy(seg => seg.DataIndex)
                    .Select(g => new LGNLColumnDto
                    {
                        Title     = g.First().TenNVL ?? g.Key,
                        DataIndex = g.Key,
                    })
                    .ToList();

                if (grp.Key == null)
                    columns.AddRange(leaves);
                else
                    columns.Add(new LGNLColumnDto { Title = grp.Key, Children = leaves });
            }

            // 7. Pivot: mỗi timestamp → 1 row, giá trị cộng vào đúng nvl_{IDNVL}
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
                        if (d.TagKey == "TS0") { row["soMe"] = d.Value; continue; }
                        if (d.TagKey == null || d.Time == null) continue;

                        var di = ResolveDataIndex(d.TagKey, d.Time.Value);
                        if (di == null) continue;

                        row[di] = row.TryGetValue(di, out var cur)
                            ? Convert.ToDecimal(cur) + Convert.ToDecimal(d.Value ?? 0f)
                            : Convert.ToDecimal(d.Value ?? 0f);
                    }

                    return row;
                })
                .ToList();

            return new LGNLDuLieuSiLoResult
            {
                Columns     = columns,
                Rows        = rows,
                NgayHieuLuc = configRef.Ngay,
                IDCaHieuLuc = configRef.IDCa,
            };
        }
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

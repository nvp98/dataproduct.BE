using dataproduct.api.DTOs.LGNL_Dto;
using dataproduct.api.Models;
using dataproduct.api.Repositories;

namespace dataproduct.api.Services
{
    public class LGNLService
    {
        private readonly ILGNLRepository _repo;

        // TagKey cố định cho Số mẻ (TS0)
        private const string TS0_KEY = "XUAT.TS0";

        public LGNLService(ILGNLRepository repo)
        {
            _repo = repo;
        }

        // ─── TS Mapping lookup ───────────────────────────────────────────────

        public Task<List<LGNLTsMappingDto>> GetTsMappingListAsync()
            => _repo.GetTsMappingListAsync();

        // ─── Dữ liệu Silo pivot (cho BM ISO) ────────────────────────────────

        /// <summary>
        /// Tính khoảng thời gian theo ca.
        /// Ca 1: 07:30 → 19:30 | Ca 2: 19:30 → 07:30 hôm sau.
        /// </summary>
        public static (DateTime from, DateTime to) GetCaRange(DateOnly ngay, int idCa)
        {
            var d = ngay.ToDateTime(TimeOnly.MinValue);
            return idCa == 1
                ? (d.AddHours(7).AddMinutes(30), d.AddHours(19).AddMinutes(30))
                : (d.AddHours(19).AddMinutes(30), d.AddDays(1).AddHours(7).AddMinutes(30));
        }

        /// <summary>
        /// Trả về dữ liệu nạp liệu đã pivot theo thời gian + cột theo NVL mapping.
        /// - rows: mỗi dòng = 1 mẻ nạp, key = MaNVL (hoặc "soMe" cho TS0).
        /// - columns: định nghĩa cột cho Ant Design Table (có thể có children).
        /// </summary>
        public async Task<LGNLDuLieuSiLoResult> GetDuLieuSiLoAsync(DateOnly ngay, int idCa, int idLoCao)
        {
            // 1. Lấy mapping cho ca này (đã JOIN Silo + NVL, có TagKey + MaNVL)
            var mappings = await _repo.GetMappingListAsync(ngay, idCa, idLoCao);

            // 2. Xây dựng TagKey → thông tin NVL (chỉ lấy entry có đủ TagKey + MaNVL)
            var tagKeyToNvl = mappings
                .Where(m => !string.IsNullOrEmpty(m.TagKey) && !string.IsNullOrEmpty(m.MaNVL))
                .GroupBy(m => m.TagKey!)
                .ToDictionary(g => g.Key, g => g.First());

            // 3. Danh sách TagKey cần query (gồm cả TS0 cho Số mẻ)
            var tagKeys = tagKeyToNvl.Keys
                .Append(TS0_KEY)
                .Distinct()
                .ToList();

            // 4. Khoảng thời gian theo ca
            var (timeFrom, timeTo) = GetCaRange(ngay, idCa);

            // 5. Lấy dữ liệu thô
            var rawData = await _repo.GetDuLieuRawAsync(tagKeys, idLoCao, timeFrom, timeTo);

            // 6. Pivot theo phút (mỗi nhóm = 1 mẻ)
            var rows = rawData
                .Where(d => d.Time.HasValue)
                .GroupBy(d => new DateTime(
                    d.Time!.Value.Year, d.Time.Value.Month, d.Time.Value.Day,
                    d.Time.Value.Hour, d.Time.Value.Minute, 0))
                .OrderBy(g => g.Key)
                .Select(g =>
                {
                    var row = new Dictionary<string, object?> { { "time", g.Key } };
                    foreach (var item in g)
                    {
                        if (string.IsNullOrEmpty(item.TagKey)) continue;

                        if (item.TagKey == TS0_KEY)
                        {
                            row["soMe"] = item.Value;
                        }
                        else if (tagKeyToNvl.TryGetValue(item.TagKey, out var nvl))
                        {
                            row[nvl.MaNVL!] = item.Value;
                        }
                    }
                    return row;
                })
                .ToList();

            // 7. Xây dựng định nghĩa cột (group by NhomHienThi)
            var orderedMappings = mappings
                .Where(m => !string.IsNullOrEmpty(m.TagKey) && !string.IsNullOrEmpty(m.MaNVL))
                .OrderBy(m => m.ThuTuNhom ?? 999)
                .ThenBy(m => m.IDNVL)
                .ToList();

            var columns = new List<LGNLColumnDto>();
            var processedGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var m in orderedMappings)
            {
                if (string.IsNullOrEmpty(m.NhomHienThi))
                {
                    // Cột lá — không thuộc nhóm nào
                    columns.Add(new LGNLColumnDto
                    {
                        Title     = m.TenNVL ?? m.MaNVL ?? "",
                        DataIndex = m.MaNVL
                    });
                }
                else if (!processedGroups.Contains(m.NhomHienThi))
                {
                    // Cột cha — thu thập toàn bộ children cùng nhóm
                    processedGroups.Add(m.NhomHienThi);
                    var children = orderedMappings
                        .Where(x => string.Equals(x.NhomHienThi, m.NhomHienThi, StringComparison.OrdinalIgnoreCase))
                        .Select(x => new LGNLColumnDto
                        {
                            Title     = x.TenNVL ?? x.MaNVL ?? "",
                            DataIndex = x.MaNVL
                        })
                        .ToList();

                    columns.Add(new LGNLColumnDto
                    {
                        Title    = m.NhomHienThi,
                        Children = children
                    });
                }
            }

            return new LGNLDuLieuSiLoResult { Columns = columns, Rows = rows };
        }

        // ─── SiLo Master ─────────────────────────────────────────────────────

        public async Task<List<LGNLSiLoMasterDto>> GetSiLoMasterListAsync(int? idLoCao)
        {
            var list = await _repo.GetSiLoMasterListAsync(idLoCao);
            return list.Select(MapSiLoMaster).ToList();
        }

        public async Task<LGNLSiLoMasterDto?> GetSiLoMasterByIdAsync(int id)
        {
            var e = await _repo.GetSiLoMasterByIdAsync(id);
            return e == null ? null : MapSiLoMaster(e);
        }

        public async Task<LGNLSiLoMasterDto> AddSiLoMasterAsync(CreateLGNLSiLoMasterDto dto)
        {
            var entity = new LG_NL_SiLo
            {
                IDLoCao = dto.IDLoCao,
                TenSiLo = dto.TenSiLo,
                ThuTu   = dto.ThuTu,
                TagKey  = dto.TagKey
            };
            var result = await _repo.AddSiLoMasterAsync(entity);
            return MapSiLoMaster(result);
        }

        public async Task<LGNLSiLoMasterDto?> UpdateSiLoMasterAsync(int id, UpdateLGNLSiLoMasterDto dto)
        {
            var entity = new LG_NL_SiLo
            {
                IDLoCao = dto.IDLoCao,
                TenSiLo = dto.TenSiLo,
                ThuTu   = dto.ThuTu,
                TagKey  = dto.TagKey
            };
            var result = await _repo.UpdateSiLoMasterAsync(id, entity);
            return result == null ? null : MapSiLoMaster(result);
        }

        public Task<bool> DeleteSiLoMasterAsync(int id) => _repo.DeleteSiLoMasterAsync(id);

        // ─── Mapping ─────────────────────────────────────────────────────────

        public Task<List<LGNLMappingDto>> GetMappingListAsync(DateOnly? ngay, int? idCa, int? idLoCao)
            => _repo.GetMappingListAsync(ngay, idCa, idLoCao);

        public async Task<LGNLMappingDto?> GetMappingByIdAsync(int id)
        {
            var e = await _repo.GetMappingByIdAsync(id);
            return e == null ? null : MapMapping(e);
        }

        public async Task<LGNLMappingDto> AddMappingAsync(CreateLGNLMappingDto dto)
        {
            var entity = new LG_NL_Mapping
            {
                Ngay    = dto.Ngay,
                IDCa    = dto.IDCa,
                IDLoCao = dto.IDLoCao,
                IDSiLo  = dto.IDSiLo,
                IDNVL   = dto.IDNVL,
                GhiChu  = dto.GhiChu
            };
            var result = await _repo.AddMappingAsync(entity);
            return MapMapping(result);
        }

        public async Task<LGNLMappingDto?> UpdateMappingAsync(int id, UpdateLGNLMappingDto dto)
        {
            var entity = new LG_NL_Mapping
            {
                Ngay    = dto.Ngay,
                IDCa    = dto.IDCa,
                IDLoCao = dto.IDLoCao,
                IDSiLo  = dto.IDSiLo,
                IDNVL   = dto.IDNVL,
                GhiChu  = dto.GhiChu
            };
            var result = await _repo.UpdateMappingAsync(id, entity);
            return result == null ? null : MapMapping(result);
        }

        public Task<bool> DeleteMappingAsync(int id) => _repo.DeleteMappingAsync(id);

        // ─── NVL ─────────────────────────────────────────────────────────────

        public async Task<List<LGNLNvlDto>> GetNvlListAsync(DateOnly? ngay, int? idCa, int? idLoCao)
        {
            var list = await _repo.GetNvlListAsync(ngay, idCa, idLoCao);
            return list.Select(MapNvl).ToList();
        }

        public async Task<LGNLNvlDto?> GetNvlByIdAsync(int id)
        {
            var e = await _repo.GetNvlByIdAsync(id);
            return e == null ? null : MapNvl(e);
        }

        public async Task<LGNLNvlDto> AddNvlAsync(CreateLGNLNvlDto dto)
        {
            var entity = new LG_NL_NVL
            {
                Ngay         = dto.Ngay,
                IDCa         = dto.IDCa,
                IDLoCao      = dto.IDLoCao,
                MaNVL        = dto.MaNVL,
                TenNVL       = dto.TenNVL,
                DonVi        = dto.DonVi,
                SoLuong      = dto.SoLuong,
                DoAm         = dto.DoAm,
                GhiChu       = dto.GhiChu,
                NhomHienThi  = dto.NhomHienThi,
                ThuTuNhom    = dto.ThuTuNhom
            };
            var result = await _repo.AddNvlAsync(entity);
            return MapNvl(result);
        }

        public async Task<LGNLNvlDto?> UpdateNvlAsync(int id, UpdateLGNLNvlDto dto)
        {
            var entity = new LG_NL_NVL
            {
                Ngay         = dto.Ngay,
                IDCa         = dto.IDCa,
                IDLoCao      = dto.IDLoCao,
                MaNVL        = dto.MaNVL,
                TenNVL       = dto.TenNVL,
                DonVi        = dto.DonVi,
                SoLuong      = dto.SoLuong,
                DoAm         = dto.DoAm,
                GhiChu       = dto.GhiChu,
                NhomHienThi  = dto.NhomHienThi,
                ThuTuNhom    = dto.ThuTuNhom
            };
            var result = await _repo.UpdateNvlAsync(id, entity);
            return result == null ? null : MapNvl(result);
        }

        public Task<bool> DeleteNvlAsync(int id) => _repo.DeleteNvlAsync(id);

        // ─── Mappers ─────────────────────────────────────────────────────────

        private static LGNLSiLoMasterDto MapSiLoMaster(LG_NL_SiLo e) => new()
        {
            ID      = e.ID,
            IDLoCao = e.IDLoCao,
            TenSiLo = e.TenSiLo,
            ThuTu   = e.ThuTu,
            NgayTao = e.NgayTao,
            TagKey  = e.TagKey
        };

        private static LGNLMappingDto MapMapping(LG_NL_Mapping e) => new()
        {
            ID      = e.ID,
            Ngay    = e.Ngay,
            IDCa    = e.IDCa,
            IDLoCao = e.IDLoCao,
            IDSiLo  = e.IDSiLo,
            IDNVL   = e.IDNVL,
            GhiChu  = e.GhiChu,
            NgayTao = e.NgayTao
            // TagKey / MaNVL / NhomHienThi = null — dùng GetMappingListAsync để có join đầy đủ
        };

        private static LGNLNvlDto MapNvl(LG_NL_NVL e) => new()
        {
            ID           = e.ID,
            Ngay         = e.Ngay,
            IDCa         = e.IDCa,
            IDLoCao      = e.IDLoCao,
            MaNVL        = e.MaNVL,
            TenNVL       = e.TenNVL,
            DonVi        = e.DonVi,
            SoLuong      = e.SoLuong,
            DoAm         = e.DoAm,
            GhiChu       = e.GhiChu,
            NgayTao      = e.NgayTao,
            NhomHienThi  = e.NhomHienThi,
            ThuTuNhom    = e.ThuTuNhom
        };
    }
}

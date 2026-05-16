using dataproduct.api.DTOs.NMLG_Dto;
using dataproduct.api.Models;
using dataproduct.api.Repositories;
using System.Text.Json;

namespace dataproduct.api.Services
{
    public class LGTSLService
    {
        private readonly ILGTSLRepository _repo;

        public LGTSLService(ILGTSLRepository repo)
        {
            _repo = repo;
        }

        // ─── SiLo ────────────────────────────────────────────────────────────────

        public Task<List<LGTSSiLoDto>> GetAllSiLoListAsync(int? idLoCao)
            => _repo.GetSiLoListAsync(idLoCao);

        public Task<List<LGTSSiLoMappingViewDto>> GetSiLoByMappingAsync(int? idLoCao, DateTime? ngay, int? ca)
            => _repo.GetSiLoByMappingAsync(idLoCao, ngay, ca);
        public async Task<LGTSSiLoDto?> GetSiLoByIdAsync(int id)
        {
            var e = await _repo.GetSiLoByIdAsync(id);
            return e == null ? null : MapSiLo(e);
        }

        public async Task<LGTSSiLoDto> AddSiLoAsync(CreateLGTSSiLoDto dto)
        {
            var entity = new LG_TSL_SiLo
            {
                ID_LoCao = dto.IDLoCao,
                TenSiLo = dto.TenSiLo,
                ThuTu = dto.ThuTu,
            };
            var result = await _repo.AddSiLoAsync(entity);
            return MapSiLo(result);
        }

        public async Task<LGTSSiLoDto?> UpdateSiLoAsync(int id, UpdateLGTSSiLoDto dto)
        {
            var entity = new LG_TSL_SiLo
            {
                ID_LoCao = dto.IDLoCao,
                TenSiLo = dto.TenSiLo,
                ThuTu = dto.ThuTu,
            };
            var result = await _repo.UpdateSiLoAsync(id, entity);
            return result == null ? null : MapSiLo(result);
        }

        public Task<bool> DeleteSiLoAsync(int id) => _repo.DeleteSiLoAsync(id);

        // ─── NVL ─────────────────────────────────────────────────────────────────

        public Task<List<LGTSNvlDto>> GetNvlListAsync(int? idLoCao)
            => _repo.GetNvlListAsync(idLoCao);

        public async Task<LGTSNvlDto?> GetNvlByIdAsync(int id)
        {
            var e = await _repo.GetNvlByIdAsync(id);
            return e == null ? null : MapNvl(e);
        }

        public async Task<LGTSNvlDto> AddNvlAsync(CreateLGTSNvlDto dto)
        {

            var entity = new LG_TSL_NVL
            {
                IDLoCao = dto.IDLoCao,
                TenNVL = dto.TenNVL?.Trim(),
                TenNVL_Tk = dto.TenNVL_TK,
                GhiChu = dto.GhiChu,
                XacNhan = dto.XacNhan,
            };
            var result = await _repo.AddNvlAsync(entity);
            return MapNvl(result);
        }

        public async Task<LGTSNvlDto?> UpdateNvlAsync(int id, UpdateLGTSNvlDto dto)
        {
       

            var entity = new LG_TSL_NVL
            {
                IDLoCao = dto.IDLoCao,
                TenNVL = dto.TenNVL.Trim(),
                TenNVL_Tk = dto.TenNVL_TK,
                GhiChu = dto.GhiChu,
                XacNhan = dto.XacNhan,
            };
            var result = await _repo.UpdateNvlAsync(id, entity);
            return result == null ? null : MapNvl(result);
        }

        public Task<bool> DeleteNvlAsync(int id) => _repo.DeleteNvlAsync(id);

        public Task<bool> UpdateXacNhanAsync(UpdateLGTSXacNhanDto dto)
            => _repo.UpdateXacNhanAsync(dto.ID, dto.XacNhan);

        // ─── Mapping ─────────────────────────────────────────────────────────────

        public Task<List<LGTSMappingDto>> GetMappingListAsync(int? idLoCao, DateTime? ngay, int? ca)
            => _repo.GetMappingListAsync(idLoCao, ngay, ca);

        public async Task<LGTSMappingDto?> GetMappingByIdAsync(int id)
        {
            var e = await _repo.GetMappingByIdAsync(id);
            return e == null ? null : MapMapping(e);
        }

        public async Task<LGTSMappingDto> AddMappingAsync(CreateLGTSMappingDto dto)
        {
            // IDSiLo trong Mapping lưu ThuTu (không phải ID) → tra cứu ThuTu từ silo ID
            var thuTu = await _repo.GetSiLoThuTuAsync(dto.IDSiLo, dto.IDLoCao)
                        ?? throw new InvalidOperationException($"Silo ID={dto.IDSiLo} không tồn tại hoặc chưa có ThuTu.");

            var entity = new LG_TSL_SiLo_Mapping
            {
                IDLoCao = dto.IDLoCao,
                IDSiLo  = thuTu,
                IDNVL   = dto.IDNVL,
                Ngay    = dto.Ngay,
                Ca      = dto.Ca,
                GhiChu  = dto.GhiChu,
            };
            var result = await _repo.AddMappingAsync(entity);
            return MapMapping(result);
        }

        public async Task<LGTSMappingDto?> UpdateMappingAsync(int id, UpdateLGTSMappingDto dto)
        {
            // IDSiLo trong Mapping lưu ThuTu → tra cứu ThuTu từ silo ID
            var thuTu = await _repo.GetSiLoThuTuAsync(dto.IDSiLo, dto.IDLoCao)
                        ?? throw new InvalidOperationException($"Silo ID={dto.IDSiLo} không tồn tại hoặc chưa có ThuTu.");

            var entity = new LG_TSL_SiLo_Mapping
            {
                IDLoCao = dto.IDLoCao,
                IDSiLo  = thuTu,
                IDNVL   = dto.IDNVL,
                Ngay    = dto.Ngay,
                Ca      = dto.Ca,
                GhiChu  = dto.GhiChu,
            };
            var result = await _repo.UpdateMappingAsync(id, entity);
            return result == null ? null : MapMapping(result);
        }

        public Task<bool> DeleteMappingAsync(int id) => _repo.DeleteMappingAsync(id);

        // ─── Chi tiết tồn silo theo phiếu ────────────────────────────────────────

        public Task UpsertChiTietAsync(UpsertLGTSChiTietDto dto) => _repo.UpsertChiTietAsync(dto);

        public Task<List<LGTSChiTietDto>> GetChiTietByPhieuAsync(Guid idPhieu) => _repo.GetChiTietByPhieuAsync(idPhieu);


        // ─── InsertFromPhieu ──────────────────────────────────────────────────────

        public async Task<int> InsertFromPhieuJsonAsync(BmPhieu phieu)
        {
            if (phieu == null || string.IsNullOrWhiteSpace(phieu.DataJson))
                return 0;

            using var jsonDoc = JsonDocument.Parse(phieu.DataJson);
            var root = jsonDoc.RootElement;

            var ngayStr = TryGetString(root, "NgaySX", "ngaySX", "ngay");
            var idLoCao = TryGetInt(root, "scope", "Scope", "idLoCao", "IDLoCao") ?? 0;
            var ca = TryGetInt(root, "ca", "Ca") ?? 0;

            if (idLoCao == 0 || ca == 0 || string.IsNullOrWhiteSpace(ngayStr))
                return 0;

            if (!DateTime.TryParse(ngayStr, out var ngay))
                return 0;

            if (!TryGetArray(root, "table1", out var table1))
                return 0;

            var items = new List<UpsertLGTSChiTietItemDto>();
            foreach (var row in table1.EnumerateArray())
            {
                if (row.ValueKind != JsonValueKind.Object)
                    continue;

                var idSiLo = TryGetInt(row, "idSiLo", "IDSiLo");
                if (idSiLo == null || idSiLo == 0)
                    continue;

                items.Add(new UpsertLGTSChiTietItemDto
                {
                    IDSiLo = idSiLo.Value,
                    IDMapping = TryGetInt(row, "idMapping", "IDMapping"),
                    IDNVL = TryGetInt(row, "idNVL", "IDNVL"),
                    TenSiLo = TryGetString(row, "silo", "tenSiLo", "TenSiLo"),
                    TenNVL = TryGetString(row, "loaiNguyenNhienLieu", "tenNVL", "TenNVL"),
                    KLTonCuoiKip = TryGetDecimal(row, "klTonCuoiKip", "KLTonCuoiKip", "ton"),
                    GhiChu = TryGetString(row, "ghiChu", "GhiChu"),
                    ThuTu = TryGetInt(row, "thuTu", "ThuTu", "stt"),
                });
            }

            // Replace mode: luôn xóa dữ liệu cũ của phiếu trước khi ghi mới.
            await _repo.DeleteByPhieuIdAsync(phieu.Idphieu);

            await _repo.UpsertChiTietAsync(new UpsertLGTSChiTietDto
            {
                IDPhieu = phieu.Idphieu,
                IDLoCao = idLoCao,
                Ngay = ngay,
                Ca = ca,
                Items = items,
            });

            return items.Count;
        }

        // ─── Mappers ─────────────────────────────────────────────────────────────

        private static LGTSSiLoDto MapSiLo(LG_TSL_SiLo e) => new()
        {
            ID = e.ID,
            IDLoCao = e.ID_LoCao,
            TenSiLo = e.TenSiLo,
            ThuTu = e.ThuTu,
        };

        private static LGTSNvlDto MapNvl(LG_TSL_NVL e) => new()
        {
            ID = e.ID,
            IDLoCao = e.IDLoCao,
            TenNVL = e.TenNVL,
            TenNVL_TK = e.TenNVL_Tk,
            GhiChu = e.GhiChu,
            NgayTao = e.NgayTao,
            XacNhan = e.XacNhan,
            NgayXacNhan = e.NgayXacNhan,
            IDNguoiXacNhan = e.IDNguoiXacNhan,
        };

        private static LGTSMappingDto MapMapping(LG_TSL_SiLo_Mapping e) => new()
        {
            ID = e.ID,
            IDLoCao = e.IDLoCao,
            IDSiLo = e.IDSiLo,
            IDNVL = e.IDNVL,
            Ngay = e.Ngay,
            Ca = e.Ca,
            GhiChu = e.GhiChu,
            NgayTao = e.NgayTao,
            NguoiTao = e.NguoiTao,
        };

        private static bool TryGetArray(JsonElement obj, string key, out JsonElement array)
        {
            array = default;
            if (obj.TryGetProperty(key, out var el) && el.ValueKind == JsonValueKind.Array)
            {
                array = el;
                return true;
            }
            return false;
        }

        private static string? TryGetString(JsonElement obj, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (obj.TryGetProperty(key, out var val) && val.ValueKind != JsonValueKind.Null)
                    return val.ValueKind == JsonValueKind.String ? val.GetString() : val.ToString();
            }
            return null;
        }

        private static int? TryGetInt(JsonElement obj, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (!obj.TryGetProperty(key, out var val) || val.ValueKind == JsonValueKind.Null)
                    continue;

                if (val.ValueKind == JsonValueKind.Number && val.TryGetInt32(out var n))
                    return n;

                if (val.ValueKind == JsonValueKind.String && int.TryParse(val.GetString(), out var s))
                    return s;
            }
            return null;
        }

        private static decimal? TryGetDecimal(JsonElement obj, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (!obj.TryGetProperty(key, out var val) || val.ValueKind == JsonValueKind.Null)
                    continue;

                if (val.ValueKind == JsonValueKind.Number && val.TryGetDecimal(out var d))
                    return d;

                if (val.ValueKind == JsonValueKind.String && decimal.TryParse(val.GetString(), out var s))
                    return s;
            }
            return null;
        }
    }
}

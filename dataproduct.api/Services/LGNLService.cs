using dataproduct.api.DTOs.NMLG_Dto;
using dataproduct.api.Models;
using dataproduct.api.Repositories;

namespace dataproduct.api.Services
{
    public class LGNLService
    {
        private readonly ILGNLRepository _repo;

        public LGNLService(ILGNLRepository repo)
        {
            _repo = repo;
        }
        // ─── TS Mapping lookup ───────────────────────────────────────────────

        public Task<List<LGNLTsMappingDto>> GetTsMappingListAsync()
            => _repo.GetTsMappingListAsync();

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

        public Task<List<LGNLMappingDto>> GetMappingListAsync(DateTime? ngay, int? idCa, int? idLoCao)
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

        // ─── Nhóm NVL ─────────────────────────────────────────────────────────

        public async Task<List<LGNLNhomNvlDto>> GetNhomNvlListAsync(int? idLoCao)
        {
            var list = await _repo.GetNhomNvlListAsync(idLoCao);
            return list.Select(MapNhomNvl).ToList();
        }

        public async Task<LGNLNhomNvlDto?> GetNhomNvlByIdAsync(int id)
        {
            var e = await _repo.GetNhomNvlByIdAsync(id);
            return e == null ? null : MapNhomNvl(e);
        }

        public async Task<LGNLNhomNvlDto> AddNhomNvlAsync(CreateLGNLNhomNvlDto dto)
        {
            var entity = new LG_NL_NhomNVL
            {
                IDLoCao = dto.IDLoCao,
                TenNhom = dto.TenNhom,
                ThuTu   = dto.ThuTu,
                GhiChu  = dto.GhiChu
            };
            var result = await _repo.AddNhomNvlAsync(entity);
            return MapNhomNvl(result);
        }

        public async Task<LGNLNhomNvlDto?> UpdateNhomNvlAsync(int id, UpdateLGNLNhomNvlDto dto)
        {
            var entity = new LG_NL_NhomNVL
            {
                IDLoCao = dto.IDLoCao,
                TenNhom = dto.TenNhom,
                ThuTu   = dto.ThuTu,
                GhiChu  = dto.GhiChu
            };
            var result = await _repo.UpdateNhomNvlAsync(id, entity);
            return result == null ? null : MapNhomNvl(result);
        }

        public Task<bool> DeleteNhomNvlAsync(int id) => _repo.DeleteNhomNvlAsync(id);

        // ─── NVL ─────────────────────────────────────────────────────────────

        public Task<List<LGNLNvlDto>> GetNvlListAsync(int? idLoCao)
            => _repo.GetNvlListAsync(idLoCao);

        public async Task<LGNLNvlDto?> GetNvlByIdAsync(int id)
        {
            var e = await _repo.GetNvlByIdAsync(id);
            return e == null ? null : MapNvlEntity(e);
        }

        public async Task<LGNLNvlDto> AddNvlAsync(CreateLGNLNvlDto dto)
        {
            var entity = new LG_NL_NVL
            {
                IDLoCao     = dto.IDLoCao,
                IDNhomNVL   = dto.IDNhomNVL,
                TenNVL_NM   = dto.TenNVL_NM,
                ThuTu       = dto.ThuTu,
                GhiChu      = dto.GhiChu,
            };
            var result = await _repo.AddNvlAsync(entity);
            return MapNvlEntity(result);
        }

        public async Task<LGNLNvlDto?> UpdateNvlAsync(int id, UpdateLGNLNvlDto dto)
        {
            var entity = new LG_NL_NVL
            {
                IDLoCao     = dto.IDLoCao,
                IDNhomNVL   = dto.IDNhomNVL,
                TenNVL_NM   = dto.TenNVL_NM,
                TenNVL_TK = dto.TenNVL_TK,
                XacNhan = dto.XacNhan,
                ThuTu       = dto.ThuTu,
                GhiChu      = dto.GhiChu,
            };
            var result = await _repo.UpdateNvlAsync(id, entity);
            return result == null ? null : MapNvlEntity(result);
        }

        public Task<bool> DeleteNvlAsync(int id) => _repo.DeleteNvlAsync(id);

        public async Task<bool> UpdateXacNhanAsync(UpdateXacNhanDto dto)
        {
            var entity = await _repo.GetNvlByIdAsync(dto.ID);
            if (entity == null) return false;

            entity.XacNhan = dto.XacNhan;
            entity.NgayXacNhan = DateTime.Now;

            await _repo.UpdateNvlAsync(dto.ID, entity);
            return true;
        }
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
            ID          = e.ID,
            Ngay        = e.Ngay,
            IDCa        = e.IDCa,
            IDLoCao     = e.IDLoCao,
            IDSiLo      = e.IDSiLo,
            IDNVL       = e.IDNVL,
            ThoiDiemBD  = e.ThoiDiemBD,
            NgayHetHL   = e.NgayHetHL,
            IDCaHetHL   = e.IDCaHetHL,
            GhiChu      = e.GhiChu,
            NgayTao     = e.NgayTao
        };

        private static LGNLNhomNvlDto MapNhomNvl(LG_NL_NhomNVL e) => new()
        {
            ID      = e.ID,
            IDLoCao = e.IDLoCao,
            TenNhom = e.TenNhom,
            ThuTu   = e.ThuTu,
            GhiChu  = e.GhiChu,
            NgayTao = e.NgayTao
        };

        private static LGNLNvlDto MapNvlEntity(LG_NL_NVL e) => new()
        {
            ID          = e.ID,
            IDLoCao     = e.IDLoCao,
            IDNhomNVL   = e.IDNhomNVL,
            TenNVL_NM   = e.TenNVL_NM,
            ThuTu       = e.ThuTu,
            GhiChu      = e.GhiChu,
            NgayTao     = e.NgayTao,
        };

        // ─── Dữ liệu theo LoCao, Ngày ───────────────────────────────

        public async Task<List<LGNLDuLieuScadaDto>> GetDataByFilterAsync(
            int? idLoCao, DateTime? ngayBatDau, DateTime? ngayKetThuc)
        {
            return await _repo.GetDataByFilterAsync(idLoCao, ngayBatDau, ngayKetThuc);
        }

        // ─── Pivot dữ liệu nạp liệu theo Silo mapping ───────────────

        public async Task<LGNLDuLieuSiLoResult> GetDuLieuSiloPivotAsync(
            DateTime ngay, int idCa, int idLoCao)
        {
            return await _repo.GetDuLieuSiloPivotAsync(ngay, idCa, idLoCao);
        }

        // ─── Snapshot trạng thái Silo ──────────────────────────────

        public Task<List<LGNLSiloSnapshotDto>> GetSiloSnapshotAsync(
            int idLoCao, DateTime ngay, int idCa)
            => _repo.GetSiloSnapshotAsync(idLoCao, ngay, idCa);

        // ─── Đổi NVL cho silo tại thời điểm cụ thể trong ca ─────────

        public async Task<LG_NL_Mapping> ChangeSiLoNVLAsync(
            int idLoCao, DateTime ngay, int idCa, int idSiLo, int idNVLMoi,
            DateTime thoiDiem, string? ghiChu)
        {
            return await _repo.ChangeSiLoNVLAsync(
                idLoCao, ngay, idCa, idSiLo, idNVLMoi, thoiDiem, ghiChu);
        }
    }
}

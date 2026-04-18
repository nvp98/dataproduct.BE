using dataproduct.api.DTOs.LGNL_Dto;
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
                IDLoCao      = dto.IDLoCao,
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
                IDLoCao      = dto.IDLoCao,
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

        public Task<bool> DeleteNvlAsync(int id) 
        {
            return  _repo.DeleteNvlAsync(id);
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
            ID      = e.ID,
            Ngay    = e.Ngay,
            IDCa    = e.IDCa,
            IDLoCao = e.IDLoCao,
            IDSiLo  = e.IDSiLo,
            IDNVL   = e.IDNVL,
            GhiChu  = e.GhiChu,
            NgayTao = e.NgayTao
        };

        private static LGNLNvlDto MapNvl(LG_NL_NVL e) => new()
        {
            ID          = e.ID,
            IDLoCao     = e.IDLoCao,
            TenNVL      = e.TenNVL,
            DonVi       = e.DonVi,
            SoLuong     = e.SoLuong,
            DoAm        = e.DoAm,
            GhiChu      = e.GhiChu,
            NgayTao     = e.NgayTao,
            NhomHienThi = e.NhomHienThi,
            ThuTuNhom   = e.ThuTuNhom
        };
    }
}

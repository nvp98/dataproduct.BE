using dataproduct.api.DTOs.NMLG_Dto;
using dataproduct.api.Models;
using dataproduct.api.Repositories;

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
            var entity = new LG_TSL_SiLo_Mapping
            {
                IDLoCao = dto.IDLoCao,
                IDSiLo = dto.IDSiLo,
                IDNVL = dto.IDNVL,
                Ngay = dto.Ngay,
                Ca = dto.Ca,
                GhiChu = dto.GhiChu,
            };
            var result = await _repo.AddMappingAsync(entity);
            return MapMapping(result);
        }

        public async Task<LGTSMappingDto?> UpdateMappingAsync(int id, UpdateLGTSMappingDto dto)
        {
            var entity = new LG_TSL_SiLo_Mapping
            {
                IDLoCao = dto.IDLoCao,
                IDSiLo = dto.IDSiLo,
                IDNVL = dto.IDNVL,
                Ngay = dto.Ngay,
                Ca = dto.Ca,
                GhiChu = dto.GhiChu,
            };
            var result = await _repo.UpdateMappingAsync(id, entity);
            return result == null ? null : MapMapping(result);
        }

        public Task<bool> DeleteMappingAsync(int id) => _repo.DeleteMappingAsync(id);

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
    }
}

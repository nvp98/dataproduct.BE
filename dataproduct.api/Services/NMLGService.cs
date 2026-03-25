using dataproduct.api.DTOs.NMLG_Dto;
using dataproduct.api.Models;
using dataproduct.api.Repositories;

namespace dataproduct.api.Services
{
    public class NMLGService
    {
        private readonly INMLGRepository _repo;

        public NMLGService(INMLGRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<SiLoTonDto>> GetSiLoTonAsync(int? idLoCao, int? idCa, DateTime? ngay)
        {
            return await _repo.GetSiLoTon(idLoCao, idCa, ngay);
        }
        public async Task<List<SiLo_LG>> GetSiLoWithLoCaAsync(int? idLoCao)
        {
            return await _repo.GetSiLoWithLoCaoAsync(idLoCao);
        }

        public async Task<SiLo_LG> AddSiLoAsync(SiLo_LG entity)
        {
            return await _repo.AddSiLoAsync(entity);
        }

        public async Task<SiLo_LG?> UpdateSiLoAsync(int id, SiLo_LG entity)
        {
            return await _repo.UpdateSiLoAsync(id, entity);
        }
    }
}

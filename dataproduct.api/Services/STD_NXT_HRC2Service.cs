using dataproduct.api.DTOs;
using dataproduct.api.Models;
using dataproduct.api.Repositories;
using dataproduct.api.ResponseModels;

namespace dataproduct.api.Services
{
    public class STD_NXT_HRC2Service
    {
        private readonly ISTD_NXT_HRC2Repository _repo;

        public STD_NXT_HRC2Service(ISTD_NXT_HRC2Repository repo)
        {
            _repo = repo;
        }

        public async Task<STD_NXT_HRC2_UpsertResponse> UpsertAsync(STD_NXT_HRC2_UpsertDto entity)
        {
            return await _repo.UpsertAsync(entity);
        }

        public async Task<STD_NXT_HRC2_GetDetailResponse> GetByPhieuIdAsync(Guid phieuId)
        {
            return await _repo.GetByPhieuIdAsync(phieuId);
        }

        public async Task<bool> PhanBoAsync(STD_NXT_HRC2_PhanBoDto entity)
        {
            return await _repo.PhanBoAsync(entity);
        }

        public async Task<bool> ThuHoiPhanBoAsync(STD_NXT_HRC2_PhanBoDto entity)
        {
            return await _repo.ThuHoiPhanBoAsync(entity);
        }

    }
}

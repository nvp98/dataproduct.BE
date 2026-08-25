using dataproduct.api.DTOs.NMTKVV_Dto;
using dataproduct.api.Repositories.NMTKVV;

namespace dataproduct.api.Services.NMTKVV
{
    public class TKVV_TonSiloService
    {
        private readonly ITKVV_TonSiloRepository _repo;

        public TKVV_TonSiloService(ITKVV_TonSiloRepository repo)
        {
            _repo = repo;
        }

        public Task<List<TKVVTonSiloRowDto>> InitRowsAsync(InitTonSiloRowsRequestDto request)
            => _repo.InitRowsAsync(request);

        public Task<List<TKVVTonSiloRowDto>> GetRowsByPhieuIdAsync(Guid phieuId)
            => _repo.GetRowsByPhieuIdAsync(phieuId);

        public Task SavePhieuRowsAsync(SaveTonSiloPhieuRequestDto request)
            => _repo.SavePhieuRowsAsync(request);
    }
}

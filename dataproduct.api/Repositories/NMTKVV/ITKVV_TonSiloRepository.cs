using dataproduct.api.DTOs.NMTKVV_Dto;

namespace dataproduct.api.Repositories.NMTKVV
{
    public interface ITKVV_TonSiloRepository
    {
        Task<List<TKVVTonSiloRowDto>> InitRowsAsync(InitTonSiloRowsRequestDto request);
        Task<List<TKVVTonSiloRowDto>> GetRowsByPhieuIdAsync(Guid phieuId);
        Task SavePhieuRowsAsync(SaveTonSiloPhieuRequestDto request);
    }
}

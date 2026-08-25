using dataproduct.api.DTOs.NMTKVV_Dto;

namespace dataproduct.api.Repositories.NMTKVV
{
    public interface ITKVV_VatTuLookupRepository
    {
        Task<VatTuLookupResultDto> SearchAsync(string? searchKey, int page, int pageSize);
        Task<Dictionary<int, VatTuLookupDto>> GetByIdsAsync(IEnumerable<int> ids);
    }
}

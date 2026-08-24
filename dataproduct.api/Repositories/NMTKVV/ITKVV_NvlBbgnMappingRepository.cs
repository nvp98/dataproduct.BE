using dataproduct.api.DTOs.NMTKVV_Dto;
using dataproduct.api.Models;

namespace dataproduct.api.Repositories.NMTKVV
{
    public interface ITKVV_NvlBbgnMappingRepository
    {
        Task<List<TKVVNvlBbgnMappingDto>> GetListAsync(int? tkvvNvlId);
        Task<TKVV_NVL_BBGN_Mapping?> GetByIdAsync(int id);
        Task<TKVV_NVL_BBGN_Mapping> AddAsync(TKVV_NVL_BBGN_Mapping entity);
        Task<TKVV_NVL_BBGN_Mapping?> UpdateAsync(int id, bool trangThai, string? ghiChu);
        Task<bool> DeleteAsync(int id);
    }
}

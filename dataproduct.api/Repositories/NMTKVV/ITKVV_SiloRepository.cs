using dataproduct.api.DTOs.NMTKVV_Dto;
using dataproduct.api.Models;

namespace dataproduct.api.Repositories.NMTKVV
{
    public interface ITKVV_SiloRepository
    {
        // Silo
        Task<List<TKVVSiloDto>> GetSiloListAsync(string? scope);
        Task<TKVV_Silo?> GetSiloByIdAsync(int id);
        Task<TKVV_NguyenVatLieu?> GetNvlByIdAsync(int id);
        Task<TKVV_Silo> AddSiloAsync(TKVV_Silo entity);
        Task<TKVV_Silo?> UpdateSiloAsync(int id, TKVV_Silo entity);
        Task<bool> DeleteSiloAsync(int id);

        // NVL ↔ Silo Mapping
        Task<List<TKVVNvlSiloMappingDto>> GetNvlSiloMappingListAsync(string? maBM, string? scope, int? nvlId, int? siloId);
        Task<TKVV_NVL_SiloMapping?> GetNvlSiloMappingByIdAsync(int id);
        Task<TKVV_NVL_SiloMapping> AddNvlSiloMappingAsync(TKVV_NVL_SiloMapping entity);
        Task<TKVV_NVL_SiloMapping?> UpdateNvlSiloMappingAsync(int id, TKVV_NVL_SiloMapping entity);
        Task<bool> DeleteNvlSiloMappingAsync(int id);

        // Silo ↔ Tag EMS Mapping
        Task<List<TKVVSiloTagMappingDto>> GetSiloTagMappingListAsync(int? siloId, string? maBM);
        Task<TKVV_Silo_TagMapping?> GetSiloTagMappingByIdAsync(int id);
        Task<TKVV_Silo_TagMapping> AddSiloTagMappingAsync(TKVV_Silo_TagMapping entity);
        Task<TKVV_Silo_TagMapping?> UpdateSiloTagMappingAsync(int id, TKVV_Silo_TagMapping entity);
        Task<bool> DeleteSiloTagMappingAsync(int id);
    }
}

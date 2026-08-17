using dataproduct.api.DTOs.NMTKVV_Dto;

namespace dataproduct.api.Repositories.NMTKVV
{
    public interface ITKVV_BCSL_ChiPhiRepository
    {
        // Giá trị NVL tự động từ EMS — gọi SP_TKVV_GetGiaTriNVL_Auto
        Task<List<TKVVGiaTriNVLAutoDto>> GetGiaTriNVLAutoAsync(
            DateTime ngay, int ca, string scopeCode, string maBM);
    }
}

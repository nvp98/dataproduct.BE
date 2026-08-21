using dataproduct.api.DTOs.NMTKVV_Dto;

namespace dataproduct.api.Repositories.NMTKVV
{
    public interface ITKVV_BCSL_ChiPhiRepository
    {
        // Giá trị NVL tự động từ EMS — gọi SP_TKVV_GetGiaTriNVL_Auto
        Task<List<TKVVGiaTriNVLAutoDto>> GetGiaTriNVLAutoAsync(
            DateTime ngay, int ca, string scopeCode, string maBM);

        // Dữ liệu cân từ EMS — gọi SP_TKVV_GetDuLieuCan (theo Silo)
        Task<List<TKVVDuLieuCanDto>> GetDuLieuCanAsync(
            DateTime ngay, int ca, string maBM, string loaiDuLieu, int scope);

        // Tải dữ liệu cân từ EMS, upsert vào TKVV_BaoCaoSanLuongChiPhi với rule IsAdjusted,
        // trả về dữ liệu đã lưu (table1=Ca ngày, table2=Ca đêm)
        Task<LoadDuLieuCanResultDto> LoadAndSaveAsync(LoadDuLieuCanRequestDto request);

        // Lấy dữ liệu đã lưu theo ngày và scope (int 1-6)
        Task<LoadDuLieuCanResultDto> GetBaoCaoDataAsync(DateOnly ngaySX, string maBM, int scope);

        // Lưu giá trị người dùng nhập (KLAm, DoAm, v.v.), backend tự xác định IsAdjusted
        Task SavePhieuRowsAsync(SaveBcSlPhieuRequestDto request);
    }
}

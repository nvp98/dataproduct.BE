using dataproduct.api.DTOs.NMTKVV_Dto;
using dataproduct.api.Models;

namespace dataproduct.api.Repositories
{
    public interface ITKVV_BBSLRepository
    {
        // Danh mục NVL (sản phẩm theo biểu mẫu)
        Task<List<TKVVNguyenVatLieuDto>> GetNvlListAsync(string? maBM, string? scope);
        Task<TKVV_NguyenVatLieu?> GetNvlByIdAsync(int id);
        Task<TKVV_NguyenVatLieu> AddNvlAsync(TKVV_NguyenVatLieu entity);
        Task<TKVV_NguyenVatLieu?> UpdateNvlAsync(int id, TKVV_NguyenVatLieu entity);
        Task<bool> DeleteNvlAsync(int id);

        // Mapping NVL ↔ Tag EMS + Ca (TKVV_NVL_TagMapping)
        Task<List<TKVVMappingDto>> GetMappingListAsync();
        Task<TKVV_NVL_TagMapping?> GetMappingByIdAsync(int id);
        Task<TKVV_NVL_TagMapping> AddMappingAsync(TKVV_NVL_TagMapping entity);
        Task<TKVV_NVL_TagMapping?> UpdateMappingAsync(int id, TKVV_NVL_TagMapping entity);
        Task<bool> DeleteMappingAsync(int id);

        // Danh sách Tag PLC từ EMS (dbo.EMS_GetMappingTag) để chọn khi tạo Mapping
        Task<List<EMS_MappingTag>> GetEmsTagListAsync(string? xuong, string? tagName);

        // Dữ liệu PLC thô, lọc theo Scope/khoảng ngày
        Task<List<TKVVDuLieuRawDto>> GetDataByFilterAsync(
            string? scope, DateTime? ngayBatDau, DateTime? ngayKetThuc);

        // KTV/KCS chỉnh tay GiaTriDieuChinh cho 1 dòng dữ liệu thô (khi nghi ngờ PLC báo sai)
        // — trả về false nếu không tìm thấy ID
        Task<bool> UpdateGiaTriDieuChinhAsync(long id, decimal? giaTriDieuChinh);

        // Tổng tự động (PLC) theo (Ngay, Ca, Scope toàn cục 1-6) — 1 số tổng duy nhất
        // (không tách theo sản phẩm), chỉ để đối chiếu, không tự điền vào bảng chi tiết
        Task<TKVVTongTuDongDto> GetTongTuDongAsync(DateTime ngay, int ca, int scope);

        // Mapping Cân (EMS) → Xưởng theo Ngày/Ca/Kíp (TKVV_SanLuongMapping)
        Task<List<TKVVSanLuongMappingDto>> GetSanLuongMappingListAsync(string? scope);
        Task<TKVV_SanLuongMapping?> GetSanLuongMappingByIdAsync(long id);
        Task<TKVV_SanLuongMapping> AddSanLuongMappingAsync(TKVV_SanLuongMapping entity);
        Task<TKVV_SanLuongMapping?> UpdateSanLuongMappingAsync(long id, TKVV_SanLuongMapping entity);
        Task<bool> DeleteSanLuongMappingAsync(long id);

        // Bấm "Tải dữ liệu": gọi SP_TKVV_GetDuLieuCan_TuMapping (join TKVV_SanLuongMapping với
        // EMS_DATA_CAN qua linked server) rồi ghi các dòng MỚI (chưa có theo TagID+ThoiGian) vào
        // TKVV_SanLuongDuLieu — trả về số dòng mới vừa thêm.
        Task<int> SyncDuLieuTuEmsAsync(DateTime ngay, byte ca, string? scope);

        // Chi tiết sản lượng theo phiếu
        Task ReplaceChiTietAsync(Guid idPhieu, List<TKVV_SanLuongChiTiet> entities);
        Task<List<TKVVChiTietDto>> GetChiTietByPhieuAsync(Guid idPhieu);

        // Phiếu
        Task<BmPhieu?> GetPhieuByIdAsync(Guid idPhieu);
    }
}

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

        Task<List<TKVVDuLieuRawDto>> GetDataByFilterAsync(
            string? scope, DateTime? ngayBatDau, DateTime? ngayKetThuc);
        Task<bool> UpdateGiaTriDieuChinhAsync(long id, decimal? giaTriDieuChinh);

        Task<TKVVTongTuDongDto> GetTongTuDongAsync(DateTime ngay, int ca, int scope);

        // Chi tiết sản lượng theo phiếu
        Task ReplaceChiTietAsync(Guid idPhieu, List<TKVV_SanLuongChiTiet> entities);
        Task<List<TKVVChiTietDto>> GetChiTietByPhieuAsync(Guid idPhieu);
        Task<bool> HasDuLieuByNgayCaScopeAsync(DateTime ngay, int ca, int scope);
        // Phiếu
        Task<BmPhieu?> GetPhieuByIdAsync(Guid idPhieu);
    }
}

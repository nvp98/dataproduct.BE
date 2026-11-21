using dataproduct.api.Models;

namespace dataproduct.api.Repositories
{
    /// <summary>4 vùng của 1 Biên bản chốt số liệu Xưởng ĐPV (HRC1_BCSX_DPV).</summary>
    public class HRC1_BCSXChiTiet
    {
        public List<HRC1_BC_ThepLong> ThepLong { get; set; } = [];
        public List<HRC1_BC_Dien> Dien { get; set; } = [];
        public List<HRC1_BC_NguyenVatLieu> NguyenVatLieu { get; set; } = [];
        public List<HRC1_BC_ThanhPham> ThanhPham { get; set; } = [];
    }

    public interface IHRC1_BCSXRepository
    {
        Task<BmPhieu?> GetPhieuByIdAsync(Guid idPhieu);

        /// <summary>Dòng chưa xóa của 1 phiếu, đã sắp theo thứ tự hiển thị biên bản.</summary>
        Task<HRC1_BCSXChiTiet> GetByPhieuAsync(Guid idPhieu);

        /// <summary>Xóa toàn bộ dòng cũ của phiếu rồi ghi lại 4 bảng trong 1 transaction.</summary>
        Task ReplaceByPhieuAsync(Guid idPhieu, HRC1_BCSXChiTiet data);

        /// <summary>
        /// Dòng chưa xóa của các phiếu <paramref name="maBM"/> còn hiệu lực (BM_Phieu.IsDelete = 0) có NgaySX trong khoảng
        /// — dùng cho Excel tổng hợp. Kèm số phiếu theo PhieuID.
        /// </summary>
        Task<(HRC1_BCSXChiTiet Data, Dictionary<Guid, string?> SoPhieu)> GetTheoKhoangNgayAsync(string maBM, DateOnly tuNgay, DateOnly denNgay);

        /// <summary>Σ giá trị động lực (DuLieu_DongLuc, kéo từ EMS) của <paramref name="maHeader"/> theo ca trong 1 ngày.</summary>
        Task<Dictionary<byte, decimal>> GetDongLucTheoCaAsync(DateOnly ngay, string maHeader);
    }
}

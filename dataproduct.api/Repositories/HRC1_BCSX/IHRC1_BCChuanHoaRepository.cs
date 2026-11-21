using dataproduct.api.Models;

namespace dataproduct.api.Repositories
{
    /// <summary>1 đối tượng KDL module HRC1_DPV_GN kèm các giá trị chữ cần để tìm mapping còn thiếu (N/A).</summary>
    public class HRC1_GNDongTraCuu
    {
        public string? VatTu { get; set; }          // KDL_DoiTuong.Mac
        public string? BpNhan { get; set; }         // KDL_DoiTuong.MayDuc
        public string? XuongNhan { get; set; }
        public string? BoPhanNhan { get; set; }
        public string? XuongGiao { get; set; }
        public string? BoPhanGiao { get; set; }
        public string? BpGiao { get; set; }
        public string? NoiDung1 { get; set; }
        /// <summary>Xưởng phía bên kia của X.ĐPV (giao → BP bên nhận, nhận → BP bên giao) — khóa tra Nội dung 1.</summary>
        public string? BpDoiTac { get; set; }
    }

    /// <summary>2 bảng tra cứu của form Giao nhận Xưởng ĐPV (HRC1_BC_XuongChuanHoa, HRC1_BC_NVLChuanHoa).</summary>
    public interface IHRC1_BCChuanHoaRepository
    {
        Task<List<HRC1_BC_XuongChuanHoa>> GetXuongAsync();
        Task<HRC1_BC_XuongChuanHoa?> GetXuongByIdAsync(int id);
        Task<List<HRC1_BC_NVLChuanHoa>> GetNvlAsync();
        Task<HRC1_BC_NVLChuanHoa?> GetNvlByIdAsync(int id);
        Task AddAsync<T>(T entity) where T : class;
        Task SaveAsync();

        /// <summary>LSX (khác rỗng) của mẻ chưa xóa trong module KDL <paramref name="maModule"/> theo (NgaySX, CaSX, Kip).</summary>
        Task<List<(DateOnly NgaySX, byte CaSX, string? Kip, string Lsx)>> GetLsxTheoCaAsync(string maModule, DateOnly tuNgay, DateOnly denNgay);

        /// <summary>Điều chỉnh phân bổ đã lưu trong khoảng ngày (tracking — để sửa/xóa).</summary>
        Task<List<HRC1_BC_PhanBoDieuChinh>> GetPhanBoDieuChinhAsync(DateOnly tuNgay, DateOnly denNgay);

        void Remove<T>(T entity) where T : class;

        /// <summary>Đối tượng chưa xóa của module KDL <paramref name="maModule"/> trong khoảng ngày + giá trị chữ cần tra.</summary>
        Task<List<HRC1_GNDongTraCuu>> GetDongTraCuuAsync(string maModule, DateOnly tuNgay, DateOnly denNgay);
    }
}

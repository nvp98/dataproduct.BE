using dataproduct.api.Models;
using System.Text.Json;

namespace dataproduct.api.DTOs
{
    public class PhieuDto
    {
        public Guid Idphieu { get; set; }

        public string MaBm { get; set; } = null!;

        public string? SoPhieu { get; set; }

        public int? XuongId { get; set; }

        public int? IdphongBan { get; set; }

        public int? Idkip { get; set; }

        public int? Ca { get; set; }

        public string? Kip { get; set; }

        public int? Scope { get; set; }

        public DateTime? NgayTao { get; set; }
        public DateOnly? NgaySX { get; set; }

        public int? MayDuc { get; set; }

        public int? NguoiTaoId { get; set; }

        public int? TinhTrang { get; set; }

        public string? DataJson { get; set; }

        public int? IsDelete { get; set; }

        public int? IsLock { get; set; }
        public int? LoaiPhieu { get; set; }
        public bool? IsClone { get; set; }
        public int? VersionClone { get; set; }
        public Guid? ID_PhieuGoc { get; set; }
        public JsonElement? JsonData { get; internal set; }
        public List<BM_PheDuyetDto>? PheDuyet { get; set; }
    }

    public class SearchPhieuRequest
    {
        public DateTime? TuNgay { get; set; }
        public DateTime? DenNgay { get; set; }
        public int? Ca { get; set; }
        public int? Scope { get; set; }
        public int? MayDuc { get; set; }
        public string? MaBm { get; set; }
        public List<string>? MaBmList { get; set; }
        public string? searchText { get; set; }
        public int? TinhTrang { get; set; }
        // Nếu có NguoiDuyetId thì trả về danh sách phiếu "việc đến tôi"
        // (phiếu có gắn user này trong BmPheDuyet, CapDuyet != 0)
        public int? NguoiDuyetId { get; set; }
        public int? NguoiTaoId { get; set; }
        public int? IsCheck { get; set; }
        public int page { get; set; } = 1;
        public int pageSize { get; set; } = 10;
    }

    /// <summary>
    /// DTO tìm kiếm phiếu dựa trên quyền chức năng của user từ BM_QuyenXL.
    /// Thay thế NguoiDuyetId + NguoiTaoId bằng UserId duy nhất.
    /// </summary>
    public class SearchPhieuByUserRequest
    {
        public DateTime? TuNgay { get; set; }
        public DateTime? DenNgay { get; set; }
        public int? Ca { get; set; }
        public string? Kip { get; set; }
        public int? Scope { get; set; }
        public List<string>? ScopeFilters { get; set; }
        public int? MayDuc { get; set; }
        public string? MaBm { get; set; }
        public List<string>? MaBmList { get; set; }
        public string? searchText { get; set; }
        public int? TinhTrang { get; set; }
        /// <summary>
        /// ID tài khoản. Backend tự tra BM_QuyenXL để xác định quyền và lọc phiếu tương ứng.
        /// </summary>
        public int? UserId { get; set; }
        /// <summary>
        /// Vùng hiển thị trên UI (đối chiếu với field vung trong menuConfig FE):
        /// 1 = Việc tôi bắt đầu (quyền 1|4): MaBm+MaKhuVuc→Scope được phép, NguoiTaoId==userId hoặc TinhTrang==0|7
        /// 2 = Việc đến tôi    (quyền 2|4): MaBm+MaKhuVuc→Scope được phép, user có dòng BmPheDuyets (CapDuyet != 0)
        /// 3 = Chỉ xem        (quyền 5):   MaBm+MaKhuVuc→Scope được phép, TinhTrang != 0
        /// 4 = Thống kê       (IsThongKeUser==true): tất cả phiếu, không filter user
        /// null → coi như 1 (tương thích client cũ)
        /// </summary>
        public int? LoaiVung { get; set; }
        /// <summary>Đánh dấu user là PKH hoặc Admin, dùng để cho phép xem vùng 3 (Thống kê).</summary>
        public bool? IsThongKeUser { get; set; }
        public int page { get; set; } = 1;
        public int pageSize { get; set; } = 10;
    }
}

namespace dataproduct.api.DTOs
{
    // -------------------------------------------------------
    // Query
    // -------------------------------------------------------
    public class HRC1_GetChoNhanQuery
    {
        public DateOnly? Ngay { get; set; }
        public int? Ca { get; set; }
    }

    public class HRC1_GetMeChoNhanQuery
    {
        public DateOnly? TuNgay { get; set; }
        public DateOnly? DenNgay { get; set; }
        public int? Ca { get; set; }
        public string? MaMe { get; set; }
        public string? ThungSo { get; set; }
        public int? LoSo { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    // Lấy mẻ BBGN thép lỏng (công đoạn đúc) theo khoảng ThoiGian thực tế (giờ nhập, HH:mm trong ca)
    public class HRC1_MeTheoThoiGianQuery
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
    }

    // -------------------------------------------------------
    // Lò thổi — fetch mẻ từ gang lỏng
    // -------------------------------------------------------
    public class HRC1_FetchMeThoiRequest
    {
        public DateOnly NgaySX { get; set; }
        public int Ca { get; set; }
        public int? IdLoThoi { get; set; }  // null = tất cả lò, có giá trị = lọc theo lò
    }

    // -------------------------------------------------------
    // Lò thổi — nhập liệu
    // -------------------------------------------------------
    public class HRC1_LoThoiUpdateRequest
    {
        public string? ThungSo { get; set; }
        public decimal? KLLFSauThep { get; set; }
        public decimal? KlLan3 { get; set; }
        public string? ThoiGian { get; set; }      // chỉ dùng khi len_thang (lò thổi tự nhập)
        public decimal? KlLan2 { get; set; }       // chỉ dùng khi len_thang (lò thổi tự nhập)
        public decimal? KlThepLong { get; set; }   // chỉ dùng khi len_thang (tính từ FE)
        public string? DichChuyen { get; set; }    // tinh_luyen | len_thang
        public int? TLDichSo { get; set; }         // TL được chỉ định (1–5)
        public int? IdMayDucDich { get; set; }     // FK→MayDuc, bắt buộc nếu len_thang
        public bool? IsThuNghiem { get; set; }
        public bool? IsTrungMeThoi { get; set; }
        public string? GhiChuLo { get; set; }
        public decimal? KlThepLongPhanBo { get; set; }
        public int? ChuyenVeMeId { get; set; }     // FK→HRC1_MeThep; chỉ áp dụng khi len_thang; null = không chuyển
    }

    // -------------------------------------------------------
    // Tinh luyện — nhận mẻ, nhập liệu, thêm dòng
    // -------------------------------------------------------
    public class HRC1_NhanMeRequest
    {
        public int MeId { get; set; }
        public Guid IdPhieu { get; set; }  // phiếu TL nhận
        public int? ScopePhieu { get; set; }  // TL số đang nhận mẻ (1-5)
    }

    public class HRC1_TinhLuyenUpdateRequest
    {
        public string? ThoiGian { get; set; }      // HH:mm
        public decimal? KlLan1 { get; set; }       // disabled nếu DichChuyen = len_thang
        public decimal? KlLan2 { get; set; }
        public decimal? KlLan3 { get; set; }
        public decimal? KlThepLong { get; set; }   // FE tự tính, gửi kèm để persist
        public int? IdMayDucDich { get; set; }     // disabled nếu DichChuyen = len_thang
        // Chỉ áp dụng khi IsManualTL — mẻ do TinhLuyen tạo tay, không có LoThoi nhập
        public string? ThungSo { get; set; }
        public decimal? KllfSauThep { get; set; }
        public string? PhanLoai { get; set; }
        public string? MacThep { get; set; }
        public string? MacThepBKMIS { get; set; }
        public int? IdMacThep { get; set; }
        public string? GhiChuTL { get; set; }
        public int? ChuyenVeMeId { get; set; }     // FK→HRC1_MeThep; null = không chuyển
    }

    public class HRC1_ThemDongTLRequest
    {
        public int MeId { get; set; }
        public Guid IdPhieu { get; set; }  // phiếu TL thêm dòng
    }

    public class HRC1_HuyNhanMeRequest
    {
        public int MeId { get; set; }
        public Guid IdPhieu { get; set; }  // phiếu TL cần hủy nhận
        public int? ScopePhieu { get; set; }  // TL số đang hủy nhận
    }

    // -------------------------------------------------------
    // Máy đúc — xác nhận theo danh sách MeId
    // -------------------------------------------------------
    public class HRC1_DucXacNhanRequest
    {
        public List<int> MeIds { get; set; } = new();
    }

    public class HRC1_DucBoXacNhanRequest
    {
        public List<int> MeIds { get; set; } = new();
    }

    // -------------------------------------------------------
    // Máy đúc — xác nhận / không xác nhận / reset PCN (chỉ áp dụng mẻ IsThuNghiem=true)
    // TrangThaiPCN: null=chưa xử lý, true=đã xác nhận, false=không xác nhận
    // -------------------------------------------------------
    public class HRC1_DucXacNhanPCNRequest
    {
        public List<int> MeIds { get; set; } = new();
    }

    public class HRC1_DucKhongXacNhanPCNRequest
    {
        public List<int> MeIds { get; set; } = new();
    }

    public class HRC1_DucResetXacNhanPCNRequest
    {
        public List<int> MeIds { get; set; } = new();
    }

    // -------------------------------------------------------
    // Chốt / bỏ chốt PCN (P.KH, từ trang Thống kê) — khóa vĩnh viễn TrangThaiPCN
    // -------------------------------------------------------
    public class HRC1_ChotPCNRequest
    {
        public List<int> MeIds { get; set; } = new();
    }

    public class HRC1_BoChotPCNRequest
    {
        public List<int> MeIds { get; set; } = new();
    }

    // -------------------------------------------------------
    // Tinh luyện — thêm mẻ tay (không qua luồng nhận mẻ)
    // -------------------------------------------------------
    public class HRC1_ThemMeTayRequest
    {
        public string MaMe { get; set; } = null!;
        public Guid IdPhieu { get; set; }
        public bool XacNhanTrung { get; set; }  // true = user đã xác nhận mẻ trùng
        public int? ScopePhieu { get; set; }     // TL số đang thêm mẻ tay (1-5)
    }

    // -------------------------------------------------------
    // Lò thổi — đồng bộ theo lò cụ thể
    // -------------------------------------------------------
    public class HRC1_SyncLoThoiRequest
    {
        public int LoSo { get; set; }  // lò thổi số 1–5 cần đồng bộ
    }

    // -------------------------------------------------------
    // Đồng bộ phân loại & mác BKMIS từ Linked Server vào HRC1_MeThep
    // -------------------------------------------------------
    public class HRC1_SyncPhanLoaiRequest
    {
        public List<string> MaMes { get; set; } = new();
    }

    // -------------------------------------------------------
    // Máy đúc — chốt / bỏ chốt mẻ
    // -------------------------------------------------------
    public class HRC1_DucChotMeRequest
    {
        public List<int> MeIds { get; set; } = new();
        public Guid IdPhieu { get; set; }
        public int IdMayDuc { get; set; }
    }

    public class HRC1_DucBoChotMeRequest
    {
        public List<int> MeIds { get; set; } = new();
        public Guid IdPhieu { get; set; }
        public int IdMayDuc { get; set; }
    }

    public class HRC1_UpdateGhiChuRequest
    {
        public string? GhiChu { get; set; }
        /// <summary>"lo" | "tl" | "duc" — mặc định "lo" nếu không truyền</summary>
        public string? Field { get; set; }
    }

    // -------------------------------------------------------
    // Export Excel / PDF — điều kiện tìm kiếm
    // -------------------------------------------------------
    public class HRC1_ExportQuery
    {
        public DateOnly? TuNgay { get; set; }
        public DateOnly? DenNgay { get; set; }
        public int? Ca { get; set; }              // 1=Ca ngày, 2=Ca đêm
        public string? Kip { get; set; }
        public string? MaMe { get; set; }
        public int? LoSo { get; set; }            // lò thổi số 1–5
        public int? TlSo { get; set; }            // tinh luyện số 1–5
        public int? IdMayDuc { get; set; }        // FK→MayDuc
        public int? TrangThaiLo { get; set; }
        public int? TrangThaiTL { get; set; }
        public int? TrangThaiDuc { get; set; }
        public string? ThungSo { get; set; }
        public string? PhanLoai { get; set; }
        public bool? IsChot { get; set; }
        public bool? IsManualTL { get; set; }
        public bool? ChuaCoNhomPhanLoai { get; set; } // true = chỉ lấy mẻ chưa có nhóm phân loại
        public int? IdNhomPhanLoai { get; set; }
        public DateOnly? TuNgayLoThoi { get; set; }
        public DateOnly? DenNgayLoThoi { get; set; }
        public string? MaMeChuyenVe { get; set; }  // chỉ lấy mẻ đã chuyển sang mẻ khác
        public bool? IsThuNghiem { get; set; }
        public bool? TrangThaiPCN { get; set; }    // chỉ áp dụng khi IsThuNghiem=true; true=đã xác nhận, false=không xác nhận
    }

    // -------------------------------------------------------
    // Export row — kết quả trả về từ repository cho exporter + thống kê
    // -------------------------------------------------------
    public class HRC1_ExportRow
    {
        public int MeId { get; set; }
        public string? MaMe { get; set; }
        public string? ThungSo { get; set; }
        public string? ThoiGian { get; set; }
        public decimal? KLLFSauThep { get; set; }
        public decimal? KlLan1 { get; set; }
        public decimal? KlLan2 { get; set; }
        public decimal? KlLan3 { get; set; }
        public decimal? KlThepLong { get; set; }
        public decimal? KlThepLongChot { get; set; }
        public decimal? KLThepLongPhanBo { get; set; }
        public string? GhiChuLo { get; set; }
        public string? GhiChuTL { get; set; }
        public string? GhiChuDuc { get; set; }
        public string? GhiChuPCN { get; set; }
        public bool? IsThuNghiem { get; set; }
        public bool? TrangThaiPCN { get; set; }
        public bool? TrangThaiChotPCN { get; set; }
        public string? TenMayDuc { get; set; }
        public string? MacThep { get; set; }
        public string? PhanLoai { get; set; }
        public string? MacThepBKMIS { get; set; }
        public string? TinhLuyenLenThang { get; set; }   // "Tinh luyện" | "Lên thẳng"
        public int? SoTinhLuyenNhan { get; set; }        // TL số đã nhận mẻ (1–5)
        public bool? IsTrungMeThoi { get; set; }
        public bool? IsManualTL { get; set; }
        public int? TrangThaiLo { get; set; }
        public int? TrangThaiTL { get; set; }
        public int? TrangThaiDuc { get; set; }
        public bool? IsChot { get; set; }
        public DateTime NgayTao { get; set; }
        public int? Ca { get; set; }
        public string? Kip { get; set; }
        public DateTime? NgayNhanTL { get; set; }
        public int? CaTinhLuyen { get; set; }
        public string? KipTinhLuyen { get; set; }
        public string? TenNhomPhanLoai { get; set; }      // Nhóm phân loại mác thép
        public string? TenCapNhatBoiLo { get; set; }   // người sửa lò thổi (CapNhatBoi)
        public string? TenCapNhatBoiTL { get; set; }   // người sửa tinh luyện (CapNhatBoiTL)
        public string? TenCapNhatBoiDuc { get; set; }  // người xác nhận máy đúc (CapNhatBoiDuc)
        public string? ChuyenVeMaMe { get; set; }       // mã mẻ đích khi TL chuyển mẻ
        public string? TenMayDucChuyen { get; set; }    // tên máy đúc của mẻ đích
        public DateOnly? NgayDuc { get; set; }          // NgaySX của phiếu máy đúc chứa mẻ này
        public int? CaDuc { get; set; }                 // Ca của phiếu máy đúc chứa mẻ này
    }

    // -------------------------------------------------------
    // Tổng hợp — item nhóm (label + KL thép lỏng)
    // -------------------------------------------------------
    public class HRC1_TongHopItem
    {
        public string Label { get; set; } = string.Empty;
        public decimal KlThepLong { get; set; }
    }

    // -------------------------------------------------------
    // Tổng hợp — kết quả nhóm theo các chiều
    // -------------------------------------------------------
    public class HRC1_TongHopResult
    {
        public List<HRC1_TongHopItem> PhanLoai { get; set; } = new();
        public List<HRC1_TongHopItem> Ca { get; set; } = new();
        public List<HRC1_TongHopItem> Kip { get; set; } = new();
        public List<HRC1_TongHopItem> TinhLuyenLenThang { get; set; } = new();
        public List<HRC1_TongHopItem> DucVuong { get; set; } = new();
        public List<HRC1_TongHopItem> DucTam { get; set; } = new();
        public List<HRC1_TongHopItem> NhomPhanLoaiMacThep { get; set; } = new();
    }

    // -------------------------------------------------------
    // Thống kê — query có phân trang (extends HRC1_ExportQuery)
    // -------------------------------------------------------
    public class HRC1_ThongKeQuery : HRC1_ExportQuery
    {
        public bool? IsTrungMeThoi { get; set; }
        public bool? IsChuyenMe { get; set; }   // true = chỉ lấy mẻ đã chuyển sang mẻ khác
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    // -------------------------------------------------------
    // Thống kê — kết quả phân trang
    // -------------------------------------------------------
    public class HRC1_ThongKeResult
    {
        public List<HRC1_ExportRow> Items { get; set; } = new();
        public int TotalRecords { get; set; }
        public decimal? TotalKlThepLong { get; set; }
        public decimal? TotalKlThepLongChot { get; set; }
        public decimal? TotalKlThepLongPhanBo { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    // -------------------------------------------------------
    // Chốt/hủy chốt nhiều phiếu HRC1_BBGN_ThepLong (batch từ P.KH)
    // -------------------------------------------------------
    public class HRC1_ChotPhieuBatchRequest
    {
        public List<Guid> IdPhieuList { get; set; } = new();
    }

    public class HRC1_ChotPhieuBatchThatBai
    {
        public Guid IdPhieu { get; set; }
        public string SoPhieu { get; set; } = string.Empty;
        public List<string> LyDo { get; set; } = new();
    }

    public class HRC1_ChotPhieuBatchResult
    {
        public List<string> ThanhCong { get; set; } = new();
        public List<HRC1_ChotPhieuBatchThatBai> ThatBai { get; set; } = new();
    }
}

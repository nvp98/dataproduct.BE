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
        public decimal? KlThepLong { get; set; }   // FE tự tính, gửi kèm để persist
        public int? IdMayDucDich { get; set; }     // disabled nếu DichChuyen = len_thang
        public string? PhanLoai { get; set; }
        public string? MacThep { get; set; }
        public string? MacThepBKMIS { get; set; }
        public int? IdMacThep { get; set; }
        public string? GhiChuTL { get; set; }
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

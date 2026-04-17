using System.ComponentModel.DataAnnotations.Schema;

namespace dataproduct.api.DTOs
{
    public class FetchMeThoiRequest
    {
        public DateOnly NgaySX { get; set; }
        public int Ca { get; set; }
        public int NhaMay { get; set; }
    }

    public class LoadBBGNThepLongRequest
    {
        public Guid IdPhieu { get; set; }
        public DateOnly NgaySX { get; set; }
        public int Ca { get; set; }
        /// <summary>HRC1_BBGN_ThepLong hoặc HRC2_BBGN_ThepLong — bắt buộc (dùng suy NhaMay cho SP).</summary>
        public string BieuMau { get; set; } = "";
    }

    public enum NhaMay
    {
        HRC1 = 1,
        HRC2 = 2
    }

    public class SyncPhanLoaiRequest
    {
        public List<string> MaMes { get; set; } = new();
        /// <summary>HRC1_BBGN_ThepLong hoặc HRC2_BBGN_ThepLong</summary>
        public string BieuMau { get; set; } = "";
    }

    public class SyncPhanLoaiResult
    {
        public int TotalFromMySQL { get; set; }
        public int TotalUpdated { get; set; }
    }

    public class SearchThongKeBBGNThepLongRequest
    {
        public DateTime? TuNgay { get; set; }
        public DateTime? DenNgay { get; set; }
        /// <summary>Filter theo ID máy đúc lưu ở Scope.</summary>
        public int? MayDuc { get; set; }
        /// <summary>Alias filter theo Scope (ID máy đúc).</summary>
        public int? Scope { get; set; }
        /// <summary>Tìm kiếm theo Mẻ hoặc Mác thép.</summary>
        public string? SearchString { get; set; }
        public string? ThungSo { get; set; }
        public string? TinhLuyenLenThang { get; set; }
        public string? PhanLoai { get; set; }
        public bool? IsTrungMeThoi { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class SumThongKeBBGNThepLongResponse
    {
        public int TotalRows { get; set; }
        public decimal? TotalKlThepLong { get; set; }
    }
}


using System;
using System.Collections.Generic;

namespace dataproduct.api.DTOs
{
    public class Hrc1InsertModel
    {
        public int? Id { get; set; }
        public DateOnly NgaySanXuat { get; set; }
        public byte Ca { get; set; }
        public int Scope { get; set; }
        public string MeThoi { get; set; } = "";
        public string? MacThep { get; set; }
        public decimal? KLGang { get; set; }
        public decimal? KLThepPhe { get; set; }
        public decimal? KLThepLong { get; set; }
        public double? O2 { get; set; }
        public double? N2 { get; set; }
        public double? AR { get; set; }
        public int? QueLayMau { get; set; }
        public int? QueDoNhiet { get; set; }
        public string? GhiChu { get; set; }
        public bool IsNM { get; set; }
        public Guid RowKey { get; set; } = Guid.NewGuid();
        public List<Hrc1_PhuLieuInsertModel> PhuLieus { get; set; } = new List<Hrc1_PhuLieuInsertModel>();
    }

    public class Hrc1_PhuLieuInsertModel
    {
        public string? MeThoi { get; set; }
        /// <summary>Trỏ tới HRC1_PhuLieuNM.ID — null nếu là cột điều chỉnh tự do (manual_col_*, dùng ID_HeaderKey).</summary>
        public int? PhuLieuID { get; set; }
        public string? TenPhuLieu { get; set; }
        public double? KLPhuGia { get; set; }
        public int? ID_HeaderKey { get; set; }
        public bool? IsManual { get; set; } = false;
        public double? KLPhuGia_Manual { get; set; }
        public bool? IsAddManual { get; set; } = false;
        /// <summary>Chỉ set khi cần ép giá trị cụ thể (VD: phụ liệu thêm tay qua nút Thêm cột điều chỉnh → false).
        /// Null nghĩa là "giữ theo IsNM của mẻ cha" (hành vi mặc định trước đây).</summary>
        public bool? IsNM { get; set; }
    }

    /// <summary>Request cho "Làm mới" trên trang Sổ Xuất-Nhập-Tồn HRC1 (POST /api/DLNMHRC1/filterSTD_NXT).
    /// Mirror FilterSTD_NXTRequest của HRC2, đổi HeaderKeyIds -> PhuLieuIds.</summary>
    public class FilterSTD_NXTRequest_HRC1
    {
        public DateTime NgaySX { get; set; }
        public int Ca { get; set; }
        /// <summary>Nếu có: sau khi filter sẽ gọi sp_Init_XuatNhapTon_HRC1 để cập nhật dữ liệu phiếu hiện tại.</summary>
        public Guid? IdPhieu { get; set; }
        /// <summary>Danh sách PhuLieuID đang hiển thị trên FE (kể cả dòng mới chưa lưu). Dùng cho Init khi có IdPhieu.</summary>
        public List<int>? PhuLieuIds { get; set; }
    }

    /// <summary>Bộ lọc thống kê tiêu hao HRC1 (BOF) — không có LoaiBM như HRC2 vì HRC1 hiện chỉ có BOF.</summary>
    public class SearchThongKeHrc1
    {
        public DateTime? TuNgay { get; set; }
        public DateTime? DenNgay { get; set; }
        public int? Ca { get; set; }
        public int? Scope { get; set; }
        public string? SearchText { get; set; }
        public bool? IsTrungMeThoi { get; set; }
        public bool? IsDelete { get; set; }
        public int? Page { get; set; } = 1;
        public int? PageSize { get; set; } = 20;
    }
}

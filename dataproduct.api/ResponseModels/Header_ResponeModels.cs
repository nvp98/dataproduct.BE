
namespace dataproduct.api.ResponseModels
{
    public class HeaderKey_ResponeModels
    {
        public Guid? KeyGuid { get; set; }
        public string? TenHienThi{ get; set; }
        public int? ID_HeaderKey { get; set; }
        public string? TenNguonDuLieu { get; set; }
        public int ID_PhuLieu { get; set; }
        public string? TenPhuLieu { get; set; }
        public double? KLPhuGia { get; set; }
        public int? MappingId { get; set; }
        public int? ThuTu { get; set; }
    }
    public class HeaderKeyGroupedByReportNoModel
    {
        public Guid? KeyGuid { get; set; }
        public string? TenHienThi{ get; set; }
        public int? ID_HeaderKey { get; set; }
        public string? TenNguonDuLieu { get; set; }
        public int ID_PhuLieu { get; set; }
        public string? TenPhuLieu { get; set; }
        public double? KLPhuGia { get; set; }
        public string? LoaiPhuLieu { get; set; }
        public double? KLPhuGiaTotal { get; set; }
        public int? MappingId { get; set; }
        public int? ThuTu { get; set; }
    }

    public class HeaderKey_ResponseModel
    {
        public int Id { get; set; }
        public Guid KeyGuid { get; set; }
        public string TenHienThi { get; set; } = string.Empty;
        public string? Mota { get; set; }
        public string? LoaiPhieu { get; set; }
        public bool IsActive { get; set; }
        public DateTime? NgayTao { get; set; }
        public bool? IsUsedNXT { get; set; }
        public int? ThuTu { get; set; }
        public decimal? TyTrong { get; set; }
        public List<HeaderMapping_ResponseModel> HeaderMappings { get; set; } = new List<HeaderMapping_ResponseModel>();
    }
    public class HeaderMapping_ResponseModel
    {
        public int Id { get; set; }
        public int ID_PhuLieu { get; set; }
        public string TenNguonDuLieu { get; set; } = string.Empty;
    }

    // Response model cho search: mỗi ID_PhuLieu trả về 1 dòng với 1 HeaderKey
    public class HeaderKeyMapping_ResponseModel
    {
        public int? MappingId { get; set; } // ID của Header_Mapping (null nếu chưa móc nối)
        public int? ID_HeaderKey { get; set; }
        public Guid? KeyGuid { get; set; }
        public string? TenHienThi { get; set; }
        public string? Mota { get; set; }
        public string? LoaiPhieu { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? NgayTao { get; set; }
        public DateTime? NgayTaoPhuLieu { get; set; } // Ngày tạo phụ liệu NM (PhuLieu_NM.NgayTao)
        public bool? IsUsedNXT { get; set; }
        public int? ThuTu { get; set; }
        public decimal? TyTrong { get; set; } // Tỷ trọng từ Header_Key
        public int? ID_PhuLieu { get; set; } // null nếu chưa móc nối
        public string? TenNguonDuLieu { get; set; } // null nếu chưa móc nối
        public string? TenPhuLieu { get; set; } // tên phụ liệu NM (từ bảng PhuLieu_NM)
    }
}

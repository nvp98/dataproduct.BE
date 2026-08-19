namespace dataproduct.api.DTOs.NMTKVV_Dto
{
    // ─── TKVV_Silo ───────────────────────────────────────────────────────────────

    public class TKVVSiloDto
    {
        public int Id { get; set; }
        public string? MaXuong { get; set; }
        public string? Scope { get; set; }
        public string? TenScope { get; set; }
        public string? MaSilo { get; set; }
        public string TenSilo { get; set; } = string.Empty;
        public string? GhiChu { get; set; }
        public bool TrangThai { get; set; }
        public DateTime NgayCapNhat { get; set; }
    }

    public class CreateTKVVSiloDto
    {
        public string? MaXuong { get; set; }
        public string? Scope { get; set; }
        public string? TenScope { get; set; }
        public string? MaSilo { get; set; }
        public string TenSilo { get; set; } = string.Empty;
        public string? GhiChu { get; set; }
    }

    public class UpdateTKVVSiloDto : CreateTKVVSiloDto
    {
        public bool TrangThai { get; set; }
    }

    // ─── TKVV_NVL_SiloMapping ─────────────────────────────────────────────────

    public class TKVVNvlSiloMappingDto
    {
        public int Id { get; set; }
        public string? MaBM { get; set; }
        public int NguyenVatLieuID { get; set; }
        public string? TenNVL { get; set; }
        public string? ScopeNVL { get; set; }
        public string? Scope { get; set; }
        public int? SiloID { get; set; }
        public string? TenSilo { get; set; }
        public string? MaSilo { get; set; }
        public int Ca { get; set; }
        public DateOnly NgaySX { get; set; }
        public int? ThuTu { get; set; }
        public string? GhiChu { get; set; }
        public bool TrangThai { get; set; }
        public DateTime NgayCapNhat { get; set; }
    }

    public class CreateTKVVNvlSiloMappingDto
    {
        public string? MaBM { get; set; }
        public int NguyenVatLieuID { get; set; }
        public string? Scope { get; set; }
        public int? SiloID { get; set; }
        public int Ca { get; set; }
        public DateOnly NgaySX { get; set; }
        public int? ThuTu { get; set; }
        public string? GhiChu { get; set; }
    }

    public class UpdateTKVVNvlSiloMappingDto : CreateTKVVNvlSiloMappingDto
    {
        public bool TrangThai { get; set; }
    }

    // ─── TKVV_Silo_TagMapping ─────────────────────────────────────────────────

    public class TKVVSiloTagMappingDto
    {
        public int Id { get; set; }
        public int SiloID { get; set; }
        public string? TenSilo { get; set; }
        public string? ScopeNVL { get; set; }
        public string MaBM { get; set; } = string.Empty;
        public string LoaiDuLieu { get; set; } = string.Empty;
        public string? TagIDEMS { get; set; }
        public string? TagName { get; set; }
        public string? TagIDEMS_Ngay { get; set; }
        public string? TagName_Ngay { get; set; }
        public string? TagIDEMS_Dem { get; set; }
        public string? TagName_Dem { get; set; }
        public string? GhiChu { get; set; }
        public bool TrangThai { get; set; }
        public DateTime NgayCapNhat { get; set; }
    }

    public class CreateTKVVSiloTagMappingDto
    {
        public int SiloID { get; set; }
        public string MaBM { get; set; } = string.Empty;
        public string LoaiDuLieu { get; set; } = string.Empty;
        public string? TagIDEMS { get; set; }
        public string? TagName { get; set; }
        public string? TagIDEMS_Ngay { get; set; }
        public string? TagName_Ngay { get; set; }
        public string? TagIDEMS_Dem { get; set; }
        public string? TagName_Dem { get; set; }
        public string? GhiChu { get; set; }
    }

    public class UpdateTKVVSiloTagMappingDto : CreateTKVVSiloTagMappingDto
    {
        public bool TrangThai { get; set; }
    }
}

namespace dataproduct.api.DTOs.LGNL_Dto
{
    // ─── SiLo Master DTOs ─────────────────────────────────────────────────────

    public class LGNLSiLoMasterDto
    {
        public int ID { get; set; }
        public int? IDLoCao { get; set; }
        public string? TenSiLo { get; set; }
        public int? ThuTu { get; set; }
        public DateTime? NgayTao { get; set; }
        public string? TagKey { get; set; }
    }

    public class CreateLGNLSiLoMasterDto
    {
        public int IDLoCao { get; set; }
        public string TenSiLo { get; set; } = string.Empty;
        public int? ThuTu { get; set; }
        public string? TagKey { get; set; }
    }

    public class UpdateLGNLSiLoMasterDto
    {
        public int IDLoCao { get; set; }
        public string TenSiLo { get; set; } = string.Empty;
        public int? ThuTu { get; set; }
        public string? TagKey { get; set; }
    }

    // ─── TS Mapping Dto (lookup dropdown) ────────────────────────────────────

    public class LGNLTsMappingDto
    {
        public int ID { get; set; }
        public string? TagKey { get; set; }
        public bool? IsActive { get; set; }
    }

    // ─── Mapping DTOs (Silo ↔ NVL theo ngày/ca) ───────────────────────────────

    public class LGNLMappingDto
    {
        public int ID { get; set; }
        public DateOnly? Ngay { get; set; }
        public int? IDCa { get; set; }
        public int? IDLoCao { get; set; }
        public int? IDSiLo { get; set; }
        public string? TenSiLo { get; set; }    // join từ LG_NL_SiLo
        public string? TagKey { get; set; }     // join từ LG_NL_SiLo
        public int? IDNVL { get; set; }
        public string? MaNVL { get; set; }      // join từ LG_NL_NVL
        public string? TenNVL { get; set; }     // join từ LG_NL_NVL
        public string? NhomHienThi { get; set; }// join từ LG_NL_NVL
        public int? ThuTuNhom { get; set; }     // join từ LG_NL_NVL
        public string? GhiChu { get; set; }
        public DateTime? NgayTao { get; set; }
    }

    public class CreateLGNLMappingDto
    {
        public DateOnly Ngay { get; set; }
        public int IDCa { get; set; }
        public int IDLoCao { get; set; }
        public int? IDSiLo { get; set; }
        public int? IDNVL { get; set; }
        public string? GhiChu { get; set; }
    }

    public class UpdateLGNLMappingDto
    {
        public DateOnly Ngay { get; set; }
        public int IDCa { get; set; }
        public int IDLoCao { get; set; }
        public int? IDSiLo { get; set; }
        public int? IDNVL { get; set; }
        public string? GhiChu { get; set; }
    }

    // ─── NVL DTOs ─────────────────────────────────────────────────────────────

    public class LGNLNvlDto
    {
        public int ID { get; set; }
        public DateOnly? Ngay { get; set; }
        public int? IDCa { get; set; }
        public int? IDLoCao { get; set; }
        public string? MaNVL { get; set; }
        public string? TenNVL { get; set; }
        public string? DonVi { get; set; }
        public decimal? SoLuong { get; set; }
        public decimal? DoAm { get; set; }
        public string? GhiChu { get; set; }
        public DateTime? NgayTao { get; set; }
        public string? NhomHienThi { get; set; }
        public int? ThuTuNhom { get; set; }
    }

    public class CreateLGNLNvlDto
    {
        public DateOnly Ngay { get; set; }
        public int IDCa { get; set; }
        public int IDLoCao { get; set; }
        public string? MaNVL { get; set; }
        public string? TenNVL { get; set; }
        public string? DonVi { get; set; }
        public decimal? SoLuong { get; set; }
        public decimal? DoAm { get; set; }
        public string? GhiChu { get; set; }
        public string? NhomHienThi { get; set; }
        public int? ThuTuNhom { get; set; }
    }

    public class UpdateLGNLNvlDto
    {
        public DateOnly Ngay { get; set; }
        public int IDCa { get; set; }
        public int IDLoCao { get; set; }
        public string? MaNVL { get; set; }
        public string? TenNVL { get; set; }
        public string? DonVi { get; set; }
        public decimal? SoLuong { get; set; }
        public decimal? DoAm { get; set; }
        public string? GhiChu { get; set; }
        public string? NhomHienThi { get; set; }
        public int? ThuTuNhom { get; set; }
    }

    // ─── Dữ liệu Silo — Pivot Result ─────────────────────────────────────────

    /// <summary>
    /// Định nghĩa 1 cột hoặc nhóm cột trên BM ISO.
    /// Nếu có Children → cột cha (header nhóm), không có dataIndex.
    /// Nếu không có Children → cột lá (leaf), có dataIndex.
    /// </summary>
    public class LGNLColumnDto
    {
        public string Title { get; set; } = string.Empty;
        public string? DataIndex { get; set; }          // null nếu là nhóm cha
        public List<LGNLColumnDto>? Children { get; set; } // null nếu là lá
    }

    /// <summary>
    /// Kết quả pivot dữ liệu nạp liệu theo Silo (đã join với LG_NL_Mapping + LG_NL_SiLo).
    /// columns → dùng để render Ant Design Table header (materialColumnsOverride).
    /// rows    → mỗi row là 1 mẻ nạp, key = MaNVL của NVL.
    /// </summary>
    public class LGNLDuLieuSiLoResult
    {
        public List<LGNLColumnDto> Columns { get; set; } = [];
        public List<Dictionary<string, object?>> Rows { get; set; } = [];
    }
}

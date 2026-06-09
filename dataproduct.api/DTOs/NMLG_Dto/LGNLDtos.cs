namespace dataproduct.api.DTOs.NMLG_Dto
{
    // ─── SiLo Master DTOs ─────────────────────────────────────────────────────

    public class LGNLSiLoMasterDto
    {
        public int Id { get; set; }
        public int? IdLoCao { get; set; }
        public string? TenSiLo { get; set; }
        public int? ThuTu { get; set; }
        public DateTime? NgayTao { get; set; }
        public string? TagKey { get; set; }
    }

    public class CreateLGNLSiLoMasterDto
    {
        public int IdLoCao { get; set; }
        public string TenSiLo { get; set; } = string.Empty;
        public int? ThuTu { get; set; }
        public string? TagKey { get; set; }
    }

    public class UpdateLGNLSiLoMasterDto
    {
        public int IdLoCao { get; set; }
        public string TenSiLo { get; set; } = string.Empty;
        public int? ThuTu { get; set; }
        public string? TagKey { get; set; }
    }

    // ─── TS Mapping Dto (lookup dropdown) ────────────────────────────────────

    public class LGNLTsMappingDto
    {
        public int Id { get; set; }
        public string? TagKey { get; set; }
        public bool? IsActive { get; set; }
    }

    // ─── Mapping DTOs (Silo ↔ NVL theo ngày/ca) ───────────────────────────────

    public class LGNLMappingDto
    {
        public int Id { get; set; }
        public DateTime? Ngay { get; set; }
        public int? IdCa { get; set; }
        public int? IdLoCao { get; set; }
        public int? IdSiLo { get; set; }
        public string? TenSiLo { get; set; }    // join từ LG_NL_SiLo
        public string? TagKey { get; set; }     // join từ LG_NL_SiLo
        public int? IdNVL { get; set; }
        public string? MaNVL { get; set; }      // join từ LG_NL_NVL
        public string? TenNVL { get; set; }     // join từ LG_NL_NVL
        public string? NhomHienThi { get; set; }// join từ LG_NL_NVL
        public int? ThuTuNhom { get; set; }     // join từ LG_NL_NVL
        public DateTime? ThoiDiemBD { get; set; } // null = từ đầu ca; có giá trị = đổi NVL giữa ca
        public DateTime? NgayHetHL { get; set; }
        public int? IdCaHetHL { get; set; }
        public string? GhiChu { get; set; }
        public DateTime? NgayTao { get; set; }
    }

    // ─── Snapshot trạng thái hiện tại của từng Silo ───────────────────────────

    public class LGNLSiloSnapshotDto
    {
        public int IdSiLo { get; set; }
        public string? TenSiLo { get; set; }
        public int? IdNVL { get; set; }
        public string? TenNVL { get; set; }
        public DateTime? ThoiDiemBD { get; set; } // null = từ đầu ca
        public bool DaDoiGiuaCa { get; set; }
        public int? ThuTu { get; set; }
    }

    public class CreateLGNLMappingDto
    {
        public DateTime? Ngay { get; set; }
        public int IdCa { get; set; }
        public int IdLoCao { get; set; }
        public int? IdSiLo { get; set; }
        public int? IdNVL { get; set; }
        public string? GhiChu { get; set; }
    }

    public class UpdateLGNLMappingDto
    {
        public DateTime? Ngay { get; set; }
        public int IdCa { get; set; }
        public int IdLoCao { get; set; }
        public int? IdSiLo { get; set; }
        public int? IdNVL { get; set; }
        public string? GhiChu { get; set; }
    }

    // ─── Nhóm NVL DTOs ───────────────────────────────────────────────────────

    public class LGNLNhomNvlDto
    {
        public int Id { get; set; }
        public string? TenNhom { get; set; }
        public int? ThuTu { get; set; }
        public string? GhiChu { get; set; }
    }

    public class CreateLGNLNhomNvlDto
    {
        public string TenNhom { get; set; } = string.Empty;
        public int? ThuTu { get; set; }
        public string? GhiChu { get; set; }
    }

    public class UpdateLGNLNhomNvlDto
    {
        public string TenNhom { get; set; } = string.Empty;
        public int? ThuTu { get; set; }
        public string? GhiChu { get; set; }
    }

    // ─── NVL DTOs ─────────────────────────────────────────────────────────────

    public class LGNLNvlDto
    {
        public int Id { get; set; }
        public int? IdLoCao { get; set; }
        public int? IdNhomNVL { get; set; }
        public string? TenNVL_NM { get; set; }
        public int? ThuTu { get; set; }
        public string? GhiChu { get; set; }
        public DateTime? NgayTao { get; set; }
        public string? NhomHienThi { get; set; }
        public int? ThuTuNhom { get; set; }
        public string? TenNVL_TK { get; set; }
        public bool? XacNhan { get; set; }
        public DateTime? NgayXacNhan { get; set; }
        public int? IdNguoiXacNhan { get; set; }
    }

    public class CreateLGNLNvlDto
    {
        public int IdLoCao { get; set; }
        public int? IdNhomNVL { get; set; }
        public string? TenNVL_NM { get; set; }
        public int? ThuTu { get; set; }
        public string? GhiChu { get; set; }
        public int? ThuTuNhom { get; set; }
    }

    public class UpdateLGNLNvlDto
    {
        public int IdLoCao { get; set; }
        public int? IdNhomNVL { get; set; }
        public string? TenNVL_NM { get; set; }
        public string? TenNVL_TK { get; set; }
        public int? ThuTu { get; set; }
        public string? GhiChu { get; set; }
        public int? ThuTuNhom { get; set; }
        public bool? XacNhan { get; set; }
    }
    public class UpdateXacNhanDto
    {
        public int Id { get; set; }
        public bool XacNhan { get; set; }
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
        // Config hiệu lực đang được áp dụng (khác ngay/idCa yêu cầu = đang dùng config cũ)
        public DateTime? NgayHieuLuc { get; set; }
        public int? IdCaHieuLuc { get; set; }
    }

    public class LGNLChangeSiLoNVLDto
    {
        public int IdLoCao { get; set; }
        public DateTime Ngay { get; set; }
        public int IdCa { get; set; }
        public int IdSiLo { get; set; }
        public int IdNVLMoi { get; set; }
        public DateTime ThoiDiem { get; set; }              // thời điểm bắt đầu dùng NVL mới
        public string? GhiChu { get; set; }
    }


    /// </summary>
    public class LGNLDuLieuScadaDto
    {
        public int Id { get; set; }
        public string? TagName { get; set; }
        public string? TagKey { get; set; }
        public DateTime? Time { get; set; }
        public double? Value { get; set; }
        public int? IdLoCao { get; set; }
    }


    /// </summary>
    public class GetLGNLDataByFilterDto
    {
        public string? TagKey { get; set; }
        public int? IdLoCao { get; set; }
        public DateTime? NgayBatDau { get; set; }
        public DateTime? NgayKetThuc { get; set; }
    }

    // ─── Chi tiết nạp liệu theo phiếu ────────────────────────────────────────

    public class LGNLChiTietDto
    {
        public int Id { get; set; }
        public Guid IdPhieu { get; set; }
        public int? IdLoCao { get; set; }
        public DateTime? Ngay { get; set; }
        public int? IdCa { get; set; }
        public string? ThoiGianNapLieu { get; set; }
        public decimal? SoMe { get; set; }
        public string? MeGio { get; set; }
        public string? CheDo { get; set; }
        public decimal? ThuocThamLieu1 { get; set; }
        public decimal? ThuocThamLieu2 { get; set; }
        public string? GhiChu { get; set; }
        public int IdNVL { get; set; }
        public decimal? GiaTri { get; set; }
        public int? ThuTu { get; set; }
        public decimal? DoAm { get; set; }
        public decimal? QuyKho { get; set; }
        public bool ManualGiaTri { get; set; }
        public decimal? GiaTri_Goc { get; set; }
    }
}

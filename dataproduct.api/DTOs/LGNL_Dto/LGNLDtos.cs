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
        public DateTime? Ngay { get; set; }
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
        public DateTime? ThoiDiemBD { get; set; } // null = từ đầu ca; có giá trị = đổi NVL giữa ca
        public DateTime? NgayHetHL { get; set; }
        public int? IDCaHetHL { get; set; }
        public string? GhiChu { get; set; }
        public DateTime? NgayTao { get; set; }
    }

    // ─── Snapshot trạng thái hiện tại của từng Silo ───────────────────────────

    public class LGNLSiloSnapshotDto
    {
        public int IDSiLo { get; set; }
        public string? TenSiLo { get; set; }
        public int? IDNVL { get; set; }
        public string? TenNVL { get; set; }
        public DateTime? ThoiDiemBD { get; set; } // null = từ đầu ca
        public bool DaDoiGiuaCa { get; set; }
        public int? ThuTu { get; set; }
    }

    public class CreateLGNLMappingDto
    {
        public DateTime? Ngay { get; set; }
        public int IDCa { get; set; }
        public int IDLoCao { get; set; }
        public int? IDSiLo { get; set; }
        public int? IDNVL { get; set; }
        public string? GhiChu { get; set; }
    }

    public class UpdateLGNLMappingDto
    {
        public DateTime? Ngay { get; set; }
        public int IDCa { get; set; }
        public int IDLoCao { get; set; }
        public int? IDSiLo { get; set; }
        public int? IDNVL { get; set; }
        public string? GhiChu { get; set; }
    }

    // ─── Nhóm NVL DTOs ───────────────────────────────────────────────────────

    public class LGNLNhomNvlDto
    {
        public int ID { get; set; }
        public int? IDLoCao { get; set; }
        public string? TenNhom { get; set; }
        public int? ThuTu { get; set; }
        public string? GhiChu { get; set; }
        public DateTime? NgayTao { get; set; }
    }

    public class CreateLGNLNhomNvlDto
    {
        public int IDLoCao { get; set; }
        public string TenNhom { get; set; } = string.Empty;
        public int? ThuTu { get; set; }
        public string? GhiChu { get; set; }
    }

    public class UpdateLGNLNhomNvlDto
    {
        public int IDLoCao { get; set; }
        public string TenNhom { get; set; } = string.Empty;
        public int? ThuTu { get; set; }
        public string? GhiChu { get; set; }
    }

    // ─── NVL DTOs ─────────────────────────────────────────────────────────────

    public class LGNLNvlDto
    {
        public int ID { get; set; }
        public int? IDLoCao { get; set; }
        public int? IDNhomNVL { get; set; }
        public string? TenNVL_NM { get; set; }
        public int? ThuTu { get; set; }
        public string? GhiChu { get; set; }
        public DateTime? NgayTao { get; set; }
        public string? NhomHienThi { get; set; }
        public int? ThuTuNhom { get; set; }
        public string? TenNVL_TK { get; set; }
        public bool? XacNhan { get; set; }
        public DateTime? NgayXacNhan { get; set; }
        public int? IDNguoiXacNhan { get; set; }
    }

    public class CreateLGNLNvlDto
    {
        public int IDLoCao { get; set; }
        public int? IDNhomNVL { get; set; }
        public string? TenNVL_NM { get; set; }
        public int? ThuTu { get; set; }
        public string? GhiChu { get; set; }
        public int? ThuTuNhom { get; set; }
    }

    public class UpdateLGNLNvlDto
    {
        public int IDLoCao { get; set; }
        public int? IDNhomNVL { get; set; }
        public string? TenNVL_NM { get; set; }
        public string? TenNVL_TK { get; set; }
        public int? ThuTu { get; set; }
        public string? GhiChu { get; set; }
        public int? ThuTuNhom { get; set; }
        public bool? XacNhan { get; set; }
    }
    public class UpdateXacNhanDto
    {
        public int ID { get; set; }
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
        public int? IDCaHieuLuc { get; set; }
    }

    public class LGNLChangeSiLoNVLDto
    {
        public int IDLoCao { get; set; }
        public DateTime Ngay { get; set; }
        public int IDCa { get; set; }
        public int IDSiLo { get; set; }
        public int IDNVLMoi { get; set; }
        public DateTime ThoiDiem { get; set; }              // thời điểm bắt đầu dùng NVL mới
        public string? GhiChu { get; set; }
    }


    /// </summary>
    public class LGNLDuLieuScadaDto
    {
        public int ID { get; set; }
        public string? TagName { get; set; }
        public string? TagKey { get; set; }
        public DateTime? Time { get; set; }
        public double? Value { get; set; }
        public int? IDLoCao { get; set; }
    }


    /// </summary>
    public class GetLGNLDataByFilterDto
    {
        public string? TagKey { get; set; }       
        public int? IDLoCao { get; set; }          
        public DateTime? NgayBatDau { get; set; }    
        public DateTime? NgayKetThuc { get; set; }   
    }
}

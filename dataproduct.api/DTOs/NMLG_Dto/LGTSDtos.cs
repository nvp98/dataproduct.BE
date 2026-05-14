using System.ComponentModel.DataAnnotations;

namespace dataproduct.api.DTOs.NMLG_Dto
{
    // ─── SiLoTon (dùng cho endpoint getkltonsilolocao hiện tại) ─────────────────
    public class SiLoTonDto
    {
        public int? ID { get; set; }
        public int? IdLoCao { get; set; }
        public int? IDSiLo { get; set; }
        public decimal? Ton { get; set; }
        public DateTime? Ngay { get; set; }
        public int? IDNguon { get; set; }

        // Từ bảng SiLo_LG
        public int? ID_LoCao { get; set; }
        public string? TenSiLo { get; set; }
        public int? ThuTu { get; set; }
        public string? TenNL { get; set; }
        public string? TenNL_DieuChinh { get; set; }
    }

    // ─── LG_TSL_NVL ──────────────────────────────────────────────────────────────
    // DB columns: TenNVL → TenNVL_NM, TenNVL_Tk → TenNVL_TK (khớp với camelCase FE)

    public class LGTSNvlDto
    {
        public int ID { get; set; }
        public int IDLoCao { get; set; }
        public string? TenNVL { get; set; }
        public string? TenNVL_TK { get; set; }
        public string? GhiChu { get; set; }
        public DateTime NgayTao { get; set; }
        public bool XacNhan { get; set; }
        public DateTime? NgayXacNhan { get; set; }
        public int? IDNguoiXacNhan { get; set; }
    }

    public class CreateLGTSNvlDto
    {
        public int IDLoCao { get; set; }
        public string TenNVL { get; set; } = string.Empty;
        public string? TenNVL_TK { get; set; }
        public string? GhiChu { get; set; }
        public bool XacNhan { get; set; }
    }

    public class UpdateLGTSNvlDto : CreateLGTSNvlDto { }

    public class UpdateLGTSXacNhanDto
    {
        public int ID { get; set; }
        public bool XacNhan { get; set; }
    }

    // ─── LG_TSL_SiLo ─────────────────────────────────────────────────────────────

    public class LGTSSiLoDto
    {
        public int ID { get; set; }
        public int? IDLoCao { get; set; }   // DB: ID_LoCao
        public string? TenSiLo { get; set; }
        public int? ThuTu { get; set; }
    }

    public class CreateLGTSSiLoDto
    {
        public int IDLoCao { get; set; }
        public string? TenSiLo { get; set; }
        public int? ThuTu { get; set; }
    }

    public class UpdateLGTSSiLoDto : CreateLGTSSiLoDto { }

    // ─── LG_TSL_SiLo_Mapping ─────────────────────────────────────────────────────
    // DB column Ca → DTO Ca (camelCase: "ca") để khớp với frontend

    public class LGTSMappingDto
    {
        public int ID { get; set; }
        public int IDLoCao { get; set; }
        public int IDSiLo { get; set; }
        public int IDNVL { get; set; }
        public DateTime Ngay { get; set; }
        public int Ca { get; set; }            // DB column: Ca
        public string? GhiChu { get; set; }
        public DateTime NgayTao { get; set; }
        public string? NguoiTao { get; set; }
        // Join fields
        public string? TenSiLo { get; set; }
        public int? ThuTuSiLo { get; set; }
        public string? TenNVL { get; set; }    // DB: TenNVL
        public string? TenNVL_TK { get; set; } // DB: TenNVL_Tk
    }

    public class CreateLGTSMappingDto
    {
        public int IDLoCao { get; set; }
        public int IDSiLo { get; set; }
        public int IDNVL { get; set; }
        public DateTime Ngay { get; set; }
        public int Ca { get; set; }
        public string? GhiChu { get; set; }
    }

    public class UpdateLGTSMappingDto : CreateLGTSMappingDto { }

    // ─── View: SiLo + NVL theo Ngày/Ca/LoCao (dùng trong tạo phiếu tồn silo) ───
    public class LGTSSiLoMappingViewDto
    {
        public int IDMapping { get; set; }
        public int IDSiLo { get; set; }
        public int IDLoCao { get; set; }
        public int IDNVL { get; set; }
        public string? TenSiLo { get; set; }
        public int? ThuTu { get; set; }
        public string? TenNVL { get; set; }
        public string? TenNVL_TK { get; set; }
        public DateTime Ngay { get; set; }
        public int Ca { get; set; }
        public string? GhiChu { get; set; }
        public decimal Ton { get; set; }
    }

    // ─── LG_TSL_ChiTiet (chi tiết tồn silo theo phiếu/ngày/ca/lò cao) ─────────────

    public class LGTSChiTietDto
    {
        public int ID { get; set; }
        public Guid IDPhieu { get; set; }
        public int IDLoCao { get; set; }
        public DateTime Ngay { get; set; }
        public int Ca { get; set; }
        public int IDSiLo { get; set; }
        public int? IDMapping { get; set; }
        public int? IDNVL { get; set; }
        public string? TenSiLo { get; set; }
        public string? TenNVL { get; set; }
        public decimal? KLTonCuoiKip { get; set; }
        public string? GhiChu { get; set; }
        public int? ThuTu { get; set; }
    }

    public class UpsertLGTSChiTietItemDto
    {
        public int IDSiLo { get; set; }
        public int? IDMapping { get; set; }
        public int? IDNVL { get; set; }
        public string? TenSiLo { get; set; }
        public string? TenNVL { get; set; }
        public decimal? KLTonCuoiKip { get; set; }
        public string? GhiChu { get; set; }
        public int? ThuTu { get; set; }
    }

    public class UpsertLGTSChiTietDto
    {
        public Guid IDPhieu { get; set; }
        public int IDLoCao { get; set; }
        public DateTime Ngay { get; set; }
        public int Ca { get; set; }
        public List<UpsertLGTSChiTietItemDto> Items { get; set; } = new();
    }
}

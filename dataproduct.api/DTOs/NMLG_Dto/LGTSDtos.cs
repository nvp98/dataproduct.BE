using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

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

    public class LGTSNvlDto
    {
        [JsonPropertyName("id")]
        public int ID { get; set; }

        [JsonPropertyName("idLoCao")]
        public int IDLoCao { get; set; }

        public string? TenNVL { get; set; }
        public string? TenNVLTk { get; set; }
        /// <summary>Tên hiển thị: TenNVLTk nếu đã xác nhận, ngược lại TenNVL.</summary>
        public string? TenHienThi { get; set; }
        public string? GhiChu { get; set; }
        public DateTime NgayTao { get; set; }
        public bool XacNhan { get; set; }
        public DateTime? NgayXacNhan { get; set; }

        [JsonPropertyName("idNguoiXacNhan")]
        public int? IDNguoiXacNhan { get; set; }
    }

    public class CreateLGTSNvlDto
    {
        [JsonPropertyName("idLoCao")]
        public int IDLoCao { get; set; }

        public string TenNVL { get; set; } = string.Empty;
        public string? TenNVLTk { get; set; }
        public string? GhiChu { get; set; }
        public bool XacNhan { get; set; }
    }

    public class UpdateLGTSNvlDto : CreateLGTSNvlDto { }

    public class UpdateLGTSXacNhanDto
    {
        [JsonPropertyName("id")]
        public int ID { get; set; }

        public bool XacNhan { get; set; }
    }

    // ─── LG_TSL_SiLo ─────────────────────────────────────────────────────────────

    public class LGTSSiLoDto
    {
        [JsonPropertyName("id")]
        public int ID { get; set; }

        [JsonPropertyName("idLoCao")]
        public int? IDLoCao { get; set; }   // DB: ID_LoCao

        public string? TenSiLo { get; set; }
        public int? ThuTu { get; set; }

        public  int? ThuTuCoDinh { get; set; }  
    }

    public class CreateLGTSSiLoDto
    {
        [JsonPropertyName("idLoCao")]
        public int IDLoCao { get; set; }

        public string? TenSiLo { get; set; }
        public int? ThuTu { get; set; }
    }

    public class UpdateLGTSSiLoDto : CreateLGTSSiLoDto { }

    // ─── LG_TSL_SiLo_Mapping ─────────────────────────────────────────────────────
    // [JsonPropertyName] trên các field ID-prefix để đảm bảo JSON output khớp với
    // TypeScript interface: idNVL, idSiLo, idLoCao thay vì iDNVL, iDSiLo, iDLoCao
    // (System.Text.Json CamelCase chỉ lowercase ký tự đầu: IDNVL → iDNVL, không phải idNVL)

    public class LGTSMappingDto
    {
        [JsonPropertyName("id")]
        public int ID { get; set; }

        [JsonPropertyName("idLoCao")]
        public int IDLoCao { get; set; }

        [JsonPropertyName("idSiLo")]
        public int IDSiLo { get; set; }

        [JsonPropertyName("idNVL")]
        public int IDNVL { get; set; }

        public DateTime Ngay { get; set; }
        public int Ca { get; set; }
        public string? GhiChu { get; set; }
        public DateTime NgayTao { get; set; }
        public string? NguoiTao { get; set; }
        public string? TenSiLo { get; set; }
        public int? ThuTuSiLo { get; set; }
        public string? TenNVL { get; set; }
        public string? TenNVLTk { get; set; }
    }

    public class CreateLGTSMappingDto
    {
        [JsonPropertyName("idLoCao")]
        public int IDLoCao { get; set; }

        [JsonPropertyName("idSiLo")]
        public int IDSiLo { get; set; }

        [JsonPropertyName("idNVL")]
        public int IDNVL { get; set; }

        public DateTime Ngay { get; set; }
        public int Ca { get; set; }
        public string? GhiChu { get; set; }
    }

    public class UpdateLGTSMappingDto : CreateLGTSMappingDto { }

    // ─── View: SiLo + NVL theo Ngày/Ca/LoCao (dùng trong tạo phiếu tồn silo) ───

    public class LGTSSiLoMappingViewDto
    {
        [JsonPropertyName("idMapping")]
        public int IDMapping { get; set; }

        [JsonPropertyName("idSiLo")]
        public int IDSiLo { get; set; }

        [JsonPropertyName("idLoCao")]
        public int IDLoCao { get; set; }

        [JsonPropertyName("idNVL")]
        public int IDNVL { get; set; }

        public string? TenSiLo { get; set; }
        public int? ThuTu { get; set; }
        public string? TenNVL { get; set; }
        public string? TenNVLTk { get; set; }
        public DateTime Ngay { get; set; }
        public int Ca { get; set; }
        public string? GhiChu { get; set; }
        public decimal Ton { get; set; }
    }

    // ─── LG_TSL_ChiTiet (chi tiết tồn silo theo phiếu/ngày/ca/lò cao) ──────────

    public class LGTSChiTietDto
    {
        [JsonPropertyName("id")]
        public int ID { get; set; }

        [JsonPropertyName("idPhieu")]
        public Guid IDPhieu { get; set; }

        [JsonPropertyName("idLoCao")]
        public int IDLoCao { get; set; }

        public DateTime Ngay { get; set; }
        public int Ca { get; set; }

        [JsonPropertyName("idSiLo")]
        public int IDSiLo { get; set; }

        [JsonPropertyName("idMapping")]
        public int? IDMapping { get; set; }

        [JsonPropertyName("idNVL")]
        public int? IDNVL { get; set; }

        public string? TenSiLo { get; set; }
        public string? TenNVL { get; set; }
        public string? TenNVLTk { get; set; }
        public decimal? KLTonCuoiKip { get; set; }
        public bool ManualKL { get; set; }
        public decimal? KLGoc { get; set; }
        public string? GhiChu { get; set; }
        public int? ThuTu { get; set; }
    }

    public class UpsertLGTSChiTietItemDto
    {
        [JsonPropertyName("idSiLo")]
        public int IDSiLo { get; set; }

        [JsonPropertyName("idMapping")]
        public int? IDMapping { get; set; }

        [JsonPropertyName("idNVL")]
        public int? IDNVL { get; set; }

        public string? TenSiLo { get; set; }
        public string? TenNVL { get; set; }
        public decimal? KLTonCuoiKip { get; set; }
        public bool ManualKL { get; set; }
        public decimal? KLGoc { get; set; }
        public string? GhiChu { get; set; }
        public int? ThuTu { get; set; }
    }

    public class UpsertLGTSChiTietDto
    {
        [JsonPropertyName("idPhieu")]
        public Guid IDPhieu { get; set; }

        [JsonPropertyName("idLoCao")]
        public int IDLoCao { get; set; }

        public DateTime Ngay { get; set; }
        public int Ca { get; set; }
        public List<UpsertLGTSChiTietItemDto> Items { get; set; } = new();
    }
}

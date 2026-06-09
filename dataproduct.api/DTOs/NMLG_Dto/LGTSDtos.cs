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

    public class LGTSNvlDto
    {
        public int Id { get; set; }
        public int IdLoCao { get; set; }
        public string? TenNVL { get; set; }
        public string? TenNVLTk { get; set; }
        /// <summary>Tên hiển thị: TenNVLTk nếu đã xác nhận, ngược lại TenNVL.</summary>
        public string? TenHienThi { get; set; }
        public string? GhiChu { get; set; }
        public DateTime NgayTao { get; set; }
        public bool XacNhan { get; set; }
        public DateTime? NgayXacNhan { get; set; }
        public int? IdNguoiXacNhan { get; set; }
    }

    public class CreateLGTSNvlDto
    {
        public int IdLoCao { get; set; }
        public string TenNVL { get; set; } = string.Empty;
        public string? TenNVLTk { get; set; }
        public string? GhiChu { get; set; }
        public bool XacNhan { get; set; }
    }

    public class UpdateLGTSNvlDto : CreateLGTSNvlDto { }

    public class UpdateLGTSXacNhanDto
    {
        public int Id { get; set; }
        public bool XacNhan { get; set; }
    }

    // ─── LG_TSL_SiLo ─────────────────────────────────────────────────────────────

    public class LGTSSiLoDto
    {
        public int Id { get; set; }
        public int? IdLoCao { get; set; }   // DB: ID_LoCao
        public string? TenSiLo { get; set; }
        public int? ThuTu { get; set; }
        public int? ThuTuCoDinh { get; set; }
    }

    public class CreateLGTSSiLoDto
    {
        public int IdLoCao { get; set; }
        public string? TenSiLo { get; set; }
        public int? ThuTu { get; set; }
    }

    public class UpdateLGTSSiLoDto : CreateLGTSSiLoDto { }

    // ─── LG_TSL_SiLo_Mapping ─────────────────────────────────────────────────────

    public class LGTSMappingDto
    {
        public int Id { get; set; }
        public int IdLoCao { get; set; }
        public int IdSiLo { get; set; }
        public int IdNVL { get; set; }
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
        public int IdLoCao { get; set; }
        public int IdSiLo { get; set; }
        public int IdNVL { get; set; }
        public DateTime Ngay { get; set; }
        public int Ca { get; set; }
        public string? GhiChu { get; set; }
    }

    public class UpdateLGTSMappingDto : CreateLGTSMappingDto { }

    // ─── View: SiLo + NVL theo Ngày/Ca/LoCao (dùng trong tạo phiếu tồn silo) ───

    public class LGTSSiLoMappingViewDto
    {
        public int IdMapping { get; set; }
        public int IdSiLo { get; set; }
        public int IdLoCao { get; set; }
        public int IdNVL { get; set; }
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
        public int Id { get; set; }
        public Guid IdPhieu { get; set; }
        public int IdLoCao { get; set; }
        public DateTime Ngay { get; set; }
        public int Ca { get; set; }
        public int IdSiLo { get; set; }
        public int? IdMapping { get; set; }
        public int? IdNVL { get; set; }
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
        public int IdSiLo { get; set; }
        public int? IdMapping { get; set; }
        public int? IdNVL { get; set; }
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
        public Guid IdPhieu { get; set; }
        public int IdLoCao { get; set; }
        public DateTime Ngay { get; set; }
        public int Ca { get; set; }
        public List<UpsertLGTSChiTietItemDto> Items { get; set; } = new();
    }
}

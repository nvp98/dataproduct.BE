using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace dataproduct.api.Models
{
    public class LG_NKVHPT_ChiTiet
    {
        [Key]
        public long ID { get; set; }

        public Guid IDPhieu { get; set; }

        [MaxLength(100)]
        public string? SoPhieu { get; set; }

        public int ID_LoCao { get; set; }

        public DateTime NgayVanHanh { get; set; }

        public int IDCa { get; set; }

        [MaxLength(50)]
        public string? Kip { get; set; }

        public DateTime ThoiGian { get; set; }

        [Column(TypeName = "decimal(10,3)")] public decimal? NhietDoSiloBotThan1_Auto { get; set; }
        [Column(TypeName = "decimal(10,3)")] public decimal? NhietDoSiloBotThan1_Manual { get; set; }

        [Column(TypeName = "decimal(10,3)")] public decimal? NhietDoSiloBotThan2_Auto { get; set; }
        [Column(TypeName = "decimal(10,3)")] public decimal? NhietDoSiloBotThan2_Manual { get; set; }

        [Column(TypeName = "decimal(10,3)")] public decimal? NhietDoBonPhunThoi1_Auto { get; set; }
        [Column(TypeName = "decimal(10,3)")] public decimal? NhietDoBonPhunThoi1_Manual { get; set; }

        [Column(TypeName = "decimal(10,3)")] public decimal? NhietDoBonPhunThoi2_Auto { get; set; }
        [Column(TypeName = "decimal(10,3)")] public decimal? NhietDoBonPhunThoi2_Manual { get; set; }

        [Column(TypeName = "decimal(10,3)")] public decimal? NhietDoBonPhunThoi3_Auto { get; set; }
        [Column(TypeName = "decimal(10,3)")] public decimal? NhietDoBonPhunThoi3_Manual { get; set; }

        [Column(TypeName = "decimal(10,3)")] public decimal? DongDienMayNghien_Auto { get; set; }
        [Column(TypeName = "decimal(10,3)")] public decimal? DongDienMayNghien_Manual { get; set; }

        [Column(TypeName = "decimal(10,3)")] public decimal? DongDienQuatGioNguoc_Auto { get; set; }
        [Column(TypeName = "decimal(10,3)")] public decimal? DongDienQuatGioNguoc_Manual { get; set; }

        [Column(TypeName = "decimal(10,3)")] public decimal? NhietDoDauVaoMayNghien_Auto { get; set; }
        [Column(TypeName = "decimal(10,3)")] public decimal? NhietDoDauVaoMayNghien_Manual { get; set; }

        [Column(TypeName = "decimal(10,3)")] public decimal? NhietDoDauRaMayNghien_Auto { get; set; }
        [Column(TypeName = "decimal(10,3)")] public decimal? NhietDoDauRaMayNghien_Manual { get; set; }

        [Column(TypeName = "decimal(10,3)")] public decimal? NhietDoKhoangLo_Auto { get; set; }
        [Column(TypeName = "decimal(10,3)")] public decimal? NhietDoKhoangLo_Manual { get; set; }

        [Column(TypeName = "decimal(10,3)")] public decimal? MucLieuSiloBotThan1_Auto { get; set; }
        [Column(TypeName = "decimal(10,3)")] public decimal? MucLieuSiloBotThan1_Manual { get; set; }

        [Column(TypeName = "decimal(10,3)")] public decimal? MucLieuSiloBotThan2_Auto { get; set; }
        [Column(TypeName = "decimal(10,3)")] public decimal? MucLieuSiloBotThan2_Manual { get; set; }

        [Column(TypeName = "decimal(10,3)")] public decimal? MucLieuSiloThanTho_Auto { get; set; }
        [Column(TypeName = "decimal(10,3)")] public decimal? MucLieuSiloThanTho_Manual { get; set; }

        [Column(TypeName = "decimal(10,3)")] public decimal? TrongLuongBonPhunThoi1_Auto { get; set; }
        [Column(TypeName = "decimal(10,3)")] public decimal? TrongLuongBonPhunThoi1_Manual { get; set; }

        [Column(TypeName = "decimal(10,3)")] public decimal? TrongLuongBonPhunThoi2_Auto { get; set; }
        [Column(TypeName = "decimal(10,3)")] public decimal? TrongLuongBonPhunThoi2_Manual { get; set; }

        [Column(TypeName = "decimal(10,3)")] public decimal? TrongLuongBonPhunThoi3_Auto { get; set; }
        [Column(TypeName = "decimal(10,3)")] public decimal? TrongLuongBonPhunThoi3_Manual { get; set; }

        [Column(TypeName = "decimal(10,3)")] public decimal? ApLucKhiThan_Auto { get; set; }
        [Column(TypeName = "decimal(10,3)")] public decimal? ApLucKhiThan_Manual { get; set; }

        [Column(TypeName = "decimal(10,3)")] public decimal? ApLucBonKhiN2_Auto { get; set; }
        [Column(TypeName = "decimal(10,3)")] public decimal? ApLucBonKhiN2_Manual { get; set; }

        [Column(TypeName = "decimal(10,3)")] public decimal? NhietDoTramDauBoiTron_Auto { get; set; }
        [Column(TypeName = "decimal(10,3)")] public decimal? NhietDoTramDauBoiTron_Manual { get; set; }

        [Column(TypeName = "decimal(10,3)")] public decimal? NhietDoStatoDongCoMayNghien_Auto { get; set; }
        [Column(TypeName = "decimal(10,3)")] public decimal? NhietDoStatoDongCoMayNghien_Manual { get; set; }

        [Column(TypeName = "decimal(10,3)")] public decimal? NhietDoTrucDongCoMayNghien_Auto { get; set; }
        [Column(TypeName = "decimal(10,3)")] public decimal? NhietDoTrucDongCoMayNghien_Manual { get; set; }

        [Column(TypeName = "decimal(10,3)")] public decimal? ApLucTrucNghien_Auto { get; set; }
        [Column(TypeName = "decimal(10,3)")] public decimal? ApLucTrucNghien_Manual { get; set; }

        [Column(TypeName = "decimal(10,3)")] public decimal? YeuCauTuLoCao_Auto { get; set; }
        [Column(TypeName = "decimal(10,3)")] public decimal? YeuCauTuLoCao_Manual { get; set; }

        [Column(TypeName = "decimal(10,3)")] public decimal? LuongThanPhunThucTe_Auto { get; set; }
        [Column(TypeName = "decimal(10,3)")] public decimal? LuongThanPhunThucTe_Manual { get; set; }

        [Column(TypeName = "decimal(10,3)")] public decimal? LuyKeLuongPhunThanTrongCa_Auto { get; set; }
        [Column(TypeName = "decimal(10,3)")] public decimal? LuyKeLuongPhunThanTrongCa_Manual { get; set; }

        public int? SoLuongSungPhun_Auto { get; set; }
        public int? SoLuongSungPhun_Manual { get; set; }

        [Column(TypeName = "decimal(10,3)")] public decimal? ApLucGioLanhLoCao_Auto { get; set; }
        [Column(TypeName = "decimal(10,3)")] public decimal? ApLucGioLanhLoCao_Manual { get; set; }

        [Column(TypeName = "decimal(18,3)")] public decimal? SanLuongNghien { get; set; }
        [Column(TypeName = "decimal(18,3)")] public decimal? SanLuongPhun { get; set; }

        public string? GhiChu { get; set; }
        public string? TinhTrangSanXuat { get; set; }
        public string? SapXepSanXuat { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}

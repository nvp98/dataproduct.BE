namespace dataproduct.api.DTOs.NMLG_Dto
{
    // ─── Auto data from LG_NKVHPT_DuLieu ────────────────────────────────────────
    public class LG_NKVHPT_DuLieuAuto
    {
        public long ID { get; set; }
        public DateTime ThoiGian { get; set; }
        public int? IDCa { get; set; }
        public decimal? TrongLuongBonPhunThoi1_Auto { get; set; }
        public decimal? TrongLuongBonPhunThoi2_Auto { get; set; }
        public decimal? TrongLuongBonPhunThoi3_Auto { get; set; }
        public decimal? YeuCauTuLoCao_Auto { get; set; }
        public decimal? LuongThanPhunThucTe_Auto { get; set; }
        public decimal? LuyKeLuongPhunThanTrongCa_Auto { get; set; }
        public int? SoLuongSungPhun_Auto { get; set; }
    }

    public class GetAutoDataRequest
    {
        public int IDLoCao { get; set; }
        public DateTime NgaySanXuat { get; set; }
    }

    // ─── Chi tiết phun than (LG_NKVHPT_ChiTiet) ─────────────────────────────────
    public class LGPTLCChiTietDto
    {
        public long ID { get; set; }
        public Guid IDPhieu { get; set; }
        public string? SoPhieu { get; set; }
        public int IDLoCao { get; set; }
        public DateTime NgayVanHanh { get; set; }
        public int IDCa { get; set; }
        public string? Kip { get; set; }
        public DateTime ThoiGian { get; set; }

        public decimal? NhietDoSiloBotThan1_Auto { get; set; }
        public decimal? NhietDoSiloBotThan1_Manual { get; set; }
        public decimal? NhietDoSiloBotThan2_Auto { get; set; }
        public decimal? NhietDoSiloBotThan2_Manual { get; set; }
        public decimal? NhietDoBonPhunThoi1_Auto { get; set; }
        public decimal? NhietDoBonPhunThoi1_Manual { get; set; }
        public decimal? NhietDoBonPhunThoi2_Auto { get; set; }
        public decimal? NhietDoBonPhunThoi2_Manual { get; set; }
        public decimal? NhietDoBonPhunThoi3_Auto { get; set; }
        public decimal? NhietDoBonPhunThoi3_Manual { get; set; }
        public decimal? DongDienMayNghien_Auto { get; set; }
        public decimal? DongDienMayNghien_Manual { get; set; }
        public decimal? DongDienQuatGioNguoc_Auto { get; set; }
        public decimal? DongDienQuatGioNguoc_Manual { get; set; }
        public decimal? NhietDoDauVaoMayNghien_Auto { get; set; }
        public decimal? NhietDoDauVaoMayNghien_Manual { get; set; }
        public decimal? NhietDoDauRaMayNghien_Auto { get; set; }
        public decimal? NhietDoDauRaMayNghien_Manual { get; set; }
        public decimal? NhietDoKhoangLo_Auto { get; set; }
        public decimal? NhietDoKhoangLo_Manual { get; set; }
        public decimal? MucLieuSiloBotThan1_Auto { get; set; }
        public decimal? MucLieuSiloBotThan1_Manual { get; set; }
        public decimal? MucLieuSiloBotThan2_Auto { get; set; }
        public decimal? MucLieuSiloBotThan2_Manual { get; set; }
        public decimal? MucLieuSiloThanTho_Auto { get; set; }
        public decimal? MucLieuSiloThanTho_Manual { get; set; }
        public decimal? TrongLuongBonPhunThoi1_Auto { get; set; }
        public decimal? TrongLuongBonPhunThoi1_Manual { get; set; }
        public decimal? TrongLuongBonPhunThoi2_Auto { get; set; }
        public decimal? TrongLuongBonPhunThoi2_Manual { get; set; }
        public decimal? TrongLuongBonPhunThoi3_Auto { get; set; }
        public decimal? TrongLuongBonPhunThoi3_Manual { get; set; }
        public decimal? ApLucKhiThan_Auto { get; set; }
        public decimal? ApLucKhiThan_Manual { get; set; }
        public decimal? ApLucBonKhiN2_Auto { get; set; }
        public decimal? ApLucBonKhiN2_Manual { get; set; }
        public decimal? NhietDoTramDauBoiTron_Auto { get; set; }
        public decimal? NhietDoTramDauBoiTron_Manual { get; set; }
        public decimal? NhietDoStatoDongCoMayNghien_Auto { get; set; }
        public decimal? NhietDoStatoDongCoMayNghien_Manual { get; set; }
        public decimal? NhietDoTrucDongCoMayNghien_Auto { get; set; }
        public decimal? NhietDoTrucDongCoMayNghien_Manual { get; set; }
        public decimal? ApLucTrucNghien_Auto { get; set; }
        public decimal? ApLucTrucNghien_Manual { get; set; }
        public decimal? YeuCauTuLoCao_Auto { get; set; }
        public decimal? YeuCauTuLoCao_Manual { get; set; }
        public decimal? LuongThanPhunThucTe_Auto { get; set; }
        public decimal? LuongThanPhunThucTe_Manual { get; set; }
        public decimal? LuyKeLuongPhunThanTrongCa_Auto { get; set; }
        public decimal? LuyKeLuongPhunThanTrongCa_Manual { get; set; }
        public int? SoLuongSungPhun_Auto { get; set; }
        public int? SoLuongSungPhun_Manual { get; set; }
        public decimal? ApLucGioLanhLoCao_Auto { get; set; }
        public decimal? ApLucGioLanhLoCao_Manual { get; set; }
        public decimal? SanLuongNghien { get; set; }
        public decimal? SanLuongPhun { get; set; }
        public string? GhiChu { get; set; }
        public string? TinhTrangSanXuat { get; set; }
        public string? SapXepSanXuat { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    // ─── Update manual values per row ────────────────────────────────────────────
    public class UpdateLGPTLCManualItemDto
    {
        public long ID { get; set; }
        public decimal? NhietDoSiloBotThan1_Manual { get; set; }
        public decimal? NhietDoSiloBotThan2_Manual { get; set; }
        public decimal? NhietDoBonPhunThoi1_Manual { get; set; }
        public decimal? NhietDoBonPhunThoi2_Manual { get; set; }
        public decimal? NhietDoBonPhunThoi3_Manual { get; set; }
        public decimal? DongDienMayNghien_Manual { get; set; }
        public decimal? DongDienQuatGioNguoc_Manual { get; set; }
        public decimal? NhietDoDauVaoMayNghien_Manual { get; set; }
        public decimal? NhietDoDauRaMayNghien_Manual { get; set; }
        public decimal? NhietDoKhoangLo_Manual { get; set; }
        public decimal? MucLieuSiloBotThan1_Manual { get; set; }
        public decimal? MucLieuSiloBotThan2_Manual { get; set; }
        public decimal? MucLieuSiloThanTho_Manual { get; set; }
        public decimal? TrongLuongBonPhunThoi1_Manual { get; set; }
        public decimal? TrongLuongBonPhunThoi2_Manual { get; set; }
        public decimal? TrongLuongBonPhunThoi3_Manual { get; set; }
        public decimal? ApLucKhiThan_Manual { get; set; }
        public decimal? ApLucBonKhiN2_Manual { get; set; }
        public decimal? NhietDoTramDauBoiTron_Manual { get; set; }
        public decimal? NhietDoStatoDongCoMayNghien_Manual { get; set; }
        public decimal? NhietDoTrucDongCoMayNghien_Manual { get; set; }
        public decimal? ApLucTrucNghien_Manual { get; set; }
        public decimal? YeuCauTuLoCao_Manual { get; set; }
        public decimal? LuongThanPhunThucTe_Manual { get; set; }
        public decimal? LuyKeLuongPhunThanTrongCa_Manual { get; set; }
        public int? SoLuongSungPhun_Manual { get; set; }
        public decimal? ApLucGioLanhLoCao_Manual { get; set; }
        public string? GhiChu { get; set; }
    }

    public class UpdateLGPTLCManualDto
    {
        public Guid IDPhieu { get; set; }
        public List<UpdateLGPTLCManualItemDto> Items { get; set; } = new();
    }

    // ─── Update phiếu-level summary ──────────────────────────────────────────────
    public class UpdateLGPTLCSummaryDto
    {
        public decimal? SanLuongNghien { get; set; }
        public decimal? SanLuongPhun { get; set; }
        public string? TinhTrangSanXuat { get; set; }
        public string? SapXepSanXuat { get; set; }
    }
}

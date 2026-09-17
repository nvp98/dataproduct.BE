namespace dataproduct.api.Models.MasterData
{
    // Kết quả trả về từ dbo.SP_Get_BBGN (keyless, không map bảng thật)
    // Ca/Kip là text ("1"/"2", "A"/"B"/"C") giống Tbl_BienBanGiaoNhan.
    public class BBGNChiTietResult
    {
        public string? Kip { get; set; }
        public string? Ca { get; set; }
        public bool? IsDelete { get; set; }
        public int? ID_TrangThai_BBGN { get; set; }
        public int? ID_QuyTrinh { get; set; }
        public int? ID_BBGN_Cu { get; set; }
        public DateTime? ThoiGianXuLyBG { get; set; }

        // ─── Xưởng/Phòng ban giao ───────────────────────────────────────────
        public int? ID_Xuong_BG { get; set; }
        public string? TenXuongGiao { get; set; }
        public int? ID_PhongBanGiao { get; set; }
        public string? TenPhongBanGiao { get; set; }
        public string? TenNganPhongBanGiao { get; set; }

        public string? TenVatTu { get; set; }

        // ─── Xưởng/Phòng ban nhận ───────────────────────────────────────────
        public int? ID_Xuong_BN { get; set; }
        public string? TenXuongNhan { get; set; }
        public int? ID_PhongBanNhan { get; set; }
        public string? TenPhongBanNhan { get; set; }
        public string? TenNganPhongBanNhan { get; set; }

        public int ID_CT_BBGN { get; set; }
        public int? ID_VatTu { get; set; }
        public string? MaLO { get; set; }
        public double? DoAm_W { get; set; }
        public double? KhoiLuong_BG { get; set; }
        public double? KL_QuyKho_BG { get; set; }
        public double? KhoiLuong_BN { get; set; }
        public double? KL_QuyKho_BN { get; set; }
        public string? BBGN_GhiChu { get; set; }
    }
}

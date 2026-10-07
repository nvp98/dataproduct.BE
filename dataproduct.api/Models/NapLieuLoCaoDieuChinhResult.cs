namespace dataproduct.api.Models
{
    // Kết quả trả về từ dbo.Sp_GetNVLNapLieuLoCao (PRODUCT_FORM) — nguồn "Nội bộ - Xuất SX" cho
    // Phiếu điều chỉnh NM.LG (dữ liệu Nạp liệu lò cao, LG_NL_ChiTiet). Cùng hình dạng cột với
    // BBGNChiTietResult (SP_Get_BBGN) để dùng chung 1 luồng map sang LG_PhieuDieuChinhBBGNDto,
    // nhưng Ca ở đây là INT (lấy từ BM_Phieu.Ca) chứ không phải text như Tbl_BienBanGiaoNhan.Ca.
    public class NapLieuLoCaoDieuChinhResult
    {
        public string? Kip { get; set; }
        public int? Ca { get; set; }
        public bool? IsDelete { get; set; }
        public DateTime? ThoiGianXuLyBG { get; set; }

        // ─── Xưởng/Phòng ban giao (= nhận, nội bộ 1 xưởng tự điều chỉnh số liệu) ────
        public int? ID_Xuong_BG { get; set; }
        public string? TenXuongGiao { get; set; }
        public int? ID_PhongBanGiao { get; set; }
        public string? TenPhongBanGiao { get; set; }
        public string? TenNganPhongBanGiao { get; set; }

        public string? TenVatTu { get; set; }

        public int? ID_Xuong_BN { get; set; }
        public string? TenXuongNhan { get; set; }
        public int? ID_PhongBanNhan { get; set; }
        public string? TenPhongBanNhan { get; set; }
        public string? TenNganPhongBanNhan { get; set; }

        // MIN(LG_NL_ChiTiet.ID) — khóa nguồn dùng để khớp lại dòng cũ khi tải lại (giống
        // ID_CT_BBGN của SP_Get_BBGN). Không cùng không gian giá trị với ID_CT_BBGN thật của BBGN
        // (khác bảng nguồn) — Service phải cộng offset trước khi lưu để tránh trùng khóa.
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

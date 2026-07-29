namespace dataproduct.api.Models.MasterData
{
    // Kết quả trả về từ sp_LG_PhanBo_TongNhanVe_BBGN (keyless, không map bảng thật)
    public class TongNhanVeBbgnResult
    {
        public int IdXuong { get; set; }
        public string? Ca { get; set; } // nchar(1) — text "1"/"2", giống Tbl_BienBanGiaoNhan.Ca
        public double KhoiLuong { get; set; } // KL_QuyKho_BG là float trong SQL Server — EF Core FromSqlRaw ép kiểu trực tiếp, không tự convert sang decimal được
    }
}

namespace dataproduct.api.DTOs.NMTKVV_Dto
{
    // ─── Tra cứu Vật tư SAP (PRODUCTDATA.Tbl_VatTu) ─────────────────────────────
    // Dùng cho ô chọn (searchable Select) khi gán mapping — trả theo trang, khớp
    // shape AutocompleteSearchApi<T> phía FE (data/totalRecords/page/pageSize).

    public class VatTuLookupDto
    {
        public int IdVatTu { get; set; }
        public string? TenVatTu { get; set; }
        public string? MaVatTuSap { get; set; }
        public string? TenVatTuSap { get; set; }
        public string? DonViTinh { get; set; }
        public int? IdNhomVatTu { get; set; }
        public string? PhongBan { get; set; }
        public int? IdTrangThai { get; set; }
    }

    public class VatTuLookupResultDto
    {
        public List<VatTuLookupDto> Data { get; set; } = new();
        public int TotalRecords { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    // ─── TKVV_NVL_BBGN_Mapping — ánh xạ NVL (TKVV_NguyenVatLieu) ↔ Vật tư BBGN ──

    public class TKVVNvlBbgnMappingDto
    {
        public int Id { get; set; }
        public int TkvvNvlId { get; set; }
        public string? TenNVL { get; set; }
        public int IdVatTuBBGN { get; set; }
        public string? TenVatTu { get; set; }
        public string? MaVatTuSap { get; set; }
        public string? TenVatTuSap { get; set; }
        public string? DonViTinh { get; set; }
        public bool TrangThai { get; set; }
        public string? GhiChu { get; set; }
        public DateTime NgayTao { get; set; }
    }

    public class CreateTKVVNvlBbgnMappingDto
    {
        public int TkvvNvlId { get; set; }
        public int IdVatTuBBGN { get; set; }
        public string? GhiChu { get; set; }
    }

    public class UpdateTKVVNvlBbgnMappingDto
    {
        public bool TrangThai { get; set; }
        public string? GhiChu { get; set; }
    }

    // ─── Dữ liệu nhận BBGN theo NVL (dbo.sp_TKVV_Get_NVL_BBGN) ──────────────────
    // SP tự join TKVV_NVL_BBGN_Mapping + chi tiết BBGN theo (Ngay, Ca, TKVV_NVL_ID, Scope).
    // Trả theo NVL (không theo Silo) — dùng để tự điền cột Nhập ở TKVV_TonSilo/init-rows.

    public class TKVVNvlBbgnDataDto
    {
        public int MappingId { get; set; }
        public int TkvvNvlId { get; set; }
        public int IdVatTuBBGN { get; set; }
        public bool MappingTrangThai { get; set; }
        public string? Kip { get; set; }
        public int Ca { get; set; }
        public int? IdXuongBG { get; set; }
        public int? IdXuongBN { get; set; }
        public long IdCtBBGN { get; set; }
        public int IdVatTu { get; set; }
        public string? MaLo { get; set; }
        public decimal? DoAmW { get; set; }
        public decimal? KhoiLuongBG { get; set; }
        public decimal? KLQuyKhoBG { get; set; }
        public decimal? KhoiLuongBN { get; set; }
        public decimal? KLQuyKhoBN { get; set; }
        public string? BBGNGhiChu { get; set; }
    }
}

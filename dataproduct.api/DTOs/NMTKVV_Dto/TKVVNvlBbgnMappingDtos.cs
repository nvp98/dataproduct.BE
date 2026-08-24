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
}

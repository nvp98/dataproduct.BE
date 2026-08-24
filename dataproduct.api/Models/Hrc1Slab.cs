namespace dataproduct.api.Models;

public class Hrc1Slab
{
    public int Id { get; set; }
    public string IDSlab { get; set; } = "";
    public string? IDPiece { get; set; }
    public string? MaMe { get; set; }
    public string? MacThep { get; set; }
    public DateOnly? NgaySX { get; set; }
    public string? CaSX { get; set; }
    public string? KipSX { get; set; }
    public string? MayDuc { get; set; }
    public DateTime? CutDate { get; set; }
    public decimal? ChieuDay { get; set; }
    public decimal? ChieuRong { get; set; }
    public decimal? ChieuDai { get; set; }
    public decimal? KhoiLuong { get; set; }
    public DateTime NgayTao { get; set; }
    public DateTime? NgayCapNhat { get; set; }
    public string? GhiChu { get; set; }
    public string? MaVatTu { get; set; }

    // ── Xóa mềm ────────────────────────────────────────────────────────────
    // Đánh dấu để loại khỏi mọi hiển thị/luồng nghiệp vụ, đồng thời chặn SyncAsync
    // ghi đè lại dữ liệu — khác xóa cứng (DELETE) trước đây sẽ bị Sync insert lại
    // vì TSC luôn trả về đủ slab của ca.
    public bool IsDeleted { get; set; }
    public int? NguoiXoa { get; set; }
    public DateTime? NgayXoa { get; set; }
}

namespace dataproduct.api.DTOs.NMTKVV_Dto
{
    // ─── Danh mục NVL (sản phẩm) theo biểu mẫu ────────────────────────────────

    public class TKVVNguyenVatLieuDto
    {
        public int Id { get; set; }
        public string MaBM { get; set; } = string.Empty;
        public string TenNVL { get; set; } = string.Empty;
        public string? DonViTinh { get; set; }
        public int? ThuTu { get; set; }
        public bool TrangThai { get; set; }
        public string? GhiChu { get; set; }
    }

    public class CreateTKVVNguyenVatLieuDto
    {
        public string MaBM { get; set; } = string.Empty;
        public string TenNVL { get; set; } = string.Empty;
        public string? DonViTinh { get; set; }
        public int? ThuTu { get; set; }
        public string? GhiChu { get; set; }
    }

    public class UpdateTKVVNguyenVatLieuDto
    {
        public string MaBM { get; set; } = string.Empty;
        public string TenNVL { get; set; } = string.Empty;
        public string? DonViTinh { get; set; }
        public int? ThuTu { get; set; }
        public bool TrangThai { get; set; }
        public string? GhiChu { get; set; }
    }

    // ─── Mapping Tag PLC (TagIDEMS) -> Scope + Ca ───────────────────────────────
    // 1 Tag = 1 BM/xưởng/ca (báo TỔNG khối lượng cả ca) — ca ngày và ca đêm dùng 2
    // Tag khác nhau nên bắt buộc khai báo Ca. Không gắn với 1 sản phẩm cụ thể —
    // KTV/KCS tự chọn sản phẩm và tự chia Loại 1/2/3/Phế phẩm khi nhập biên bản.

    public class TKVVMappingDto
    {
        public long Id { get; set; }
        public string TagID { get; set; } = string.Empty;
        public string MaKey { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public byte Ca { get; set; }
        public int? ThuTu { get; set; }
        public DateOnly? TuNgay { get; set; }
        public DateOnly? DenNgay { get; set; }
        public bool TrangThai { get; set; }
        public string? GhiChu { get; set; }
        public DateTime NgayTao { get; set; }
    }

    public class CreateTKVVMappingDto
    {
        public string TagID { get; set; } = string.Empty;
        public string MaKey { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public byte Ca { get; set; }
        public int? ThuTu { get; set; }
        public DateOnly? TuNgay { get; set; }
        public DateOnly? DenNgay { get; set; }
        public string? GhiChu { get; set; }
    }

    public class UpdateTKVVMappingDto
    {
        public string TagID { get; set; } = string.Empty;
        public string MaKey { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public byte Ca { get; set; }
        public int? ThuTu { get; set; }
        public DateOnly? TuNgay { get; set; }
        public DateOnly? DenNgay { get; set; }
        public bool TrangThai { get; set; }
        public string? GhiChu { get; set; }
    }

    // ─── Danh sách Tag PLC từ EMS (dbo.EMS_GetMappingTag) để chọn khi tạo Mapping ─
    // Không lọc theo Loai — dùng chung cho toàn NM.TKVV, không riêng biên bản sản
    // lượng, nên trả mọi loại Tag của xưởng (SANLUONG, TONSILO, ...). Ca suy ra từ
    // LoaiDuLieu: "1"=ngày, "2"=đêm; "0" (đang tích lũy ca hiện tại, không cố định
    // ngày/đêm) trả về Ca=null — không dùng để Mapping.

    public class EMSMappingTagDto
    {
        public int Id { get; set; }
        public string Xuong { get; set; } = string.Empty;
        public string? Loai { get; set; }
        public string? TenCan { get; set; }
        public string TagIDEMS { get; set; } = string.Empty;
        public string TagName { get; set; } = string.Empty;
        public int? Ca { get; set; }
        public string? GhiChu { get; set; }
    }

    // ─── Dữ liệu thô lọc theo Scope/khoảng ngày ────────────────────────────────

    public class TKVVDuLieuRawDto
    {
        public long Id { get; set; }
        public string TagID { get; set; } = string.Empty;
        public string? MaKey { get; set; }
        public decimal? Value { get; set; }
        public DateOnly Ngay { get; set; }
        public byte Ca { get; set; }
        public string Scope { get; set; } = string.Empty;
        public DateTime? ThoiGian { get; set; }
    }

    public class GetTKVVDataByFilterDto
    {
        public string? Scope { get; set; }
        public DateTime? NgayBatDau { get; set; }
        public DateTime? NgayKetThuc { get; set; }
    }

    // ─── Tổng tự động (PLC) theo Ngay/Ca/Scope — 1 BM/xưởng chỉ có 1 số tổng duy
    //     nhất (không tách theo sản phẩm), dùng để đối chiếu — KHÔNG tự điền vào
    //     Loại 1/2/3/Phế phẩm vì PLC không phân loại được ───────────────────────

    public class TKVVTongTuDongDto
    {
        public decimal TongTuDong { get; set; }
    }

    // ─── Chi tiết sản lượng theo phiếu ─────────────────────────────────────────

    public class TKVVChiTietDto
    {
        public long Id { get; set; }
        public Guid IdPhieu { get; set; }
        public int? Scope { get; set; }
        public DateOnly? Ngay { get; set; }
        public byte? Ca { get; set; }
        public int NguyenVatLieuID { get; set; }
        public string? TenNVL { get; set; }
        public int? ThuTuDong { get; set; }
        public TimeOnly? ThoiGian { get; set; }
        public decimal? Loai1 { get; set; }
        public decimal? Loai2 { get; set; }
        public decimal? Loai3 { get; set; }
        public decimal? PhePham { get; set; }
        public bool IsEdited { get; set; }
        public int? NguoiSuaID { get; set; }
        public DateTime? NgaySua { get; set; }
        public string? LyDoSua { get; set; }
        public string? GhiChu { get; set; }
    }
}

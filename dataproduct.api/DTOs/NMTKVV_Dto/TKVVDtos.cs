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
        public string? Scope { get; set; }
        public string? TenScope { get; set; }
    }

    public class CreateTKVVNguyenVatLieuDto
    {
        public string MaBM { get; set; } = string.Empty;
        public string TenNVL { get; set; } = string.Empty;
        public string? DonViTinh { get; set; }
        public int? ThuTu { get; set; }
        public string? GhiChu { get; set; }
        public string? Scope { get; set; }
        public string? TenScope { get; set; }
    }

    public class UpdateTKVVNguyenVatLieuDto
    {
        public string MaBM { get; set; } = string.Empty;
        public string TenNVL { get; set; } = string.Empty;
        public string? DonViTinh { get; set; }
        public int? ThuTu { get; set; }
        public bool TrangThai { get; set; }
        public string? GhiChu { get; set; }
        public string? Scope { get; set; }
        public string? TenScope { get; set; }
    }

    // ─── Mapping NVL ↔ Tag EMS + Ca (bảng TKVV_NVL_TagMapping) ─────────────────
    // 1 bản ghi = 1 NVL dùng TagIDEMS nào ở Ca nào. Scope không lưu ở mapping —
    // kế thừa từ TKVV_NguyenVatLieu.Scope qua NguyenVatLieuID.

    public class TKVVMappingDto
    {
        public int Id { get; set; }
        public int NguyenVatLieuID { get; set; }
        public string? TenNVL { get; set; }
        public string? ScopeNVL { get; set; }
        public string TagIDEMS { get; set; } = string.Empty;
        public int Ca { get; set; }
        public bool TrangThai { get; set; }
        public string? GhiChu { get; set; }
        public DateTime NgayCapNhat { get; set; }
    }

    public class CreateTKVVMappingDto
    {
        public int NguyenVatLieuID { get; set; }
        public string TagIDEMS { get; set; } = string.Empty;
        public int Ca { get; set; }
        public string? GhiChu { get; set; }
    }

    public class UpdateTKVVMappingDto
    {
        public int NguyenVatLieuID { get; set; }
        public string TagIDEMS { get; set; } = string.Empty;
        public int Ca { get; set; }
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
        public string? MaCan { get; set; }
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
        public decimal? GiaTriTuDong { get; set; }
        public decimal? GiaTriDieuChinh { get; set; }
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

    // ─── Giá trị NVL tự động từ EMS (SP_TKVV_GetGiaTriNVL_Auto) ────────────────
    // Gọi SP với (Ngay, Ca, Scope code, MaBM) — trả tổng GiaTri từ EMS_DATA_CAN
    // theo mapping TKVV_NVL_TagMapping. Dùng để đổ dữ liệu vào bảng khi tạo phiếu.

    public class TKVVGiaTriNVLAutoDto
    {
        public int NguyenVatLieuID { get; set; }
        public string MaBM { get; set; } = string.Empty;
        public string TenNVL { get; set; } = string.Empty;
        public string? DonViTinh { get; set; }
        public int? ThuTu { get; set; }
        public string? Scope { get; set; }
        public string? TenScope { get; set; }
        public decimal GiaTri { get; set; }
        public int SoLuongTag { get; set; }
        public DateTime? ThoiGianTu { get; set; }
        public DateTime? ThoiGianDen { get; set; }
    }

    // ─── Dữ liệu cân từ SP_TKVV_GetDuLieuCan — đổ bảng khi tạo phiếu BC SLCP ──
    // Gọi với (Ngay, Ca, MaBM, LoaiDuLieu, Scope int) — trả từng dòng NVL/Silo.

    public class TKVVDuLieuCanDto
    {
        public DateTime Ngay { get; set; }
        public int Ca { get; set; }
        public string? MaBM { get; set; }
        public string? Scope { get; set; }
        public string? Xuong { get; set; }
        public int? SiloID { get; set; }
        public string? MaSilo { get; set; }
        public int NguyenVatLieuID { get; set; }
        public string TenNVL { get; set; } = string.Empty;
        public string? DonViTinh { get; set; }
        public decimal GiaTri { get; set; }
        public decimal GiaTriXuat { get; set; }
        public int SoLuongSilo { get; set; }
    }

    // ─── TKVV_BaoCaoSanLuongChiPhi — bảng lưu dữ liệu cân + trạng thái điều chỉnh ─

    public class TKVVBaoCaoSanLuongChiPhiDto
    {
        public long Id { get; set; }
        public Guid? PhieuID { get; set; }
        public DateOnly NgaySX { get; set; }
        public int Ca { get; set; }
        public string? Kip { get; set; }
        public int? Scope { get; set; }          // INT — khớp cột Scope trong DB
        public int? ThuTu { get; set; }
        public int NguyenVatLieuID { get; set; }
        public string? TenNVL { get; set; }
        public decimal? KLAm { get; set; }
        public decimal? KLAmAuto { get; set; }
        public decimal? DoAm { get; set; }
        public decimal? QuyKho { get; set; }
        public decimal? ThanhPhamL1 { get; set; }
        public decimal? ThanhPhamL2 { get; set; }
        public string? GhiChu { get; set; }
        public bool IsAdjusted { get; set; }
        public int? AdjustedBy { get; set; }
        public DateTime? AdjustedDate { get; set; }
    }

    public class LoadDuLieuCanRequestDto
    {
        public DateOnly NgaySX { get; set; }
        public string MaBM { get; set; } = string.Empty;
        public string LoaiDuLieu { get; set; } = "SANLUONG";
        public int Scope { get; set; }           // INT 1-6
        public int? CreatedBy { get; set; }
    }

    public class LoadDuLieuCanResultDto
    {
        public List<TKVVBaoCaoSanLuongChiPhiDto> Table1 { get; set; } = new(); // Ca ngày
        public List<TKVVBaoCaoSanLuongChiPhiDto> Table2 { get; set; } = new(); // Ca đêm
    }

    public class SaveBcSlRowDto
    {
        public long? Id { get; set; }
        public DateOnly NgaySX { get; set; }
        public int Ca { get; set; }
        public int? Scope { get; set; }          // INT
        public int NguyenVatLieuID { get; set; }
        public string? Kip { get; set; }
        public int? ThuTu { get; set; }
        public decimal? KLAm { get; set; }
        public decimal? KLAmAuto { get; set; }
        public decimal? DoAm { get; set; }
        public decimal? QuyKho { get; set; }
        public decimal? ThanhPhamL1 { get; set; }
        public decimal? ThanhPhamL2 { get; set; }
        public string? GhiChu { get; set; }
    }

    public class SaveBcSlPhieuRequestDto
    {
        public string MaBM { get; set; } = string.Empty;
        public Guid? PhieuID { get; set; }
        public int CurrentUserId { get; set; }
        public List<SaveBcSlRowDto> Rows { get; set; } = new();
    }

    // ─── Mapping Cân (EMS) → Xưởng theo Ngày/Ca/Kíp (bảng TKVV_SanLuongMapping) ─

    public class TKVVSanLuongMappingDto
    {
        public long Id { get; set; }
        public string TagID { get; set; } = string.Empty;
        public string? Scope { get; set; }
        public string? TenCan { get; set; }
        public byte Ca { get; set; }
        public string? Kip { get; set; }
        public DateOnly? TuNgay { get; set; }
        public DateOnly? DenNgay { get; set; }
        public bool TrangThai { get; set; }
        public string? GhiChu { get; set; }
        public DateTime NgayTao { get; set; }
        public int? NguoiTaoID { get; set; }
    }

    public class CreateTKVVSanLuongMappingDto
    {
        public string TagID { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public byte Ca { get; set; }
        public string? Kip { get; set; }
        public DateOnly? TuNgay { get; set; }
        public DateOnly? DenNgay { get; set; }
        public string? GhiChu { get; set; }
        public int? NguoiTaoID { get; set; }
    }

    public class UpdateTKVVSanLuongMappingDto
    {
        public string TagID { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public byte Ca { get; set; }
        public string? Kip { get; set; }
        public DateOnly? TuNgay { get; set; }
        public DateOnly? DenNgay { get; set; }
        public bool TrangThai { get; set; }
        public string? GhiChu { get; set; }
        public int? NguoiTaoID { get; set; }
    }

    // ─── Request đơn giản cho controller ───────────────────────────────────────

    public class UpdateGiaTriDieuChinhRequestDto
    {
        public decimal? GiaTriDieuChinh { get; set; }
    }

    public class SyncDuLieuTuEmsRequestDto
    {
        public DateTime Ngay { get; set; }
        public byte Ca { get; set; }
        public int Scope { get; set; }
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

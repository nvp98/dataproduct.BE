namespace dataproduct.api.DTOs.NMLG_Dto
{
    // ─── Nhóm phân bổ ──────────────────────────────────────────────────────────

    public class NhomPhanBoDto
    {
        public int Id { get; set; }
        public string TenNhom { get; set; } = string.Empty;
        public byte LoaiPhanBo { get; set; }
        public byte PhuongThucPhanBo { get; set; }
        public string? MaVatTu { get; set; }
        public int? ThuTu { get; set; }
    }

    public class CreateNhomPhanBoDto
    {
        public string TenNhom { get; set; } = string.Empty;
        public byte LoaiPhanBo { get; set; }
        public byte PhuongThucPhanBo { get; set; }
        public string? MaVatTu { get; set; }
        public int? ThuTu { get; set; }
    }

    public class UpdateNhomPhanBoDto
    {
        public string TenNhom { get; set; } = string.Empty;
        public byte LoaiPhanBo { get; set; }
        public byte PhuongThucPhanBo { get; set; }
        public string? MaVatTu { get; set; }
        public int? ThuTu { get; set; }
    }

    public class NvlNhomPhanBoDto
    {
        public int Id { get; set; }
        public int IdNvl { get; set; }
        public string? TenNvl { get; set; }   // join từ LG_NL_NVL
        public int IdNhomPhanBo { get; set; }
        public DateTime Ngay { get; set; }
        public byte Ca { get; set; }
        public int IdLoCao { get; set; }
    }

    public class AddNvlNhomPhanBoDto
    {
        public int IdNvl { get; set; }
        public DateTime Ngay { get; set; }
        public byte Ca { get; set; }
        public int IdLoCao { get; set; }
    }

    // ─── Tỷ lệ phân bổ (PP2 — nhập tay) ────────────────────────────────────────

    public class TyLePhanBoDto
    {
        public int Id { get; set; }
        public int IdNvl { get; set; }
        public string? TenNvl { get; set; }
        public DateTime Ngay { get; set; }
        public byte? Ca { get; set; }
        public decimal TyLe { get; set; }
        public string? GhiChu { get; set; }
        public int IdNguoiNhap { get; set; }
        public DateTime NgayNhap { get; set; }
    }

    public class CreateTyLePhanBoDto
    {
        public int IdNvl { get; set; }
        public DateTime Ngay { get; set; }
        public byte? Ca { get; set; }
        public decimal TyLe { get; set; }
        public string? GhiChu { get; set; }
        public int IdNguoiNhap { get; set; }
    }

    // ─── % theo nhóm (chỉ nhóm PP2 — cascade xuống từng NVL thành viên) ────────

    public class TyLeNhomDto
    {
        public int IdNhomPhanBo { get; set; }
        public DateTime Ngay { get; set; }
        public byte Ca { get; set; }
        public int IdLoCao { get; set; }
        public decimal TyLe { get; set; }
        public string? GhiChu { get; set; }
        public int IdNguoiNhap { get; set; }
        public DateTime NgayNhap { get; set; }
    }

    public class CreateTyLeNhomDto
    {
        public int IdNhomPhanBo { get; set; }
        public DateTime Ngay { get; set; }
        public byte Ca { get; set; }
        public int IdLoCao { get; set; }
        public decimal TyLe { get; set; }
        public string? GhiChu { get; set; }
        public int IdNguoiNhap { get; set; }
    }

    // ─── Sao chép cấu hình nhóm/NVL/% từ ca liền kề gần nhất có cấu hình (cùng Lò cao) ─

    public class SaoChepNhomPhanBoRequestDto
    {
        public byte LoaiPhanBo { get; set; }
        public DateTime NgayDich { get; set; }
        public byte CaDich { get; set; }
        public int IdLoCaoDich { get; set; }
        public int IdNguoiThucHien { get; set; }
    }

    public class SaoChepNhomPhanBoResultDto
    {
        public int SoNvlDaCopy { get; set; }
        public int SoTyLeDaCopy { get; set; }
        public DateTime NgayNguon { get; set; }
        public byte CaNguon { get; set; }
    }

    // ─── Tính / chốt phân bổ ───────────────────────────────────────────────────

    public class TinhPhanBoRequestDto
    {
        public DateTime Ngay { get; set; }
        public int IdNguoiThucThi { get; set; }
    }

    public class ChotPhanBoRequestDto
    {
        public DateTime Ngay { get; set; }
        public int IdNguoiXacNhan { get; set; }
    }

    public class UpdateMaCongDoanChiPhiRequestDto
    {
        public DateTime Ngay { get; set; }
        public int IdNvl { get; set; }
        public string? MaCongDoanChiPhi { get; set; } // DQ1 / DQ2
    }

    public class KetQuaPhanBoDto
    {
        public int Id { get; set; }
        public DateTime Ngay { get; set; }
        public byte? Ca { get; set; }
        public int IdLoCao { get; set; }
        public byte LoaiPhanBo { get; set; }
        public int IdNvl { get; set; }
        public string? TenNvl { get; set; }
        public string? MaCongDoanChiPhi { get; set; }       // DQ1 / DQ2
        public int IdNhomPhanBo { get; set; }
        public string? TenNhomPhanBo { get; set; }
        public byte PhuongThucPhanBo { get; set; }
        public decimal KhoiLuongNapLieu { get; set; }       // E
        public decimal KhoiLuongNapLieuNhom { get; set; }   // Tổng E của cả nhóm (SUM E các NVL thành viên)
        public decimal TyLePhanBo { get; set; }             // F
        public decimal KhoiLuongNhanVe { get; set; }        // G — lấy từ biên bản giao nhận (LG_PB_BienBanNhanQHLCCVH), dùng chung cho cả bảng
        public decimal KhoiLuongPhanBo { get; set; }        // H
        public decimal KhoiLuongChotCuoi { get; set; }      // I = E + H
        public bool LaDongConLai { get; set; }
        public byte TrangThai { get; set; }
    }

    public class ValidatePhanBoResultDto
    {
        public bool IsMatched { get; set; }
        public decimal TongNhanVe { get; set; }
        public decimal TongPhanBo { get; set; }
        public decimal ChenhLech { get; set; }
    }

    public class KetQuaPhanBoQueryResultDto
    {
        public List<KetQuaPhanBoDto> KetQua { get; set; } = new();
        public ValidatePhanBoResultDto Validate { get; set; } = new();
    }

    // ─── Kết quả gộp CVH + Than cốc <10mm (cùng nhóm/NVL/tỷ lệ, khác nguồn G) ──

    public class KetQuaThanCocDto
    {
        public int IdLoCao { get; set; }
        public byte? Ca { get; set; }
        public int IdNvl { get; set; }
        public string? TenNvl { get; set; }
        public string? MaCongDoanChiPhi { get; set; }         // DQ1 / DQ2
        public int IdNhomPhanBo { get; set; }
        public string? TenNhomPhanBo { get; set; }
        public byte PhuongThucPhanBo { get; set; }
        public decimal KhoiLuongNapLieu { get; set; }         // E (dùng chung)
        public decimal KhoiLuongNapLieuNhom { get; set; }     // Tổng E của cả nhóm (SUM E các NVL thành viên)
        public decimal TyLePhanBo { get; set; }               // % (dùng chung, snapshot từ CVH)
        public decimal KhoiLuongNhanVeCvh { get; set; }         // G_CVH — lấy từ biểu nạp liệu, dùng chung cho cả bảng
        public decimal KhoiLuongPhanBoCvh { get; set; }        // H_CVH
        public decimal KhoiLuongNhanVeThanCoc10 { get; set; }   // G_ThanCoc10 — lấy từ biên bản giao nhận, dùng chung cho cả bảng
        public decimal KhoiLuongPhanBoThanCoc10 { get; set; }  // H_ThanCoc10
        public decimal KhoiLuongChotCuoi { get; set; }         // I = E + H_CVH + H_ThanCoc10
        public bool LaDongConLai { get; set; }
        public byte TrangThai { get; set; }
    }

    public class KetQuaThanCocQueryResultDto
    {
        public List<KetQuaThanCocDto> KetQua { get; set; } = new();
        public ValidatePhanBoResultDto ValidateCvh { get; set; } = new();
        public ValidatePhanBoResultDto ValidateThanCoc10 { get; set; } = new();
    }

    // ─── Nội bộ (dùng trong Service, không phải HTTP DTO) ─────────────────────

    public class NapLieuTheoNvlDto
    {
        public DateTime Ngay { get; set; }
        public byte? Ca { get; set; }
        public int IdLoCao { get; set; }
        public int IdNvl { get; set; }
        public decimal KhoiLuongNapLieu { get; set; }
    }

    public class TongNhanVeDto
    {
        public DateTime Ngay { get; set; }
        public byte? Ca { get; set; }
        public int IdLoCao { get; set; }
        public decimal KhoiLuongNhanVe { get; set; }
    }
}

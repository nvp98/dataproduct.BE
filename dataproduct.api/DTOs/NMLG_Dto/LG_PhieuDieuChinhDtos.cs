namespace dataproduct.api.DTOs.NMLG_Dto
{
    // Một dòng chi tiết dùng làm nguồn cho Phiếu điều chỉnh số liệu NM.LG — có thể đến từ
    // SP_Get_BBGN (LoaiDieuChinh = 1, "Nhập - Xuất") hoặc Sp_GetNVLNapLieuLoCao
    // (LoaiDieuChinh = 2, "Nội bộ - Xuất SX"); xem LG_PhieuDieuChinhService.GetNguonAsync.
    public class LG_PhieuDieuChinhBBGNDto
    {
        public int LoaiDieuChinh { get; set; }
        public int IdCtBBGN { get; set; }
        public int? IdCa { get; set; }
        // Lò cao (1..6) tra từ xưởng nhận (IdXuongBN) — null nếu xưởng chưa có trong LG_PB_Map_Xuong_LoCao
        public int? IdLoCao { get; set; }
        public DateTime? ThoiGianXuLyBG { get; set; }

        // Bên giao — tên Xưởng/Phòng ban tra sẵn từ SP_Get_BBGN (join Tbl_Xuong/Tbl_PhongBan)
        public int? IdXuongBG { get; set; }
        public string? TenXuongGiao { get; set; }
        public int? IdPhongBanGiao { get; set; }
        public string? TenPhongBanGiao { get; set; }
        public string? TenNganPhongBanGiao { get; set; }

        // Bên nhận — tên Xưởng/Phòng ban tra sẵn từ SP_Get_BBGN
        public int? IdXuongBN { get; set; }
        public string? TenXuongNhan { get; set; }
        public int? IdPhongBanNhan { get; set; }
        public string? TenPhongBanNhan { get; set; }
        public string? TenNganPhongBanNhan { get; set; }

        public int? IdVatTu { get; set; }
        public string? TenVatTu { get; set; }
        public string? MaLo { get; set; }
        public double? DoAm { get; set; }
        public double? KhoiLuongBG { get; set; }
        public double? KLQuyKhoBG { get; set; }
        public double? KhoiLuongBN { get; set; }
        public double? KLQuyKhoBN { get; set; }
        public string? GhiChu { get; set; }
    }

    // Xưởng của 1 phòng ban — mỗi xưởng 1 tab trong phiếu theo phòng ban
    public class LG_PhieuDieuChinhXuongDto
    {
        public int Id { get; set; }
        public string Ten { get; set; } = string.Empty;
    }

    // 1 dòng BBGN ở phần Nhập hoặc Xuất của phiếu nhà máy — kèm số điều chỉnh của CẢ 2 bên (bản ghi dùng chung
    // theo IdCtBBGN). Bên đang xem chỉ sửa cột của mình; độ ẩm lấy từ BBGN, không nhập tay.
    public class LG_PhieuDieuChinhBBGNLineDto
    {
        public int IdCtBBGN { get; set; }
        public int? IdCa { get; set; }
        public int? IdVatTu { get; set; }
        public string? TenVatTu { get; set; }
        public string? MaLo { get; set; }
        public DateTime? ThoiGianXuLyBG { get; set; }
        public int? IdXuongGiao { get; set; }
        public string? TenXuongGiao { get; set; }
        public string? TenPhongBanGiao { get; set; }
        public int? IdXuongNhan { get; set; }
        public string? TenXuongNhan { get; set; }
        public string? TenPhongBanNhan { get; set; }
        /// <summary>Id tab chứa dòng = xưởng của bên đang xem (Nhập: xưởng nhận, Xuất: xưởng giao).</summary>
        public int? IdTab { get; set; }
        // Độ ẩm của dòng trong BBGN (DoAm_W)
        public double? DoAm { get; set; }

        public int? LoaiSoDieuChinh { get; set; }
        public decimal? KhoiLuongXuat { get; set; }
        public decimal? KLQuyKhoXuat { get; set; }
        public string? GhiChuXuat { get; set; }
        public int? NguoiXuat { get; set; }
        public DateTime? ThoiGianXuat { get; set; }
        public decimal? KhoiLuongNhap { get; set; }
        public decimal? KLQuyKhoNhap { get; set; }
        public string? GhiChuNhap { get; set; }
        public int? NguoiNhap { get; set; }
        public DateTime? ThoiGianNhap { get; set; }
    }

    // Lưu số điều chỉnh của 1 bên (chiều "nhan" hoặc "xuat") cho 1 dòng BBGN. Mỗi phiếu có dòng riêng trong
    // LG_PhieuDieuChinh_ChiTiet (Nhập = loại 1, Xuất = loại 3) nên kèm các thông tin mô tả dòng BBGN để ghi vào đó.
    public class SaveLG_PhieuDieuChinhBBGNDto
    {
        public int IdCtBBGN { get; set; }
        public int? IdVatTu { get; set; }
        public string? TenVatTu { get; set; }
        public string? MaLo { get; set; }
        public string? TenXuongGiao { get; set; }
        public string? TenPhongBanGiao { get; set; }
        public string? TenXuongNhan { get; set; }
        public string? TenPhongBanNhan { get; set; }
        public int? LoaiSoDieuChinh { get; set; }
        public decimal? KhoiLuong { get; set; }
        // Độ ẩm của dòng (lấy từ BBGN) — để BE tự tính KL quy khô của bên mình khi loại số = Sau ẩm
        public double? DoAm { get; set; }
        public string? GhiChu { get; set; }
        // Chỉ để ghi vào thông báo lỗi cho dễ nhận ra dòng (vd tên vật tư - mã lô)
        public string? MoTa { get; set; }
    }

    // "Tải dữ liệu nguồn" của phiếu đã lưu: ghi các dòng BBGN Nhập/Xuất vào database cho phiếu
    public class DongBoLG_PhieuDieuChinhBBGNRequest
    {
        // Phòng ban của phiếu (từ file cấu hình biểu mẫu) — phạm vi xưởng theo LG_PDC_Scope_PhongBan
        public int IdPhongBan { get; set; }
        // Ngày cần lấy (ngày đang chọn trên phiếu); bỏ trống thì lấy ngày đã lưu của phiếu
        public DateTime? Ngay { get; set; }
        public int? IdNguoiThucHien { get; set; }
    }

    public class LG_PhieuDieuChinhDongBoBBGNResult
    {
        public int SoDongNhap { get; set; }
        public int SoDongXuat { get; set; }
        /// <summary>Dòng bỏ qua vì đang do phiếu khác (còn hiệu lực) của cùng bên giữ</summary>
        public int SoDongBoQua { get; set; }
    }

    public class SaveLG_PhieuDieuChinhBBGNRequest
    {
        // Phòng ban của phiếu (từ file cấu hình biểu mẫu) — ghi vào ID_PhongBan của dòng
        public int? IdPhongBan { get; set; }
        public int? IdNguoiThucHien { get; set; }
        public List<SaveLG_PhieuDieuChinhBBGNDto> Items { get; set; } = new();
    }

    // Reset số điều chỉnh của BÊN KIA cho 1 dòng BBGN — bắt buộc có ghi chú (lý do)
    public class ResetLG_PhieuDieuChinhBBGNRequest
    {
        public int IdCtBBGN { get; set; }
        public string? GhiChu { get; set; }
        public int? IdNguoiThucHien { get; set; }
    }

    // Lò cao hiển thị thành tab trên Phiếu điều chỉnh
    public class LG_PhieuDieuChinhLoCaoDto
    {
        public int Id { get; set; }
        public string TenLoCao { get; set; } = string.Empty;
    }

    // Chi tiết Phiếu điều chỉnh — ánh xạ bảng LG_PhieuDieuChinh_ChiTiet
    public class LG_PhieuDieuChinhChiTietDto
    {
        public long Id { get; set; }
        public Guid IdPhieu { get; set; }
        public int? IdNVL { get; set; } // legacy — xem IdNvlBBGN/IdNvlNapLieu
        // Rõ ràng theo đúng không gian ID nguồn — chỉ 1 trong 2 có giá trị tùy LoaiDieuChinh.
        public int? IdNvlBBGN { get; set; }      // ID vật tư BBGN (LoaiDieuChinh = 1)
        public int? IdNvlNapLieu { get; set; }   // ID LG_NL_NVL (LoaiDieuChinh = 2)
        public string TenNVL { get; set; } = string.Empty;
        public int? IdNVLChiTiet { get; set; }
        public int? IdNhomNVL { get; set; }
        public string? Dvt { get; set; }
        public string? MaLo { get; set; }
        public int? ThuTu { get; set; }
        public int? LoaiDieuChinh { get; set; }
        public int? LoaiSoDieuChinh { get; set; }
        public string? PhongBanXuat { get; set; }
        public string? XuongXuat { get; set; }
        public decimal? KhoiLuongXuat { get; set; }
        public decimal? KhoiLuongQuyKhoXuat { get; set; }
        public string? PhongBanNhap { get; set; }
        public string? XuongNhap { get; set; }
        public decimal? KhoiLuongNhap { get; set; }
        public decimal? KhoiLuongQuyKhoNhap { get; set; }
        public decimal? DoAm { get; set; }
        public string? ViTri { get; set; }
        public string? PhanLoai { get; set; }
        public string? GhiChu { get; set; }
        public string? NguoiTao { get; set; }
        public DateTime ThoiGianTao { get; set; }
        public string? NguoiSua { get; set; }
        public DateTime? ThoiGianSua { get; set; }
        // Người điều chỉnh nhận (FE tự gán khi người đó sửa Khối lượng/Độ ẩm của dòng).
        public int? NguoiDieuChinhNhan { get; set; }
        public DateTime? ThoiGianDieuChinhNhan { get; set; }
        // Khóa liên kết ngược tới dòng BBGN nguồn — dùng để khớp lại khi bấm "Tải dữ liệu" lần
        // sau, tránh mất số liệu điều chỉnh tay đã nhập cho dòng này (null nếu dòng nhập tay).
        public int? IdCtBBGN { get; set; }
        public int? IdLoCao { get; set; }
        public int? IdCa { get; set; }
        // Phòng ban của phiếu
        public int? IdPhongBan { get; set; }
    }

    // Dùng khi lưu (ghi đè toàn bộ) danh sách chi tiết của 1 phiếu
    public class SaveLG_PhieuDieuChinhChiTietDto
    {
        // null/0 = dòng mới; có giá trị = dòng đã lưu (cập nhật tại chỗ, không xóa-ghi lại)
        public long? Id { get; set; }
        // ThoiGianSua của dòng lúc FE tải — dùng phát hiện người khác đã sửa dòng này
        public DateTime? ThoiGianSua { get; set; }
        public int? IdNVL { get; set; }
        public string TenNVL { get; set; } = string.Empty;
        public int? IdNVLChiTiet { get; set; }
        public int? IdNhomNVL { get; set; }
        public string? Dvt { get; set; }
        public string? MaLo { get; set; }
        public int? ThuTu { get; set; }
        public int? LoaiDieuChinh { get; set; }
        public int? LoaiSoDieuChinh { get; set; }
        public string? PhongBanXuat { get; set; }
        public string? XuongXuat { get; set; }
        public decimal? KhoiLuongXuat { get; set; }
        public decimal? KhoiLuongQuyKhoXuat { get; set; }
        public string? PhongBanNhap { get; set; }
        public string? XuongNhap { get; set; }
        public decimal? KhoiLuongNhap { get; set; }
        public decimal? KhoiLuongQuyKhoNhap { get; set; }
        public decimal? DoAm { get; set; }
        public string? ViTri { get; set; }
        public string? PhanLoai { get; set; }
        public string? GhiChu { get; set; }
        public int? NguoiDieuChinhNhan { get; set; }
        public DateTime? ThoiGianDieuChinhNhan { get; set; }
        public int? IdCtBBGN { get; set; }
        // Lò cao (1..6) — dòng từ nguồn mang sẵn; dòng thêm tay thì FE gán theo tab đang mở
        public int? IdLoCao { get; set; }
        public int? IdCa { get; set; }
        // Phòng ban của phiếu (từ file cấu hình biểu mẫu)
        public int? IdPhongBan { get; set; }
    }

    public class ReplaceLG_PhieuDieuChinhChiTietRequest
    {
        public string? NguoiSua { get; set; }
        public List<SaveLG_PhieuDieuChinhChiTietDto> Items { get; set; } = new();
        // ID các dòng người dùng đã xóa trên giao diện (xóa mềm ở BE)
        public List<long> DeletedIds { get; set; } = new();
    }

    public class SyncLG_PhieuDieuChinhTuBBGNRequest
    {
        public string? NguoiThucHien { get; set; }
        // Phòng ban của phiếu (từ file cấu hình biểu mẫu) — giới hạn lò cao/xưởng theo LG_PDC_Scope_PhongBan
        public int? IdPhongBan { get; set; }
    }
}

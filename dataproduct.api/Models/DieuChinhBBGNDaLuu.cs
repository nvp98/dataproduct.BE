namespace dataproduct.api.Models
{
    // Số điều chỉnh Nhập/Xuất đã lưu của các dòng BBGN, đọc từ LG_PhieuDieuChinh_ChiTiet (khóa theo ID_CT_BBGN), ở các
    // phiếu còn hiệu lực — mỗi bên 1 dòng (dòng mới nhất):
    // - Nhap: dòng LoaiDieuChinh = 1 (bên nhận, giữ KL nhập)
    // - Xuat: dòng LoaiDieuChinh = 3 (bên giao, giữ KL xuất)
    public class DieuChinhBBGNDaLuu
    {
        public Dictionary<int, LG_PhieuDieuChinh_ChiTiet> Nhap { get; set; } = new();
        public Dictionary<int, LG_PhieuDieuChinh_ChiTiet> Xuat { get; set; } = new();
    }
}

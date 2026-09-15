using dataproduct.api.DTOs.NMLG_Dto;
using dataproduct.api.Models;
using dataproduct.api.Repositories;

namespace dataproduct.api.Services
{
    public class PhieuDieuChinhService
    {
        private readonly IPhieuDieuChinhRepository _repository;

        public PhieuDieuChinhService(IPhieuDieuChinhRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<PhieuDieuChinhBBGNDto>> GetBBGNAsync(DateTime ngay, int? ca, string? kip)
        {
            var rows = await _repository.GetBBGNAsync(ngay, ca, kip);

            return rows.Select(r => new PhieuDieuChinhBBGNDto
            {
                IdCtBBGN = r.ID_CT_BBGN,
                Kip = r.Kip,
                Ca = r.Ca,
                ThoiGianXuLyBG = r.ThoiGianXuLyBG,
                IdXuongBG = r.ID_Xuong_BG,
                IdXuongBN = r.ID_Xuong_BN,
                IdVatTu = r.ID_VatTu,
                TenVatTu = r.TenVatTu,
                MaLo = r.MaLO,
                DoAm = r.DoAm_W,
                KhoiLuongBG = r.KhoiLuong_BG,
                KLQuyKhoBG = r.KL_QuyKho_BG,
                KhoiLuongBN = r.KhoiLuong_BN,
                KLQuyKhoBN = r.KL_QuyKho_BN,
                GhiChu = r.BBGN_GhiChu,
            }).ToList();
        }

        // ─── Chi tiết Phiếu điều chỉnh ──────────────────────────────────────

        public async Task<List<PhieuDieuChinhChiTietDto>> GetChiTietByPhieuAsync(Guid idPhieu)
        {
            var rows = await _repository.GetChiTietByPhieuAsync(idPhieu);
            return rows.Select(x => new PhieuDieuChinhChiTietDto
            {
                Id = x.ID,
                IdPhieu = x.IDPhieu,
                IdNVL = x.IDNVL,
                TenNVL = x.TenNVL,
                IdNhomNVL = x.IDNhomNVL,
                Dvt = x.DVT,
                MaLo = x.MaLo,
                ThuTu = x.ThuTu,
                PhongBanXuat = x.PhongBanXuat,
                XuongXuat = x.XuongXuat,
                KhoiLuongXuat = x.KhoiLuongXuat,
                KhoiLuongQuyKhoXuat = x.KhoiLuongQuyKhoXuat,
                PhongBanNhap = x.PhongBanNhap,
                XuongNhap = x.XuongNhap,
                KhoiLuongNhap = x.KhoiLuongNhap,
                KhoiLuongQuyKhoNhap = x.KhoiLuongQuyKhoNhap,
                DoAm = x.DoAm,
                ViTri = x.ViTri,
                PhanLoai = x.PhanLoai,
                GhiChu = x.GhiChu,
                NguoiTao = x.NguoiTao,
                ThoiGianTao = x.ThoiGianTao,
                NguoiSua = x.NguoiSua,
                ThoiGianSua = x.ThoiGianSua,
            }).ToList();
        }

        // Ghi đè toàn bộ chi tiết của 1 phiếu (xóa cũ, ghi lại theo danh sách mới) —
        // dùng mỗi lần người dùng bấm Lưu trên trang Tạo/Sửa phiếu điều chỉnh.
        public async Task ReplaceChiTietAsync(Guid idPhieu, List<SavePhieuDieuChinhChiTietDto> items, string? nguoiSua)
        {
            var now = DateTime.Now;
            var entities = items.Select(i => new LG_PhieuDieuChinh_ChiTiet
            {
                IDPhieu = idPhieu,
                IDNVL = i.IdNVL,
                TenNVL = i.TenNVL,
                IDNhomNVL = i.IdNhomNVL,
                DVT = i.Dvt,
                MaLo = i.MaLo,
                ThuTu = i.ThuTu,
                PhongBanXuat = i.PhongBanXuat,
                XuongXuat = i.XuongXuat,
                KhoiLuongXuat = i.KhoiLuongXuat,
                KhoiLuongQuyKhoXuat = i.KhoiLuongQuyKhoXuat,
                PhongBanNhap = i.PhongBanNhap,
                XuongNhap = i.XuongNhap,
                KhoiLuongNhap = i.KhoiLuongNhap,
                KhoiLuongQuyKhoNhap = i.KhoiLuongQuyKhoNhap,
                DoAm = i.DoAm,
                ViTri = i.ViTri,
                PhanLoai = i.PhanLoai,
                GhiChu = i.GhiChu,
                NguoiTao = nguoiSua,
                ThoiGianTao = now,
                NguoiSua = nguoiSua,
                ThoiGianSua = now,
                IsDelete = false,
            }).ToList();

            await _repository.ReplaceChiTietAsync(idPhieu, entities);
        }
    }
}

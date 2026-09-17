using dataproduct.api.DTOs.NMLG_Dto;
using dataproduct.api.Models;
using dataproduct.api.Models.MasterData;
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
                TenXuongGiao = r.TenXuongGiao,
                IdPhongBanGiao = r.ID_PhongBanGiao,
                TenPhongBanGiao = r.TenPhongBanGiao,
                TenNganPhongBanGiao = r.TenNganPhongBanGiao,
                IdXuongBN = r.ID_Xuong_BN,
                TenXuongNhan = r.TenXuongNhan,
                IdPhongBanNhan = r.ID_PhongBanNhan,
                TenPhongBanNhan = r.TenPhongBanNhan,
                TenNganPhongBanNhan = r.TenNganPhongBanNhan,
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
                IdNVLChiTiet = x.IDNVLChiTiet,
                IdNhomNVL = x.IDNhomNVL,
                Dvt = x.DVT,
                MaLo = x.MaLo,
                ThuTu = x.ThuTu,
                LoaiDieuChinh = x.LoaiDieuChinh,
                LoaiSoDieuChinh = x.LoaiSoDieuChinh,
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
                IDNVLChiTiet = i.IdNVLChiTiet,
                IDNhomNVL = i.IdNhomNVL,
                DVT = i.Dvt,
                MaLo = i.MaLo,
                ThuTu = i.ThuTu,
                LoaiDieuChinh = i.LoaiDieuChinh,
                LoaiSoDieuChinh = i.LoaiSoDieuChinh,
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

        // ─── Danh mục NVL (LG_PhieuDieuChinh_NVL) ──────────────────────────────

        public async Task<List<LGPhieuDieuChinhNvlDto>> GetNvlListAsync(bool onlyActive)
        {
            var rows = await _repository.GetNvlListAsync(onlyActive);
            return rows.Select(x => new LGPhieuDieuChinhNvlDto
            {
                Id = x.ID,
                TenNVL = x.TenNVL,
                IsActive = x.IsActive,
            }).ToList();
        }

        public async Task<LGPhieuDieuChinhNvlDto> AddNvlAsync(CreateLGPhieuDieuChinhNvlDto dto)
        {
            var entity = await _repository.AddNvlAsync(new LG_PhieuDieuChinh_NVL
            {
                TenNVL = dto.TenNVL,
                IsActive = true,
            });
            return new LGPhieuDieuChinhNvlDto { Id = entity.ID, TenNVL = entity.TenNVL, IsActive = entity.IsActive };
        }

        public async Task<LGPhieuDieuChinhNvlDto?> UpdateNvlAsync(int id, UpdateLGPhieuDieuChinhNvlDto dto)
        {
            var entity = await _repository.UpdateNvlAsync(id, new LG_PhieuDieuChinh_NVL
            {
                TenNVL = dto.TenNVL,
                IsActive = dto.IsActive,
            });
            if (entity == null) return null;
            return new LGPhieuDieuChinhNvlDto { Id = entity.ID, TenNVL = entity.TenNVL, IsActive = entity.IsActive };
        }

        public Task<bool> DeleteNvlAsync(int id) => _repository.DeleteNvlAsync(id);
    }
}

using dataproduct.api.DTOs.NMLG_Dto;
using dataproduct.api.Models;
using dataproduct.api.Models.MasterData;
using dataproduct.api.Repositories;

namespace dataproduct.api.Services
{
    public class LG_PhieuDieuChinhService
    {
        private readonly ILG_PhieuDieuChinhRepository _repository;

        public LG_PhieuDieuChinhService(ILG_PhieuDieuChinhRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<LG_PhieuDieuChinhBBGNDto>> GetBBGNAsync(DateTime ngay, int? ca, string? kip)
        {
            var rows = await _repository.GetBBGNAsync(ngay, ca, kip);

            return rows.Select(r => new LG_PhieuDieuChinhBBGNDto
            {
                LoaiDieuChinh = 1,
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

        // Nguồn "Nội bộ - Xuất SX" — dữ liệu Nạp liệu lò cao (LG_NL_ChiTiet) cho phiếu MỚI (chưa
        // lưu, chưa có IDPhieu để insert chi tiết). FE tự gán LoaiDieuChinh = 2 khi nhận danh sách
        // này (giống cách xử lý BBGN => LoaiDieuChinh = 1).
        public async Task<List<LG_PhieuDieuChinhBBGNDto>> GetNapLieuLoCaoAsync(DateTime ngay, int? ca, string? kip)
        {
            var rows = await _repository.GetNapLieuLoCaoAsync(ngay, ca, kip);

            return rows.Select(r => new LG_PhieuDieuChinhBBGNDto
            {
                LoaiDieuChinh = 2,
                IdCtBBGN = r.ID_CT_BBGN,
                Kip = r.Kip,
                Ca = r.Ca?.ToString(),
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

        public async Task<List<LG_PhieuDieuChinhBBGNDto>> GetNguonAsync(DateTime ngay, int? ca, string? kip)
        {
            var bbgn = await GetBBGNAsync(ngay, ca, kip);
            var napLieuLoCao = await GetNapLieuLoCaoAsync(ngay, ca, kip);
            return bbgn.Concat(napLieuLoCao).ToList();
        }

        // ─── Chi tiết Phiếu điều chỉnh ──────────────────────────────────────

        private static LG_PhieuDieuChinhChiTietDto MapChiTietToDto(LG_PhieuDieuChinh_ChiTiet x) => new()
        {
            Id = x.ID,
            IdPhieu = x.IDPhieu,
            IdNVL = x.IDNVL,
            IdNvlBBGN = x.IDNVL_BBGN,
            IdNvlNapLieu = x.IDNVL_NapLieu,
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
            NguoiDieuChinhGiao = x.NguoiDieuChinhGiao,
            ThoiGianDieuChinhGiao = x.ThoiGianDieuChinhGiao,
            NguoiDieuChinhNhan = x.NguoiDieuChinhNhan,
            ThoiGianDieuChinhNhan = x.ThoiGianDieuChinhNhan,
            TrangThai = x.TrangThai,
            IdCtBBGN = x.ID_CT_BBGN,
        };

        public async Task<List<LG_PhieuDieuChinhChiTietDto>> GetChiTietByPhieuAsync(Guid idPhieu)
        {
            var rows = await _repository.GetChiTietByPhieuAsync(idPhieu);
            return rows.Select(MapChiTietToDto).ToList();
        }

        public async Task<LG_PhieuDieuChinhChiTietDto?> XacNhanGiaoAsync(long id, XacNhanChiTietDto dto)
        {
            var updated = await _repository.UpdateXacNhanGiaoAsync(
                id,
                dto.Confirmed ? dto.IdNguoiThucHien : null,
                dto.Confirmed ? DateTime.Now : null,
                dto.Confirmed ? 1 : 0);

            return updated == null ? null : MapChiTietToDto(updated);
        }

     
        public async Task<List<LG_PhieuDieuChinhChiTietDto>> SyncChiTietFromBBGNAsync(Guid idPhieu, string? nguoiThucHien)
        {
            var phieu = await _repository.GetPhieuByIdAsync(idPhieu)
                ?? throw new InvalidOperationException($"Không tìm thấy phiếu {idPhieu}.");

            if (phieu.NgaySX is null || phieu.Ca is null || string.IsNullOrEmpty(phieu.Kip))
                throw new InvalidOperationException(
                    "Phiếu thiếu thông tin Ngày/Ca/Kíp — vui lòng chọn đủ rồi Lưu phiếu trước khi tải dữ liệu nguồn.");

            var ngay = phieu.NgaySX.Value.ToDateTime(TimeOnly.MinValue);

            // BBGN => LoaiDieuChinh = 1
            var bbgnRows = await _repository.GetBBGNAsync(
                ngay,
                phieu.Ca,
                phieu.Kip);

            var updated = await _repository.SyncChiTietFromBBGNAsync(idPhieu, bbgnRows, nguoiThucHien);

            return updated.Select(MapChiTietToDto).ToList();
        }

        // SyncChiTietFromBBGNAsync ở trên nhưng nguồn là Nạp liệu lò cao, LoaiDieuChinh = 2 ("Nội


        public async Task<List<LG_PhieuDieuChinhChiTietDto>> SyncChiTietFromNapLieuLoCaoAsync(Guid idPhieu, string? nguoiThucHien)
        {
            var phieu = await _repository.GetPhieuByIdAsync(idPhieu)
                ?? throw new InvalidOperationException($"Không tìm thấy phiếu {idPhieu}.");

            if (phieu.NgaySX is null || phieu.Ca is null || string.IsNullOrEmpty(phieu.Kip))
                throw new InvalidOperationException(
                    "Phiếu thiếu thông tin Ngày/Ca/Kíp — vui lòng chọn đủ rồi Lưu phiếu trước khi tải dữ liệu nguồn.");

            var ngay = phieu.NgaySX.Value.ToDateTime(TimeOnly.MinValue);

            var rows = await _repository.GetNapLieuLoCaoAsync(ngay, phieu.Ca, phieu.Kip);
            var updated = await _repository.SyncChiTietFromNapLieuLoCaoAsync(idPhieu, rows, nguoiThucHien);

            return updated.Select(MapChiTietToDto).ToList();
        }

        // Gộp cả 2 nguồn cho phiếu ĐÃ LƯU — 1 lần bấm "Tải dữ liệu nguồn" đồng bộ cả BBGN
        // (LoaiDieuChinh = 1) lẫn Nạp liệu lò cao (LoaiDieuChinh = 2) vào LG_PhieuDieuChinh_ChiTiet.
        // Mỗi sync chỉ chạm các dòng của đúng nguồn đó (repository khớp theo cặp LoaiDieuChinh +
        // ID_CT_BBGN), không đụng dòng của nguồn còn lại hay dòng nhập tay.
        public async Task<List<LG_PhieuDieuChinhChiTietDto>> SyncChiTietFromNguonAsync(Guid idPhieu, string? nguoiThucHien)
        {
            var phieu = await _repository.GetPhieuByIdAsync(idPhieu)
                ?? throw new InvalidOperationException($"Không tìm thấy phiếu {idPhieu}.");

            if (phieu.NgaySX is null || phieu.Ca is null || string.IsNullOrEmpty(phieu.Kip))
                throw new InvalidOperationException(
                    "Phiếu thiếu thông tin Ngày/Ca/Kíp — vui lòng chọn đủ rồi Lưu phiếu trước khi tải dữ liệu nguồn.");

            var ngay = phieu.NgaySX.Value.ToDateTime(TimeOnly.MinValue);

            var bbgnRows = await _repository.GetBBGNAsync(ngay, phieu.Ca, phieu.Kip);
            await _repository.SyncChiTietFromBBGNAsync(idPhieu, bbgnRows, nguoiThucHien);

            var napLieuLoCaoRows = await _repository.GetNapLieuLoCaoAsync(ngay, phieu.Ca, phieu.Kip);
            var updated = await _repository.SyncChiTietFromNapLieuLoCaoAsync(idPhieu, napLieuLoCaoRows, nguoiThucHien);

            return updated.Select(MapChiTietToDto).ToList();
        }

        // Ghi đè toàn bộ chi tiết của 1 phiếu (xóa cũ, ghi lại theo danh sách mới) —
        // dùng mỗi lần người dùng bấm Lưu trên trang Tạo/Sửa phiếu điều chỉnh.
        public async Task ReplaceChiTietAsync(Guid idPhieu, List<SaveLG_PhieuDieuChinhChiTietDto> items, string? nguoiSua)
        {
            var now = DateTime.Now;
            var entities = items.Select(i => new LG_PhieuDieuChinh_ChiTiet
            {
                IDPhieu = idPhieu,
                IDNVL = i.IdNVL,
                // Tách rõ theo đúng không gian ID nguồn dựa trên LoaiDieuChinh — xem comment ở
                // Models/LG_PhieuDieuChinh_ChiTiet.cs. FE không cần gửi riêng 2 field này, chỉ
                // cần gửi đúng IdNVL + LoaiDieuChinh như hiện tại.
                IDNVL_BBGN = i.LoaiDieuChinh == 1 ? i.IdNVL : null,
                IDNVL_NapLieu = i.LoaiDieuChinh == 2 ? i.IdNVL : null,
                TenNVL = i.TenNVL,
                IDNVLChiTiet = i.IdNVLChiTiet,
                IDNhomNVL = i.IdNhomNVL,
                DVT = "Tấn",
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
                NguoiDieuChinhGiao = i.NguoiDieuChinhGiao,
                ThoiGianDieuChinhGiao = i.ThoiGianDieuChinhGiao,
                NguoiDieuChinhNhan = i.NguoiDieuChinhNhan,
                ThoiGianDieuChinhNhan = i.ThoiGianDieuChinhNhan,
                TrangThai = i.TrangThai,
                ID_CT_BBGN = i.IdCtBBGN,
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

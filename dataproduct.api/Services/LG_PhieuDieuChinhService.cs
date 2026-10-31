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
        // Loại điều chỉnh của phần Nhập / Xuất: 1 = Nhập (bên nhận), 3 = Xuất (bên giao) — cùng giá trị LoaiDieuChinh
        // (2 = Nội bộ). Trả true nếu là bên nhận.
        private const int LoaiNhap = 1;
        private const int LoaiXuat = 3;
        private static bool LaBenNhan(int loai)
            => loai switch
            {
                LoaiNhap => true,
                LoaiXuat => false,
                _ => throw new InvalidOperationException("Loại phải là 1 (Nhập) hoặc 3 (Xuất).")
            };

        // Gán số điều chỉnh của 2 bên vào dòng trả về: dòng Nhập (LoaiDieuChinh = 1) giữ KL nhập, dòng Xuất (= 3) giữ KL
        // xuất — mỗi bên đọc từ dòng của chính bên đó (có thể ở 2 phiếu khác nhau). Loại số là của riêng bên đang xem.
        private static void GanSoDieuChinh(
            LG_PhieuDieuChinhBBGNLineDto dto, LG_PhieuDieuChinh_ChiTiet? nhap, LG_PhieuDieuChinh_ChiTiet? xuat, bool laBenNhan)
        {
            dto.LoaiSoDieuChinh = (laBenNhan ? nhap : xuat)?.LoaiSoDieuChinh;
            dto.KhoiLuongXuat = xuat?.KhoiLuongXuat;
            dto.KLQuyKhoXuat = xuat?.KhoiLuongQuyKhoXuat;
            dto.GhiChuXuat = xuat?.GhiChu;
            dto.NguoiXuat = xuat?.NguoiDieuChinhGiao;
            dto.ThoiGianXuat = xuat?.ThoiGianDieuChinhGiao;
            dto.KhoiLuongNhap = nhap?.KhoiLuongNhap;
            dto.KLQuyKhoNhap = nhap?.KhoiLuongQuyKhoNhap;
            dto.GhiChuNhap = nhap?.GhiChu;
            dto.NguoiNhap = nhap?.NguoiDieuChinhNhan;
            dto.ThoiGianNhap = nhap?.ThoiGianDieuChinhNhan;
        }

        // idPhieu có giá trị (phiếu đã lưu): đọc các dòng (LoaiDieuChinh 1 hoặc 3) của phiếu đã ghi lúc "Tải dữ liệu nguồn"
        // (xem DongBoBBGNAsync), không gọi lại store. Không có idPhieu (phiếu mới chưa lưu): xem trước thẳng từ store, chưa
        // ghi gì.
        public async Task<List<LG_PhieuDieuChinhBBGNLineDto>> GetBBGNAsync(
            int loai, DateTime ngay, int? idPhongBan, Guid? idPhieu = null)
        {
            var laBenNhan = LaBenNhan(loai);

            if (idPhieu.HasValue)
            {
                var cuaPhieu = await _repository.GetDieuChinhTheoPhieuAsync(idPhieu.Value, laBenNhan);
                var daLuuDb = await _repository.GetDieuChinhBBGNAsync(cuaPhieu.Select(x => x.ID_CT_BBGN!.Value).ToList());
                return cuaPhieu.Select(rec =>
                {
                    var ct = rec.ID_CT_BBGN!.Value;
                    // Bên đang xem lấy từ dòng của chính phiếu này; số của bên kia lấy từ dòng loại ngược lại
                    var nhap = laBenNhan ? rec : daLuuDb.Nhap.GetValueOrDefault(ct);
                    var xuat = laBenNhan ? daLuuDb.Xuat.GetValueOrDefault(ct) : rec;
                    var dongDb = new LG_PhieuDieuChinhBBGNLineDto
                    {
                        IdCtBBGN = ct,
                        IdVatTu = rec.IDNVL_BBGN,
                        TenVatTu = rec.TenNVL,
                        MaLo = rec.MaLo,
                        ThoiGianXuLyBG = rec.ThoiGianXuLyBG,
                        IdXuongGiao = rec.ID_Xuong_BG,
                        TenXuongGiao = rec.XuongXuat,
                        TenPhongBanGiao = rec.PhongBanXuat,
                        IdXuongNhan = rec.ID_Xuong_BN,
                        TenXuongNhan = rec.XuongNhap,
                        TenPhongBanNhan = rec.PhongBanNhap,
                        IdTab = laBenNhan ? rec.ID_Xuong_BN : rec.ID_Xuong_BG,
                        IdCa = rec.IDCa,
                        DoAm = rec.DoAm.HasValue ? (double)rec.DoAm.Value : null,
                    };
                    GanSoDieuChinh(dongDb, nhap, xuat, laBenNhan);
                    return dongDb;
                }).ToList();
            }

        
            if (!idPhongBan.HasValue) return new List<LG_PhieuDieuChinhBBGNLineDto>();
            var xuongCuaPb = (await _repository.GetXuongTheoPhongBanAsync(idPhongBan.Value))
                .Select(x => x.ID_Xuong).ToList();
            if (xuongCuaPb.Count == 0) return new List<LG_PhieuDieuChinhBBGNLineDto>();
            var xuongTab = xuongCuaPb.ToHashSet();

            var rows = laBenNhan
                ? await _repository.GetBBGNNhanTheoDanhSachXuongAsync(ngay, xuongCuaPb)
                : await _repository.GetBBGNXuatAsync(ngay, xuongCuaPb);

            var daLuu = await _repository.GetDieuChinhBBGNAsync(rows.Select(r => r.ID_CT_BBGN).ToList());

            return rows.Select(r =>
            {
                // Xem trước (phiếu mới chưa lưu): chỉ hiện số của BÊN KIA nếu đã có ở phiếu khác; bên mình chưa có gì
                var nhap = laBenNhan ? null : daLuu.Nhap.GetValueOrDefault(r.ID_CT_BBGN);
                var xuat = laBenNhan ? daLuu.Xuat.GetValueOrDefault(r.ID_CT_BBGN) : null;
                var dong = new LG_PhieuDieuChinhBBGNLineDto
                {
                    IdCtBBGN = r.ID_CT_BBGN,
                    IdVatTu = r.ID_VatTu,
                    TenVatTu = r.TenVatTu,
                    MaLo = r.MaLO,
                    ThoiGianXuLyBG = r.ThoiGianXuLyBG,
                    IdXuongGiao = r.ID_Xuong_BG,
                    TenXuongGiao = r.TenXuongGiao,
                    TenPhongBanGiao = r.TenPhongBanGiao,
                    IdXuongNhan = r.ID_Xuong_BN,
                    TenXuongNhan = r.TenXuongNhan,
                    TenPhongBanNhan = r.TenPhongBanNhan,
                    // Id tab = xưởng của bên đang xem (Nhập: xưởng nhận, Xuất: xưởng giao)
                    IdTab = (laBenNhan ? r.ID_Xuong_BN : r.ID_Xuong_BG) is int xuongCuaBen
                            && xuongTab.Contains(xuongCuaBen) ? xuongCuaBen : null,
                    IdCa = LG_PhieuDieuChinhRepository.ParseCa(r.Ca),
                    DoAm = r.DoAm_W, // độ ẩm lấy từ BBGN, không nhập tay
                };
                GanSoDieuChinh(dong, nhap, xuat, laBenNhan);
                return dong;
            }).ToList();
        }

        public async Task<LG_PhieuDieuChinhDongBoBBGNResult> DongBoBBGNAsync(
            Guid idPhieu, int idPhongBan, DateTime? ngay, int? nguoiThucHien)
        {
            var phieu = await _repository.GetPhieuByIdAsync(idPhieu)
                ?? throw new InvalidOperationException($"Không tìm thấy phiếu {idPhieu}.");

            var ngayLay = ngay?.Date
                ?? phieu.NgaySX?.ToDateTime(TimeOnly.MinValue)
                ?? throw new InvalidOperationException("Phiếu thiếu thông tin Ngày — vui lòng chọn Ngày trước khi tải dữ liệu nguồn.");

            var xuongCuaPb = (await _repository.GetXuongTheoPhongBanAsync(idPhongBan))
                .Select(x => x.ID_Xuong).ToList();
            if (xuongCuaPb.Count == 0)
                throw new InvalidOperationException("Phòng ban chưa có xưởng nào trong LG_PDC_Scope_PhongBan.");

            var rowsNhap = await _repository.GetBBGNNhanTheoDanhSachXuongAsync(ngayLay, xuongCuaPb);
            var rowsXuat = await _repository.GetBBGNXuatAsync(ngayLay, xuongCuaPb);

            var (nhap, xuat, boQua) = await _repository.DongBoBBGNAsync(idPhieu, rowsNhap, rowsXuat, nguoiThucHien, idPhongBan);
            return new LG_PhieuDieuChinhDongBoBBGNResult { SoDongNhap = nhap, SoDongXuat = xuat, SoDongBoQua = boQua };
        }

        // Các xưởng của 1 phòng ban — mỗi xưởng 1 tab trong phiếu theo phòng ban (vd NMTKVV)
        public async Task<List<LG_PhieuDieuChinhXuongDto>> GetDanhMucXuongAsync(int idPhongBan)
        {
            var list = await _repository.GetXuongTheoPhongBanAsync(idPhongBan);
            return list.Select(x => new LG_PhieuDieuChinhXuongDto
            {
                Id = x.ID_Xuong,
                Ten = string.IsNullOrWhiteSpace(x.TenXuong) ? $"Xưởng {x.ID_Xuong}" : x.TenXuong
            }).ToList();
        }

        // Lưu số điều chỉnh của 1 bên vào phiếu. 2 bên đã cùng nhập mà khác nhau → InvalidOperationException (xem repository).
        public Task SaveBBGNAsync(int loai, Guid idPhieu, SaveLG_PhieuDieuChinhBBGNRequest request)
            => _repository.SaveDieuChinhBBGNAsync(idPhieu, LaBenNhan(loai), request.IdNguoiThucHien, request.Items, request.IdPhongBan);

        // Reset số điều chỉnh của bên kia — bắt buộc có ghi chú (lý do)
        public Task ResetBBGNAsync(int loai, Guid idPhieu, ResetLG_PhieuDieuChinhBBGNRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.GhiChu))
                throw new InvalidOperationException("Phải nhập ghi chú (lý do) khi reset số của bên kia.");
            return _repository.ResetBenKiaBBGNAsync(
                idPhieu, LaBenNhan(loai), request.IdCtBBGN, request.GhiChu, request.IdNguoiThucHien);
        }

        // ─── Nội bộ - Xuất vào sản xuất, chia theo xưởng nhận (cùng danh sách xưởng với Nhập / Xuất) ─────

        // Chia dòng nguồn theo xưởng của phòng ban (LG_PDC_Scope_PhongBan, cột ID_Xuong — cùng nguồn với tab xưởng của
        // Nhập / Xuất): mỗi xưởng lấy các dòng có xưởng NHẬN là xưởng đó. Dòng có xưởng nhận không thuộc phòng ban thì
        // không được lấy. idPhongBan null = không giới hạn (lấy mọi xưởng nhận có trong dòng nguồn).
        private async Task<List<(int idXuong, List<T> rows)>> ChiaTheoXuongAsync<T>(
            IReadOnlyCollection<T> rows, Func<T, int?> idXuongNhan, int? idPhongBan)
        {
            var dsXuong = idPhongBan.HasValue
                ? (await _repository.GetXuongTheoPhongBanAsync(idPhongBan.Value)).Select(x => x.ID_Xuong).ToList()
                : rows.Select(idXuongNhan).Where(x => x.HasValue).Select(x => x!.Value).Distinct().OrderBy(x => x).ToList();

            return dsXuong
                .Select(idXuong => (idXuong, rows.Where(r => idXuongNhan(r) == idXuong).ToList()))
                .ToList();
        }

        // IdLoCao của dòng Nội bộ = ID xưởng nhận (cột IDLoCao đóng vai trò khóa tab xưởng)
        private static LG_PhieuDieuChinhBBGNDto ToNguonDto(NapLieuLoCaoDieuChinhResult r, int idXuong) => new()
        {
            LoaiDieuChinh = 2,
            IdCtBBGN = r.ID_CT_BBGN,
            IdLoCao = idXuong,
            IdCa = r.Ca,
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
        };

        // Nguồn "Nội bộ - Xuất SX" — dữ liệu Nạp liệu lò cao (LG_NL_ChiTiet) cho phiếu MỚI (chưa lưu, chưa có
        // IDPhieu để insert chi tiết).
        public async Task<List<LG_PhieuDieuChinhBBGNDto>> GetNapLieuLoCaoAsync(DateTime ngay, int? idPhongBan)
        {
            var rows = await _repository.GetNapLieuLoCaoAsync(ngay);
            var theoXuong = await ChiaTheoXuongAsync(rows, r => r.ID_Xuong_BN, idPhongBan);
            return theoXuong.SelectMany(g => g.rows.Select(r => ToNguonDto(r, g.idXuong))).ToList();
        }

        // ─── Chi tiết Phiếu điều chỉnh (phần Nội bộ) ─────────────────────────

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
            NguoiDieuChinhNhan = x.NguoiDieuChinhNhan,
            ThoiGianDieuChinhNhan = x.ThoiGianDieuChinhNhan,
            IdCtBBGN = x.ID_CT_BBGN,
            IdLoCao = x.IDLoCao,
            IdCa = x.IDCa,
            IdPhongBan = x.ID_PhongBan,
        };

        // Toàn bộ chi tiết Nội bộ của phiếu. FE chia dòng vào tab xưởng theo IDLoCao (= ID xưởng nhận); dòng chưa rõ
        // xưởng được FE hiện ở xưởng đầu tiên.
        public async Task<List<LG_PhieuDieuChinhChiTietDto>> GetChiTietByPhieuAsync(Guid idPhieu)
        {
            var rows = await _repository.GetChiTietByPhieuAsync(idPhieu);
            return rows.Select(MapChiTietToDto).ToList();
        }

        // Id phòng ban NM.LG: phòng ban mặc định (và khi FE không truyền idPhongBan) lấy nguồn Nội bộ từ Nạp liệu lò cao
        private const int ID_PHONGBAN_NM_LG = 63;

        // Nguồn dữ liệu phần Nội bộ - Xuất vào sản xuất theo phòng ban. Phòng ban có Nội bộ thì viết 1 hàm riêng
        // (idPhieu, ngay, nguoiThucHien, idPhongBan): ghi dòng loại 2 vào LG_PhieuDieuChinh_ChiTiet của phiếu với
        // IDLoCao = ID xưởng nhận (khóa tab xưởng), rồi thêm 1 dòng vào switch dưới đây. Chưa đăng ký = chưa có nguồn.
        private Func<Guid, DateTime, string?, int?, Task>? LayNguonNoiBo(int idPhongBan) => idPhongBan switch
        {
            ID_PHONGBAN_NM_LG => DongBoNapLieuLoCaoAsync,
            // 64 => DongBoNoiBoNLAsync,
            _ => null,
        };

        // NM.LG: nguồn Nội bộ = Nạp liệu lò cao, chia theo xưởng nhận
        private async Task DongBoNapLieuLoCaoAsync(Guid idPhieu, DateTime ngay, string? nguoiThucHien, int? idPhongBan)
        {
            var rows = await _repository.GetNapLieuLoCaoAsync(ngay);
            foreach (var (idXuong, rowsCuaXuong) in await ChiaTheoXuongAsync(rows, r => r.ID_Xuong_BN, idPhongBan))
                await _repository.SyncChiTietFromNapLieuLoCaoAsync(idPhieu, idXuong, rowsCuaXuong, nguoiThucHien, idPhongBan);
        }

        public async Task<List<LG_PhieuDieuChinhChiTietDto>> SyncChiTietFromNapLieuLoCaoAsync(Guid idPhieu, string? nguoiThucHien, int? idPhongBan)
        {
            var phieu = await _repository.GetPhieuByIdAsync(idPhieu)
                ?? throw new InvalidOperationException($"Không tìm thấy phiếu {idPhieu}.");

            // Mỗi phiếu chỉ lấy theo 1 Ngày (không còn chọn Ca, Kíp)
            if (phieu.NgaySX is null)
                throw new InvalidOperationException(
                    "Phiếu thiếu thông tin Ngày — vui lòng chọn Ngày rồi Lưu phiếu trước khi tải dữ liệu nguồn.");

            var ngay = phieu.NgaySX.Value.ToDateTime(TimeOnly.MinValue);

            // Phòng ban chưa có hàm nguồn Nội bộ thì chỉ trả chi tiết hiện có (không báo lỗi)
            var nguon = LayNguonNoiBo(idPhongBan ?? ID_PHONGBAN_NM_LG);
            if (nguon != null) await nguon(idPhieu, ngay, nguoiThucHien, idPhongBan);

            return await GetChiTietByPhieuAsync(idPhieu);
        }

        // Lưu từng dòng chi tiết của phiếu (upsert theo Id) — dùng mỗi lần bấm Lưu trên trang Tạo/Sửa phiếu.
        public async Task ReplaceChiTietAsync(
            Guid idPhieu, List<SaveLG_PhieuDieuChinhChiTietDto> items, List<long>? deletedIds, string? nguoiSua)
        {
            var now = DateTime.Now;
            var entities = items.Select(i => new LG_PhieuDieuChinh_ChiTiet
            {
                // Dòng đã lưu mang ID thật; ThoiGianSua của dòng đã lưu ở đây là "phiên bản FE đã
                // tải" để repository so với DB (khóa lạc quan), không phải thời điểm sửa mới.
                ID = i.Id ?? 0,
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
                ThoiGianSua = (i.Id ?? 0) > 0 ? i.ThoiGianSua : now,
                IsDelete = false,
                NguoiDieuChinhNhan = i.NguoiDieuChinhNhan,
                ThoiGianDieuChinhNhan = i.ThoiGianDieuChinhNhan,
                ID_CT_BBGN = i.IdCtBBGN,
                IDLoCao = i.IdLoCao,
                IDCa = i.IdCa,
                ID_PhongBan = i.IdPhongBan,
            }).ToList();

            await _repository.ReplaceChiTietAsync(idPhieu, entities, deletedIds ?? new List<long>(), nguoiSua);
        }

    }
}

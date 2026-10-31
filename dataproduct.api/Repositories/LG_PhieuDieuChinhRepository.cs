using dataproduct.api.DTOs.NMLG_Dto;
using dataproduct.api.Models;
using dataproduct.api.Models.MasterData;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{

    public class LG_PhieuDieuChinhRepository : ILG_PhieuDieuChinhRepository
    {
        private readonly ProductDataMasterDbContext _context;
        private readonly ProductFormContext _formContext;

        public LG_PhieuDieuChinhRepository(ProductDataMasterDbContext context, ProductFormContext formContext)
        {
            _context = context;
            _formContext = formContext;
        }

        // Lấy theo Ngày; ca = null thì lấy mọi ca (@p_ca NULL), không lọc kíp (@p_kip NULL).
        public async Task<List<BBGNChiTietResult>> GetBBGNNhanTheoDanhSachXuongAsync(
            DateTime ngay, IReadOnlyCollection<int> idXuong, int? ca = null)
        {
            return await _context.BBGNChiTietResults
                .FromSqlRaw(
                    "EXEC dbo.SP_PDC_Getnvl_BBGN_Nhan @p_ngay, @p_ca, @p_kip, @p_ds_xuong",
                    new SqlParameter("@p_ngay", ngay.Date),
                    new SqlParameter("@p_ca", (object?)ca ?? DBNull.Value),
                    new SqlParameter("@p_kip", DBNull.Value),
                    new SqlParameter("@p_ds_xuong", string.Join(",", idXuong)))
                .AsNoTracking()
                .ToListAsync();
        }

        // Các xưởng của 1 phòng ban: lấy ID_Xuong từ bảng map LG_PDC_Scope_PhongBan (PRODUCT_FORM), rồi lấy tên/trạng
        // thái xưởng ở PRODUCTDATA.Tbl_Xuong. 2 bảng nằm ở 2 DB khác nhau nên lấy riêng rồi giao nhau.
        public async Task<List<Tbl_Xuong>> GetXuongTheoPhongBanAsync(int idPhongBan)
        {
            var idXuongs = await _formContext.LG_PDC_Scope_PhongBan
                .AsNoTracking()
                .Where(x => x.ID_PhongBan == idPhongBan && !x.IsDelete && x.ID_Xuong != null)
                .Select(x => x.ID_Xuong!.Value)
                .Distinct()
                .ToListAsync();
            if (idXuongs.Count == 0) return new List<Tbl_Xuong>();

            return await _context.Tbl_Xuong
                .AsNoTracking()
                .Where(x => idXuongs.Contains(x.ID_Xuong) && x.ID_TrangThai == 1)
                .OrderBy(x => x.ID_Xuong)
                .ToListAsync();
        }

        // Phần "Xuất": các dòng BBGN có xưởng GIAO thuộc danh sách xưởng.
        public async Task<List<BBGNChiTietResult>> GetBBGNXuatAsync(
            DateTime ngay, IReadOnlyCollection<int> idXuong, int? ca = null)
        {
            return await _context.BBGNChiTietResults
                .FromSqlRaw(
                    "EXEC dbo.SP_PDC_Getnvl_BBGN_Xuat @p_ngay, @p_ca, @p_kip, @p_ds_xuong",
                    new SqlParameter("@p_ngay", ngay.Date),
                    new SqlParameter("@p_ca", (object?)ca ?? DBNull.Value),
                    new SqlParameter("@p_kip", DBNull.Value),
                    new SqlParameter("@p_ds_xuong", string.Join(",", idXuong)))
                .AsNoTracking()
                .ToListAsync();
        }

        // Số điều chỉnh Nhập / Xuất của dòng BBGN lưu trong LG_PhieuDieuChinh_ChiTiet — cùng bảng với phần Nội bộ, tách bằng
        private const int LoaiNhap = 1;
        private const int LoaiXuat = 3;

        // Id các phiếu còn hiệu lực (chưa bị xóa) trong tập đã cho
        private async Task<HashSet<Guid>> PhieuConSongAsync(IEnumerable<Guid> idPhieus)
        {
            var ds = idPhieus.Distinct().ToList();
            if (ds.Count == 0) return new HashSet<Guid>();
            return (await _formContext.BmPhieus
                    .AsNoTracking()
                    .Where(p => ds.Contains(p.Idphieu) && (p.IsDelete == null || p.IsDelete == 0))
                    .Select(p => p.Idphieu)
                    .ToListAsync())
                .ToHashSet();
        }

        // Dòng Nhập/Xuất đã lưu của các dòng BBGN (ở bất kỳ phiếu còn hiệu lực nào) — để hiển thị và đối chiếu 2 bên
        public async Task<DieuChinhBBGNDaLuu> GetDieuChinhBBGNAsync(IReadOnlyCollection<int> idCtBbgn)
        {
            var kq = new DieuChinhBBGNDaLuu();
            var ids = idCtBbgn.ToList();
            if (ids.Count == 0) return kq;

            var list = await _formContext.LG_PhieuDieuChinh_ChiTiet
                .AsNoTracking()
                .Where(x => !x.IsDelete && (x.LoaiDieuChinh == LoaiNhap || x.LoaiDieuChinh == LoaiXuat)
                            && x.ID_CT_BBGN != null && ids.Contains(x.ID_CT_BBGN.Value))
                .ToListAsync();
            var conSong = await PhieuConSongAsync(list.Select(x => x.IDPhieu));
            list = list.Where(x => conSong.Contains(x.IDPhieu)).ToList();

            kq.Nhap = list.Where(x => x.LoaiDieuChinh == LoaiNhap)
                .GroupBy(x => x.ID_CT_BBGN!.Value)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.ID).First());
            kq.Xuat = list.Where(x => x.LoaiDieuChinh == LoaiXuat)
                .GroupBy(x => x.ID_CT_BBGN!.Value)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.ID).First());
            return kq;
        }

        public async Task SaveDieuChinhBBGNAsync(
            Guid idPhieu, bool laBenNhan, int? nguoiThucHien, List<SaveLG_PhieuDieuChinhBBGNDto> items, int? idPhongBan)
        {
            var loai = laBenNhan ? LoaiNhap : LoaiXuat;
            var loaiKia = laBenNhan ? LoaiXuat : LoaiNhap;
            var ids = items.Select(i => i.IdCtBBGN).Distinct().ToList();

            // Dòng của chính phiếu này (đúng bên)
            var own = (await _formContext.LG_PhieuDieuChinh_ChiTiet
                    .Where(x => x.IDPhieu == idPhieu && x.LoaiDieuChinh == loai && !x.IsDelete
                                && x.ID_CT_BBGN != null && ids.Contains(x.ID_CT_BBGN.Value))
                    .ToListAsync())
                .GroupBy(x => x.ID_CT_BBGN!.Value)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.ID).First());

            // Dòng cùng bên ở phiếu KHÁC (chặn giành dòng) và dòng bên kia ở mọi phiếu (đối chiếu), chỉ tính phiếu còn hiệu lực
            var khac = await _formContext.LG_PhieuDieuChinh_ChiTiet
                .AsNoTracking()
                .Where(x => !x.IsDelete && x.ID_CT_BBGN != null && ids.Contains(x.ID_CT_BBGN.Value)
                            && ((x.LoaiDieuChinh == loai && x.IDPhieu != idPhieu) || x.LoaiDieuChinh == loaiKia))
                .ToListAsync();
            var conSong = await PhieuConSongAsync(khac.Select(x => x.IDPhieu));
            khac = khac.Where(x => conSong.Contains(x.IDPhieu)).ToList();

            var phieuKhacGiu = khac.Where(x => x.LoaiDieuChinh == loai)
                .GroupBy(x => x.ID_CT_BBGN!.Value)
                .ToDictionary(g => g.Key, g => g.First().IDPhieu);
            var soPhieu = phieuKhacGiu.Count == 0
                ? new Dictionary<Guid, string?>()
                : await _formContext.BmPhieus
                    .AsNoTracking()
                    .Where(p => phieuKhacGiu.Values.Contains(p.Idphieu))
                    .ToDictionaryAsync(p => p.Idphieu, p => p.SoPhieu);
            var doiTac = khac.Where(x => x.LoaiDieuChinh == loaiKia)
                .GroupBy(x => x.ID_CT_BBGN!.Value)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.ID).First());

            var nguoi = nguoiThucHien?.ToString();
            var now = DateTime.Now;
            var loi = new List<string>();

            foreach (var item in items)
            {
                if (item.KhoiLuong is < 0)
                {
                    loi.Add($"{MoTaDong(item)}: khối lượng không được âm.");
                    continue;
                }

                // Có khối lượng thì phải chọn loại số; "Sau ẩm" cần độ ẩm (lấy từ BBGN) để tính KL quy khô
                if (item.KhoiLuong.HasValue)
                {
                    if (item.LoaiSoDieuChinh is not (1 or 2))
                    {
                        loi.Add($"{MoTaDong(item)}: chưa chọn Loại số điều chỉnh (Trước ẩm / Sau ẩm).");
                        continue;
                    }
                    if (item.LoaiSoDieuChinh == 2 && !item.DoAm.HasValue)
                    {
                        loi.Add($"{MoTaDong(item)}: dòng BBGN không có độ ẩm nên không chọn được Sau ẩm.");
                        continue;
                    }
                }

                if (!own.TryGetValue(item.IdCtBBGN, out var rec))
                {
                    if (phieuKhacGiu.TryGetValue(item.IdCtBBGN, out var phieuGiu))
                    {
                        loi.Add($"{MoTaDong(item)}: dòng này đã được điều chỉnh ở phiếu " +
                                $"{(soPhieu.TryGetValue(phieuGiu, out var so) ? so : null) ?? phieuGiu.ToString()}.");
                        continue;
                    }

                    // Phiếu chưa có dòng này mà bên này cũng chưa nhập gì → không tạo dòng rỗng
                    if (!item.KhoiLuong.HasValue && string.IsNullOrWhiteSpace(item.GhiChu)) continue;

                    rec = new LG_PhieuDieuChinh_ChiTiet
                    {
                        IDPhieu = idPhieu,
                        LoaiDieuChinh = loai,
                        ID_CT_BBGN = item.IdCtBBGN,
                        IDNVL_BBGN = item.IdVatTu,
                        TenNVL = item.TenVatTu ?? "",
                        MaLo = item.MaLo,
                        PhongBanXuat = item.TenPhongBanGiao,
                        XuongXuat = item.TenXuongGiao,
                        PhongBanNhap = item.TenPhongBanNhan,
                        XuongNhap = item.TenXuongNhan,
                        DVT = "Tấn",
                        NguoiTao = nguoi,
                        ThoiGianTao = now,
                        IsDelete = false,
                    };
                    _formContext.LG_PhieuDieuChinh_ChiTiet.Add(rec);
                    own[item.IdCtBBGN] = rec;
                }

                // Đối chiếu với bên kia (nếu bên kia đã nhập khối lượng)
                if (item.KhoiLuong.HasValue && doiTac.TryGetValue(item.IdCtBBGN, out var kia))
                {
                    var klDoiTac = laBenNhan ? kia.KhoiLuongXuat : kia.KhoiLuongNhap;
                    if (klDoiTac.HasValue && klDoiTac.Value != item.KhoiLuong.Value)
                    {
                        var chenhLech = laBenNhan ? item.KhoiLuong.Value - klDoiTac.Value : klDoiTac.Value - item.KhoiLuong.Value;
                        var nhap = laBenNhan ? item.KhoiLuong.Value : klDoiTac.Value;
                        var xuat = laBenNhan ? klDoiTac.Value : item.KhoiLuong.Value;
                        loi.Add($"{MoTaDong(item)}: KL nhập ({nhap:0.###}) khác KL xuất ({xuat:0.###}), " +
                                $"chênh lệch {chenhLech:0.###} — 2 bên phải bằng nhau mới lưu được.");
                        continue;
                    }
                }

                var quyKho = TinhQuyKho(item.KhoiLuong, item.LoaiSoDieuChinh, item.DoAm);
                if (idPhongBan.HasValue) rec.ID_PhongBan = idPhongBan;
                rec.DoAm = item.DoAm.HasValue ? (decimal)item.DoAm.Value : null;
                rec.LoaiSoDieuChinh = item.LoaiSoDieuChinh;
                rec.GhiChu = item.GhiChu;
                if (laBenNhan)
                {
                    rec.KhoiLuongNhap = item.KhoiLuong;
                    rec.KhoiLuongQuyKhoNhap = quyKho;
                    rec.NguoiDieuChinhNhan = item.KhoiLuong.HasValue ? nguoiThucHien : null;
                    rec.ThoiGianDieuChinhNhan = item.KhoiLuong.HasValue ? now : null;
                }
                else
                {
                    rec.KhoiLuongXuat = item.KhoiLuong;
                    rec.KhoiLuongQuyKhoXuat = quyKho;
                    rec.NguoiDieuChinhGiao = item.KhoiLuong.HasValue ? nguoiThucHien : null;
                    rec.ThoiGianDieuChinhGiao = item.KhoiLuong.HasValue ? now : null;
                }
                rec.NguoiSua = nguoi;
                rec.ThoiGianSua = now;
            }

            // Có dòng vi phạm thì không SaveChanges — các thay đổi đang theo dõi bị bỏ khi request kết thúc.
            if (loi.Count > 0)
                throw new InvalidOperationException(string.Join("\n", loi));

            await _formContext.SaveChangesAsync();
        }

        public async Task ResetBenKiaBBGNAsync(Guid idPhieu, bool laBenNhan, int idCtBBGN, string lyDo, int? nguoiThucHien)
        {
            var loai = laBenNhan ? LoaiNhap : LoaiXuat;
            var loaiKia = laBenNhan ? LoaiXuat : LoaiNhap;

            var coDongCuaMinh = await _formContext.LG_PhieuDieuChinh_ChiTiet.AnyAsync(x =>
                x.IDPhieu == idPhieu && x.LoaiDieuChinh == loai && !x.IsDelete && x.ID_CT_BBGN == idCtBBGN);
            if (!coDongCuaMinh)
                throw new InvalidOperationException("Phiếu này chưa có dòng BBGN này — hãy Tải dữ liệu nguồn trước.");

            var kia = (await _formContext.LG_PhieuDieuChinh_ChiTiet
                    .Where(x => !x.IsDelete && x.LoaiDieuChinh == loaiKia && x.ID_CT_BBGN == idCtBBGN)
                    .ToListAsync())
                .OrderByDescending(x => x.ID)
                .ToList();
            var conSong = await PhieuConSongAsync(kia.Select(x => x.IDPhieu));
            var rec = kia.FirstOrDefault(x => conSong.Contains(x.IDPhieu));
            var klKia = rec == null ? null : (laBenNhan ? rec.KhoiLuongXuat : rec.KhoiLuongNhap);
            if (rec == null || !klKia.HasValue)
                throw new InvalidOperationException($"Bên {(laBenNhan ? "xuất" : "nhận")} chưa điều chỉnh khối lượng nên không có gì để reset.");

            var now = DateTime.Now;
            var nguoi = nguoiThucHien?.ToString();
            if (laBenNhan)
            {
                rec.KhoiLuongXuat = null;
                rec.KhoiLuongQuyKhoXuat = null;
                rec.NguoiDieuChinhGiao = null;
                rec.ThoiGianDieuChinhGiao = null;
            }
            else
            {
                rec.KhoiLuongNhap = null;
                rec.KhoiLuongQuyKhoNhap = null;
                rec.NguoiDieuChinhNhan = null;
                rec.ThoiGianDieuChinhNhan = null;
            }

            var dong = $"[Reset bởi bên {(laBenNhan ? "nhận" : "xuất")} {now:dd/MM/yyyy HH:mm} (KL cũ {klKia:0.###}): {lyDo.Trim()}]";
            rec.GhiChu = string.IsNullOrWhiteSpace(rec.GhiChu) ? dong : $"{rec.GhiChu}\n{dong}";
            rec.NguoiSua = nguoi;
            rec.ThoiGianSua = now;
            await _formContext.SaveChangesAsync();
        }

        // Các dòng Nhập (laBenNhan) hoặc Xuất mà phiếu này đang giữ — bản chụp đã ghi lúc "Tải dữ liệu nguồn"
        public async Task<List<LG_PhieuDieuChinh_ChiTiet>> GetDieuChinhTheoPhieuAsync(Guid idPhieu, bool laBenNhan)
        {
            var loai = laBenNhan ? LoaiNhap : LoaiXuat;
            return await _formContext.LG_PhieuDieuChinh_ChiTiet
                .AsNoTracking()
                .Where(x => x.IDPhieu == idPhieu && !x.IsDelete && x.LoaiDieuChinh == loai && x.ID_CT_BBGN != null)
                .OrderBy(x => x.ID)
                .ToListAsync();
        }

        public async Task<(int nhap, int xuat, int boQua)> DongBoBBGNAsync(
            Guid idPhieu,
            IReadOnlyCollection<BBGNChiTietResult> rowsNhap,
            IReadOnlyCollection<BBGNChiTietResult> rowsXuat,
            int? nguoiThucHien,
            int idPhongBan)
        {
            var ids = rowsNhap.Select(r => r.ID_CT_BBGN).Concat(rowsXuat.Select(r => r.ID_CT_BBGN)).Distinct().ToList();

            var tatCa = ids.Count == 0
                ? new List<LG_PhieuDieuChinh_ChiTiet>()
                : await _formContext.LG_PhieuDieuChinh_ChiTiet
                    .Where(x => !x.IsDelete && (x.LoaiDieuChinh == LoaiNhap || x.LoaiDieuChinh == LoaiXuat)
                                && x.ID_CT_BBGN != null && ids.Contains(x.ID_CT_BBGN.Value))
                    .ToListAsync();

            // Dòng của chính phiếu này, khóa (loại, ID_CT_BBGN)
            var own = tatCa.Where(x => x.IDPhieu == idPhieu)
                .GroupBy(x => (x.LoaiDieuChinh!.Value, x.ID_CT_BBGN!.Value))
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.ID).First());

            // (loại, ID_CT_BBGN) đang do phiếu khác còn hiệu lực giữ
            var conSong = await PhieuConSongAsync(tatCa.Where(x => x.IDPhieu != idPhieu).Select(x => x.IDPhieu));
            var khacGiu = tatCa.Where(x => x.IDPhieu != idPhieu && conSong.Contains(x.IDPhieu))
                .Select(x => (x.LoaiDieuChinh!.Value, x.ID_CT_BBGN!.Value))
                .ToHashSet();

            var nguoi = nguoiThucHien?.ToString();
            var now = DateTime.Now;
            int nhap = 0, xuat = 0, boQua = 0;

            void XuLy(BBGNChiTietResult r, int loai)
            {
                var khoa = (loai, r.ID_CT_BBGN);
                if (!own.TryGetValue(khoa, out var rec))
                {
                    if (khacGiu.Contains(khoa))
                    {
                        boQua++;
                        return;
                    }
                    rec = new LG_PhieuDieuChinh_ChiTiet
                    {
                        IDPhieu = idPhieu,
                        LoaiDieuChinh = loai,
                        ID_CT_BBGN = r.ID_CT_BBGN,
                        DVT = "Tấn",
                        NguoiTao = nguoi,
                        ThoiGianTao = now,
                        IsDelete = false,
                    };
                    _formContext.LG_PhieuDieuChinh_ChiTiet.Add(rec);
                    own[khoa] = rec;
                }

                // Mô tả dòng BBGN. Độ ẩm chỉ làm mới khi chưa điều chỉnh, vì KL quy khô đã tính theo độ ẩm lúc nhập.
                rec.IDNVL_BBGN = r.ID_VatTu;
                rec.TenNVL = r.TenVatTu ?? "";
                rec.MaLo = r.MaLO;
                rec.PhongBanXuat = r.TenPhongBanGiao;
                rec.XuongXuat = r.TenXuongGiao;
                rec.PhongBanNhap = r.TenPhongBanNhan;
                rec.XuongNhap = r.TenXuongNhan;
                rec.ID_PhongBan = idPhongBan;
                rec.IDCa = ParseCa(r.Ca);
                rec.ID_Xuong_BG = r.ID_Xuong_BG;
                rec.ID_Xuong_BN = r.ID_Xuong_BN;
                rec.ThoiGianXuLyBG = r.ThoiGianXuLyBG;
                var daDieuChinh = loai == LoaiNhap ? rec.KhoiLuongNhap.HasValue : rec.KhoiLuongXuat.HasValue;
                if (!daDieuChinh)
                    rec.DoAm = r.DoAm_W.HasValue ? (decimal)r.DoAm_W.Value : null;

                if (loai == LoaiNhap) nhap++; else xuat++;
                rec.NguoiSua = nguoi;
                rec.ThoiGianSua = now;
            }

            foreach (var r in rowsNhap) XuLy(r, LoaiNhap);
            foreach (var r in rowsXuat) XuLy(r, LoaiXuat);

            await _formContext.SaveChangesAsync();
            return (nhap, xuat, boQua);
        }

        // Ca của BBGN là text ("1"/"2") — đổi sang IDCa; không đọc được thì null
        public static int? ParseCa(string? ca) => int.TryParse(ca?.Trim(), out var v) ? v : null;

        // Trước ẩm: quy khô = khối lượng. Sau ẩm: quy khô = khối lượng x (1 - độ ẩm / 100).
        private static decimal? TinhQuyKho(decimal? khoiLuong, int? loaiSo, double? doAm)
        {
            if (!khoiLuong.HasValue) return null;
            if (loaiSo == 2)
                return doAm.HasValue ? Math.Round(khoiLuong.Value * (1 - (decimal)doAm.Value / 100m), 3) : null;
            return khoiLuong.Value;
        }

        private static string MoTaDong(SaveLG_PhieuDieuChinhBBGNDto item)
            => string.IsNullOrWhiteSpace(item.MoTa) ? $"Dòng BBGN #{item.IdCtBBGN}" : item.MoTa!;

        // ─── Nội bộ - Xuất vào sản xuất (Nạp liệu lò cao) ────────────────────

        // Lấy theo Ngày; ca = null thì lấy mọi ca (@p_ca NULL), không lọc kíp (@p_kip NULL).
        public async Task<List<NapLieuLoCaoDieuChinhResult>> GetNapLieuLoCaoAsync(DateTime ngay, int? ca = null)
        {
            return await _formContext.Database
                .SqlQueryRaw<NapLieuLoCaoDieuChinhResult>(
                    "EXEC dbo.SP_PCD_GetNVLNapLieuLoCao @p_ngay, @p_ca, @p_kip",
                    new SqlParameter("@p_ngay", ngay.Date),
                    new SqlParameter("@p_ca", (object?)ca ?? DBNull.Value),
                    new SqlParameter("@p_kip", DBNull.Value))
                .ToListAsync();
        }

        public async Task<BmPhieu?> GetPhieuByIdAsync(Guid idPhieu)
            => await _formContext.BmPhieus.FindAsync(idPhieu);

        // ─── Chi tiết Phiếu điều chỉnh (LG_PhieuDieuChinh_ChiTiet) ────────────

        public async Task<List<LG_PhieuDieuChinh_ChiTiet>> GetChiTietByPhieuAsync(Guid idPhieu)
        {
            return await _formContext.LG_PhieuDieuChinh_ChiTiet
                .Where(x => x.IDPhieu == idPhieu && !x.IsDelete && x.LoaiDieuChinh == 2) // chỉ phần Nội bộ
                .OrderBy(x => x.ThuTu)
                .ThenBy(x => x.ID)
                .AsNoTracking()
                .ToListAsync();
        }

        // Các trường "số liệu" người dùng được sửa tay khi Lưu phiếu. KHÔNG gồm LoaiDieuChinh/ID_CT_BBGN/
        // IDLoCao/thông tin tạo (do nguồn dữ liệu quyết định) và các cột xác nhận cũ (đã bỏ chức năng xác nhận).
        private static bool HasEditableChanges(LG_PhieuDieuChinh_ChiTiet db, LG_PhieuDieuChinh_ChiTiet inc) =>
            db.IDNVL != inc.IDNVL || db.IDNVL_BBGN != inc.IDNVL_BBGN || db.IDNVL_NapLieu != inc.IDNVL_NapLieu
            || db.TenNVL != inc.TenNVL || db.IDNVLChiTiet != inc.IDNVLChiTiet || db.IDNhomNVL != inc.IDNhomNVL
            || db.MaLo != inc.MaLo || db.LoaiSoDieuChinh != inc.LoaiSoDieuChinh
            || db.PhongBanXuat != inc.PhongBanXuat || db.XuongXuat != inc.XuongXuat
            || db.KhoiLuongXuat != inc.KhoiLuongXuat || db.KhoiLuongQuyKhoXuat != inc.KhoiLuongQuyKhoXuat
            || db.PhongBanNhap != inc.PhongBanNhap || db.XuongNhap != inc.XuongNhap
            || db.KhoiLuongNhap != inc.KhoiLuongNhap || db.KhoiLuongQuyKhoNhap != inc.KhoiLuongQuyKhoNhap
            || db.DoAm != inc.DoAm || db.ViTri != inc.ViTri || db.PhanLoai != inc.PhanLoai || db.GhiChu != inc.GhiChu
            || db.NguoiDieuChinhNhan != inc.NguoiDieuChinhNhan || db.ThoiGianDieuChinhNhan != inc.ThoiGianDieuChinhNhan;

        private static void CopyEditable(LG_PhieuDieuChinh_ChiTiet db, LG_PhieuDieuChinh_ChiTiet inc)
        {
            db.IDNVL = inc.IDNVL;
            db.IDNVL_BBGN = inc.IDNVL_BBGN;
            db.IDNVL_NapLieu = inc.IDNVL_NapLieu;
            db.TenNVL = inc.TenNVL;
            db.IDNVLChiTiet = inc.IDNVLChiTiet;
            db.IDNhomNVL = inc.IDNhomNVL;
            db.MaLo = inc.MaLo;
            db.LoaiSoDieuChinh = inc.LoaiSoDieuChinh;
            db.PhongBanXuat = inc.PhongBanXuat;
            db.XuongXuat = inc.XuongXuat;
            db.KhoiLuongXuat = inc.KhoiLuongXuat;
            db.KhoiLuongQuyKhoXuat = inc.KhoiLuongQuyKhoXuat;
            db.PhongBanNhap = inc.PhongBanNhap;
            db.XuongNhap = inc.XuongNhap;
            db.KhoiLuongNhap = inc.KhoiLuongNhap;
            db.KhoiLuongQuyKhoNhap = inc.KhoiLuongQuyKhoNhap;
            db.DoAm = inc.DoAm;
            db.ViTri = inc.ViTri;
            db.PhanLoai = inc.PhanLoai;
            db.GhiChu = inc.GhiChu;
            db.NguoiDieuChinhNhan = inc.NguoiDieuChinhNhan;
            db.ThoiGianDieuChinhNhan = inc.ThoiGianDieuChinhNhan;
        }

        public async Task ReplaceChiTietAsync(
            Guid idPhieu, List<LG_PhieuDieuChinh_ChiTiet> entities, List<long> deletedIds, string? nguoiSua)
        {
            await using var transaction = await _formContext.Database.BeginTransactionAsync();
            try
            {
                var now = DateTime.Now;
                // Chỉ phần Nội bộ (LoaiDieuChinh = 2): dòng Nhập/Xuất lưu qua SaveDieuChinhBBGNAsync, không sửa/xóa ở đây
                var byId = (await _formContext.LG_PhieuDieuChinh_ChiTiet
                        .Where(x => x.IDPhieu == idPhieu && x.LoaiDieuChinh == 2)
                        .ToListAsync())
                    .ToDictionary(x => x.ID);

                var inserts = new List<LG_PhieuDieuChinh_ChiTiet>();
                var updates = new List<(LG_PhieuDieuChinh_ChiTiet db, LG_PhieuDieuChinh_ChiTiet inc, bool changed)>();
                var conflicts = new List<int>();

                // Lượt 1: chỉ phân loại + phát hiện xung đột, chưa sửa entity nào.
                foreach (var inc in entities)
                {
                    if (inc.ID == 0)
                    {
                        inserts.Add(inc);
                        continue;
                    }

                    if (!byId.TryGetValue(inc.ID, out var db) || db.IsDelete)
                    {
                        conflicts.Add(inc.ThuTu ?? 0);
                        continue;
                    }

                    var changed = HasEditableChanges(db, inc);
                    if (changed && db.ThoiGianSua != inc.ThoiGianSua)
                    {
                        conflicts.Add(inc.ThuTu ?? 0);
                        continue;
                    }
                    updates.Add((db, inc, changed));
                }

                if (conflicts.Count > 0)
                    throw new ChiTietConflictException(
                        $"Các dòng thứ tự {string.Join(", ", conflicts)} đã bị người khác thay đổi hoặc xóa. " +
                        "Vui lòng tải lại phiếu rồi nhập lại.");

                // Lượt 2: ghi.
                foreach (var (db, inc, changed) in updates)
                {
                    db.ThuTu = inc.ThuTu; // đánh lại số thứ tự không tính là "sửa số liệu"
                    if (!changed) continue;
                    CopyEditable(db, inc);
                    db.NguoiSua = nguoiSua;
                    db.ThoiGianSua = now;
                }

                foreach (var id in deletedIds.Distinct())
                {
                    if (byId.TryGetValue(id, out var db) && !db.IsDelete)
                    {
                        db.IsDelete = true;
                        db.NguoiSua = nguoiSua;
                        db.ThoiGianSua = now;
                    }
                }

                if (inserts.Count > 0)
                {
                    foreach (var e in inserts) e.IDPhieu = idPhieu;
                    await _formContext.LG_PhieuDieuChinh_ChiTiet.AddRangeAsync(inserts);
                }

                await _formContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // Đồng bộ nguồn Nạp liệu lò cao (SP_PCD_GetNVLNapLieuLoCao) vào chi tiết của phiếu, luôn gán
        // LoaiDieuChinh = 2 ("Nội bộ - Xuất SX") cho dòng mới.
        public async Task<List<LG_PhieuDieuChinh_ChiTiet>> SyncChiTietFromNapLieuLoCaoAsync(Guid idPhieu, int idLoCao, List<NapLieuLoCaoDieuChinhResult> rows, string? nguoiThucHien, int? idPhongBan)
        {
            var now = DateTime.Now;

            if (rows == null || rows.Count == 0)
                return await GetChiTietByPhieuAsync(idPhieu);

            var existingList = await _formContext
                .LG_PhieuDieuChinh_ChiTiet
                .Where(x => x.IDPhieu == idPhieu)
                .ToListAsync();

            // Khớp theo (LoaiDieuChinh, ID_CT_BBGN) — chỉ xét trong đúng nguồn Nạp liệu lò cao
            // (LoaiDieuChinh = 2), không khớp nhầm với dòng BBGN dù ID_CT_BBGN trùng số.
            var existingDict = existingList
                .Where(x => x.LoaiDieuChinh == 2 && x.ID_CT_BBGN.HasValue)
                .ToDictionary(x => x.ID_CT_BBGN!.Value);

            var newEntities = new List<LG_PhieuDieuChinh_ChiTiet>();

            for (int idx = 0; idx < rows.Count; idx++)
            {
                var item = rows[idx];

                if (existingDict.TryGetValue(item.ID_CT_BBGN, out var existing))
                {
                    // Dòng người dùng đã xóa (xóa mềm) → không hồi sinh lại khi tải lại nguồn.
                    if (existing.IsDelete) continue;

                    // ĐÃ CÓ → chỉ cập nhật dữ liệu từ nguồn, giữ nguyên các trường điều chỉnh tay
                    existing.ThuTu = idx + 1;
                    existing.IDNVL = item.ID_VatTu;
                    existing.IDNVL_NapLieu = item.ID_VatTu;
                    existing.TenNVL = item.TenVatTu ?? "";
                    existing.MaLo = item.MaLO;

                    existing.PhongBanXuat = item.TenPhongBanGiao;
                    existing.XuongXuat = item.TenXuongGiao;

                    existing.PhongBanNhap = item.TenPhongBanNhan;
                    existing.XuongNhap = item.TenXuongNhan;
                    existing.IDLoCao = idLoCao;
                    existing.IDCa = item.Ca;
                    if (idPhongBan.HasValue) existing.ID_PhongBan = idPhongBan;

                    // Ghi chú KHÔNG lấy từ nguồn — để trống cho người dùng tự nhập tại Phiếu điều
                    // chỉnh, và không ghi đè ghi chú người dùng đã gõ mỗi lần tải lại dữ liệu.

                    // Không đổi NguoiSua/ThoiGianSua: đó là "phiên bản" số liệu dùng phát hiện xung đột khi Lưu phiếu.
                }
                else
                {
                    // CHƯA CÓ → thêm dòng mới
                    newEntities.Add(new LG_PhieuDieuChinh_ChiTiet
                    {
                        IDPhieu = idPhieu,
                        ID_CT_BBGN = item.ID_CT_BBGN,
                        ThuTu = idx + 1,
                        LoaiDieuChinh = 2,

                        IDNVL = item.ID_VatTu,
                        IDNVL_NapLieu = item.ID_VatTu,
                        TenNVL = item.TenVatTu ?? "",
                        MaLo = item.MaLO,

                        PhongBanXuat = item.TenPhongBanGiao,
                        XuongXuat = item.TenXuongGiao,

                        PhongBanNhap = item.TenPhongBanNhan,
                        XuongNhap = item.TenXuongNhan,
                        IDLoCao = idLoCao,
                        IDCa = item.Ca,
                        ID_PhongBan = idPhongBan,

                        // Ghi chú không lấy từ nguồn — để trống cho người dùng tự nhập.
                        GhiChu = null,

                        NguoiTao = nguoiThucHien,
                        ThoiGianTao = now,
                        NguoiSua = nguoiThucHien,
                        ThoiGianSua = now,

                        IsDelete = false,
                        TrangThai = 0
                    });
                }
            }

            if (newEntities.Count > 0)
            {
                await _formContext
                    .LG_PhieuDieuChinh_ChiTiet
                    .AddRangeAsync(newEntities);
            }

            await _formContext.SaveChangesAsync();

            return await GetChiTietByPhieuAsync(idPhieu);
        }
    }
}

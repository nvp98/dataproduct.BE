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

        // Phiếu còn sửa được: đang lưu (0), đã thu hồi (3), hiệu chỉnh (7) — cùng quy tắc với FE (TrangThaiPhieuConst).
        // Các trạng thái khác (đã gửi / đang phê duyệt / hoàn thành / đã chốt) là phiếu khóa.
        private static bool ChoPhepSua(int? tinhTrang) => tinhTrang is null or 0 or 3 or 7;

        // Trong tập phiếu đã cho, các phiếu đang khóa → số phiếu (để báo lỗi / hiển thị)
        public async Task<Dictionary<Guid, string?>> PhieuDangKhoaAsync(IEnumerable<Guid> idPhieus)
        {
            var ds = idPhieus.Distinct().ToList();
            if (ds.Count == 0) return new Dictionary<Guid, string?>();
            var list = await _formContext.BmPhieus
                .AsNoTracking()
                .Where(p => ds.Contains(p.Idphieu) && (p.IsDelete == null || p.IsDelete == 0))
                .Select(p => new { p.Idphieu, p.SoPhieu, p.TinhTrang })
                .ToListAsync();
            return list.Where(p => !ChoPhepSua(p.TinhTrang)).ToDictionary(p => p.Idphieu, p => p.SoPhieu);
        }

        private static string TenPhieu(Guid id, string? so) => string.IsNullOrWhiteSpace(so) ? id.ToString() : so;

        // Khóa theo từng dòng BBGN (sp_getapplock, giữ tới hết transaction): 2 người lưu/reset cùng 1 dòng BBGN — dù ở cùng
        // phiếu hay 2 phiếu của 2 bên — phải lần lượt từng người, người sau đọc lại dữ liệu người trước vừa ghi rồi mới
        // đối chiếu. Không khóa thì 2 bên cùng lúc lưu số khác nhau sẽ cùng qua bước đối chiếu và để lại dữ liệu lệch.
        // Khóa theo thứ tự id tăng dần để 2 request nhiều dòng không khóa chéo nhau (deadlock).
        private async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> KhoaDongBBGNAsync(IEnumerable<int> idCtBbgn)
        {
            var tx = await _formContext.Database.BeginTransactionAsync();
            try
            {
                foreach (var id in idCtBbgn.Distinct().OrderBy(x => x))
                {
                    var ketQua = new SqlParameter("@kq", System.Data.SqlDbType.Int) { Direction = System.Data.ParameterDirection.Output };
                    await _formContext.Database.ExecuteSqlRawAsync(
                        "EXEC @kq = sp_getapplock @Resource = @tenKhoa, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 15000",
                        ketQua, new SqlParameter("@tenKhoa", $"PDC_BBGN_{id}"));
                    if (ketQua.Value is int kq && kq < 0)
                        throw new InvalidOperationException("Dòng này đang được người khác lưu, vui lòng thử lại sau giây lát.");
                }
                return tx;
            }
            catch
            {
                await tx.DisposeAsync();
                throw;
            }
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

        // Cùng "phiên bản" dòng: thời gian điều chỉnh của bên mình mà người dùng đang giữ == trong database (sai số 10ms vì cột có
        // thể làm tròn mili-giây). Cùng rỗng (dòng chưa ai nhập KL từ lúc tải) cũng là cùng phiên bản.
        private static bool CungPhienBan(DateTime? dangGiu, DateTime? trongDb)
            => dangGiu.HasValue && trongDb.HasValue
                ? Math.Abs((dangGiu.Value - trongDb.Value).TotalMilliseconds) <= 10
                : !dangGiu.HasValue && !trongDb.HasValue;

        // Trả về "phiên bản" (thời gian điều chỉnh của bên mình) mới của các dòng vừa ghi — người dùng giữ lại để lần Lưu sau không bị báo xung đột với chính mình
        public async Task<Dictionary<int, DateTime?>> SaveDieuChinhBBGNAsync(
            Guid idPhieu, bool laBenNhan, int? nguoiThucHien, List<SaveLG_PhieuDieuChinhBBGNDto> items, int? idPhongBan)
        {
            var loai = laBenNhan ? LoaiNhap : LoaiXuat;
            var loaiKia = laBenNhan ? LoaiXuat : LoaiNhap;
            var ids = items.Select(i => i.IdCtBBGN).Distinct().ToList();
            var phienBanMoi = new Dictionary<int, DateTime?>();

            // Từ đây mọi thứ đọc/ghi trong 1 transaction có khóa các dòng BBGN, tránh 2 người ghi chồng nhau
            await using var tx = await KhoaDongBBGNAsync(ids);

            var khoaMinh = await PhieuDangKhoaAsync(new[] { idPhieu });
            if (khoaMinh.Count > 0)
                throw new InvalidOperationException("Phiếu đã gửi/đang khóa nên không sửa được — cần thu hồi phiếu trước.");

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

            // Phiếu của bên kia đã gửi/khóa (số phiếu) — KL của họ là số chốt, bên mình không được xóa KL đã khớp với số đó
            var phieuBenKiaKhoa = await PhieuDangKhoaAsync(doiTac.Values.Select(x => x.IDPhieu));

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
                else if (item.LoaiSoDieuChinh == 2 && !item.DoAm.HasValue)
                {
                    // Mới chọn Loại số (chưa nhập KL) vẫn được lưu để bên kia thấy và chọn theo
                    loi.Add($"{MoTaDong(item)}: dòng BBGN không có độ ẩm nên không chọn được Sau ẩm.");
                    continue;
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
                    if (!item.KhoiLuong.HasValue && !item.LoaiSoDieuChinh.HasValue && string.IsNullOrWhiteSpace(item.GhiChu)) continue;

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
                else if (!CungPhienBan(item.ThoiGianPhienBan, laBenNhan ? rec.ThoiGianDieuChinhNhan : rec.ThoiGianDieuChinhGiao))
                {
                    // Người khác (cùng bên, hoặc bên kia reset) đã điều chỉnh dòng này sau khi người dùng tải màn hình
                    var ai = laBenNhan ? rec.NguoiDieuChinhNhan : rec.NguoiDieuChinhGiao;
                    var luc = laBenNhan ? rec.ThoiGianDieuChinhNhan : rec.ThoiGianDieuChinhGiao;
                    loi.Add($"{MoTaDong(item)}: dòng này vừa được " +
                            (ai.HasValue ? $"người dùng {ai} " : "") + "thay đổi" +
                            (luc.HasValue ? $" lúc {luc:dd/MM/yyyy HH:mm}" : "") +
                            " — hãy tải lại trang để xem số mới rồi chỉnh lại.");
                    continue;
                }

                // Loại số là của cả cặp dòng: bên nào chọn trước thì bên kia phải chọn giống (muốn đổi thì Reset loại số)
                if (item.LoaiSoDieuChinh.HasValue && doiTac.TryGetValue(item.IdCtBBGN, out var kiaLoai)
                    && kiaLoai.LoaiSoDieuChinh.HasValue && kiaLoai.LoaiSoDieuChinh != item.LoaiSoDieuChinh)
                {
                    loi.Add($"{MoTaDong(item)}: bên {(laBenNhan ? "xuất" : "nhận")} đã chọn " +
                            $"{(kiaLoai.LoaiSoDieuChinh == 1 ? "Trước ẩm" : "Sau ẩm")} — phải chọn giống, " +
                            "hoặc Reset loại số điều chỉnh để chọn lại.");
                    continue;
                }

                // Bên kia đã gửi phiếu với KL chốt: bên mình đã nhập KL thì không được xóa (để trống) — làm vậy cặp dòng sẽ
                // lệch dù phiếu bên kia đã khóa. Đổi sang số khác thì đã bị chặn ở bước đối chiếu bên dưới.
                var klHienCo = laBenNhan ? rec.KhoiLuongNhap : rec.KhoiLuongXuat;
                if (!item.KhoiLuong.HasValue && klHienCo.HasValue
                    && doiTac.TryGetValue(item.IdCtBBGN, out var kiaDaGui)
                    && phieuBenKiaKhoa.TryGetValue(kiaDaGui.IDPhieu, out var soKiaDaGui)
                    && (laBenNhan ? kiaDaGui.KhoiLuongXuat : kiaDaGui.KhoiLuongNhap).HasValue)
                {
                    loi.Add($"{MoTaDong(item)}: phiếu {TenPhieu(kiaDaGui.IDPhieu, soKiaDaGui)} của bên " +
                            $"{(laBenNhan ? "xuất" : "nhận")} đã gửi nên không được xóa KL đã nhập — " +
                            "cần thu hồi phiếu đó nếu muốn thay đổi.");
                    continue;
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
                if (laBenNhan) rec.GhiChuNhap = item.GhiChu; else rec.GhiChuXuat = item.GhiChu;
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
                phienBanMoi[item.IdCtBBGN] = item.KhoiLuong.HasValue ? now : null;
            }

            // Có dòng vi phạm thì không SaveChanges — các thay đổi đang theo dõi bị bỏ khi request kết thúc.
            if (loi.Count > 0)
                throw new InvalidOperationException(string.Join("\n", loi));

            await _formContext.SaveChangesAsync();
            await tx.CommitAsync();
            return phienBanMoi;
        }

        // Trước khi gửi phiếu: mọi dòng Nhập/Xuất mà phiếu này đã điều chỉnh KL phải được bên kia nhập KHỚP (cùng KL, cùng
        // loại số). Trả về các dòng chưa khớp (rỗng = được gửi). Bên kia nằm ngoài các xưởng của LG_PDC_Scope_PhongBan
        // (không có phiếu điều chỉnh) thì không đòi.
        public async Task<List<string>> KiemTraDieuKienGuiAsync(Guid idPhieu)
        {
            var cuaPhieu = await _formContext.LG_PhieuDieuChinh_ChiTiet
                .AsNoTracking()
                .Where(x => x.IDPhieu == idPhieu && !x.IsDelete && x.ID_CT_BBGN != null
                            && (x.LoaiDieuChinh == LoaiNhap || x.LoaiDieuChinh == LoaiXuat))
                .ToListAsync();
            cuaPhieu = cuaPhieu
                .Where(x => x.LoaiDieuChinh == LoaiNhap ? x.KhoiLuongNhap.HasValue : x.KhoiLuongXuat.HasValue)
                .ToList();
            if (cuaPhieu.Count == 0) return new List<string>();

            var daLuu = await GetDieuChinhBBGNAsync(cuaPhieu.Select(x => x.ID_CT_BBGN!.Value).ToList());
            var xuongTrongPhamVi = (await _formContext.LG_PDC_Scope_PhongBan
                    .AsNoTracking()
                    .Where(x => !x.IsDelete && x.ID_Xuong != null)
                    .Select(x => x.ID_Xuong!.Value)
                    .Distinct()
                    .ToListAsync())
                .ToHashSet();

            var loi = new List<string>();
            foreach (var rec in cuaPhieu)
            {
                var laNhap = rec.LoaiDieuChinh == LoaiNhap;
                var xuongKia = laNhap ? rec.ID_Xuong_BG : rec.ID_Xuong_BN;
                if (xuongKia.HasValue && !xuongTrongPhamVi.Contains(xuongKia.Value)) continue;

                var ct = rec.ID_CT_BBGN!.Value;
                var kia = laNhap ? daLuu.Xuat.GetValueOrDefault(ct) : daLuu.Nhap.GetValueOrDefault(ct);
                var klKia = kia == null ? null : (laNhap ? kia.KhoiLuongXuat : kia.KhoiLuongNhap);
                var tenBenKia = laNhap ? "xuất" : "nhận";
                var mo = $"{rec.TenNVL}{(string.IsNullOrWhiteSpace(rec.MaLo) ? "" : " - " + rec.MaLo)}";

                if (!klKia.HasValue)
                    loi.Add($"{mo}: bên {tenBenKia} chưa nhập số điều chỉnh.");
                else if (klKia.Value != (laNhap ? rec.KhoiLuongNhap : rec.KhoiLuongXuat))
                    loi.Add($"{mo}: KL hai bên chưa khớp (nhập {(laNhap ? rec.KhoiLuongNhap : klKia):0.###}, " +
                            $"xuất {(laNhap ? klKia : rec.KhoiLuongXuat):0.###}).");
                else if (!rec.LoaiSoDieuChinh.HasValue || !kia!.LoaiSoDieuChinh.HasValue)
                    loi.Add($"{mo}: chưa chọn Loại số điều chỉnh (Trước ẩm / Sau ẩm) — cần chọn lại sau khi Reset loại số.");
                else if (kia.LoaiSoDieuChinh != rec.LoaiSoDieuChinh)
                    loi.Add($"{mo}: hai bên chọn Loại số khác nhau (Trước ẩm / Sau ẩm).");
            }
            return loi;
        }

        // Nối dòng "[Reset ...]" vào ghi chú của bên đang reset. Ghi chú hiện tại chính là lý do người dùng vừa gõ thì thay luôn
        // (lý do đã nằm trong dòng reset), tránh hiện lặp.
        private static string NoiGhiChuReset(string? cu, string lyDo, string dong)
            => string.IsNullOrWhiteSpace(cu) || cu.Trim() == lyDo.Trim() ? dong : $"{cu}\n{dong}";

        // Reset Loại số điều chỉnh của CẢ 2 bên cho 1 dòng BBGN (bên nào cũng bấm được, bắt buộc có lý do). KL của 2 bên giữ
        // nguyên, KL quy khô bị xóa (vì phụ thuộc loại số) — 2 bên chọn lại loại số rồi lưu lại. Cần cả 2 phiếu còn sửa được.
        public async Task ResetLoaiSoBBGNAsync(
            Guid idPhieu, bool laBenNhan, int idCtBBGN, string lyDo, int? nguoiThucHien, int? loaiSoBenKiaDangThay = null)
        {
            var loai = laBenNhan ? LoaiNhap : LoaiXuat;
            var loaiKia = laBenNhan ? LoaiXuat : LoaiNhap;

            await using var tx = await KhoaDongBBGNAsync(new[] { idCtBBGN });

            if ((await PhieuDangKhoaAsync(new[] { idPhieu })).Count > 0)
                throw new InvalidOperationException("Phiếu đã gửi/đang khóa nên không reset được — cần thu hồi phiếu trước.");

            var own =(await _formContext.LG_PhieuDieuChinh_ChiTiet
                    .Where(x => x.IDPhieu == idPhieu && x.LoaiDieuChinh == loai && !x.IsDelete && x.ID_CT_BBGN == idCtBBGN)
                    .ToListAsync())
                .OrderByDescending(x => x.ID)
                .FirstOrDefault()
                ?? throw new InvalidOperationException("Phiếu này chưa có dòng BBGN này — hãy Tải dữ liệu nguồn trước.");

            var kiaList = (await _formContext.LG_PhieuDieuChinh_ChiTiet
                    .Where(x => !x.IsDelete && x.LoaiDieuChinh == loaiKia && x.ID_CT_BBGN == idCtBBGN)
                    .ToListAsync())
                .OrderByDescending(x => x.ID)
                .ToList();
            var conSong = await PhieuConSongAsync(kiaList.Select(x => x.IDPhieu));
            var kia = kiaList.FirstOrDefault(x => conSong.Contains(x.IDPhieu));

            if (!own.LoaiSoDieuChinh.HasValue && !(kia?.LoaiSoDieuChinh).HasValue)
                throw new InvalidOperationException("Dòng này chưa chọn Loại số điều chỉnh nên không có gì để reset.");

            // Màn hình đang hiện loại số cũ (bên kia vừa đổi hoặc vừa reset) → không reset nhầm, để người dùng xem lại
            if (loaiSoBenKiaDangThay.HasValue && kia?.LoaiSoDieuChinh != loaiSoBenKiaDangThay)
                throw new InvalidOperationException(
                    "Loại số điều chỉnh của bên kia đã thay đổi so với màn hình của bạn — hãy tải lại trang để xem số mới, " +
                    "rồi reset lại nếu vẫn cần.");

            if (kia != null)
            {
                var khoaKia = await PhieuDangKhoaAsync(new[] { kia.IDPhieu });
                if (khoaKia.TryGetValue(kia.IDPhieu, out var soKia))
                    throw new InvalidOperationException(
                        $"Phiếu {TenPhieu(kia.IDPhieu, soKia)} của bên {(laBenNhan ? "xuất" : "nhận")} đã gửi nên không reset được — " +
                        "cần thu hồi phiếu đó trước.");
            }

            var now = DateTime.Now;
            var loaiCu = own.LoaiSoDieuChinh ?? kia?.LoaiSoDieuChinh;

            own.LoaiSoDieuChinh = null;
            if (laBenNhan) own.KhoiLuongQuyKhoNhap = null; else own.KhoiLuongQuyKhoXuat = null;

            if (kia != null)
            {
                kia.LoaiSoDieuChinh = null;
                if (laBenNhan) kia.KhoiLuongQuyKhoXuat = null; else kia.KhoiLuongQuyKhoNhap = null;
                var dong = $"[Reset loại số bởi bên {(laBenNhan ? "nhận" : "xuất")} {now:dd/MM/yyyy HH:mm} " +
                           $"(loại cũ {(loaiCu == 1 ? "Trước ẩm" : "Sau ẩm")}): {lyDo.Trim()}]";
                // Dòng reset ghi vào ghi chú của chính bên đang reset
                if (laBenNhan) own.GhiChuNhap = NoiGhiChuReset(own.GhiChuNhap, lyDo, dong);
                else own.GhiChuXuat = NoiGhiChuReset(own.GhiChuXuat, lyDo, dong);
            }
            await _formContext.SaveChangesAsync();
            await tx.CommitAsync();
        }

        public async Task ResetBenKiaBBGNAsync(
            Guid idPhieu, bool laBenNhan, int idCtBBGN, string lyDo, int? nguoiThucHien, decimal? klBenKiaDangThay = null)
        {
            var loai = laBenNhan ? LoaiNhap : LoaiXuat;
            var loaiKia = laBenNhan ? LoaiXuat : LoaiNhap;

            await using var tx = await KhoaDongBBGNAsync(new[] { idCtBBGN });

            if ((await PhieuDangKhoaAsync(new[] { idPhieu })).Count > 0)
                throw new InvalidOperationException("Phiếu đã gửi/đang khóa nên không reset được — cần thu hồi phiếu trước.");

            var dongCuaMinh = (await _formContext.LG_PhieuDieuChinh_ChiTiet
                    .Where(x => x.IDPhieu == idPhieu && x.LoaiDieuChinh == loai && !x.IsDelete && x.ID_CT_BBGN == idCtBBGN)
                    .ToListAsync())
                .OrderByDescending(x => x.ID)
                .FirstOrDefault();
            if (dongCuaMinh == null)
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

            // Màn hình đang hiện KL cũ (bên kia vừa sửa lại) → không reset nhầm số mới, để người dùng xem lại
            if (klBenKiaDangThay.HasValue && Math.Abs(klBenKiaDangThay.Value - klKia!.Value) > 0.0005m)
                throw new InvalidOperationException(
                    $"Bên {(laBenNhan ? "xuất" : "nhận")} vừa điều chỉnh KL thành {klKia:0.###} " +
                    $"(màn hình bạn đang hiện {klBenKiaDangThay:0.###}) — hãy tải lại trang để xem số mới, " +
                    "rồi reset lại nếu vẫn cần.");

            var khoaKia = await PhieuDangKhoaAsync(new[] { rec.IDPhieu });
            if (khoaKia.TryGetValue(rec.IDPhieu, out var soKia))
                throw new InvalidOperationException(
                    $"Phiếu {TenPhieu(rec.IDPhieu, soKia)} của bên {(laBenNhan ? "xuất" : "nhận")} đã gửi nên không reset được — " +
                    "cần thu hồi phiếu đó trước.");

            var now = DateTime.Now;
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
            // Dòng reset ghi vào ghi chú của chính bên đang reset
            if (laBenNhan) dongCuaMinh.GhiChuNhap = NoiGhiChuReset(dongCuaMinh.GhiChuNhap, lyDo, dong);
            else dongCuaMinh.GhiChuXuat = NoiGhiChuReset(dongCuaMinh.GhiChuXuat, lyDo, dong);
            await _formContext.SaveChangesAsync();
            await tx.CommitAsync();
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
                rec.IDNhomNVL = r.ID_NhomVatTu;
                rec.TenNhomNVL = r.TenNhomVatTu;
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
            || db.TenNVL != inc.TenNVL || db.IDNVLChiTiet != inc.IDNVLChiTiet || db.IDNhomNVL != inc.IDNhomNVL || db.TenNhomNVL != inc.TenNhomNVL
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
            db.TenNhomNVL = inc.TenNhomNVL;
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
            Guid idPhieu, List<LG_PhieuDieuChinh_ChiTiet> entities, List<long> deletedIds)
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
                }

                foreach (var id in deletedIds.Distinct())
                {
                    if (byId.TryGetValue(id, out var db) && !db.IsDelete)
                    {
                        db.IsDelete = true;
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

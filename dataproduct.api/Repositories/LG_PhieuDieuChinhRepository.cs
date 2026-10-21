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

        public async Task<List<BBGNChiTietResult>> GetBBGNAsync(DateTime ngay, int? ca, string? kip)
        {
            return await _context.BBGNChiTietResults
                .FromSqlRaw(
                    "EXEC dbo.SP_PDC_Getnvl_BBGN @p_ngay, @p_ca, @p_kip",
                    new SqlParameter("@p_ngay", ngay.Date),
                    new SqlParameter("@p_ca", (object?)ca ?? DBNull.Value),
                    new SqlParameter("@p_kip", (object?)kip ?? DBNull.Value))
                .AsNoTracking()
                .ToListAsync();
        }

        // Sp_GetNVLNapLieuLoCao nằm ở PRODUCT_FORM (BM_Phieu, LG_NL_ChiTiet, LG_NL_NVL đều ở đó)
        // — gọi qua _formContext, khác GetBBGNAsync ở trên (SP_Get_BBGN nằm ở PRODUCTDATA/_context).
        // Dùng SqlQueryRaw<T> (EF Core 7+) thay vì FromSqlRaw vì kiểu kết quả không cần đăng ký 
        // DbSet/HasNoKey trong ProductFormContext.
        public async Task<List<NapLieuLoCaoDieuChinhResult>> GetNapLieuLoCaoAsync(DateTime ngay, int? ca, string? kip)
        {
            return await _formContext.Database
                .SqlQueryRaw<NapLieuLoCaoDieuChinhResult>(
                    "EXEC dbo.SP_PCD_GetNVLNapLieuLoCao @p_ngay, @p_ca, @p_kip",
                    new SqlParameter("@p_ngay", ngay.Date),
                    new SqlParameter("@p_ca", (object?)ca ?? DBNull.Value),
                    new SqlParameter("@p_kip", (object?)kip ?? DBNull.Value))
                .ToListAsync();
        }

        // BmPhieu (header) nằm ở PRODUCT_FORM — dùng để đọc NgaySX/Ca/Kip khi đồng bộ
        // chi tiết từ BBGN cho 1 phiếu đã lưu (xem SyncChiTietFromBBGNAsync).
        public async Task<BmPhieu?> GetPhieuByIdAsync(Guid idPhieu)
            => await _formContext.BmPhieus.FindAsync(idPhieu);

        // ─── Chi tiết Phiếu điều chỉnh (LG_PhieuDieuChinh_ChiTiet) ────────────

        public async Task<List<LG_PhieuDieuChinh_ChiTiet>> GetChiTietByPhieuAsync(Guid idPhieu)
        {
            return await _formContext.LG_PhieuDieuChinh_ChiTiet
                .Where(x => x.IDPhieu == idPhieu && !x.IsDelete)
                .OrderBy(x => x.ThuTu)
                .ThenBy(x => x.ID)
                .AsNoTracking()
                .ToListAsync();
        }

        // Các trường "số liệu" người dùng được sửa tay khi Lưu phiếu. KHÔNG gồm nhóm xác nhận
        // (NguoiDieuChinhGiao/ThoiGianDieuChinhGiao/TrangThai — chỉ API xac-nhan-giao được ghi)
        // và không gồm LoaiDieuChinh/ID_CT_BBGN/thông tin tạo.
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

        // Lưu theo từng dòng (upsert theo ID) thay vì xóa-ghi lại cả phiếu:
        //  - ID = 0            → dòng mới, thêm.
        //  - ID khớp trong DB  → chỉ cập nhật các trường số liệu; dòng đã khóa (TrangThai = 1) bỏ qua.
        //  - deletedIds        → xóa mềm (FE gửi đúng các dòng người dùng đã xóa — KHÔNG suy ra từ
        //                        "dòng nào vắng mặt", để không xóa nhầm dòng người khác vừa sync).
        // Khóa lạc quan: ThoiGianSua FE gửi lên phải khớp DB, nếu không mà số liệu lại khác → dòng
        // đã bị người khác sửa → ném ChiTietConflictException, không ghi gì cả.
        public async Task ReplaceChiTietAsync(
            Guid idPhieu, List<LG_PhieuDieuChinh_ChiTiet> entities, List<long> deletedIds, string? nguoiSua)
        {
            await using var transaction = await _formContext.Database.BeginTransactionAsync();
            try
            {
                var now = DateTime.Now;
                var byId = (await _formContext.LG_PhieuDieuChinh_ChiTiet
                        .Where(x => x.IDPhieu == idPhieu)
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

                    if (db.TrangThai == 1) continue; // đã xác nhận → khóa, không cho Lưu phiếu đụng vào

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
                    if (byId.TryGetValue(id, out var db) && !db.IsDelete && db.TrangThai != 1)
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

        // Cập nhật riêng "Người điều chỉnh giao" cho 1 dòng chi tiết — dùng khi người dùng (bên
        // giao) tích/hủy tích xác nhận, lưu ngay xuống DB thay vì chờ Lưu cả phiếu. ("Người điều
        // chỉnh nhận" là bên nhận tự động được gán khi họ sửa Khối lượng/Độ ẩm — xem FE recomputeRows.)
        public async Task<LG_PhieuDieuChinh_ChiTiet?> UpdateXacNhanGiaoAsync(
            long id, int? nguoiDieuChinhGiao, DateTime? thoiGianDieuChinhGiao, int trangThai)
        {
            var existing = await _formContext.LG_PhieuDieuChinh_ChiTiet.FindAsync(id);

            if (existing == null || existing.IsDelete) return null;

            // Chỉ chặn khi XÁC NHẬN (trangThai = 1); hủy xác nhận luôn cho phép.
            if (trangThai == 1)
            {
                if (existing.KhoiLuongNhap == null)
                    throw new InvalidOperationException(
                        "Chưa có Khối lượng nhập — vui lòng điền và Lưu phiếu trước khi xác nhận.");

                if (existing.NguoiDieuChinhNhan == null)
                    throw new InvalidOperationException(
                        "Bên nhận chưa điều chỉnh số liệu — chưa thể xác nhận.");
            }

            existing.NguoiDieuChinhGiao = nguoiDieuChinhGiao;
            existing.ThoiGianDieuChinhGiao = thoiGianDieuChinhGiao;
            existing.TrangThai = trangThai;

            await _formContext.SaveChangesAsync();
            return existing;
        }

        // ─── Danh mục NVL (LG_PhieuDieuChinh_NVL, PRODUCT_FORM) ────────────────

        public async Task<List<LG_PhieuDieuChinh_NVL>> GetNvlListAsync(bool onlyActive)
        {
            return await _formContext.LG_PhieuDieuChinh_NVL
                .Where(x => !onlyActive || x.IsActive)
                .OrderBy(x => x.TenNVL)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<LG_PhieuDieuChinh_NVL?> GetNvlByIdAsync(int id)
            => await _formContext.LG_PhieuDieuChinh_NVL.FindAsync(id);

        public async Task<LG_PhieuDieuChinh_NVL> AddNvlAsync(LG_PhieuDieuChinh_NVL entity)
        {
            await _formContext.LG_PhieuDieuChinh_NVL.AddAsync(entity);
            await _formContext.SaveChangesAsync();
            return entity;
        }

        public async Task<LG_PhieuDieuChinh_NVL?> UpdateNvlAsync(int id, LG_PhieuDieuChinh_NVL entity)
        {
            var existing = await _formContext.LG_PhieuDieuChinh_NVL.FindAsync(id);
            if (existing == null) return null;

            existing.TenNVL = entity.TenNVL;
            existing.IsActive = entity.IsActive;

            await _formContext.SaveChangesAsync();
            return existing;
        }

        // Xóa mềm — set IsActive=false thay vì xóa hẳn, để không phá dữ liệu chi tiết
        // đã lưu (TenNVL trong LG_PhieuDieuChinh_ChiTiet lưu text, không phải FK cứng).
        public async Task<bool> DeleteNvlAsync(int id)
        {
            var existing = await _formContext.LG_PhieuDieuChinh_NVL.FindAsync(id);
            if (existing == null) return false;
            existing.IsActive = false;
            await _formContext.SaveChangesAsync();
            return true;
        }
        public async Task<List<LG_PhieuDieuChinh_ChiTiet>> SyncChiTietFromBBGNAsync(Guid idPhieu, List<BBGNChiTietResult> bbgnData, string? nguoiThucHien)
        {
            var now = DateTime.Now;

            if (bbgnData == null || bbgnData.Count == 0)
                return await GetChiTietByPhieuAsync(idPhieu);

            var existingList = await _formContext
                .LG_PhieuDieuChinh_ChiTiet
                .Where(x => x.IDPhieu == idPhieu)
                .ToListAsync();

            // Khớp theo (LoaiDieuChinh, ID_CT_BBGN) — chỉ xét trong đúng nguồn BBGN (LoaiDieuChinh = 1)
            // để không khớp nhầm với dòng của nguồn khác (vd Nạp liệu lò cao) dù ID_CT_BBGN trùng số
            // (2 nguồn lấy ID từ 2 bảng khác nhau, không có gì đảm bảo không trùng).
            var existingDict = existingList
                .Where(x => x.LoaiDieuChinh == 1 && x.ID_CT_BBGN.HasValue)
                .ToDictionary(x => x.ID_CT_BBGN!.Value);

            var newEntities = new List<LG_PhieuDieuChinh_ChiTiet>();

            for (int idx = 0; idx < bbgnData.Count; idx++)
            {
                var item = bbgnData[idx];

                if (existingDict.TryGetValue(
                        item.ID_CT_BBGN,
                        out var existing))
                {
                    // Dòng người dùng đã xóa (xóa mềm) → không hồi sinh lại khi tải lại nguồn.
                    if (existing.IsDelete) continue;

                    // ĐÃ CÓ → chỉ cập nhật dữ liệu từ BBGN, giữ nguyên các trường điều chỉnh tay

                    existing.ThuTu = idx + 1;
                    existing.IDNVL = item.ID_VatTu;
                    existing.IDNVL_BBGN = item.ID_VatTu;
                    existing.TenNVL = item.TenVatTu ?? "";
                    existing.MaLo = item.MaLO;

                    existing.PhongBanXuat = item.TenPhongBanGiao;
                    existing.XuongXuat = item.TenXuongGiao;

                    existing.PhongBanNhap = item.TenPhongBanNhan;
                    existing.XuongNhap = item.TenXuongNhan;

                    // Ghi chú KHÔNG lấy từ nguồn — để trống cho người dùng tự nhập tại Phiếu điều
                    // chỉnh, và không ghi đè ghi chú người dùng đã gõ mỗi lần tải lại dữ liệu.

                    // Không đổi NguoiSua/ThoiGianSua: đó là "phiên bản" số liệu dùng để phát hiện
                    // xung đột khi Lưu phiếu (xem ReplaceChiTietAsync) — sync chỉ cập nhật trường nguồn.
                }
                else
                {
                    // CHƯA CÓ → thêm dòng mới

                    newEntities.Add(new LG_PhieuDieuChinh_ChiTiet
                    {
                        IDPhieu = idPhieu,
                        ID_CT_BBGN = item.ID_CT_BBGN,
                        ThuTu = idx + 1,
                        LoaiDieuChinh = 1,

                        IDNVL = item.ID_VatTu,
                        IDNVL_BBGN = item.ID_VatTu,
                        TenNVL = item.TenVatTu ?? "",
                        MaLo = item.MaLO,

                        PhongBanXuat = item.TenPhongBanGiao,
                        XuongXuat = item.TenXuongGiao,

                        PhongBanNhap = item.TenPhongBanNhan,
                        XuongNhap = item.TenXuongNhan,

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

        // Giống SyncChiTietFromBBGNAsync ở trên nhưng nguồn là Sp_GetNVLNapLieuLoCao và luôn gán
        // LoaiDieuChinh = 2 ("Nội bộ - Xuất SX") cho dòng mới.
        public async Task<List<LG_PhieuDieuChinh_ChiTiet>> SyncChiTietFromNapLieuLoCaoAsync(Guid idPhieu, List<NapLieuLoCaoDieuChinhResult> rows, string? nguoiThucHien)
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

                    // Ghi chú KHÔNG lấy từ nguồn — để trống cho người dùng tự nhập tại Phiếu điều
                    // chỉnh, và không ghi đè ghi chú người dùng đã gõ mỗi lần tải lại dữ liệu.

                    // Không đổi NguoiSua/ThoiGianSua — xem ghi chú ở SyncChiTietFromBBGNAsync.
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

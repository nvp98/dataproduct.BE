using dataproduct.api.DTOs;
using dataproduct.api.Models;
using dataproduct.api.Models.MasterData;
using dataproduct.api.ResponseModels;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public interface IHRC1_BBGNRepository
    {
        Task<BmPhieu?> GetBmPhieuAsync(Guid idPhieu);
        Task<List<BmPhieu>> GetBmPhieusByIdsAsync(IEnumerable<Guid> ids);
        Task<BmPhieu?> GetPhieuDucAsync(DateOnly ngay, int ca, int idMayDuc);
        Task<HRC1_MeThep?> GetMeByIdAsync(int meId);
        Task<HRC1_MeThep?> GetMeByMaMeAsync(string maMe);
        Task<List<HRC1_MeThep>> SearchMeThepAsync(string q, int limit);
        Task<HRC1_MePhanCong?> GetMePhanCongByIdAsync(int id);
        Task<List<HRC1_MePhanCong>> GetMePhanCongsByPhieuAsync(Guid idPhieu, string congDoan, int? scopePhieu = null);
        Task<List<HRC1_MePhanCong>> GetAllMePhanCongsByMeIdAsync(int meId);
        Task<List<HRC1_MeThep>> GetMeThepsByIdsAsync(IEnumerable<int> meIds);
        Task<List<HRC1_MeThep>> GetMeThepsByMayDucAsync(DateOnly ngay, int ca, int idMayDuc);
        Task<List<HRC1_MeThep>> GetChoNhanAsync();
        Task<HRC1_PagedResult<HRC1_ChoNhanMeVm>> GetMeChoNhanPagedAsync(DateOnly? tuNgay, DateOnly? denNgay, int? ca, string? maMe, string? thungSo, int? loSo, int page, int pageSize);
        Task<List<MayDuc>> GetMayDucsHRC1Async();
        Task<int> GetMaxThuTuTLAsync(int meId);
        Task<bool> ExistsMePhanCongAsync(int meId, string congDoan);
        Task<bool> ExistsMePhanCongInPhieuAsync(int meId, Guid idPhieu);
        Task<List<HRC1_TrungMeInfo>> GetAllTinhLuyenPhieuByMaMeAsync(string maMe);
        Task<List<HRC1_MeThep>> GetActiveTLMesByMaMeAsync(string maMe);
        /// <summary>Mẻ "active conflict": TrangThaiTL &gt;= 1 HOẶC DichChuyen = "len_thang"</summary>
        Task<List<HRC1_MeThep>> GetActiveConflictMesByMaMeAsync(string maMe);
        Task<List<HRC1_MeThep>> GetLenThangMesByMaMeAsync(string maMe);
        Task<Dictionary<int, int?>> GetTLScopesByMeIdsAsync(IEnumerable<int> meIds);
        Task<Dictionary<int, string?>> GetUserNamesByIdsAsync(IEnumerable<int> ids);
        Task<List<HRC1_MePhanCong>> GetTLPhanCongsByMePhieuAsync(int meId, Guid idPhieu, int? scopePhieu = null);
        Task<List<HRC1_ExportRow>> GetMeThepsForExportAsync(HRC1_ExportQuery query);
        Task<HRC1_ThongKeResult> GetMeThepsPagedAsync(HRC1_ThongKeQuery query);
        Task<HRC1_TongHopResult> GetTongHopAsync(HRC1_ThongKeQuery query);
        Task<List<int>> GetActiveDucPhieuScopesAsync(DateOnly ngay, int ca);
        void AddMeThep(HRC1_MeThep me);
        void RemoveMeThep(HRC1_MeThep me);
        void AddMePhanCong(HRC1_MePhanCong pc);
        void RemoveMePhanCongs(IEnumerable<HRC1_MePhanCong> pcs);
        void AddLichSu(HRC1_LichSu ls);
        Task SaveChangesAsync();
    }

    public class HRC1_BBGNRepository : IHRC1_BBGNRepository
    {
        private readonly ProductFormContext _ctx;
        private readonly ProductDataMasterDbContext _masterCtx;

        public HRC1_BBGNRepository(ProductFormContext ctx, ProductDataMasterDbContext masterCtx)
        {
            _ctx = ctx;
            _masterCtx = masterCtx;
        }

        public Task<BmPhieu?> GetBmPhieuAsync(Guid idPhieu) =>
            _ctx.BmPhieus.FirstOrDefaultAsync(p => p.Idphieu == idPhieu);

        public Task<List<BmPhieu>> GetBmPhieusByIdsAsync(IEnumerable<Guid> ids) =>
            _ctx.BmPhieus.Where(p => ids.Contains(p.Idphieu)).ToListAsync();

        public Task<BmPhieu?> GetPhieuDucAsync(DateOnly ngay, int ca, int idMayDuc) =>
            _ctx.BmPhieus.FirstOrDefaultAsync(p =>
                p.NgaySX == ngay && p.Ca == ca &&
                p.MaBm == "HRC1_BBGN_ThepLong" && p.Scope == idMayDuc);

        public Task<HRC1_MeThep?> GetMeByIdAsync(int meId) =>
            _ctx.HRC1_MeTheps.FindAsync(meId).AsTask();

        public Task<HRC1_MeThep?> GetMeByMaMeAsync(string maMe) =>
            _ctx.HRC1_MeTheps.FirstOrDefaultAsync(m => m.MaMe == maMe);

        public Task<List<HRC1_MeThep>> SearchMeThepAsync(string q, int limit) =>
            _ctx.HRC1_MeTheps
                .Where(m => m.MaMe != null && m.MaMe.Contains(q))
                .OrderByDescending(m => m.NgayTao)
                .Take(limit)
                .ToListAsync();

        public Task<HRC1_MePhanCong?> GetMePhanCongByIdAsync(int id) =>
            _ctx.HRC1_MePhanCongs.FindAsync(id).AsTask();

        public Task<List<HRC1_MePhanCong>> GetMePhanCongsByPhieuAsync(Guid idPhieu, string congDoan, int? scopePhieu = null) =>
            _ctx.HRC1_MePhanCongs
                .Where(pc => pc.IdPhieu == idPhieu && pc.CongDoan == congDoan
                          && (scopePhieu == null || pc.ScopePhieu == scopePhieu))
                .OrderBy(pc => pc.ThuTuTL)
                .ToListAsync();

        public Task<List<HRC1_MePhanCong>> GetAllMePhanCongsByMeIdAsync(int meId) =>
            _ctx.HRC1_MePhanCongs.Where(pc => pc.MeId == meId).ToListAsync();

        public Task<List<HRC1_MeThep>> GetMeThepsByIdsAsync(IEnumerable<int> meIds) =>
            _ctx.HRC1_MeTheps.Where(m => meIds.Contains(m.Id)).ToListAsync();

        // Mẻ thuộc phiếu máy đúc:
        // - tinh_luyen: IdMayDucDich + CaTinhLuyen + date(NgayNhanTL) == ngay
        // - len_thang:  IdMayDucDich + Ca           + date(NgayTao)    == ngay
        public Task<List<HRC1_MeThep>> GetMeThepsByMayDucAsync(DateOnly ngay, int ca, int idMayDuc)
        {
            var ngayStart = ngay.ToDateTime(TimeOnly.MinValue);
            var ngayEnd   = ngay.AddDays(1).ToDateTime(TimeOnly.MinValue);

            return _ctx.HRC1_MeTheps
                .Where(m => m.IdMayDucDich == idMayDuc
                         && (
                             (m.NgayNhanTL.HasValue
                                 && m.CaTinhLuyen == ca
                                 && m.NgayNhanTL.Value >= ngayStart && m.NgayNhanTL.Value < ngayEnd)
                             ||
                             (m.DichChuyen == "len_thang"
                                 && m.Ca == ca
                                 && m.NgayTao >= ngayStart && m.NgayTao < ngayEnd)
                         ))
                .OrderBy(m => m.NgayNhanTL.HasValue ? m.NgayNhanTL : m.NgayTao)
                .ToListAsync();
        }

        // Mẻ chờ TL nhận: lò đã xác nhận, chỉ định tinh_luyen, TL chưa nhận
        public Task<List<HRC1_MeThep>> GetChoNhanAsync() =>
            _ctx.HRC1_MeTheps
                .Where(m => m.TrangThaiLo == 1
                         && m.DichChuyen == "tinh_luyen"
                         && (m.TrangThaiTL == null || m.TrangThaiTL == 0))
                .OrderBy(m => m.NgayTao)
                .ToListAsync();

        // Trả về mẻ chờ TL nhận (loại len_thang), order by NgayTao DESC, có phân trang
        public async Task<HRC1_PagedResult<HRC1_ChoNhanMeVm>> GetMeChoNhanPagedAsync(
            DateOnly? tuNgay, DateOnly? denNgay, int? ca, string? maMe, string? thungSo, int? loSo, int page, int pageSize)
        {
            var q = _ctx.HRC1_MeTheps.AsQueryable();

            // Loại mẻ lên thẳng — không cần TL nhận
            q = q.Where(m => m.DichChuyen != "len_thang");

            if (tuNgay.HasValue)
                q = q.Where(m => m.NgayTao >= tuNgay.Value.ToDateTime(TimeOnly.MinValue));
            if (denNgay.HasValue)
                q = q.Where(m => m.NgayTao < denNgay.Value.AddDays(1).ToDateTime(TimeOnly.MinValue));
            if (ca == 1)
                q = q.Where(m => m.NgayTao.Hour >= 6 && m.NgayTao.Hour < 18);
            else if (ca == 2)
                q = q.Where(m => m.NgayTao.Hour >= 18 || m.NgayTao.Hour < 6);
            if (!string.IsNullOrEmpty(maMe))
                q = q.Where(m => m.MaMe != null && m.MaMe.Contains(maMe));
            if (!string.IsNullOrEmpty(thungSo))
                q = q.Where(m => m.ThungSo != null && m.ThungSo.Contains(thungSo));
            if (loSo.HasValue)
                q = q.Where(m => m.LoSo == loSo);

            var total = await q.CountAsync();
            // var raw = await q
            //     .OrderByDescending(m => m.NgayTao)
            //     .Skip((page - 1) * pageSize)
            //     .Take(pageSize)
            //     .Select(m => new { m.Id, m.MaMe, m.ThungSo, m.LoSo, m.ThoiGian, m.KlThepLong, m.TLDichSo, m.DichChuyen, m.TrangThaiTL, m.CapNhatBoi, m.NgayTao, m.NgayNhanTL, m.TenMayDuc })
            //     .ToListAsync();
            var raw = await (
                        from m in q
                        join md in _ctx.MayDucs
                            on m.IdMayDucDich equals md.Id into mdGroup
                        from md in mdGroup.DefaultIfEmpty()
                        orderby m.NgayTao descending
                        select new
                        {
                            m.Id,
                            m.MaMe,
                            m.ThungSo,
                            m.LoSo,
                            m.ThoiGian,
                            m.KlThepLong,
                            m.TLDichSo,
                            m.DichChuyen,
                            m.TrangThaiTL,
                            m.CapNhatBoi,
                            m.NgayTao,
                            m.NgayNhanTL,
                            TenMayDuc = md != null ? md.TenMayDuc : null
                        }
                    )
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

            var meIds = raw.Select(m => m.Id).ToList();

            // Tra cứu họ tên người nhận từ master DB
            var userIds = raw.Where(m => m.CapNhatBoi.HasValue && m.TrangThaiTL >= 1)
                             .Select(m => m.CapNhatBoi!.Value).Distinct().ToList();
            var names = userIds.Count > 0
                ? await _masterCtx.Tbl_TaiKhoan
                    .Where(t => userIds.Contains(t.ID_TaiKhoan))
                    .ToDictionaryAsync(t => t.ID_TaiKhoan, t => t.HoVaTen)
                : new Dictionary<int, string?>();

            // Tra cứu TL scope đã nhận mẻ — đọc ScopePhieu trực tiếp từ MePhanCong
            var tlScopes = meIds.Count > 0
                ? await _ctx.HRC1_MePhanCongs
                    .Where(pc => meIds.Contains(pc.MeId) && pc.CongDoan == "tinh_luyen" && pc.ThuTuTL == null)
                    .ToDictionaryAsync(pc => pc.MeId, pc => pc.ScopePhieu)
                : new Dictionary<int, int?>();

            var items = raw.Select(m => new HRC1_ChoNhanMeVm
            {
                MeId            = m.Id,
                MaMe            = m.MaMe,
                ThungSo         = m.ThungSo,
                LoSo            = m.LoSo,
                ThoiGian        = m.ThoiGian,
                KlThepLong      = m.KlThepLong,
                TLDichSo        = m.TLDichSo,
                DichChuyen      = m.DichChuyen,
                TrangThaiTL     = m.TrangThaiTL,
                SoTinhLuyenNhan = (m.TrangThaiTL >= 1) && tlScopes.TryGetValue(m.Id, out var sc) ? sc : null,
                TenNguoiNhan    = (m.TrangThaiTL >= 1) && m.CapNhatBoi.HasValue && names.TryGetValue(m.CapNhatBoi.Value, out var n) ? n : null,
                NgayTao         = m.NgayTao,
                NgayNhanTL      = m.NgayNhanTL,
                TenMayDuc       = m.TenMayDuc
            }).ToList();

            return new HRC1_PagedResult<HRC1_ChoNhanMeVm> { Items = items, Total = total };
        }

        public Task<List<MayDuc>> GetMayDucsHRC1Async() =>
            _ctx.MayDucs.Where(m => m.NhaMay == 1).OrderBy(m => m.Id).ToListAsync();

        public async Task<int> GetMaxThuTuTLAsync(int meId) =>
            await _ctx.HRC1_MePhanCongs
                .Where(pc => pc.MeId == meId && pc.CongDoan == "tinh_luyen" && pc.ThuTuTL != null)
                .MaxAsync(pc => (int?)pc.ThuTuTL) ?? 0;

        public Task<bool> ExistsMePhanCongAsync(int meId, string congDoan) =>
            _ctx.HRC1_MePhanCongs.AnyAsync(pc => pc.MeId == meId && pc.CongDoan == congDoan);

        public Task<bool> ExistsMePhanCongInPhieuAsync(int meId, Guid idPhieu) =>
            _ctx.HRC1_MePhanCongs.AnyAsync(pc => pc.MeId == meId && pc.IdPhieu == idPhieu && pc.CongDoan == "tinh_luyen");

        // Tất cả phiếu TL đã có mẻ với MaMe này (toàn hệ thống) — dùng để check trùng
        public async Task<List<HRC1_TrungMeInfo>> GetAllTinhLuyenPhieuByMaMeAsync(string maMe)
        {
            var raw = await (
                from m in _ctx.HRC1_MeTheps
                where m.MaMe == maMe
                join pc in _ctx.HRC1_MePhanCongs on m.Id equals pc.MeId
                where pc.CongDoan == "tinh_luyen"
                join p in _ctx.BmPhieus on pc.IdPhieu equals p.Idphieu
                select new { p.SoPhieu, p.Idphieu, pc.ScopePhieu }
            ).ToListAsync();

            return raw
                .DistinctBy(x => x.Idphieu)
                .Select(x => new HRC1_TrungMeInfo
                {
                    SoPhieu = x.SoPhieu ?? x.Idphieu.ToString(),
                    TenTinhLuyen = x.ScopePhieu.HasValue ? $"Tinh luyện {x.ScopePhieu}" : "Tinh luyện"
                })
                .ToList();
        }

        // Tất cả HRC1_MeThep có MaMe == maMe VÀ đang có ít nhất 1 MePhanCong tinh_luyen
        public Task<List<HRC1_MeThep>> GetActiveTLMesByMaMeAsync(string maMe) =>
            _ctx.HRC1_MeTheps
                .Where(m => m.MaMe == maMe &&
                       _ctx.HRC1_MePhanCongs.Any(pc => pc.MeId == m.Id && pc.CongDoan == "tinh_luyen"))
                .ToListAsync();

        // Mẻ "active conflict" cho IsTrungMeThoi: TL đã nhận (TrangThaiTL >= 1) HOẶC lên thẳng máy đúc
        public Task<List<HRC1_MeThep>> GetActiveConflictMesByMaMeAsync(string maMe) =>
            _ctx.HRC1_MeTheps
                .Where(m => m.MaMe == maMe && (m.TrangThaiTL >= 1 || m.DichChuyen == "len_thang"))
                .ToListAsync();

        // Mẻ đang chỉ định lên thẳng máy đúc (để check trùng khi thêm mẻ tay)
        public Task<List<HRC1_MeThep>> GetLenThangMesByMaMeAsync(string maMe) =>
            _ctx.HRC1_MeTheps
                .Where(m => m.MaMe == maMe && m.DichChuyen == "len_thang")
                .ToListAsync();

        // Tất cả MePhanCong tinh_luyen của 1 mẻ trong 1 phiếu (dùng khi hủy nhận)
        public Task<List<HRC1_MePhanCong>> GetTLPhanCongsByMePhieuAsync(int meId, Guid idPhieu, int? scopePhieu = null) =>
            _ctx.HRC1_MePhanCongs
                .Where(pc => pc.MeId == meId && pc.IdPhieu == idPhieu && pc.CongDoan == "tinh_luyen"
                          && (scopePhieu == null || pc.ScopePhieu == scopePhieu))
                .ToListAsync();

        // Trả về {meId → scope TL đã nhận} cho danh sách mẻ (chỉ lấy lần nhận đầu tiên ThuTuTL=null)
        public Task<Dictionary<int, string?>> GetUserNamesByIdsAsync(IEnumerable<int> ids) =>
            _masterCtx.Tbl_TaiKhoan
                .Where(t => ids.Contains(t.ID_TaiKhoan))
                .ToDictionaryAsync(t => t.ID_TaiKhoan, t => (string?)t.HoVaTen);

        // Trả về {meId → ScopePhieu TL đã nhận} — đọc trực tiếp từ MePhanCong, không join BmPhieu
        public Task<Dictionary<int, int?>> GetTLScopesByMeIdsAsync(IEnumerable<int> meIds) =>
            _ctx.HRC1_MePhanCongs
                .Where(pc => meIds.Contains(pc.MeId) && pc.CongDoan == "tinh_luyen" && pc.ThuTuTL == null)
                .ToDictionaryAsync(pc => pc.MeId, pc => pc.ScopePhieu);

        // ── Helper: áp dụng filter trực tiếp lên HRC1_MeThep ──────────────────────
        private IQueryable<HRC1_MeThep> ApplyMeThepFilters(IQueryable<HRC1_MeThep> q, HRC1_ExportQuery f)
        {
            if (f.TuNgay.HasValue)
            {
                var _tuNgay = f.TuNgay.Value.ToDateTime(TimeOnly.MinValue);
                q = q.Where(m => (m.DichChuyen == "len_thang" && m.NgayTao   >= _tuNgay)
                               || (m.DichChuyen != "len_thang" && m.NgayNhanTL >= _tuNgay));
            }
            if (f.DenNgay.HasValue)
            {
                var _denNgay = f.DenNgay.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
                q = q.Where(m => (m.DichChuyen == "len_thang" && m.NgayTao   < _denNgay)
                               || (m.DichChuyen != "len_thang" && m.NgayNhanTL < _denNgay));
            }
            if (f.TuNgayLoThoi.HasValue)
            {
                var _tuNgayLo = f.TuNgayLoThoi.Value.ToDateTime(TimeOnly.MinValue);
                q = q.Where(m => m.NgayTao >= _tuNgayLo);
            }
            if (f.DenNgayLoThoi.HasValue)
            {
                var _denNgayLo = f.DenNgayLoThoi.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
                q = q.Where(m => m.NgayTao < _denNgayLo);
            }
            if (!string.IsNullOrEmpty(f.MaMe))
                q = q.Where(m => m.MaMe != null && m.MaMe.Contains(f.MaMe));
            if (f.LoSo.HasValue)
                q = q.Where(m => m.LoSo == f.LoSo);
            if (f.IdMayDuc.HasValue)
                q = q.Where(m => m.IdMayDucDich == f.IdMayDuc);
            if (f.TrangThaiLo.HasValue)
                q = f.TrangThaiLo.Value == 0
                    ? q.Where(m => m.TrangThaiLo == null || m.TrangThaiLo == 0)
                    : q.Where(m => m.TrangThaiLo == f.TrangThaiLo);
            if (f.TrangThaiTL.HasValue)
                q = f.TrangThaiTL.Value == 0
                    ? q.Where(m => m.TrangThaiTL == null || m.TrangThaiTL == 0)
                    : q.Where(m => m.TrangThaiTL == f.TrangThaiTL);
            if (f.TrangThaiDuc.HasValue)
                q = f.TrangThaiDuc.Value == 0
                    ? q.Where(m => m.TrangThaiDuc == null || m.TrangThaiDuc == 0)
                    : q.Where(m => m.TrangThaiDuc == f.TrangThaiDuc);
            if (!string.IsNullOrEmpty(f.ThungSo))
                q = q.Where(m => m.ThungSo != null && m.ThungSo.Contains(f.ThungSo));
            if (!string.IsNullOrEmpty(f.PhanLoai))
                q = q.Where(m => m.PhanLoai != null && m.PhanLoai.Contains(f.PhanLoai));
            if (f.IsChot.HasValue)
                q = q.Where(m => m.IsChot == f.IsChot);
            if (f.IsManualTL.HasValue)
                q = q.Where(m => m.IsManualTL == f.IsManualTL);
            if (f.ChuaCoNhomPhanLoai == true)
            {
                var hasNhomIds =
                    from m2 in _ctx.HRC1_MeTheps
                    where m2.MacThepBKMIS != null
                    join mt in _ctx.MacTheps on m2.MacThepBKMIS equals mt.TenMacThep
                    where mt.Id_NhomPhanLoaiMacThep.HasValue
                    join nhom in _ctx.NhomPhanLoaiMacTheps on mt.Id_NhomPhanLoaiMacThep!.Value equals nhom.Id
                    select m2.Id;
                q = q.Where(m => !hasNhomIds.Contains(m.Id));
            }
            if (f.IdNhomPhanLoai.HasValue)
            {
                var nhomMeIds =
                    from m2 in _ctx.HRC1_MeTheps
                    where m2.MacThepBKMIS != null
                    join mt in _ctx.MacTheps on m2.MacThepBKMIS equals mt.TenMacThep
                    where mt.Id_NhomPhanLoaiMacThep == f.IdNhomPhanLoai
                    select m2.Id;
                q = q.Where(m => nhomMeIds.Contains(m.Id));
            }
            return q;
        }

        // ── Helper: load TL/phieu + mayDuc + userNames + nhomPhanLoai + chuyenVe + tlScope cho danh sách mẻ
        private async Task<(
            Dictionary<int, string?> MayDucs,
            Dictionary<int, string?> UserNames,
            Dictionary<string, string> NhomPhanLoai,
            Dictionary<int, (string? maMe, string? tenMayDuc)> ChuyenVeDict,
            Dictionary<int, int?> TlScopeDict)>
            LoadMeThepLookupAsync(List<HRC1_MeThep> mes)
        {
            var mayDucs = await _ctx.MayDucs.ToDictionaryAsync(m => m.Id, m => (string?)m.TenMayDuc);

            var auditIds = mes
                .SelectMany(m => new[] { m.CapNhatBoi, m.CapNhatBoiTL, m.CapNhatBoiDuc })
                .Where(id => id.HasValue).Select(id => id!.Value)
                .Distinct().ToList();

            var userNames = auditIds.Count > 0
                ? await _masterCtx.Tbl_TaiKhoan
                    .Where(t => auditIds.Contains(t.ID_TaiKhoan))
                    .ToDictionaryAsync(t => t.ID_TaiKhoan, t => (string?)t.HoVaTen)
                : new Dictionary<int, string?>();

            // Nhóm phân loại mác thép: MacThepBKMIS → TenNhom
            var bkmisList = mes.Where(m => m.MacThepBKMIS != null)
                               .Select(m => m.MacThepBKMIS!).Distinct().ToList();
            var nhomDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (bkmisList.Count > 0)
            {
                var nhomData = await (
                    from mt in _ctx.MacTheps
                    where bkmisList.Contains(mt.TenMacThep) && mt.Id_NhomPhanLoaiMacThep.HasValue
                    join nhom in _ctx.NhomPhanLoaiMacTheps on mt.Id_NhomPhanLoaiMacThep!.Value equals nhom.Id
                    select new { mt.TenMacThep, nhom.TenNhom }
                ).ToListAsync();
                foreach (var x in nhomData)
                    nhomDict[x.TenMacThep] = x.TenNhom;
            }

            // ChuyenVe: lấy ChuyenVeMeId từ MePhanCong tinh_luyen (dòng chính, ThuTuTL = null)
            var meIds = mes.Select(m => m.Id).ToList();
            var chuyenVeDict = new Dictionary<int, (string? maMe, string? tenMayDuc)>();
            var tlPcs = await _ctx.HRC1_MePhanCongs
                .Where(pc => pc.CongDoan == "tinh_luyen" && pc.ThuTuTL == null
                          && meIds.Contains(pc.MeId) && pc.ChuyenVeMeId.HasValue)
                .Select(pc => new { pc.MeId, pc.ChuyenVeMeId })
                .ToListAsync();
            if (tlPcs.Count > 0)
            {
                var chuyenVeMeIds = tlPcs.Select(pc => pc.ChuyenVeMeId!.Value).Distinct().ToList();
                var chuyenVeMes = await _ctx.HRC1_MeTheps
                    .Where(m => chuyenVeMeIds.Contains(m.Id))
                    .Select(m => new { m.Id, m.MaMe, m.IdMayDucDich })
                    .ToListAsync();
                var chuyenVeMeById = chuyenVeMes.ToDictionary(m => m.Id);
                foreach (var pc in tlPcs)
                {
                    if (chuyenVeMeById.TryGetValue(pc.ChuyenVeMeId!.Value, out var cvm))
                    {
                        var tenMdc = cvm.IdMayDucDich.HasValue && mayDucs.TryGetValue(cvm.IdMayDucDich.Value, out var tmd) ? tmd : null;
                        chuyenVeDict[pc.MeId] = (cvm.MaMe, tenMdc);
                    }
                }
            }

            // TlScope: meId → ScopePhieu TL đã nhận (dòng chính ThuTuTL = null)
            var tlScopeRaw = await _ctx.HRC1_MePhanCongs
                .Where(pc => pc.CongDoan == "tinh_luyen" && pc.ThuTuTL == null && meIds.Contains(pc.MeId))
                .Select(pc => new { pc.MeId, pc.ScopePhieu })
                .ToListAsync();
            var tlScopeDict = tlScopeRaw
                .GroupBy(x => x.MeId)
                .ToDictionary(g => g.Key, g => (int?)g.First().ScopePhieu);

            return (mayDucs, userNames, nhomDict, chuyenVeDict, tlScopeDict);
        }

        private static HRC1_ExportRow MapToExportRow(
            HRC1_MeThep m,
            Dictionary<int, string?> mayDucs,
            Dictionary<int, string?> userNames,
            Dictionary<string, string> nhomPhanLoai,
            Dictionary<int, (string? maMe, string? tenMayDuc)> chuyenVeDict,
            Dictionary<int, int?> tlScopeDict)
        {
            string? tenMayDuc    = m.IdMayDucDich.HasValue   && mayDucs.TryGetValue(m.IdMayDucDich.Value, out var tn)    ? tn : null;
            string? tenLoThoi    = m.CapNhatBoi.HasValue     && userNames.TryGetValue(m.CapNhatBoi.Value, out var uLo)   ? uLo : null;
            string? tenTinhLuyen = m.CapNhatBoiTL.HasValue   && userNames.TryGetValue(m.CapNhatBoiTL.Value, out var uTL) ? uTL : null;
            string? tenDuc       = m.CapNhatBoiDuc.HasValue  && userNames.TryGetValue(m.CapNhatBoiDuc.Value, out var uD) ? uD : null;
            string? tenNhom      = m.MacThepBKMIS != null    && nhomPhanLoai.TryGetValue(m.MacThepBKMIS, out var nh)     ? nh : null;
            bool isLenThang = m.DichChuyen == "len_thang";
            int? soTLNhan   = tlScopeDict.TryGetValue(m.Id, out var tlSc) ? tlSc : null;
            string? tlLt = isLenThang                   ? "Lên thẳng"
                         : m.DichChuyen == "tinh_luyen" ? "Tinh luyện"
                         : null;
            DateOnly? ngayDuc = isLenThang
                ? DateOnly.FromDateTime(m.NgayTao)
                : (m.NgayNhanTL.HasValue ? DateOnly.FromDateTime(m.NgayNhanTL.Value) : null);
            int? caDuc = isLenThang ? m.Ca : m.CaTinhLuyen;
            chuyenVeDict.TryGetValue(m.Id, out var chuyenVe);
            return new HRC1_ExportRow
            {
                MeId               = m.Id,
                MaMe               = m.MaMe,
                ThungSo            = m.ThungSo,
                ThoiGian           = m.ThoiGian,
                KLLFSauThep        = m.KLLFSauThep,
                KlLan1             = m.KlLan1,
                KlLan2             = m.KlLan2,
                KlLan3             = m.KlLan3,
                KlThepLong         = m.KlThepLong,
                KLThepLongPhanBo   = m.KLThepLongPhanBo,
                GhiChuLo           = m.GhiChuLo,
                GhiChuTL           = m.GhiChuTL,
                GhiChuDuc          = m.GhiChuDuc,
                IsThuNghiem        = m.IsThuNghiem,
                IsManualTL         = m.IsManualTL,
                TenMayDuc          = tenMayDuc,
                MacThep            = m.MacThep,
                PhanLoai           = m.PhanLoai,
                MacThepBKMIS       = m.MacThepBKMIS,
                TinhLuyenLenThang  = tlLt,
                SoTinhLuyenNhan    = soTLNhan,
                IsTrungMeThoi      = m.IsTrungMeThoi,
                TrangThaiLo        = m.TrangThaiLo,
                TrangThaiTL        = m.TrangThaiTL,
                TrangThaiDuc       = m.TrangThaiDuc,
                IsChot             = m.IsChot,
                NgayTao            = m.NgayTao,
                Ca                 = m.Ca,
                Kip                = m.Kip,
                NgayNhanTL         = m.NgayNhanTL,
                CaTinhLuyen        = m.CaTinhLuyen,
                KipTinhLuyen       = m.KipTinhLuyen,
                TenNhomPhanLoai    = tenNhom,
                TenCapNhatBoiLo    = tenLoThoi,
                TenCapNhatBoiTL    = tenTinhLuyen,
                TenCapNhatBoiDuc   = tenDuc,
                ChuyenVeMaMe       = chuyenVe.maMe,
                TenMayDucChuyen    = chuyenVe.tenMayDuc,
                NgayDuc            = ngayDuc,
                CaDuc              = caDuc,
            };
        }

        public async Task<List<HRC1_ExportRow>> GetMeThepsForExportAsync(HRC1_ExportQuery q)
        {
            var meQuery = ApplyMeThepFilters(_ctx.HRC1_MeTheps.AsQueryable(), q);

            if (q.TlSo.HasValue)
            {
                var tlMeIds = _ctx.HRC1_MePhanCongs
                    .Where(pc => pc.CongDoan == "tinh_luyen" && pc.ThuTuTL == null && pc.ScopePhieu == q.TlSo)
                    .Select(pc => pc.MeId);
                meQuery = meQuery.Where(m => tlMeIds.Contains(m.Id));
            }

            var mes = await meQuery.OrderBy(m => m.NgayTao).ToListAsync();
            if (mes.Count == 0) return new List<HRC1_ExportRow>();

            var (mayDucs, userNames, nhomDict, chuyenVeDict, tlScopeDict) = await LoadMeThepLookupAsync(mes);

            if (q.Ca.HasValue)
                mes = mes.Where(m => (m.DichChuyen == "len_thang" ? m.Ca : m.CaTinhLuyen) == q.Ca).ToList();
            if (!string.IsNullOrEmpty(q.Kip))
                mes = mes.Where(m => (m.DichChuyen == "len_thang" ? m.Kip : m.KipTinhLuyen) == q.Kip).ToList();

            return mes.Select(m => MapToExportRow(m, mayDucs, userNames, nhomDict, chuyenVeDict, tlScopeDict)).ToList();
        }

        public async Task<HRC1_ThongKeResult> GetMeThepsPagedAsync(HRC1_ThongKeQuery q)
        {
            var meQuery = ApplyMeThepFilters(_ctx.HRC1_MeTheps.AsQueryable(), q);

            if (q.TlSo.HasValue)
            {
                var tlMeIds = _ctx.HRC1_MePhanCongs
                    .Where(pc => pc.CongDoan == "tinh_luyen" && pc.ThuTuTL == null && pc.ScopePhieu == q.TlSo)
                    .Select(pc => pc.MeId);
                meQuery = meQuery.Where(m => tlMeIds.Contains(m.Id));
            }
            if (q.IsTrungMeThoi.HasValue)
                meQuery = meQuery.Where(m => m.IsTrungMeThoi == q.IsTrungMeThoi);
            if (q.IsManualTL.HasValue)
                meQuery = meQuery.Where(m => m.IsManualTL == q.IsManualTL);
            if (q.IsChuyenMe == true)
            {
                var chuyenMeIds = _ctx.HRC1_MePhanCongs
                    .Where(pc => pc.CongDoan == "tinh_luyen" && pc.ThuTuTL == null
                              && pc.ChuyenVeMeId.HasValue && pc.ChuyenVeMeId != pc.MeId)
                    .Select(pc => pc.MeId);
                meQuery = meQuery.Where(m => chuyenMeIds.Contains(m.Id));
            }

            // ── Aggregate trước khi lọc Ca/Kíp (để count/sum chính xác) ──
            // Note: Ca/Kíp lọc in-memory nên count bên dưới sẽ xử lý sau khi load
            var allMes = await meQuery
                .OrderByDescending(m => m.NgayTao)
                .ThenByDescending(m => m.Ca.HasValue ? m.Ca.Value : 0)
                .ThenBy(m => m.IdMayDucDich)
                .ThenBy(m => m.MaMe)
                .ToListAsync();
            if (allMes.Count == 0)
                return new HRC1_ThongKeResult { Page = q.Page, PageSize = q.PageSize };

            var (mayDucs, userNames, nhomDict, chuyenVeDict, tlScopeDict) = await LoadMeThepLookupAsync(allMes);

            // ── Lọc Ca/Kíp in-memory ──
            if (q.Ca.HasValue)
                allMes = allMes.Where(m => (m.DichChuyen == "len_thang" ? m.Ca : m.CaTinhLuyen) == q.Ca).ToList();
            if (!string.IsNullOrEmpty(q.Kip))
                allMes = allMes.Where(m => (m.DichChuyen == "len_thang" ? m.Kip : m.KipTinhLuyen) == q.Kip).ToList();

            int total = allMes.Count;
            decimal? totalKl       = allMes.Sum(m => m.KlThepLong);
            decimal? totalKlPhanBo = allMes.Sum(m => m.KLThepLongPhanBo);

            // ── KlThepLongChot: mẻ chuyển đi → 0; mẻ đích → ownKl + sum(srcKl từ global) ──
            var allMeIds = allMes.Select(m => m.Id).ToList();

            // Mẻ trong tập lọc đã bị chuyển sang mẻ khác (ChuyenVeMeId != self)
            var srcPcsInSet = await _ctx.HRC1_MePhanCongs
                .Where(pc => pc.CongDoan == "tinh_luyen" && pc.ThuTuTL == null
                          && allMeIds.Contains(pc.MeId)
                          && pc.ChuyenVeMeId.HasValue
                          && pc.ChuyenVeMeId != pc.MeId)
                .Select(pc => new { pc.MeId, ChuyenVeMeId = pc.ChuyenVeMeId!.Value })
                .ToListAsync();
            var sourceSet = srcPcsInSet.Select(pc => pc.MeId).ToHashSet();

            // Với mẻ không phải source, tìm tất cả mẻ toàn cục (ngoài filter) đang trỏ vào
            var nonSourceMeIds = allMeIds.Where(id => !sourceSet.Contains(id)).ToList();
            var destToSrcKlSum = new Dictionary<int, decimal>();
            if (nonSourceMeIds.Count > 0)
            {
                var incomingPcs = await _ctx.HRC1_MePhanCongs
                    .Where(pc => pc.CongDoan == "tinh_luyen" && pc.ThuTuTL == null
                              && pc.ChuyenVeMeId.HasValue
                              && nonSourceMeIds.Contains(pc.ChuyenVeMeId!.Value)
                              && pc.ChuyenVeMeId != pc.MeId)
                    .Select(pc => new { pc.MeId, ChuyenVeMeId = pc.ChuyenVeMeId!.Value })
                    .ToListAsync();

                if (incomingPcs.Count > 0)
                {
                    var srcMeIds = incomingPcs.Select(pc => pc.MeId).Distinct().ToList();
                    var srcKlById = await _ctx.HRC1_MeTheps
                        .Where(m => srcMeIds.Contains(m.Id))
                        .ToDictionaryAsync(m => m.Id, m => m.KlThepLong ?? 0m);

                    foreach (var pc in incomingPcs)
                    {
                        destToSrcKlSum.TryAdd(pc.ChuyenVeMeId, 0m);
                        if (srcKlById.TryGetValue(pc.MeId, out var kl))
                            destToSrcKlSum[pc.ChuyenVeMeId] += kl;
                    }
                }
            }

            decimal? ComputeKlChot(HRC1_MeThep m)
            {
                if (sourceSet.Contains(m.Id)) return 0m;
                var incoming = destToSrcKlSum.TryGetValue(m.Id, out var s) ? s : 0m;
                if (m.KlThepLong == null && incoming == 0m) return null;
                return (m.KlThepLong ?? 0m) + incoming;
            }

            decimal totalKlChot = allMes.Sum(m => ComputeKlChot(m) ?? 0m);

            var paged = allMes
                .Skip((q.Page - 1) * q.PageSize)
                .Take(q.PageSize)
                .ToList();

            return new HRC1_ThongKeResult
            {
                Items = paged.Select(m =>
                {
                    var row = MapToExportRow(m, mayDucs, userNames, nhomDict, chuyenVeDict, tlScopeDict);
                    row.KlThepLongChot = ComputeKlChot(m);
                    return row;
                }).ToList(),
                TotalRecords            = total,
                TotalKlThepLong         = totalKl,
                TotalKlThepLongChot     = (decimal?)totalKlChot,
                TotalKlThepLongPhanBo   = totalKlPhanBo,
                Page                    = q.Page,
                PageSize                = q.PageSize,
            };
        }

        public async Task<HRC1_TongHopResult> GetTongHopAsync(HRC1_ThongKeQuery q)
        {
            var meQuery = ApplyMeThepFilters(_ctx.HRC1_MeTheps.AsQueryable(), q);

            if (q.TlSo.HasValue)
            {
                var tlMeIds = _ctx.HRC1_MePhanCongs
                    .Where(pc => pc.CongDoan == "tinh_luyen" && pc.ThuTuTL == null && pc.ScopePhieu == q.TlSo)
                    .Select(pc => pc.MeId);
                meQuery = meQuery.Where(m => tlMeIds.Contains(m.Id));
            }
            if (q.IsTrungMeThoi.HasValue)
                meQuery = meQuery.Where(m => m.IsTrungMeThoi == q.IsTrungMeThoi);
            if (q.IsManualTL.HasValue)
                meQuery = meQuery.Where(m => m.IsManualTL == q.IsManualTL);
            if (q.Ca.HasValue)
                meQuery = meQuery.Where(m => (m.DichChuyen == "len_thang" ? m.Ca : m.CaTinhLuyen) == q.Ca);
            if (!string.IsNullOrEmpty(q.Kip))
                meQuery = meQuery.Where(m => (m.DichChuyen == "len_thang" ? m.Kip : m.KipTinhLuyen) == q.Kip);
            // ── 1. Phân loại ──────────────────────────────────────────────────
            var phanLoai = await meQuery
                .Where(m => !string.IsNullOrEmpty(m.PhanLoai))
                .GroupBy(m => m.PhanLoai!)
                .Select(g => new HRC1_TongHopItem { Label = g.Key, KlThepLong = g.Sum(m => m.KlThepLong ?? 0) })
                .OrderBy(x => x.Label)
                .ToListAsync();

            // ── 2. Ca ─────────────────────────────────────────────────────────
            var caRaw = await meQuery
                .Where(m => (m.DichChuyen == "len_thang" ? m.Ca : m.CaTinhLuyen).HasValue)
                .GroupBy(m => (m.DichChuyen == "len_thang" ? m.Ca : m.CaTinhLuyen)!.Value)
                .Select(g => new { Ca = g.Key, KlThepLong = g.Sum(m => m.KlThepLong ?? 0) })
                .OrderBy(x => x.Ca)
                .ToListAsync();
            var ca = caRaw.Select(x => new HRC1_TongHopItem
            {
                Label      = x.Ca == 1 ? "Ca ngày" : "Ca đêm",
                KlThepLong = x.KlThepLong,
            }).ToList();

            // ── 3. Kíp ────────────────────────────────────────────────────────
            var kip = await meQuery
                .Where(m => (m.DichChuyen == "len_thang" ? m.Kip : m.KipTinhLuyen) != null
                         && (m.DichChuyen == "len_thang" ? m.Kip : m.KipTinhLuyen) != "")
                .GroupBy(m => (m.DichChuyen == "len_thang" ? m.Kip : m.KipTinhLuyen)!)
                .OrderBy(g => g.Key)
                .Select(g => new HRC1_TongHopItem { Label = g.Key, KlThepLong = g.Sum(m => m.KlThepLong ?? 0) })
                .ToListAsync();

            // ── 4. TL / Lên thẳng ─────────────────────────────────────────────
            var tlLt = await meQuery
                .GroupBy(m => m.DichChuyen == "tinh_luyen" ? "Tinh luyện"
                            : m.DichChuyen == "len_thang"  ? "Lên thẳng"
                            : "Chưa xác định")
                .Select(g => new HRC1_TongHopItem { Label = g.Key, KlThepLong = g.Sum(m => m.KlThepLong ?? 0) })
                .OrderBy(x => x.Label)
                .ToListAsync();

            // ── 5. Đúc vuông (DV) — join MayDuc ──────────────────────────────
            var ducVuong = (await (
                from m in meQuery
                where m.IdMayDucDich.HasValue
                join md in _ctx.MayDucs on m.IdMayDucDich!.Value equals md.Id
                where md.LoaiMayDuc == "DV"
                group m by md.TenMayDuc into g
                orderby g.Key
                select new { Key = g.Key, KlThepLong = g.Sum(m => m.KlThepLong ?? 0) }
            ).ToListAsync())
            .Select(x => new HRC1_TongHopItem { Label = x.Key ?? "Không xác định", KlThepLong = x.KlThepLong })
            .ToList();

            // ── 6. Đúc tấm (DT) — join MayDuc ────────────────────────────────
            var ducTam = (await (
                from m in meQuery
                where m.IdMayDucDich.HasValue
                join md in _ctx.MayDucs on m.IdMayDucDich!.Value equals md.Id
                where md.LoaiMayDuc == "DT"
                group m by md.TenMayDuc into g
                orderby g.Key
                select new { Key = g.Key, KlThepLong = g.Sum(m => m.KlThepLong ?? 0) }
            ).ToListAsync())
            .Select(x => new HRC1_TongHopItem { Label = x.Key ?? "Không xác định", KlThepLong = x.KlThepLong })
            .ToList();

            // ── 7. Nhóm phân loại mác thép
            //      MacThepBKMIS → MacThep.TenMacThep → Id_NhomPhanLoaiMacThep → NhomPhanLoaiMacThep.TenNhom
            var nhomPhanLoai = (await (
                from m in meQuery
                where m.MacThepBKMIS != null
                join mt in _ctx.MacTheps on m.MacThepBKMIS equals mt.TenMacThep
                where mt.Id_NhomPhanLoaiMacThep.HasValue
                join nhom in _ctx.NhomPhanLoaiMacTheps on mt.Id_NhomPhanLoaiMacThep!.Value equals nhom.Id
                group m by nhom.TenNhom into g
                orderby g.Key
                select new { Key = g.Key, KlThepLong = g.Sum(m => m.KlThepLong ?? 0) }
            ).ToListAsync())
            .Select(x => new HRC1_TongHopItem { Label = x.Key ?? "Không xác định", KlThepLong = x.KlThepLong })
            .ToList();

            return new HRC1_TongHopResult
            {
                PhanLoai            = phanLoai,
                Ca                  = ca,
                Kip                 = kip,
                TinhLuyenLenThang   = tlLt,
                DucVuong            = ducVuong,
                DucTam              = ducTam,
                NhomPhanLoaiMacThep = nhomPhanLoai,
            };
        }

        public Task<List<int>> GetActiveDucPhieuScopesAsync(DateOnly ngay, int ca) =>
            _ctx.BmPhieus
                .Where(p => p.MaBm == "HRC1_BBGN_ThepLong"
                         && p.NgaySX == ngay
                         && p.Ca == ca
                         && p.TinhTrang != 5
                         && p.Scope.HasValue)
                .Select(p => p.Scope!.Value)
                .ToListAsync();

        public void AddMeThep(HRC1_MeThep me) => _ctx.HRC1_MeTheps.Add(me);

        public void RemoveMeThep(HRC1_MeThep me) => _ctx.HRC1_MeTheps.Remove(me);

        public void AddMePhanCong(HRC1_MePhanCong pc) => _ctx.HRC1_MePhanCongs.Add(pc);

        public void RemoveMePhanCongs(IEnumerable<HRC1_MePhanCong> pcs) =>
            _ctx.HRC1_MePhanCongs.RemoveRange(pcs);

        public void AddLichSu(HRC1_LichSu ls) => _ctx.HRC1_LichSus.Add(ls);

        public Task SaveChangesAsync() => _ctx.SaveChangesAsync();
    }
}

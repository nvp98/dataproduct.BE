using dataproduct.api.Models;
using dataproduct.api.Models.MasterData;
using dataproduct.api.ResponseModels;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public interface IHRC1_BBGNRepository
    {
        Task<BmPhieu?> GetBmPhieuAsync(Guid idPhieu);
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

        // Mẻ thuộc phiếu máy đúc: lọc theo IdMayDucDich + khoảng ca
        // - tinh_luyen: dùng NgayNhanTL (ngày TL nhận mẻ, không phải ngày lò thổi tạo)
        // - len_thang:  dùng NgayTao    (ngày lò thổi sync, vì không qua TL)
        // Ca 1 (ngày): 06:00–18:00; Ca 2 (đêm): 18:00–06:00 hôm sau
        public Task<List<HRC1_MeThep>> GetMeThepsByMayDucAsync(DateOnly ngay, int ca, int idMayDuc)
        {
            var start = ca == 1
                ? ngay.ToDateTime(new TimeOnly(6, 0))
                : ngay.ToDateTime(new TimeOnly(18, 0));
            var end = ca == 1
                ? ngay.ToDateTime(new TimeOnly(18, 0))
                : ngay.AddDays(1).ToDateTime(new TimeOnly(6, 0));

            return _ctx.HRC1_MeTheps
                .Where(m => m.IdMayDucDich == idMayDuc
                         && (
                             (m.NgayNhanTL.HasValue
                                 && m.NgayNhanTL.Value >= start && m.NgayNhanTL.Value < end)
                             ||
                             (m.DichChuyen == "len_thang"
                                 && m.NgayTao >= start && m.NgayTao < end)
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

        // Trả về toàn bộ mẻ theo filter, order by NgayTao DESC, có phân trang
        public async Task<HRC1_PagedResult<HRC1_ChoNhanMeVm>> GetMeChoNhanPagedAsync(
            DateOnly? tuNgay, DateOnly? denNgay, int? ca, string? maMe, string? thungSo, int? loSo, int page, int pageSize)
        {
            var q = _ctx.HRC1_MeTheps.AsQueryable();

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
            var raw = await q
                .OrderByDescending(m => m.NgayTao)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new { m.Id, m.MaMe, m.ThungSo, m.LoSo, m.ThoiGian, m.KlThepLong, m.TLDichSo, m.DichChuyen, m.TrangThaiTL, m.CapNhatBoi, m.NgayTao, m.NgayNhanTL })
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

        public void AddMeThep(HRC1_MeThep me) => _ctx.HRC1_MeTheps.Add(me);

        public void RemoveMeThep(HRC1_MeThep me) => _ctx.HRC1_MeTheps.Remove(me);

        public void AddMePhanCong(HRC1_MePhanCong pc) => _ctx.HRC1_MePhanCongs.Add(pc);

        public void RemoveMePhanCongs(IEnumerable<HRC1_MePhanCong> pcs) =>
            _ctx.HRC1_MePhanCongs.RemoveRange(pcs);

        public void AddLichSu(HRC1_LichSu ls) => _ctx.HRC1_LichSus.Add(ls);

        public Task SaveChangesAsync() => _ctx.SaveChangesAsync();
    }
}

using System.Text.Json;
using dataproduct.api.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public class DongLucRepository : IDongLucRepository
    {
        private readonly ProductFormContext _context;

        public DongLucRepository(ProductFormContext context)
        {
            _context = context;
        }

        // ── Tag ────────────────────────────────────────────────────────────────

        public async Task<List<TagID_DongLuc>> GetTagsAsync(string? maXuong, byte? tinhTrang, string? searchKey)
        {
            var query = _context.TagID_DongLucs.Where(x => !x.IsDelete);

            if (!string.IsNullOrWhiteSpace(maXuong))
                query = query.Where(x => x.MaXuong == maXuong);
            if (tinhTrang.HasValue)
                query = query.Where(x => x.TinhTrang == tinhTrang.Value);
            if (!string.IsNullOrWhiteSpace(searchKey))
                query = query.Where(x => x.TagIDEMS.Contains(searchKey)
                                      || x.MaHeader.Contains(searchKey)
                                      || (x.YNghia != null && x.YNghia.Contains(searchKey)));

            return await query.OrderBy(x => x.MaXuong).ThenBy(x => x.ThuTu).ThenBy(x => x.TagIDEMS).ToListAsync();
        }

        public Task<TagID_DongLuc?> GetTagByIdAsync(int id)
            => _context.TagID_DongLucs.FirstOrDefaultAsync(x => x.Id == id && !x.IsDelete);

        public Task<bool> ExistsTagAsync(string tagIdEms, int? excludeId = null)
        {
            var query = _context.TagID_DongLucs.Where(x => !x.IsDelete && x.TagIDEMS == tagIdEms);
            if (excludeId.HasValue)
                query = query.Where(x => x.Id != excludeId.Value);
            return query.AnyAsync();
        }

        public async Task AddTagAsync(TagID_DongLuc entity)
        {
            _context.TagID_DongLucs.Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateTagAsync(TagID_DongLuc entity)
        {
            _context.TagID_DongLucs.Update(entity);
            await _context.SaveChangesAsync();
        }

        // ── Dữ liệu ────────────────────────────────────────────────────────────

        private IQueryable<DuLieu_DongLuc> QueryDuLieu(DateOnly tuNgay, DateOnly denNgay, byte? caSX, string? maXuong, string? maHeader)
        {
            var query = _context.DuLieu_DongLucs.AsNoTracking()
                .Where(x => x.Ngay >= tuNgay && x.Ngay <= denNgay);

            if (caSX.HasValue)
                query = query.Where(x => x.CaSX == caSX.Value);
            if (!string.IsNullOrWhiteSpace(maXuong))
                query = query.Where(x => x.MaXuong == maXuong);
            if (!string.IsNullOrWhiteSpace(maHeader))
                query = query.Where(x => x.MaHeader == maHeader);

            return query;
        }

        public async Task<(List<DuLieu_DongLuc> Rows, int Total)> SearchDuLieuAsync(
            DateOnly tuNgay, DateOnly denNgay, byte? caSX, string? maXuong, string? maHeader, int page, int pageSize)
        {
            var query = QueryDuLieu(tuNgay, denNgay, caSX, maXuong, maHeader);
            var total = await query.CountAsync();
            var rows = await query
                .OrderBy(x => x.Ngay).ThenBy(x => x.CaSX).ThenBy(x => x.MaXuong).ThenBy(x => x.MaHeader)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (rows, total);
        }

        public Task<List<DuLieu_DongLuc>> GetDuLieuAsync(DateOnly tuNgay, DateOnly denNgay, byte? caSX, string? maXuong, string? maHeader)
            => QueryDuLieu(tuNgay, denNgay, caSX, maXuong, maHeader)
                .OrderBy(x => x.Ngay).ThenBy(x => x.CaSX).ThenBy(x => x.MaXuong).ThenBy(x => x.MaHeader)
                .ToListAsync();

        /// <summary>
        /// Upsert hàng loạt theo (Ngay, CaSX, TagID) bằng MERGE + OPENJSON (1 câu lệnh, không lặp từng dòng).
        /// Trả về số dòng bị ảnh hưởng (thêm + cập nhật).
        /// </summary>
        public async Task<int> UpsertDuLieuAsync(IReadOnlyCollection<DuLieu_DongLuc> rows)
        {
            if (rows.Count == 0) return 0;

            var json = JsonSerializer.Serialize(rows.Select(r => new
            {
                r.Ngay,
                r.CaSX,
                r.MaHeader,
                r.TagID,
                r.GiaTri,
                r.MaXuong,
            }));

            const string sql = @"
MERGE DuLieu_DongLuc AS t
USING (
    SELECT Ngay, CaSX, MaHeader, TagID, GiaTri, MaXuong
    FROM OPENJSON(@json) WITH (
        Ngay     DATE           '$.Ngay',
        CaSX     TINYINT        '$.CaSX',
        MaHeader NVARCHAR(50)   '$.MaHeader',
        TagID    NVARCHAR(50)   '$.TagID',
        GiaTri   DECIMAL(18,3)  '$.GiaTri',
        MaXuong  NVARCHAR(20)   '$.MaXuong'
    )
) AS s
ON t.Ngay = s.Ngay AND t.CaSX = s.CaSX AND t.TagID = s.TagID
WHEN MATCHED THEN
    UPDATE SET t.GiaTri = s.GiaTri, t.MaHeader = s.MaHeader, t.MaXuong = s.MaXuong, t.NgayCapNhat = GETDATE()
WHEN NOT MATCHED THEN
    INSERT (Ngay, CaSX, MaHeader, TagID, GiaTri, NgayCapNhat, MaXuong)
    VALUES (s.Ngay, s.CaSX, s.MaHeader, s.TagID, s.GiaTri, GETDATE(), s.MaXuong);";

            return await _context.Database.ExecuteSqlRawAsync(sql, new SqlParameter("@json", json));
        }

        // ── Nhật ký đồng bộ ───────────────────────────────────────────────────

        public async Task AddSyncLogAsync(DongLuc_SyncLog log)
        {
            _context.DongLuc_SyncLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateSyncLogAsync(DongLuc_SyncLog log)
        {
            _context.DongLuc_SyncLogs.Update(log);
            await _context.SaveChangesAsync();
        }

        public Task<DongLuc_SyncLog?> GetSyncLogGanNhatAsync()
            => _context.DongLuc_SyncLogs.AsNoTracking().OrderByDescending(x => x.Id).FirstOrDefaultAsync();
    }
}

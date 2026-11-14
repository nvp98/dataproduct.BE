using System.Data;
using System.Data.Common;
using System.Text;
using System.Text.Json;
using dataproduct.api.Models;
using dataproduct.api.Services.Kdl;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace dataproduct.api.Repositories
{
    public class KdlRepository : IKdlRepository
    {
        private readonly ProductFormContext _context;

        public KdlRepository(ProductFormContext context)
        {
            _context = context;
        }

        // Cột KDL_DoiTuong được phép nhóm khi tổng hợp (chống SQL injection: chỉ ghép tên trong danh sách này)
        private static readonly HashSet<string> CotNhomHopLe = new(StringComparer.OrdinalIgnoreCase)
            { "NgaySX", "CaSX", "Kip", "MayDuc", "Mac", "KichThuoc", "LenhSanXuat" };

        // ── Cấu hình ───────────────────────────────────────────────────────────

        public Task<List<KDL_Key>> GetKeysAsync(string maModule, bool chiActive = true)
            => _context.KDL_Keys.AsNoTracking()
                .Where(x => x.MaModule == maModule && (!chiActive || x.IsActive))
                .OrderBy(x => x.ThuTu).ThenBy(x => x.MaKey)
                .ToListAsync();

        public Task<KDL_Form?> GetFormAsync(string maForm)
            => _context.KDL_Forms.AsNoTracking().FirstOrDefaultAsync(x => x.MaForm == maForm && x.IsActive);

        public Task<List<KDL_Form>> GetFormsAsync(string? maModule)
            => _context.KDL_Forms.AsNoTracking()
                .Where(x => x.IsActive && (maModule == null || x.MaModule == maModule))
                .OrderBy(x => x.MaModule).ThenBy(x => x.MaForm)
                .ToListAsync();

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default)
            => _context.Database.BeginTransactionAsync(ct);

        // ── Đối tượng ──────────────────────────────────────────────────────────

        public async Task<List<KDL_DoiTuong>> UpsertDoiTuongAsync(string maModule, DateOnly tuNgay, DateOnly denNgay,
            IReadOnlyCollection<KdlDoiTuongVao> rows, bool danhDauXoa)
        {
            var json = JsonSerializer.Serialize(rows.Select(r => new
            {
                r.Me,
                NgaySX = r.NgaySX.ToString("yyyy-MM-dd"),
                r.CaSX,
                r.Kip,
                r.MayDuc,
                r.Mac,
                r.KichThuoc,
                r.LenhSanXuat,
            }));

            const string sqlMerge = @"
MERGE KDL_DoiTuong AS t
USING (
    SELECT Me, NgaySX, CaSX, Kip, MayDuc, Mac, KichThuoc, LenhSanXuat
    FROM OPENJSON(@json) WITH (
        Me NVARCHAR(50) '$.Me',
        NgaySX     DATE         '$.NgaySX',
        CaSX       TINYINT      '$.CaSX',
        Kip        CHAR(1)      '$.Kip',
        MayDuc  NVARCHAR(30) '$.MayDuc',
        Mac  NVARCHAR(50) '$.Mac',
        KichThuoc    NVARCHAR(50) '$.KichThuoc',
        LenhSanXuat     NVARCHAR(30) '$.LenhSanXuat'
    )
) AS s
ON t.MaModule = @maModule AND t.Me = s.Me
WHEN MATCHED THEN
    UPDATE SET t.NgaySX = s.NgaySX, t.CaSX = s.CaSX, t.Kip = s.Kip, t.MayDuc = s.MayDuc,
               t.Mac = s.Mac, t.KichThuoc = s.KichThuoc, t.LenhSanXuat = s.LenhSanXuat,
               t.IsDelete = 0, t.NgayDongBo = GETDATE()
WHEN NOT MATCHED THEN
    INSERT (MaModule, Me, NgaySX, CaSX, Kip, MayDuc, Mac, KichThuoc, LenhSanXuat, IsDelete, NgayDongBo)
    VALUES (@maModule, s.Me, s.NgaySX, s.CaSX, s.Kip, s.MayDuc, s.Mac, s.KichThuoc, s.LenhSanXuat, 0, GETDATE());";

            await _context.Database.ExecuteSqlRawAsync(sqlMerge,
                new SqlParameter("@json", json), new SqlParameter("@maModule", maModule));

            if (danhDauXoa)
            {
                const string sqlXoa = @"
UPDATE KDL_DoiTuong SET IsDelete = 1, NgayDongBo = GETDATE()
WHERE MaModule = @maModule AND IsDelete = 0 AND NgaySX BETWEEN @tu AND @den
  AND Me NOT IN (SELECT Me FROM OPENJSON(@json) WITH (Me NVARCHAR(50) '$.Me'));";
                await _context.Database.ExecuteSqlRawAsync(sqlXoa,
                    new SqlParameter("@json", json), new SqlParameter("@maModule", maModule),
                    new SqlParameter("@tu", tuNgay.ToDateTime(TimeOnly.MinValue)) { SqlDbType = SqlDbType.Date },
                    new SqlParameter("@den", denNgay.ToDateTime(TimeOnly.MinValue)) { SqlDbType = SqlDbType.Date });
            }

            var maList = rows.Select(r => r.Me).Distinct().ToList();
            return await _context.KDL_DoiTuongs.AsNoTracking()
                .Where(x => x.MaModule == maModule && maList.Contains(x.Me))
                .ToListAsync();
        }

        public Task<KDL_DoiTuong?> GetDoiTuongByIdAsync(int idDoiTuong)
            => _context.KDL_DoiTuongs.AsNoTracking().FirstOrDefaultAsync(x => x.IdDoiTuong == idDoiTuong && !x.IsDelete);

        private IQueryable<KDL_DoiTuong> QueryDoiTuong(KdlLoc loc)
        {
            var q = _context.KDL_DoiTuongs.AsNoTracking()
                .Where(x => x.MaModule == loc.MaModule && !x.IsDelete && x.NgaySX >= loc.TuNgay && x.NgaySX <= loc.DenNgay);

            if (loc.CaSX.HasValue) q = q.Where(x => x.CaSX == loc.CaSX);
            if (!string.IsNullOrWhiteSpace(loc.Kip)) q = q.Where(x => x.Kip == loc.Kip);
            if (!string.IsNullOrWhiteSpace(loc.MayDuc)) q = q.Where(x => x.MayDuc == loc.MayDuc);
            if (!string.IsNullOrWhiteSpace(loc.Mac)) q = q.Where(x => x.Mac == loc.Mac);
            if (!string.IsNullOrWhiteSpace(loc.KichThuoc)) q = q.Where(x => x.KichThuoc == loc.KichThuoc);
            if (!string.IsNullOrWhiteSpace(loc.LenhSanXuat)) q = q.Where(x => x.LenhSanXuat == loc.LenhSanXuat);
            if (!string.IsNullOrWhiteSpace(loc.SearchKey)) q = q.Where(x => x.Me.Contains(loc.SearchKey));
            return q;
        }

        // Excel CSDL: sắp theo ngày → ca → mẻ A→Z
        private static IQueryable<KDL_DoiTuong> SapXep(IQueryable<KDL_DoiTuong> q)
            => q.OrderBy(x => x.NgaySX).ThenBy(x => x.CaSX).ThenBy(x => x.Me);

        public async Task<(List<KDL_DoiTuong> Rows, int Total)> SearchDoiTuongAsync(KdlLoc loc, int page, int pageSize)
        {
            var q = QueryDoiTuong(loc);
            var total = await q.CountAsync();
            var rows = await SapXep(q).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (rows, total);
        }

        public Task<List<KDL_DoiTuong>> GetDoiTuongAsync(KdlLoc loc, int toiDa)
            => SapXep(QueryDoiTuong(loc)).Take(toiDa).ToListAsync();

        // ── Giá trị ────────────────────────────────────────────────────────────

        public async Task<List<KDL_GiaTri>> GetGiaTriAsync(IReadOnlyCollection<int> idDoiTuongs, DateOnly tuNgay, DateOnly denNgay,
            IReadOnlyCollection<byte>? nguons = null, IReadOnlyCollection<int>? idKeys = null)
        {
            if (idDoiTuongs.Count == 0) return [];

            var ids = idDoiTuongs.Distinct().ToList();
            // NgaySX để SQL Server loại partition
            var q = _context.KDL_GiaTris.AsNoTracking()
                .Where(x => x.NgaySX >= tuNgay && x.NgaySX <= denNgay && ids.Contains(x.IdDoiTuong));
            if (nguons != null) { var n = nguons.ToList(); q = q.Where(x => n.Contains(x.Nguon)); }
            if (idKeys != null) { var k = idKeys.ToList(); q = q.Where(x => k.Contains(x.IdKey)); }
            return await q.ToListAsync();
        }

        public async Task<int> ThayGiaTriAsync(string maModule, DateOnly tuNgay, DateOnly denNgay, IReadOnlyCollection<byte> nguons,
            IReadOnlyCollection<int>? idDoiTuongs, IReadOnlyCollection<int>? idKeys, IReadOnlyCollection<KDL_GiaTri> rows)
        {
            var sql = new StringBuilder(@"
DELETE g FROM KDL_GiaTri g
WHERE g.NgaySX BETWEEN @tu AND @den
  AND g.Nguon IN (SELECT CAST(value AS TINYINT) FROM OPENJSON(@nguons))
  AND g.IdDoiTuong IN (SELECT IdDoiTuong FROM KDL_DoiTuong WHERE MaModule = @maModule)");
            var ps = new List<SqlParameter>
            {
                new("@tu", SqlDbType.Date) { Value = tuNgay.ToDateTime(TimeOnly.MinValue) },
                new("@den", SqlDbType.Date) { Value = denNgay.ToDateTime(TimeOnly.MinValue) },
                new("@nguons", JsonSerializer.Serialize(nguons)),
                new("@maModule", maModule),
            };
            if (idDoiTuongs != null)
            {
                sql.Append("\n  AND g.IdDoiTuong IN (SELECT CAST(value AS INT) FROM OPENJSON(@ids))");
                ps.Add(new SqlParameter("@ids", JsonSerializer.Serialize(idDoiTuongs)));
            }
            if (idKeys != null)
            {
                sql.Append("\n  AND g.IdKey IN (SELECT CAST(value AS INT) FROM OPENJSON(@keys))");
                ps.Add(new SqlParameter("@keys", JsonSerializer.Serialize(idKeys)));
            }
            await _context.Database.ExecuteSqlRawAsync(sql.ToString(), ps);

            if (rows.Count == 0) return 0;

            var table = new DataTable();
            table.Columns.Add("NgaySX", typeof(DateTime));
            table.Columns.Add("CaSX", typeof(byte));
            table.Columns.Add("IdDoiTuong", typeof(int));
            table.Columns.Add("IdKey", typeof(int));
            table.Columns.Add("Nguon", typeof(byte));
            table.Columns.Add("GiaTri", typeof(decimal));
            table.Columns.Add("GiaTriText", typeof(string));
            table.Columns.Add("NguoiCapNhat", typeof(string));
            table.Columns.Add("NgayCapNhat", typeof(DateTime));
            var bayGio = DateTime.Now;
            foreach (var r in rows)
                table.Rows.Add(r.NgaySX.ToDateTime(TimeOnly.MinValue), (object?)r.CaSX ?? DBNull.Value, r.IdDoiTuong, r.IdKey, r.Nguon,
                    (object?)r.GiaTri ?? DBNull.Value, (object?)r.GiaTriText ?? DBNull.Value,
                    (object?)r.NguoiCapNhat ?? DBNull.Value, bayGio);

            var conn = (SqlConnection)_context.Database.GetDbConnection();
            var tx = _context.Database.CurrentTransaction?.GetDbTransaction() as SqlTransaction;
            using var bulk = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, tx)
            {
                DestinationTableName = "KDL_GiaTri",
                BatchSize = 10000,
            };
            foreach (DataColumn c in table.Columns)
                bulk.ColumnMappings.Add(c.ColumnName, c.ColumnName);
            await bulk.WriteToServerAsync(table);
            return rows.Count;
        }

        public async Task<List<KdlNhomTongHop>> TongHopAsync(KdlLoc loc, IReadOnlyList<string> cotNhom, IReadOnlyCollection<int> idKeys)
        {
            var cols = cotNhom.Where(CotNhomHopLe.Contains).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (cols.Count != cotNhom.Count)
                throw new InvalidOperationException($"Chiều nhóm không hợp lệ. Cho phép: {string.Join(", ", CotNhomHopLe)}.");

            var (where, ps) = DieuKienLoc(loc);
            var colD = string.Join("", cols.Select(c => $"d.{c}, "));
            var colV = string.Join("", cols.Select(c => $"{c}, "));
            var groupD = cols.Count == 0 ? "" : "GROUP BY " + string.Join(", ", cols.Select(c => $"d.{c}"));
            ps.Add(new SqlParameter("@keys", JsonSerializer.Serialize(idKeys)));

            var sql = $@"
WITH v AS (
    SELECT {colD}g.IdKey, g.GiaTri,
           ROW_NUMBER() OVER (PARTITION BY g.IdDoiTuong, g.IdKey
                              ORDER BY CASE g.Nguon WHEN 4 THEN 0 WHEN 3 THEN 1 ELSE 2 END) AS rn
    FROM KDL_GiaTri g
    JOIN KDL_DoiTuong d ON d.IdDoiTuong = g.IdDoiTuong
    WHERE {where} AND g.NgaySX BETWEEN @tu AND @den
      AND g.IdKey IN (SELECT CAST(value AS INT) FROM OPENJSON(@keys))
)
SELECT {colV}IdKey, SUM(GiaTri) AS GiaTri FROM v WHERE rn = 1 GROUP BY {colV}IdKey;

SELECT {colD}COUNT(*) AS SoDoiTuong FROM KDL_DoiTuong d WHERE {where} {groupD};";

            var nhoms = new Dictionary<string, KdlNhomTongHop>();
            KdlNhomTongHop LayNhom(DbDataReader r)
            {
                var gtNhom = cols.Select(c => r[c] is DBNull ? null : r[c]).ToList();
                var khoa = string.Join("\u001f", gtNhom.Select(x => Convert.ToString(x, System.Globalization.CultureInfo.InvariantCulture)));
                if (!nhoms.TryGetValue(khoa, out var n))
                {
                    n = new KdlNhomTongHop();
                    for (int i = 0; i < cols.Count; i++)
                        n.Nhom[cols[i]] = gtNhom[i] is DateTime dt ? DateOnly.FromDateTime(dt) : gtNhom[i];
                    nhoms[khoa] = n;
                }
                return n;
            }

            var conn = _context.Database.GetDbConnection();
            var dongKetNoi = conn.State != ConnectionState.Open;
            if (dongKetNoi) await conn.OpenAsync();
            try
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                cmd.Transaction = _context.Database.CurrentTransaction?.GetDbTransaction();
                cmd.Parameters.AddRange(ps.ToArray());
                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                    LayNhom(r).GiaTri[r.GetInt32(r.GetOrdinal("IdKey"))] = r["GiaTri"] is DBNull ? null : (decimal)r["GiaTri"];
                await r.NextResultAsync();
                while (await r.ReadAsync())
                    LayNhom(r).SoDoiTuong = r.GetInt32(r.GetOrdinal("SoDoiTuong"));
            }
            finally
            {
                if (dongKetNoi) await conn.CloseAsync();
            }

            return nhoms.Values.ToList();
        }

        private static (string Where, List<SqlParameter> Params) DieuKienLoc(KdlLoc loc)
        {
            var w = new StringBuilder("d.MaModule = @maModule AND d.IsDelete = 0 AND d.NgaySX BETWEEN @tu AND @den");
            var ps = new List<SqlParameter>
            {
                new("@maModule", loc.MaModule),
                new("@tu", SqlDbType.Date) { Value = loc.TuNgay.ToDateTime(TimeOnly.MinValue) },
                new("@den", SqlDbType.Date) { Value = loc.DenNgay.ToDateTime(TimeOnly.MinValue) },
            };
            void Them(string cot, object? giaTri)
            {
                if (giaTri == null || (giaTri is string s && string.IsNullOrWhiteSpace(s))) return;
                w.Append($" AND d.{cot} = @{cot}");
                ps.Add(new SqlParameter($"@{cot}", giaTri));
            }
            Them("CaSX", loc.CaSX);
            Them("Kip", loc.Kip);
            Them("MayDuc", loc.MayDuc);
            Them("Mac", loc.Mac);
            Them("KichThuoc", loc.KichThuoc);
            Them("LenhSanXuat", loc.LenhSanXuat);
            if (!string.IsNullOrWhiteSpace(loc.SearchKey))
            {
                w.Append(" AND d.Me LIKE @search");
                ps.Add(new SqlParameter("@search", $"%{loc.SearchKey.Trim()}%"));
            }
            return (w.ToString(), ps);
        }

        // ── Nhật ký đồng bộ ───────────────────────────────────────────────────

        public async Task AddSyncLogAsync(KDL_SyncLog log)
        {
            _context.KDL_SyncLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateSyncLogAsync(KDL_SyncLog log)
        {
            _context.KDL_SyncLogs.Update(log);
            await _context.SaveChangesAsync();
        }

        public Task<KDL_SyncLog?> GetSyncLogGanNhatAsync(string maModule)
            => _context.KDL_SyncLogs.AsNoTracking()
                .Where(x => x.MaModule == maModule)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();
    }
}

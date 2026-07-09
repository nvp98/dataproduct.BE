using dataproduct.api.DTOs;
using dataproduct.api.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace dataproduct.api.Services
{
    public class HRC1_NMSyncService
    {
        private readonly ProductFormContext _context;
        private readonly string _gangLongConnStr;

        public HRC1_NMSyncService(ProductFormContext context, IConfiguration config)
        {
            _context = context;

            // GangLong metrics nằm ở DB khác (giống HRC2_NMSyncService) — ưu tiên connection string
            // riêng nếu có, fallback về MasterDbConnection để tương thích config cũ.
            _gangLongConnStr =
                config.GetConnectionString("GangLongDbConnection")
                ?? config.GetConnectionString("MasterDbConnection")
                ?? throw new InvalidOperationException("GangLongDbConnection/MasterDbConnection connection string is not configured");
        }

        /// <summary>
        /// Gọi SP_HRC1_BOF_Sync_Full để đồng bộ mẻ thổi + phụ liệu từ NM cho đúng 1 tổ hợp Ngày+Ca+Lò,
        /// sau đó bổ sung KLGangLongCCT/KLThepPheGang từ DB GangLong (chỉ áp dụng BOF).
        /// Khác HRC2 (sync toàn bộ rồi lọc), SP này đã nhận tham số nên chỉ sync đúng phạm vi cần.
        /// </summary>
        public async Task SyncHRC1FromNMAsync(SyncFromNM_HRC1_Request request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.SP_HRC1_BOF_Sync_Full @NgaySanXuat={0}, @Ca={1}, @LoThoi={2}",
                DateOnly.FromDateTime(request.NgaySX), request.Ca, request.Scope);

            await EnsureGangLongMetricsAsync(request);
            await EnsureThepPheGangAsync(request);
        }

        /// <summary>
        /// Bổ sung KLGangLongCCT (usp_Sum_KL_GangLong_By_MeThoi_CCT) cho các mẻ NM trong đúng slot
        /// Ngày+Ca+Lò+BieuMau — mirror HRC2_NMSyncService.EnsureGangLongMetricsAsync. Luôn ghi đè để
        /// phản ánh thay đổi từ phía gang. Chỉ áp dụng BOF (LF chưa có nguồn NM nào để bổ sung metric này).
        /// </summary>
        private async Task EnsureGangLongMetricsAsync(SyncFromNM_HRC1_Request request)
        {
            if (!string.Equals(request.BieuMau, "BOF", StringComparison.OrdinalIgnoreCase))
                return;

            var ngay = DateOnly.FromDateTime(request.NgaySX);

            var rows = await _context.Hrc1TieuHaos
                .Where(x =>
                    x.IsNM == true &&
                    !x.IsDeleted &&
                    x.NgaySanXuat == ngay &&
                    x.Ca == request.Ca &&
                    x.Scope == request.Scope &&
                    x.BieuMau == request.BieuMau &&
                    x.MeThoi != null)
                .ToListAsync();

            if (rows.Count == 0) return;

            var allMeThoi = rows
                .Select(x => x.MeThoi!)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var cache = await ExecuteGangLongStoredProcBatchAsync("usp_Sum_KL_GangLong_By_MeThoi_CCT", allMeThoi);

            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.MeThoi)) continue;
                if (!cache.TryGetValue(row.MeThoi, out var kl)) continue;

                row.KLGangLongCCT = (decimal?)kl;
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Bổ sung KLThepPheGang (usp_Get_KLThepPhe_By_MeThoi) cho các mẻ NM trong đúng slot — mirror
        /// HRC2_NMSyncService.EnsureThepPheGangAsync. Luôn ghi đè. Chỉ áp dụng BOF.
        /// </summary>
        private async Task EnsureThepPheGangAsync(SyncFromNM_HRC1_Request request)
        {
            if (!string.Equals(request.BieuMau, "BOF", StringComparison.OrdinalIgnoreCase))
                return;

            var ngay = DateOnly.FromDateTime(request.NgaySX);

            var rows = await _context.Hrc1TieuHaos
                .Where(x =>
                    x.IsNM == true &&
                    !x.IsDeleted &&
                    x.NgaySanXuat == ngay &&
                    x.Ca == request.Ca &&
                    x.Scope == request.Scope &&
                    x.BieuMau == request.BieuMau &&
                    x.MeThoi != null)
                .ToListAsync();

            if (rows.Count == 0) return;

            var allMeThoi = rows
                .Select(x => x.MeThoi!)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var cache = await ExecuteGangLongStoredProcBatchAsync(
                "usp_Get_KLThepPhe_By_MeThoi", allMeThoi, "KLThepPhe");

            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.MeThoi)) continue;
                if (!cache.TryGetValue(row.MeThoi, out var kl)) continue;

                row.KLThepPheGang = (decimal?)kl;
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Làm mới KLGangLongCCT và KLThepPheGang cho danh sách rows đã tải sẵn — mirror
        /// HRC2_NMSyncService.RefreshGangMetricsForRowsAsync. Dùng 2 TVP stored proc batch → 1
        /// SaveChanges, không phụ thuộc filter slot (vd cho 1 tính năng "Làm mới lại chỉ số" sau này).
        /// </summary>
        public async Task<int> RefreshGangMetricsForRowsAsync(List<Hrc1TieuHao> rows)
        {
            var validRows = rows.Where(x => !string.IsNullOrWhiteSpace(x.MeThoi)).ToList();
            if (validRows.Count == 0) return 0;

            var allMeThoi = validRows
                .Select(x => x.MeThoi!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var gangLongCache = await ExecuteGangLongStoredProcBatchAsync(
                "usp_Sum_KL_GangLong_By_MeThoi_CCT", allMeThoi);

            var thepPheCache = await ExecuteGangLongStoredProcBatchAsync(
                "usp_Get_KLThepPhe_By_MeThoi", allMeThoi, "KLThepPhe");

            foreach (var row in validRows)
            {
                if (gangLongCache.TryGetValue(row.MeThoi!, out var kl))
                    row.KLGangLongCCT = (decimal?)kl;
                if (thepPheCache.TryGetValue(row.MeThoi!, out var thepPhe))
                    row.KLThepPheGang = (decimal?)thepPhe;
            }

            await _context.SaveChangesAsync();
            return validRows.Count;
        }

        /// <summary>
        /// Gọi 1 stored proc batch trên DB GangLong (TVP dbo.TVP_MeThoi @ListMeThoi) — copy nguyên văn
        /// từ HRC2_NMSyncService.ExecuteGangLongStoredProcBatchAsync (logic hoàn toàn generic theo
        /// MeThoi, không phụ thuộc HRC1/HRC2).
        /// </summary>
        private async Task<Dictionary<string, double?>> ExecuteGangLongStoredProcBatchAsync(
            string procedureName,
            IEnumerable<string> listMeThoi,
            string resultColumnName = "TotalKL")
        {
            var result = new Dictionary<string, double?>(StringComparer.OrdinalIgnoreCase);

            var tvp = new DataTable();
            tvp.Columns.Add("MaMeThoi", typeof(string));
            foreach (var mt in listMeThoi)
                tvp.Rows.Add(mt);

            await using var connection = new SqlConnection(_gangLongConnStr);
            await connection.OpenAsync();

            try
            {
                using var command = new SqlCommand(procedureName, connection);
                command.CommandType = CommandType.StoredProcedure;
                command.CommandTimeout = 30;

                var param = command.Parameters.AddWithValue("@ListMeThoi", tvp);
                param.SqlDbType = SqlDbType.Structured;
                param.TypeName = "dbo.TVP_MeThoi";

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var meThoi = reader["MaMeThoi"]?.ToString();
                    var kl = reader[resultColumnName] == DBNull.Value ? (double?)null : Convert.ToDouble(reader[resultColumnName]);

                    if (!string.IsNullOrWhiteSpace(meThoi))
                        result[meThoi] = kl;
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Error executing {procedureName}: {ex.Message}", ex);
            }
        }
    }
}

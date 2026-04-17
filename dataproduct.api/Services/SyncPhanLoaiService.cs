using dataproduct.api.DTOs;
using dataproduct.api.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text.Json;

namespace dataproduct.api.Services
{
    public class SyncPhanLoaiService
    {
        private readonly ProductFormContext _context;
        private readonly IConfiguration _config;
        private readonly ILogger<SyncPhanLoaiService> _logger;
        private readonly string _sqlConnStr;

        private static readonly HashSet<string> ValidBieuMaus = new(StringComparer.OrdinalIgnoreCase)
        {
            "HRC1_BBGN_ThepLong",
            "HRC2_BBGN_ThepLong"
        };

        public SyncPhanLoaiService(
            ProductFormContext context,
            IConfiguration config,
            ILogger<SyncPhanLoaiService> logger)
        {
            _context = context;
            _config = config;
            _logger = logger;
            _sqlConnStr = config.GetConnectionString("DbConnectionString")
                ?? throw new InvalidOperationException("DbConnectionString is not configured.");
        }

        /// <summary>
        /// Đồng bộ phân loại từ Linked Server về SQL Server cho danh sách mã mẻ theo bieuMau.
        /// </summary>
        public async Task<SyncPhanLoaiResult> SyncAsync(SyncPhanLoaiRequest request)
        {
            if (!ValidBieuMaus.Contains(request.BieuMau))
                throw new ArgumentException($"BieuMau không hợp lệ: '{request.BieuMau}'. Chỉ chấp nhận HRC1_BBGN_ThepLong hoặc HRC2_BBGN_ThepLong.");

            if (request.MaMes == null || request.MaMes.Count == 0)
                return new SyncPhanLoaiResult();

            var viewName = request.BieuMau.StartsWith("HRC1", StringComparison.OrdinalIgnoreCase)
                ? "view_dq1_nmlt_nuocthep"
                : "view_dq2_nmlt_nuocthep";

            // 1. Query qua SP (Linked Server)
            var phanLoaiMap = await QueryViaStoredProcAsync(viewName, request.MaMes);

            _logger.LogInformation("[SyncPhanLoai] BieuMau={BieuMau} | MaMes={Count} | Linked Server có phân loại={Count2}",
                request.BieuMau, request.MaMes.Count, phanLoaiMap.Count);

            if (phanLoaiMap.Count == 0)
                return new SyncPhanLoaiResult { TotalFromMySQL = 0, TotalUpdated = 0 };

            // 2. Update vào SQL Server (chỉ những mẻ PhanLoai IS NULL, không phải ghost)
            var maMesCoData = phanLoaiMap.Keys.ToList();
            var rows = await _context.BBGN_ThepLongs
                .Where(x => maMesCoData.Contains(x.Me!)
                         && x.PhanLoai == null
                         && x.BieuMau == request.BieuMau
                         && x.IsGhost != true)
                .ToListAsync();

            int updated = 0;
            foreach (var row in rows)
            {
                if (row.Me != null && phanLoaiMap.TryGetValue(row.Me, out var pl))
                {
                    row.PhanLoai = pl;
                    _context.BBGN_ThepLongs.Update(row);
                    updated++;
                }
            }

            if (updated > 0)
                await _context.SaveChangesAsync();

            _logger.LogInformation("[SyncPhanLoai] BieuMau={BieuMau} | Updated={Updated}",
                request.BieuMau, updated);

            return new SyncPhanLoaiResult
            {
                TotalFromMySQL = phanLoaiMap.Count,
                TotalUpdated   = updated
            };
        }

        /// <summary>
        /// Lấy danh sách mã mẻ có PhanLoai IS NULL trong N ngày gần nhất.
        /// Dùng cho Background Service.
        /// </summary>
        public async Task<List<string>> GetMeThoi_PhanLoaiNullAsync(string bieuMau, int lookbackDays)
        {
            var fromDate = DateTime.Today.AddDays(-lookbackDays);
            return await _context.BBGN_ThepLongs
                .Where(x => x.BieuMau == bieuMau
                         && x.PhanLoai == null
                         && x.Me != null
                         && x.IsGhost != true
                         && x.NgaySX >= fromDate)
                .Select(x => x.Me!)
                .Distinct()
                .ToListAsync();
        }

        /// <summary>
        /// Gọi SP usp_GetPhanLoaiThepLong trong PRODUCT_FORM.
        /// SP tự query Linked Server theo ViewName và danh sách MaMe (JSON array).
        /// Trả về: BilletLotCode, ClassifyName
        /// </summary>
        private async Task<Dictionary<string, string>> QueryViaStoredProcAsync(string viewName, List<string> maMes)
        {
            var result  = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var maMesJson = JsonSerializer.Serialize(maMes);

            await using var conn = new SqlConnection(_sqlConnStr);
            await conn.OpenAsync();

            await using var cmd = new SqlCommand("usp_GetPhanLoaiThepLong", conn);
            cmd.CommandType    = CommandType.StoredProcedure;
            cmd.CommandTimeout = 60;
            cmd.Parameters.Add("@ViewName",   SqlDbType.NVarChar, 50).Value  = viewName;
            cmd.Parameters.Add("@MaMesJson",  SqlDbType.NVarChar, -1).Value  = maMesJson;

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var maMe    = reader.IsDBNull(0) ? null : reader.GetString(0); // BilletLotCode
                var phanLoai = reader.IsDBNull(1) ? null : reader.GetString(1); // ClassifyName
                if (!string.IsNullOrWhiteSpace(maMe) && !string.IsNullOrWhiteSpace(phanLoai))
                    result[maMe] = phanLoai;
            }

            return result;
        }
    }
}

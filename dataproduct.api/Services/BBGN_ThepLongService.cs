using dataproduct.api.DTOs;
using dataproduct.api.DTOs.Export;
using dataproduct.api.Models;
using dataproduct.api.Repositories;
using dataproduct.api.ResponseModels;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;
using System.Data;
using Microsoft.Data.SqlClient;

namespace dataproduct.api.Services
{
    public class BBGN_ThepLongService
    {
        private readonly IBBGN_ThepLongRepository _repo;
        private readonly ProductFormContext _context;
        private readonly string _gangLongConnStr;

        public BBGN_ThepLongService(IBBGN_ThepLongRepository repo, ProductFormContext context, IConfiguration config)
        {
            _repo = repo;
            _context = context;
             _gangLongConnStr =
                config.GetConnectionString("GangLongDbConnection")
                ?? config.GetConnectionString("MasterDbConnection")
                ?? throw new InvalidOperationException("GangLongDbConnection/MasterDbConnection connection string is not configured");
        }

        public async Task SaveHRC2BBGNThepLongAsync(JsonElement formData, Guid idPhieu)
        {
             if (!formData.TryGetProperty("table1", out var table))
                return;

            // ===== PRELOAD DATA theo IdPhieu =====
            var existingData = await _context.BBGN_ThepLongs
                .Where(x => x.IdPhieu == idPhieu)
                .ToListAsync();

            // map để lookup nhanh theo Me
            var map = existingData
                .Where(x => !string.IsNullOrWhiteSpace(x.Me))
                .ToDictionary(x => x.Me!, StringComparer.OrdinalIgnoreCase);

            var toInsert = new List<BBGN_ThepLong>();

            // ===== LOOP DATA =====
            foreach (var row in table.EnumerateArray())
            {
                var me = GetString(row, "me");
                if (string.IsNullOrWhiteSpace(me)) continue;

                var klLan1 = TryParseDecimal(row, "klLan1");
                var klLan2 = TryParseDecimal(row, "klLan2");
                var klLan3 = TryParseDecimal(row, "klLan3");

                var klThepLong = TryParseDecimal(row, "klThepLong")
                                ?? SumValues(klLan1, klLan2, klLan3);

                if (map.TryGetValue(me, out var existing))
                {
                    // ===== UPDATE =====
                    existing.MayDuc = GetString(row, "mayDuc");
                    existing.MacThep = GetString(row, "macThep");
                    existing.ThungSo = GetString(row, "thungSo");
                    existing.ThoiGian = TryParseDateTime(row, "thoiGian");

                    existing.KlLan1 = klLan1;
                    existing.KlLan2 = klLan2;
                    existing.KlLan3 = klLan3;
                    existing.KlThepLong = klThepLong;

                    existing.GhiChu = GetString(row, "ghiChu");
                    existing.TinhLuyenLenThang = GetString(row, "tinhLuyenLenThang");
                    existing.PhanLoai = GetString(row, "phanLoai");

                    _context.BBGN_ThepLongs.Update(existing);
                }
                else
                {
                    // ===== INSERT =====
                    var entity = new BBGN_ThepLong
                    {
                        Me = me,
                        MayDuc = GetString(row, "mayDuc"),
                        MacThep = GetString(row, "macThep"),
                        ThungSo = GetString(row, "thungSo"),
                        ThoiGian = TryParseDateTime(row, "thoiGian"),

                        KlLan1 = klLan1,
                        KlLan2 = klLan2,
                        KlLan3 = klLan3,
                        KlThepLong = klThepLong,

                        GhiChu = GetString(row, "ghiChu"),
                        TinhLuyenLenThang = GetString(row, "tinhLuyenLenThang"),
                        PhanLoai = GetString(row, "phanLoai"),
                        IdPhieu = idPhieu,
                        IsGhost = false
                    };

                    toInsert.Add(entity);
                }
            }

            // ===== SAVE =====
            if (toInsert.Any())
                await _context.BBGN_ThepLongs.AddRangeAsync(toInsert);

            await _context.SaveChangesAsync();
        }
    

        private decimal? SumValues(params decimal?[] values)
        {
            decimal total = 0;
            bool hasValue = false;

            foreach (var v in values)
            {
                if (v.HasValue)
                {
                    total += v.Value;
                    hasValue = true;
                }
            }

            return hasValue ? total : null;
        }
        private string? GetString(JsonElement row, string key)
        {
            return row.TryGetProperty(key, out var val) && val.ValueKind == JsonValueKind.String
                ? val.GetString()
                : null;
        }

        private decimal? TryParseDecimal(JsonElement row, string key)
        {
            if (!row.TryGetProperty(key, out var val)) return null;

            if (val.ValueKind == JsonValueKind.Number)
                return val.GetDecimal();

            if (val.ValueKind == JsonValueKind.String &&
                decimal.TryParse(val.GetString(), out var d))
                return d;

            return null;
        }

        private DateTime? TryParseDateTime(JsonElement row, string key)
        {
            if (!row.TryGetProperty(key, out var val)) return null;

            if (val.ValueKind == JsonValueKind.String &&
                DateTime.TryParse(val.GetString(), out var dt))
                return dt;

            return null;
        }

        private decimal? Sum(JsonElement row, params string[] keys)
        {
            decimal total = 0;
            bool hasValue = false;

            foreach (var key in keys)
            {
                var val = TryParseDecimal(row, key);
                if (val.HasValue)
                {
                    total += val.Value;
                    hasValue = true;
                }
            }

            return hasValue ? total : null;
        }

        public async Task<bool> FetchMeThoiAsync(FetchMeThoiRequest request)
        {
            var data = await ExecuteGetMeThoiAsync("usp_GetMeThoi_ByNgayCaNhaMay", request.NgaySX, request.Ca, request.NhaMay);

            await _repo.XuLyDuLieuMeThoiGangLongAsync(data, request);

            return true;
        }

        public async Task<List<BBGN_ThepLong>> LoadAsync(LoadBBGNThepLongRequest request)
        {
            // Fetch trước để đảm bảo dữ liệu mẻ thoi được cập nhật
            await FetchMeThoiAsync(new FetchMeThoiRequest
            {
                NgaySX = request.NgaySX,
                Ca = request.Ca,
                NhaMay = request.NhaMay
            });

            return await _context.BBGN_ThepLongs
                .Where(x => x.IdPhieu == request.IdPhieu)
                .ToListAsync();
        }

        private async Task<List<string>> ExecuteGetMeThoiAsync(
            string procedureName,
            DateOnly ngaySX,
            int ca,
            int nhaMay)
        {
            var result = new List<string>();

            await using var connection = new SqlConnection(_gangLongConnStr);
            await connection.OpenAsync();

            try
            {
                using var command = new SqlCommand(procedureName, connection);
                command.CommandType = CommandType.StoredProcedure;
                command.CommandTimeout = 30;

                command.Parameters.Add("@Ngay", SqlDbType.Date).Value = ngaySX.ToDateTime(TimeOnly.MinValue);
                command.Parameters.Add("@Ca", SqlDbType.Int).Value = ca;
                command.Parameters.Add("@NhaMay", SqlDbType.Int).Value = nhaMay;

                await using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    if (!reader.IsDBNull(0))
                    {
                        result.Add(reader.GetString(0)); 
                    }
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

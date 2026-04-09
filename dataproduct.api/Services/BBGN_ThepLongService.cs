using dataproduct.api.DTOs;
using dataproduct.api.DTOs.Export;
using dataproduct.api.Models;
using dataproduct.api.Repositories;
using dataproduct.api.ResponseModels;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;

namespace dataproduct.api.Services
{
    public class BBGN_ThepLongService
    {
        private readonly IBBGN_ThepLongRepository _repo;
private readonly ProductFormContext _context;

        public BBGN_ThepLongService(IBBGN_ThepLongRepository repo, ProductFormContext context)
        {
            _repo = repo;
            _context = context;
        }

        public async Task SaveHRC2BBGNThepLongAsync(JsonElement formData)
        {   
             if (!formData.TryGetProperty("table1", out var table))
                return;

            // ✅ khai báo ngoài
            DateTime? ngaySX = null;
            int? ca = null;
            string? bieuMau = null;
            if (formData.TryGetProperty("NgaySX", out var nsxProp) &&
                nsxProp.ValueKind == JsonValueKind.String &&
                DateTime.TryParse(nsxProp.GetString(), out var nsx))
            {
                ngaySX = nsx.Date;
            }

            if (formData.TryGetProperty("ca", out var caProp) &&
                caProp.ValueKind == JsonValueKind.Number)
            {
                ca = caProp.GetInt32();
            }

            if (formData.TryGetProperty("maBm", out var bmProp) &&
                bmProp.ValueKind == JsonValueKind.String)
            {
                bieuMau = bmProp.GetString();
            }

            if (ngaySX == null || ca == null || string.IsNullOrEmpty(bieuMau))
                return;

            // ===== PRELOAD DATA =====
            var meList = table.EnumerateArray()
                .Select(x => GetString(x, "me"))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            var existingData = await _context.BBGN_ThepLongs
                .Where(x =>
                    meList.Contains(x.Me) &&
                    x.NgaySX == ngaySX &&
                    x.Ca == ca &&
                    x.BieuMau == bieuMau)
                .ToListAsync();

            // map để lookup nhanh
            var map = existingData.ToDictionary(
                x => $"{x.Me}_{x.BieuMau}_{x.NgaySX:yyyyMMdd}_{x.Ca}"
            );

            var toInsert = new List<BBGN_ThepLong>();

            // ===== LOOP DATA =====
            foreach (var row in table.EnumerateArray())
            {
                var me = GetString(row, "me");
                if (string.IsNullOrWhiteSpace(me)) continue;

                var key = $"{me}_{bieuMau}_{ngaySX:yyyyMMdd}_{ca}";

                var klLan1 = TryParseDecimal(row, "klLan1");
                var klLan2 = TryParseDecimal(row, "klLan2");
                var klLan3 = TryParseDecimal(row, "klLan3");

                var klThepLong = TryParseDecimal(row, "klThepLong")
                                ?? SumValues(klLan1, klLan2, klLan3);

                if (map.TryGetValue(key, out var existing))
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
                        NgaySX = ngaySX.Value,
                        Ca = ca.Value,
                        BieuMau = bieuMau,

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
                        PhanLoai = GetString(row, "phanLoai")
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
    }
}

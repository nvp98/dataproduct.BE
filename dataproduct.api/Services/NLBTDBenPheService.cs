using dataproduct.api.Models;
using dataproduct.api.Repositories;
using System.Text.Json;

namespace dataproduct.api.Services
{
    public class NLBTDBenPheService
    {
        private readonly INL_BTDBenPheRepository _repo;

        public NLBTDBenPheService(INL_BTDBenPheRepository repo)
        {
            _repo = repo;
        }

        public async Task<int> InsertNLBTDBenPheFromPhieuJsonAsync(BmPhieu phieu)
        {
            if (phieu == null)
                throw new ArgumentNullException(nameof(phieu));

            var entities = new List<NL_BTDBenPhe>();

            if (!string.IsNullOrWhiteSpace(phieu.DataJson))
            {
                using var jsonDoc = JsonDocument.Parse(phieu.DataJson);
                var root = jsonDoc.RootElement;

                // Try to extract table1 data
                if (root.TryGetProperty("table1", out var table1Element) && table1Element.ValueKind == JsonValueKind.Array)
                {
                    foreach (var row in table1Element.EnumerateArray())
                    {
                        if (row.ValueKind != JsonValueKind.Object)
                            continue;

                        // Skip empty rows
                        var maBSX = TryGetString(row, "maBSX", "MaBSX");
                        if (string.IsNullOrWhiteSpace(maBSX))
                            continue;

                        entities.Add(new NL_BTDBenPhe
                        {
                            IDPhieu = phieu.Idphieu,
                            NgaySX = phieu.NgaySX ?? DateOnly.FromDateTime(DateTime.Today),
                            Ca = phieu.Ca.ToString(),
                            Kip = phieu.Kip ?? string.Empty,
                            MaBSX = TryGetString(row, "maBSX", "MaBSX") ?? string.Empty,
                            SoHieuBen = TryGetString(row, "soHieuBen", "SoHieuBen") ?? string.Empty,
                            KhoiLuong = TryGetDecimal(row, "khoiLuongBen", "KhoiLuongBen", "khoiLuong"),
                            GhiChu = TryGetString(row, "ghiChu", "GhiChu") ?? string.Empty,
                        });
                    }
                }
            }

            // Delete old records
            await _repo.DeleteByPhieuIdAsync(phieu.Idphieu);

            // Delete clone phieu's original data if applicable
            if (phieu.ID_PhieuGoc.HasValue
                && phieu.ID_PhieuGoc.Value != Guid.Empty
                && phieu.ID_PhieuGoc.Value != phieu.Idphieu)
            {
                await _repo.DeleteByPhieuIdAsync(phieu.ID_PhieuGoc.Value);
            }

            // Insert new records
            if (entities.Count > 0)
            {
                await _repo.AddRangeAsync(entities);
            }

            return entities.Count;
        }

        public async Task<List<NL_BTDBenPhe>> GetNLBTDBenPheByPhieuIdAsync(Guid idPhieu)
        {
            return await _repo.GetByPhieuIdAsync(idPhieu);
        }

        public async Task DeleteNLBTDBenPheByPhieuAsync(Guid idPhieu)
        {
            await _repo.DeleteByPhieuIdAsync(idPhieu);
        }

        // Helper method to get string value from JSON with multiple possible keys
        private string? TryGetString(JsonElement row, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (!row.TryGetProperty(key, out var value))
                    continue;

                switch (value.ValueKind)
                {
                    case JsonValueKind.String:
                        return value.GetString()?.Trim();

                    case JsonValueKind.Number:
                        return value.ToString(); // convert number ? string

                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        return value.GetBoolean().ToString();

                    default:
                        continue;
                }
            }

            return null;
        }

        // Helper method to get decimal value from JSON with multiple possible keys
        private decimal? TryGetDecimal(JsonElement row, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (!row.TryGetProperty(key, out var value))
                    continue;

                switch (value.ValueKind)
                {
                    case JsonValueKind.Number:
                        return value.GetDecimal();

                    case JsonValueKind.String:
                        var str = value.GetString()?.Trim();

                        if (string.IsNullOrEmpty(str))
                            continue;

                        str = str.Replace(",", "");

                        if (decimal.TryParse(str, out var result))
                            return result;
                        break;
                }
            }

            return null;
        }
    }
}

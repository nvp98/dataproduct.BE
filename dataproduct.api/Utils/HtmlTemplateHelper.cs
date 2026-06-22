using System.Text.Json;
using System.Text.RegularExpressions;

namespace dataproduct.api.Utils
{
    public static class HtmlTemplateHelper
    {
        private static readonly SemaphoreSlim _configLock = new(1, 1);
        private static Dictionary<string, (string MaBM, string NgayHieuLuc, string LanSuaDoi)>? _configCache;
        private static DateTime _configCacheTime;

        /// <summary>
        /// Trả về chuỗi BM header cho cell Excel:
        /// "BM.08/QT.05.13\nNgày hiệu lực: 15/04/2023\nLần sửa đổi: 01"
        /// Ưu tiên đọc từ bm-config.json, fallback regex nếu chưa có.
        /// </summary>
        public static async Task<string> GetBmHeaderTextAsync(string htmlFilePath)
        {
            var (maBm, ngayHieuLuc, lanSuaDoi) = await ParseBmHeaderAsync(htmlFilePath);
            var parts = new List<string> { maBm };
            if (!string.IsNullOrEmpty(ngayHieuLuc)) parts.Add($"Ngày hiệu lực: {ngayHieuLuc}");
            if (!string.IsNullOrEmpty(lanSuaDoi)) parts.Add($"Lần sửa đổi: {lanSuaDoi}");
            return string.Join("\n", parts);
        }

        public static async Task<(string MaBm, string NgayHieuLuc, string LanSuaDoi)> ParseBmHeaderAsync(string htmlFilePath)
        {
            // Đọc từ bm-config.json (wwwroot/bm-config.json)
            var configPath = FindConfigPath(htmlFilePath);
            var key = Path.GetFileNameWithoutExtension(htmlFilePath);

            if (configPath != null)
            {
                var entry = await ReadFromConfigAsync(configPath, key);
                if (entry.HasValue)
                    return entry.Value;
            }

            // Fallback: parse từ HTML bằng regex
            return await ParseFromHtmlAsync(htmlFilePath);
        }

        private static string? FindConfigPath(string htmlFilePath)
        {
            // template_html/ nằm trong wwwroot/ → lên 1 cấp để tìm bm-config.json
            var dir = Path.GetDirectoryName(htmlFilePath);
            if (dir == null) return null;
            var candidate = Path.Combine(dir, "..", "bm-config.json");
            return File.Exists(candidate) ? Path.GetFullPath(candidate) : null;
        }

        private static async Task<(string MaBm, string NgayHieuLuc, string LanSuaDoi)?> ReadFromConfigAsync(string configPath, string key)
        {
            await _configLock.WaitAsync();
            try
            {
                var lastWrite = File.GetLastWriteTime(configPath);
                if (_configCache == null || lastWrite > _configCacheTime)
                {
                    var json = await File.ReadAllTextAsync(configPath);
                    var raw = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                    _configCache = new Dictionary<string, (string, string, string)>(StringComparer.OrdinalIgnoreCase);
                    if (raw != null)
                    {
                        foreach (var kv in raw)
                        {
                            var maBM = kv.Value.TryGetProperty("MaBM", out var m) ? m.GetString() ?? "" : "";
                            var ngay = kv.Value.TryGetProperty("NgayHieuLuc", out var n) ? n.GetString() ?? "" : "";
                            var lan = kv.Value.TryGetProperty("LanSuaDoi", out var l) ? l.GetString() ?? "" : "";
                            _configCache[kv.Key] = (maBM, ngay, lan);
                        }
                    }
                    _configCacheTime = lastWrite;
                }

                return _configCache.TryGetValue(key, out var entry) ? entry : null;
            }
            finally { _configLock.Release(); }
        }

        private static async Task<(string MaBm, string NgayHieuLuc, string LanSuaDoi)> ParseFromHtmlAsync(string htmlFilePath)
        {
            if (!File.Exists(htmlFilePath))
                return ("", "", "");

            var html = await File.ReadAllTextAsync(htmlFilePath);

            var maBm = Regex.Match(html, @"<b>(BM\.[^<\s]+)</b>", RegexOptions.IgnoreCase)
                            .Groups[1].Value.Trim();

            var ngayHieuLuc = Regex.Match(html, @"Ngày hiệu lực:\s*</i>\s*([\d/]+)", RegexOptions.IgnoreCase)
                                   .Groups[1].Value.Trim();
            if (string.IsNullOrEmpty(ngayHieuLuc))
                ngayHieuLuc = Regex.Match(html, @"Ngày hiệu lực:[^>]*>\s*([\d/]+)")
                                   .Groups[1].Value.Trim();

            var lanSuaDoi = Regex.Match(html, @"Lần sửa đổi:\s*</i>\s*(\w+)", RegexOptions.IgnoreCase)
                                 .Groups[1].Value.Trim();
            if (string.IsNullOrEmpty(lanSuaDoi))
                lanSuaDoi = Regex.Match(html, @"Lần sửa đổi:[^>]*>\s*(\w+)")
                                 .Groups[1].Value.Trim();

            return (maBm, ngayHieuLuc, lanSuaDoi);
        }
    }
}

using System.Text.Json;
using System.Text.Json.Serialization;

namespace dataproduct.api.Services
{
    public class BmConfigEntry
    {
        [JsonPropertyName("MaBM")]
        public string MaBM { get; set; } = "";
        [JsonPropertyName("NgayHieuLuc")]
        public string NgayHieuLuc { get; set; } = "";
        [JsonPropertyName("LanSuaDoi")]
        public string LanSuaDoi { get; set; } = "";
    }

    public class BmConfigService
    {
        private readonly string _configPath;
        private readonly SemaphoreSlim _lock = new(1, 1);
        private Dictionary<string, BmConfigEntry>? _cache;
        private DateTime _cacheTime;

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public BmConfigService(IWebHostEnvironment env)
        {
            _configPath = Path.Combine(env.WebRootPath, "bm-config.json");
        }

        public async Task<Dictionary<string, BmConfigEntry>> GetAllAsync()
        {
            await _lock.WaitAsync();
            try
            {
                await EnsureCacheAsync();
                return new Dictionary<string, BmConfigEntry>(_cache!);
            }
            finally { _lock.Release(); }
        }

        public async Task<BmConfigEntry?> GetAsync(string key)
        {
            await _lock.WaitAsync();
            try
            {
                await EnsureCacheAsync();
                return _cache!.TryGetValue(key, out var entry) ? entry : null;
            }
            finally { _lock.Release(); }
        }

        public async Task SaveAsync(string key, BmConfigEntry entry)
        {
            await _lock.WaitAsync();
            try
            {
                await EnsureCacheAsync();
                _cache![key] = entry;
                var json = JsonSerializer.Serialize(_cache, _jsonOpts);
                await File.WriteAllTextAsync(_configPath, json);
            }
            finally { _lock.Release(); }
        }

        /// <summary>
        /// Đọc file HTML template và inject {{MaBM}}, {{NgayHieuLuc}}, {{LanSuaDoi}}.
        /// Key được tự động lấy từ tên file (không có .html).
        /// </summary>
        public async Task<string> LoadTemplateAsync(string templatePath)
        {
            var html = await File.ReadAllTextAsync(templatePath);
            var key = Path.GetFileNameWithoutExtension(templatePath);
            var entry = await GetAsync(key);
            if (entry == null) return html;
            return html
                .Replace("{{MaBM}}", entry.MaBM)
                .Replace("{{NgayHieuLuc}}", entry.NgayHieuLuc)
                .Replace("{{LanSuaDoi}}", entry.LanSuaDoi);
        }

        /// <summary>
        /// Trả về HTML dùng cho placeholder {{BmCode}} dạng kết hợp một dòng.
        /// Ví dụ: "BM.16/QT.05.10&lt;br/&gt;Ngày hiệu lực: 01/09/2023"
        /// </summary>
        public async Task<string> GetBmCodeHtmlAsync(string key)
        {
            var entry = await GetAsync(key);
            if (entry == null) return "";
            var parts = new List<string> { entry.MaBM };
            if (!string.IsNullOrEmpty(entry.NgayHieuLuc))
                parts.Add($"Ngày hiệu lực: {entry.NgayHieuLuc}");
            if (!string.IsNullOrEmpty(entry.LanSuaDoi))
                parts.Add($"Lần sửa đổi: {entry.LanSuaDoi}");
            return string.Join("<br/>", parts);
        }

        private async Task EnsureCacheAsync()
        {
            if (_cache != null && File.GetLastWriteTime(_configPath) <= _cacheTime)
                return;

            if (!File.Exists(_configPath))
            {
                _cache = new Dictionary<string, BmConfigEntry>();
                return;
            }

            var json = await File.ReadAllTextAsync(_configPath);
            _cache = JsonSerializer.Deserialize<Dictionary<string, BmConfigEntry>>(json, _jsonOpts)
                     ?? new Dictionary<string, BmConfigEntry>();
            _cacheTime = File.GetLastWriteTime(_configPath);
        }
    }
}

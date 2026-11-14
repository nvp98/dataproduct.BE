using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace dataproduct.api.Services
{
    /// <summary>Cấu hình "EmsApi" trong appsettings.json.</summary>
    public class EmsApiOptions
    {
        public string? BaseUrl { get; set; }
        public string TokenPath { get; set; } = "/connect/token";
        public string ValuePath { get; set; } = "/api/GetTagValueShift";
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string Scope { get; set; } = "tags.read";
        public int TimeoutSeconds { get; set; } = 60;
        /// <summary>Số lời gọi GetTagValueShift chạy song song.</summary>
        public int SoLuongGoiSongSong { get; set; } = 4;
        /// <summary>0: shift=2 của ngày N là ca 20:00 N → 08:00 N+1 (khớp CaSX). 1: là ca 20:00 N−1 → 08:00 N.</summary>
        public int Ca2LechNgay { get; set; }
        /// <summary>Chu kỳ worker tự đồng bộ (phút).</summary>
        public int SyncIntervalMinutes { get; set; } = 30;
        /// <summary>Worker kéo lại từ (hôm nay − SyncSoNgayLui) đến hôm nay.</summary>
        public int SyncSoNgayLui { get; set; } = 1;

        public bool DaCauHinh => !string.IsNullOrWhiteSpace(BaseUrl)
                              && !string.IsNullOrWhiteSpace(ClientId)
                              && !string.IsNullOrWhiteSpace(ClientSecret);
    }

    /// <summary>Một giá trị động lực theo ca do EMS trả về (đã tổng theo ca).</summary>
    public record EmsGiaTriCa(DateOnly Ngay, byte CaSX, string TagID, decimal? GiaTri);

    /// <summary>
    /// Gọi API EMS (TagGateway):
    ///   POST {TokenPath}  — client_credentials (multipart form) → access_token, hết hạn sau expires_in giây
    ///   GET  {ValuePath}?tagId=&amp;time=yyyy-MM-dd 00:00:00&amp;shift=1|2 — 1 tag × 1 ngày × 1 ca
    ///        → {"tagId":5467,"Value":106.66545}; không có dữ liệu → null
    /// shift: 1 = ca ngày, 2 = ca đêm. Lưu ý: tag không tồn tại EMS vẫn trả Value = 0.
    /// </summary>
    public class EmsApiService
    {
        private static readonly SemaphoreSlim _khoaToken = new(1, 1);
        private static string? _token;
        private static DateTime _tokenHetHanUtc = DateTime.MinValue;

        private readonly IHttpClientFactory _httpFactory;
        private readonly IConfiguration _configuration;

        public EmsApiService(IHttpClientFactory httpFactory, IConfiguration configuration)
        {
            _httpFactory = httpFactory;
            _configuration = configuration;
        }

        public EmsApiOptions Options => _configuration.GetSection("EmsApi").Get<EmsApiOptions>() ?? new EmsApiOptions();

        public async Task<List<EmsGiaTriCa>> LayGiaTriTheoCaAsync(DateOnly tuNgay, DateOnly denNgay, IReadOnlyCollection<string> tagIds, CancellationToken ct = default)
        {
            var opts = Options;
            if (!opts.DaCauHinh)
                throw new InvalidOperationException("Chưa cấu hình EmsApi (BaseUrl, ClientId, ClientSecret) trong appsettings.json.");
            if (tagIds.Count == 0) return [];

            var client = _httpFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);

            var viecCanGoi = new List<(DateOnly Ngay, byte Ca, string Tag)>();
            for (var ngay = tuNgay; ngay <= denNgay; ngay = ngay.AddDays(1))
                foreach (byte ca in new byte[] { 1, 2 })
                    foreach (var tag in tagIds)
                        viecCanGoi.Add((ngay, ca, tag));

            using var gioiHan = new SemaphoreSlim(Math.Max(1, opts.SoLuongGoiSongSong));
            var tasks = viecCanGoi.Select(async v =>
            {
                await gioiHan.WaitAsync(ct);
                try
                {
                    var giaTri = await GoiGiaTriCaAsync(client, opts, v.Ngay, v.Ca, v.Tag, ct);
                    return giaTri is null ? null : new EmsGiaTriCa(v.Ngay, v.Ca, v.Tag, giaTri);
                }
                finally
                {
                    gioiHan.Release();
                }
            });

            var ketQua = await Task.WhenAll(tasks);
            return ketQua.Where(x => x != null).Select(x => x!).ToList();
        }

        /// <summary>Giá trị 1 tag × 1 ngày × 1 ca; null = EMS không có dữ liệu.</summary>
        private async Task<decimal?> GoiGiaTriCaAsync(HttpClient client, EmsApiOptions opts, DateOnly ngay, byte ca, string tag, CancellationToken ct)
        {
            // CaSX = 2 tính vào ngày bắt đầu ca; nếu EMS gắn ca đêm vào ngày kết thúc thì lệch +1 ngày
            var ngayEms = ca == 2 ? ngay.AddDays(opts.Ca2LechNgay) : ngay;
            var url = $"{opts.BaseUrl!.TrimEnd('/')}{opts.ValuePath}" +
                      $"?tagId={Uri.EscapeDataString(tag)}" +
                      $"&time={Uri.EscapeDataString(ngayEms.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + " 00:00:00")}" +
                      $"&shift={ca}";

            for (var lan = 1; ; lan++)
            {
                var token = await LayTokenAsync(client, opts, lamMoi: lan > 1, ct);
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                using var resp = await client.SendAsync(req, ct);
                if (resp.StatusCode == HttpStatusCode.Unauthorized && lan == 1)
                    continue; // token hết hạn phía EMS → xin token mới, thử lại 1 lần

                if (!resp.IsSuccessStatusCode)
                    throw new HttpRequestException($"EMS trả {(int)resp.StatusCode} cho tag {tag}, ngày {ngay:dd/MM/yyyy}, ca {ca}.");

                var body = await resp.Content.ReadAsStringAsync(ct);
                return DocGiaTri(body, tag, ngay, ca);
            }
        }

        private static decimal? DocGiaTri(string body, string tag, DateOnly ngay, byte ca)
        {
            if (string.IsNullOrWhiteSpace(body)) return null;

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Null) return null;
            if (root.ValueKind != JsonValueKind.Object)
                throw new InvalidOperationException($"EMS trả dữ liệu không đúng định dạng cho tag {tag}, ngày {ngay:dd/MM/yyyy}, ca {ca}.");

            foreach (var prop in root.EnumerateObject())
            {
                if (!string.Equals(prop.Name, "Value", StringComparison.OrdinalIgnoreCase)) continue;
                return prop.Value.ValueKind switch
                {
                    JsonValueKind.Number => prop.Value.GetDecimal(),
                    JsonValueKind.String when decimal.TryParse(prop.Value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d) => d,
                    _ => null,
                };
            }
            return null;
        }

        private async Task<string> LayTokenAsync(HttpClient client, EmsApiOptions opts, bool lamMoi, CancellationToken ct)
        {
            if (!lamMoi && _token != null && DateTime.UtcNow < _tokenHetHanUtc)
                return _token;

            await _khoaToken.WaitAsync(ct);
            try
            {
                // Luồng khác có thể vừa xin xong
                if (!lamMoi && _token != null && DateTime.UtcNow < _tokenHetHanUtc)
                    return _token;

                using var form = new MultipartFormDataContent
                {
                    { new StringContent("client_credentials"), "grant_type" },
                    { new StringContent(opts.Scope), "scope" },
                    { new StringContent(opts.ClientId!), "client_id" },
                    { new StringContent(opts.ClientSecret!), "client_secret" },
                };

                using var resp = await client.PostAsync($"{opts.BaseUrl!.TrimEnd('/')}{opts.TokenPath}", form, ct);
                if (!resp.IsSuccessStatusCode)
                    throw new HttpRequestException($"Không lấy được token EMS ({(int)resp.StatusCode}). Kiểm tra ClientId/ClientSecret.");

                using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
                var root = doc.RootElement;
                var token = root.TryGetProperty("access_token", out var t) ? t.GetString() : null;
                if (string.IsNullOrEmpty(token))
                    throw new InvalidOperationException("Token EMS trả về không có access_token.");

                var hetHanSau = root.TryGetProperty("expires_in", out var e) && e.TryGetInt32(out var s) ? s : 1800;
                _token = token;
                _tokenHetHanUtc = DateTime.UtcNow.AddSeconds(Math.Max(60, hetHanSau - 60)); // làm mới trước 60 giây
                return token;
            }
            finally
            {
                _khoaToken.Release();
            }
        }
    }
}

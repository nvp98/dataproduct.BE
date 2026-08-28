using dataproduct.api.DTOs.Export;
using dataproduct.api.DTOs.NMTKVV_Dto;
using dataproduct.api.Repositories;
using dataproduct.api.Repositories.NMTKVV;
using DinkToPdf;
using DinkToPdf.Contracts;
using System.Text;

namespace dataproduct.api.Services.NMTKVV
{
    public class TKVV_TonSiloService
    {
        private readonly ITKVV_TonSiloRepository _repo;
        private readonly IPhieuRepository _repoPhieu;
        private readonly IConverter _pdfConverter;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly PheDuyetService _pheDuyetService;

        public TKVV_TonSiloService(
            ITKVV_TonSiloRepository repo,
            IPhieuRepository repoPhieu,
            IConverter pdfConverter,
            IWebHostEnvironment env,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            PheDuyetService pheDuyetService)
        {
            _repo = repo;
            _repoPhieu = repoPhieu;
            _pdfConverter = pdfConverter;
            _env = env;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _pheDuyetService = pheDuyetService;
        }

        public Task<List<TKVVTonSiloRowDto>> InitRowsAsync(InitTonSiloRowsRequestDto request)
            => _repo.InitRowsAsync(request);

        public Task<List<TKVVTonSiloRowDto>> GetRowsByPhieuIdAsync(Guid phieuId)
            => _repo.GetRowsByPhieuIdAsync(phieuId);

        public Task SavePhieuRowsAsync(SaveTonSiloPhieuRequestDto request)
            => _repo.SavePhieuRowsAsync(request);

        // ─── Export PDF Sổ theo dõi Xuất Nhập Tồn Silo (BM.05-QT.05.03) ─────────
        // Cùng cơ chế với TKVV_BBSLService.ExportBienBanPdfAsync (Biên bản sản lượng):
        // HTML template + DinkToPdf.

        private static readonly Dictionary<int, string> ScopeCodeMap = new()
        {
            { 1, "TK1" }, { 2, "TK2" }, { 3, "TK3" }, { 4, "TK4" },
            { 5, "VV1" }, { 6, "VV2" },
        };

        public async Task<ExportFileResult> ExportTonSiloPdfAsync(Guid idPhieu)
        {
            var phieu = await _repoPhieu.GetByIdAsync(idPhieu)
                ?? throw new Exception("Không tìm thấy phiếu.");

            var rows = (await _repo.GetRowsByPhieuIdAsync(idPhieu))
                .OrderBy(x => x.ThuTu)
                .ToList();

            var ngay = phieu.NgaySX ?? DateOnly.FromDateTime(DateTime.Today);
            var ca = phieu.Ca ?? 0;
            var tenScope = phieu.TenScope
                ?? (phieu.Scope.HasValue && ScopeCodeMap.TryGetValue(phieu.Scope.Value, out var code) ? code : "");

            var pheDuyets = await _pheDuyetService.GetPheDuyetPhieuAsync(idPhieu);
            var nguoiGiaoKip = pheDuyets.FirstOrDefault(x => x.CapDuyet == 0);
            var nguoiNhanKip = pheDuyets.FirstOrDefault(x => x.CapDuyet == 1);

            // Không có dòng Tổng — khớp đúng biểu mẫu giấy gốc (BM.05/QT.05.03), chỉ liệt kê
            // từng Silo theo STT, không cộng dồn (các Silo khác NVL/đơn vị không cộng chung được).
            var rowsHtml = new StringBuilder();
            int stt = 0;
            foreach (var r in rows)
            {
                stt++;
                rowsHtml.Append("<tr class=\"data-row\">");
                rowsHtml.Append($"<td>{stt}</td>");
                rowsHtml.Append($"<td>{System.Net.WebUtility.HtmlEncode(r.MaSilo ?? "")}</td>");
                rowsHtml.Append($"<td class=\"text-left\">{System.Net.WebUtility.HtmlEncode(r.TenNVL ?? "")}</td>");
                rowsHtml.Append($"<td class=\"text-right\">{(r.DoAm.HasValue ? r.DoAm.Value.ToString("#,##0.##") : "")}</td>");
                rowsHtml.Append($"<td class=\"text-right\">{(r.TonDau.HasValue ? r.TonDau.Value.ToString("#,##0.###") : "")}</td>");
                rowsHtml.Append($"<td class=\"text-right\">{(r.Nhap.HasValue ? r.Nhap.Value.ToString("#,##0.###") : "")}</td>");
                rowsHtml.Append($"<td class=\"text-right\">{(r.Xuat.HasValue ? r.Xuat.Value.ToString("#,##0.###") : "")}</td>");
                rowsHtml.Append($"<td class=\"text-right\">{(r.TonCuoi.HasValue ? r.TonCuoi.Value.ToString("#,##0.###") : "")}</td>");
                rowsHtml.Append($"<td>{System.Net.WebUtility.HtmlEncode(r.GhiChu ?? "")}</td>");
                rowsHtml.Append("</tr>");
            }

            var logoBase64 = $"data:image/png;base64,{Convert.ToBase64String(await File.ReadAllBytesAsync(Path.Combine(_env.WebRootPath, "imgs", "LogoPDF.png")))}";
            var signGiaoKip = await FormatChuKyBase64Async(nguoiGiaoKip?.ChuKy, nguoiGiaoKip?.TinhTrang == 1);
            var signNhanKip = await FormatChuKyBase64Async(nguoiNhanKip?.ChuKy, nguoiNhanKip?.TinhTrang == 1);

            var templatePath = Path.Combine(
                _env.WebRootPath, "template_html", "BM.05-QT.05.03_So_theo_doi_xuat_nhap_ton_silo.html");
            var html = await File.ReadAllTextAsync(templatePath);

            html = html
                .Replace("{{LogoUrl}}", logoBase64)
                .Replace("{{Xuong}}", System.Net.WebUtility.HtmlEncode(tenScope))
                .Replace("{{Ca}}", ca == 1 ? "Ca ngày" : ca == 2 ? "Ca đêm" : $"Ca {ca}")
                .Replace("{{Ngay}}", ngay.Day.ToString("00"))
                .Replace("{{Thang}}", ngay.Month.ToString("00"))
                .Replace("{{Nam}}", ngay.Year.ToString())
                .Replace("{{Rows}}", rowsHtml.ToString())
                .Replace("{{Sign_GiaoKip}}", signGiaoKip)
                .Replace("{{Sign_NhanKip}}", signNhanKip)
                .Replace("{{Name_GiaoKip}}", System.Net.WebUtility.HtmlEncode(nguoiGiaoKip?.HoVaTen ?? ""))
                .Replace("{{Name_NhanKip}}", System.Net.WebUtility.HtmlEncode(nguoiNhanKip?.HoVaTen ?? ""));

            var doc = new HtmlToPdfDocument
            {
                GlobalSettings =
                {
                    PaperSize = PaperKind.A4,
                    Orientation = Orientation.Landscape,
                },
                Objects =
                {
                    new ObjectSettings
                    {
                        HtmlContent = html,
                        WebSettings =
                        {
                            DefaultEncoding = "utf-8",
                            LoadImages = true,
                            EnableJavascript = false,
                            PrintMediaType = true,
                        },
                        LoadSettings =
                        {
                            BlockLocalFileAccess = false,
                            LoadErrorHandling = ContentErrorHandling.Ignore,
                        }
                    }
                }
            };

            var pdfBytes = _pdfConverter.Convert(doc);
            var fileName = $"TonSilo_{phieu.SoPhieu ?? idPhieu.ToString("N")}_{DateTime.Now:yyyyMMdd_HHmm}.pdf";

            return new ExportFileResult
            {
                Content = pdfBytes,
                FileName = fileName,
                ContentType = "application/pdf",
            };
        }

        private async Task<string> ConvertImageUrlToBase64Async(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return "";

            if (imageUrl.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
                return imageUrl;

            try
            {
                using var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10);
                var imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
                var base64 = Convert.ToBase64String(imageBytes);

                var extension = Path.GetExtension(imageUrl).ToLower();
                var mimeType = extension switch
                {
                    ".png" => "image/png",
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".gif" => "image/gif",
                    ".svg" => "image/svg+xml",
                    _ => "image/png"
                };

                return $"data:{mimeType};base64,{base64}";
            }
            catch
            {
                return "";
            }
        }

        private async Task<string> FormatChuKyBase64Async(string? chuKy, bool daKy = false)
        {
            if (string.IsNullOrWhiteSpace(chuKy))
                return daKy ? "<div style='font-style:italic;color:red'>Đã ký</div>" : "";

            if (chuKy.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
                return $"<img src=\"{chuKy}\" style=\"max-width:150px;max-height:80px;\" />";

            if (chuKy.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                var base64 = await ConvertImageUrlToBase64Async(chuKy);
                if (!string.IsNullOrEmpty(base64))
                    return $"<img src=\"{base64}\" style=\"max-width:150px;max-height:80px;\" />";
            }
            else if (chuKy.StartsWith("/"))
            {
                var domain = _configuration.GetValue<string>("AppSettings:Domain") ?? "https://report.hoaphatdungquat.vn";
                var fullUrl = domain.TrimEnd('/') + chuKy;
                var base64 = await ConvertImageUrlToBase64Async(fullUrl);
                if (!string.IsNullOrEmpty(base64))
                    return $"<img src=\"{base64}\" style=\"max-width:150px;max-height:80px;\" />";
            }

            return daKy ? "<div style='font-style:italic;color:red'>Đã ký</div>" : "";
        }
    }
}

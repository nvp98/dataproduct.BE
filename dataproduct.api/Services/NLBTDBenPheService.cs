using dataproduct.api.Models;
using dataproduct.api.DTOs.Export;
using dataproduct.api.Repositories;
using dataproduct.api.Utils;
using DinkToPdf;
using DinkToPdf.Contracts;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;

namespace dataproduct.api.Services
{
    public class NLBTDBenPheService
    {
        private readonly INL_BTDBenPheRepository _repo;
        private readonly IPhieuRepository _repoPhieu;
        private readonly IConverter _pdfConverter;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly PheDuyetService _pheDuyetService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ProductFormContext _context;

        public NLBTDBenPheService(
            INL_BTDBenPheRepository repo,
            IPhieuRepository repoPhieu,
            IConverter pdfConverter,
            IWebHostEnvironment env,
            IConfiguration configuration,
            PheDuyetService pheDuyetService,
            IHttpClientFactory httpClientFactory,
            ProductFormContext context)
        {
            _repo = repo;
            _repoPhieu = repoPhieu;
            _pdfConverter = pdfConverter;
            _env = env;
            _configuration = configuration;
            _pheDuyetService = pheDuyetService;
            _httpClientFactory = httpClientFactory;
            _context = context;
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

        /// <summary>
        /// Export PDF cho BM.18-HD.25.08 Bảng theo dõi ben phế
        /// </summary>
        public async Task<ExportFileResult> ExportPdfAsync(Guid phieuId, List<string>? filters = null)
        {
            var phieu = await _repoPhieu.GetByIdAsync(phieuId);
            if (phieu == null)
                throw new Exception("Không tìm thấy phiếu");

            var data = (await _repo.GetByPhieuIdAsync(phieuId)).ToList();
            if (filters != null && filters.Count > 0)
            {
                // Áp dụng lọc động nếu có
                data = data.Where(d =>
                {
                    // Lọc theo GhiChu (nếu có filter ghiChu: format là "ghiChu:value1|value2|...")
                    var ghiChuFilter = filters.FirstOrDefault(f => f.StartsWith("ghiChu:"));
                    if (ghiChuFilter != null)
                    {
                        var selectedValues = ghiChuFilter.Substring(7).Split('|', StringSplitOptions.RemoveEmptyEntries);
                        if (selectedValues.Length > 0 && !selectedValues.Contains(d.GhiChu ?? ""))
                            return false;
                    }

                    // Lọc theo KhoiLuong (min/max)
                    var minKlFilter = filters.FirstOrDefault(f => f.StartsWith("minKL:"));
                    if (minKlFilter != null && decimal.TryParse(minKlFilter.Substring(6), out decimal minKl))
                    {
                        if ((d.KhoiLuong ?? 0) < minKl)
                            return false;
                    }

                    var maxKlFilter = filters.FirstOrDefault(f => f.StartsWith("maxKL:"));
                    if (maxKlFilter != null && decimal.TryParse(maxKlFilter.Substring(6), out decimal maxKl))
                    {
                        if ((d.KhoiLuong ?? 0) > maxKl)
                            return false;
                    }

                    return true;
                }).ToList();
            }
            if (!data.Any())
                throw new Exception("Không có dữ liệu bảng theo dõi ben phế để xuất PDF");

            var pheDuyets = await _pheDuyetService.GetPheDuyetPhieuAsync(phieuId);

            // Lấy thông tin từ phiếu
            var ca = phieu.Ca ?? 1;
            var kip = phieu.Kip ?? "";
            var ngaySX = phieu.NgaySX ?? DateOnly.FromDateTime(DateTime.Today);
            var ngaySXText = $"Ngày {ngaySX.Day:00} tháng {ngaySX.Month:00} năm {ngaySX.Year}";



            // Lấy người phê duyệt theo cấp
            var nguoiNhanHRC1 = pheDuyets.FirstOrDefault(x => x.CapDuyet == 1)
                ?? pheDuyets.FirstOrDefault();
            var nguoiNhanNL = pheDuyets.FirstOrDefault(x => x.CapDuyet == 0)
                ?? pheDuyets.LastOrDefault();

            // Xây dựng table rows
            var rows = new StringBuilder();
            int stt = 1;
            foreach (var item in data.OrderBy(x => x.ID))
            {
                rows.Append($@"
                <tr>
                    <td>{stt++}</td>
                    <td>{item.Ca + item.Kip}</td>
                    <td>{item.NgaySX?.ToString("dd/MM/yyyy")}</td>
                    <td>{item.MaBSX}</td>
                    <td>{item.SoHieuBen}</td>
                    <td>{(item.KhoiLuong.HasValue ? item.KhoiLuong.Value.ToString("N3") : "")}</td>
                    <td>{item.GhiChu}</td>
                </tr>");
            }

            // Tính tổng khối lượng
            var tongKL = data.Sum(x => x.KhoiLuong ?? 0);

            // thêm dòng tổng vào cuối bảng
            rows.Append($@"
                <tr>
                    <td style='text-align:right;font-weight:bold'>Tổng</td>
                    <td></td>
                    <td></td>
                    <td></td>
                    <td></td>
                    <td style='font-weight:bold'>{tongKL.ToString("N3")}</td>
                    <td></td>
                </tr>");

            // Load template HTML
            var templatePath = Path.Combine(
                _env.WebRootPath,
                "template_html",
                "BM.18-HD.25.08_Bang_theo_doi_ben_phe.html"
            );
            var html = await File.ReadAllTextAsync(templatePath);

            // Logo
            var logoUrl = _configuration.GetValue<string>("AppSettings:LogoUrl")
                          ?? "https://report.hoaphatdungquat.vn/img/logoHP.png";
            var logoBase64 = await ConvertImageUrlToBase64Async(logoUrl);

            // Format chữ ký
            var signHRC1 = await FormatChuKyBase64Async(nguoiNhanHRC1?.ChuKy, nguoiNhanHRC1?.TinhTrang == 1);
            var signNL = await FormatChuKyBase64Async(nguoiNhanNL?.ChuKy, nguoiNhanNL?.TinhTrang == 1);

            // Replace placeholders
            html = html
                .Replace("{{LogoUrl}}", logoBase64)
                .Replace("{{Ca}}", ca.ToString())
                .Replace("{{Kip}}", kip)
                .Replace("{{NgaySX}}", ngaySXText)
                .Replace("{{Rows}}", rows.ToString())
                // .Replace("{{TongKL}}", tongKL.ToString("N3"))
                .Replace("{{ChuKyNguoiNhanHRC1}}", signHRC1)
                .Replace("{{ChuKyNguoiNhanNL}}", signNL)
                .Replace("{{NguoiNhanHRC1}}", nguoiNhanHRC1?.HoVaTen ?? "")
                .Replace("{{NguoiNhanNL}}", nguoiNhanNL?.HoVaTen ?? "");

            // Convert to PDF
            var doc = new HtmlToPdfDocument
            {
                GlobalSettings =
                {
                    PaperSize = PaperKind.A4,
                    Orientation = Orientation.Portrait
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
                            PrintMediaType = true
                        },
                        LoadSettings =
                        {
                            BlockLocalFileAccess = false,
                            LoadErrorHandling = ContentErrorHandling.Ignore
                        }
                    }
                }
            };

            var pdfBytes = _pdfConverter.Convert(doc);

            return new ExportFileResult
            {
                Content = pdfBytes,
                FileName = $"BM.18-HD.25.08_Bang_theo_doi_ben_phe_{ngaySX:yyyyMMdd}_Ca{ca}{kip}_{DateTime.Now:HHmmss}.pdf",
                ContentType = "application/pdf"
            };
        }

        /// <summary>
        /// Convert image URL to Base64 string for embedding in HTML
        /// </summary>
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
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting image URL to base64: {imageUrl} - {ex.Message}");
                return "";
            }
        }

        /// <summary>
        /// Format chữ ký từ URL hoặc base64 để nhúng vào HTML
        /// </summary>
        private async Task<string> FormatChuKyBase64Async(string? chuKy, bool daKy = false)
        {
            if (string.IsNullOrWhiteSpace(chuKy))
            {
                if (daKy)
                {
                    return @"
                        <div style='text-align:center'>
                            <div style='font-style:italic;color:red'>Đã ký</div>
                            <div style='font-size:11px;color:red'>(Chưa cập nhật chữ ký)</div>
                        </div>";
                }
                return "";
            }

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

            return @"
                    <div style='text-align:center'>
                        <div style='font-style:italic;color:red'>Đã ký</div>
                        <div style='font-size:11px;color:red'>(Chưa cập nhật chữ ký)</div>
                    </div>";
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

        /// <summary>
        /// Export Excel tổng hợp bảng theo dõi ben phế theo khoảng thời gian
        /// </summary>
        public async Task<byte[]> ExportExcelByBmPhieuAsync(DateOnly? fromDate, DateOnly? toDate)
        {
            var maBmList = new[]
            {
                "NL_BB_TheoDoiBenPhe",
                "BM.18-HD.25.08",
                "BM.18/HD.25.08"
            };

            var phieuQuery = _context.BmPhieus
                .AsNoTracking()
                .Where(x => x.IsDelete != 1 && maBmList.Contains(x.MaBm));

            if (fromDate.HasValue)
                phieuQuery = phieuQuery.Where(x => x.NgaySX >= fromDate.Value);
            if (toDate.HasValue)
                phieuQuery = phieuQuery.Where(x => x.NgaySX <= toDate.Value);

            var phieus = await phieuQuery
                .Select(x => new { x.Idphieu, x.SoPhieu, x.NgaySX, x.Ca, x.Kip, x.TinhTrang })
                .OrderBy(x => x.NgaySX).ThenBy(x => x.Ca).ThenBy(x => x.Kip)
                .ToListAsync();

            var phieuIds = phieus.Select(x => x.Idphieu).ToList();

            var dataList = await _context.NL_BTDBenPhes
                .AsNoTracking()
                .Where(x => x.IDPhieu.HasValue && phieuIds.Contains(x.IDPhieu.Value))
                .ToListAsync();

            var templatePath = Path.Combine(_env.WebRootPath, "templates", "BM_TongHopTheoDoiBenPhe.xlsx");
            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"Không tìm thấy file mẫu Excel: {templatePath}");

            using var workbook = new XLWorkbook(templatePath);
            var ws = workbook.Worksheet(1);

            // Cập nhật tiêu đề với khoảng thời gian
            if (fromDate.HasValue && toDate.HasValue)
            {
                ws.Cell("C3").Value = $"Từ ngày: {fromDate:dd/MM/yyyy} đến ngày: {toDate:dd/MM/yyyy}";
            }
            else if (fromDate.HasValue)
            {
                ws.Cell("C3").Value = $"Từ ngày: {fromDate:dd/MM/yyyy}";
            }
            else if (toDate.HasValue)
            {
                ws.Cell("C3").Value = $"Đến ngày: {toDate:dd/MM/yyyy}";
            }

            // Xây dựng dữ liệu
            var rows = (from p in phieus
                        join d in dataList on p.Idphieu equals d.IDPhieu
                        orderby p.NgaySX, p.Ca, p.Kip, p.SoPhieu
                        select new { p, d }).ToList();

            const int startRow = 3; // Dòng bắt đầu chèn dữ liệu (sau 2 dòng tiêu đề)
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var r = startRow + i;

                // Copy style từ dòng template
                if (i > 0)
                    ws.Row(startRow).CopyTo(ws.Row(r));

                ws.Cell(r, 1).Value = r - startRow + 1; // STT
                ws.Cell(r, 2).Value = row.p.NgaySX?.ToDateTime(TimeOnly.MinValue);
                ws.Cell(r, 3).Value = row.p.Ca;
                ws.Cell(r, 4).Value = row.p.Kip;
                ws.Cell(r, 5).Value = row.d.MaBSX;
                ws.Cell(r, 6).Value = row.d.SoHieuBen;
                ws.Cell(r, 7).Value = row.d.KhoiLuong.HasValue ? row.d.KhoiLuong.Value.ToString("N2") : "";
                ws.Cell(r, 8).Value = row.d.GhiChu;
                ws.Cell(r, 9).Value = row.p.SoPhieu;
                ws.Cell(r, 10).Value = PhieuStatusDisplay.GetText(row.p.TinhTrang);
            }

            // Format cột ngày
            if (rows.Count > 0)
                ws.Range(startRow, 2, startRow + rows.Count - 1, 2)
                   .Style.DateFormat.Format = "dd/MM/yyyy";

            // Save to bytes
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

    }
}

using dataproduct.api.Models;
using dataproduct.api.DTOs.Export;
using dataproduct.api.Repositories;
using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;

namespace dataproduct.api.Services
{
    public class CTDGiaoNhanPhoiService
    {
        private readonly ProductFormContext _context;
        private readonly IPhieuRepository _repoPhieu;
        private readonly IConverter _pdfConverter;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly PheDuyetService _pheDuyetService;
        private readonly IHttpClientFactory _httpClientFactory;

        public CTDGiaoNhanPhoiService(
            ProductFormContext context,
            IPhieuRepository repoPhieu,
            IConverter pdfConverter,
            IWebHostEnvironment env,
            IConfiguration configuration,
            PheDuyetService pheDuyetService,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _repoPhieu = repoPhieu;
            _pdfConverter = pdfConverter;
            _env = env;
            _configuration = configuration;
            _pheDuyetService = pheDuyetService;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<int> InsertFromPhieuJsonAsync(BmPhieu phieu)
        {
            if (phieu == null)
                throw new ArgumentNullException(nameof(phieu));

            var entities = new List<CtdGiaoNhanPhoi>();

            if (!string.IsNullOrWhiteSpace(phieu.DataJson))
            {
                using var jsonDoc = JsonDocument.Parse(phieu.DataJson);
                var root = jsonDoc.RootElement;

                if (TryGetRowsElement(root, out var rows))
                {
                    foreach (var row in rows.EnumerateArray())
                    {
                        if (row.ValueKind != JsonValueKind.Object)
                            continue;

                        entities.Add(new CtdGiaoNhanPhoi
                        {
                            IdPhieu = phieu.Idphieu,
                            MaViTri = TryGetString(row, "mavitri", "maViTri", "MaViTri"),
                            ViTri = TryGetString(row, "vitri", "viTri", "ViTri"),
                            MacThep = TryGetString(row, "macThep", "MacThep"),
                            KichThuoc = TryGetString(row, "kichThuoc", "KichThuoc"),
                            SoCay = TryGetInt(row, "soCay", "SoCay"),
                            GhiChu = TryGetString(row, "ghiChu", "GhiChu")
                        });
                    }
                }
            }

            await _context.CtdGiaoNhanPhois
                .Where(x => x.IdPhieu == phieu.Idphieu)
                .ExecuteDeleteAsync();

            if (entities.Count == 0)
                return 0;

            await _context.CtdGiaoNhanPhois.AddRangeAsync(entities);
            await _context.SaveChangesAsync();
            return entities.Count;
        }

        public async Task<ExportFileResult> ExportPdfAsync(Guid phieuId)
        {
            var phieu = await _repoPhieu.GetByIdAsync(phieuId);
            if (phieu == null)
                throw new Exception("Không tìm thấy phiếu");

            var data = await _context.CtdGiaoNhanPhois
                .AsNoTracking()
                .Where(x => x.IdPhieu == phieuId)
                .OrderBy(x => x.Id)
                .ToListAsync();

            if (!data.Any())
                throw new Exception("Không có dữ liệu giao nhận phôi để xuất PDF");

            var pheDuyets = await _pheDuyetService.GetPheDuyetPhieuAsync(phieuId);

            var ca = phieu.Ca ?? 1;
            var kip = phieu.Kip ?? "";
            var ngaySX = phieu.NgaySX ?? DateOnly.FromDateTime(DateTime.Today);
            var xuong = phieu.Scope?.ToString() ?? "";

            var tuNgay = ngaySX.ToString("dd/MM/yyyy");
            var denNgay = ngaySX.ToString("dd/MM/yyyy");
            var tuGio = ca == 1 ? "08" : "20";
            var denGio = ca == 1 ? "20" : "08";
            if (ca == 2)
                denNgay = ngaySX.AddDays(1).ToString("dd/MM/yyyy");

            var nguoiNhanKip = pheDuyets.FirstOrDefault(x => x.CapDuyet == 1)
                ?? pheDuyets.FirstOrDefault();
            var nguoiGiaoKip = pheDuyets.FirstOrDefault(x => x.CapDuyet == 0);

            var grouped = data
                .GroupBy(x => string.IsNullOrWhiteSpace(x.MaViTri) ? (x.ViTri ?? "") : x.MaViTri!)
                .ToList();

            var rows = new StringBuilder();
            var tongSoCay = data.Sum(x => x.SoCay ?? 0);
            int stt = 1;
            foreach (var group in grouped)
            {
                var items = group.ToList();
                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    rows.Append("<tr class=\"data-row\">");

                    if (i == 0)
                    {
                        rows.Append($"<td rowspan=\"{items.Count}\">{stt}</td>");
                        rows.Append($"<td class=\"text-left\" rowspan=\"{items.Count}\">{item.ViTri ?? ""}</td>");
                    }

                    rows.Append($"<td>{item.MacThep ?? ""}</td>");
                    rows.Append($"<td>{item.KichThuoc ?? ""}</td>");
                    rows.Append($"<td>{item.SoCay?.ToString() ?? ""}</td>");
                    rows.Append($"<td>{item.GhiChu ?? ""}</td>");
                    rows.Append("</tr>");
                }
                stt++;
            }

            rows.Append($@"
                <tr class=""total-row"">
                    <td colspan=""2"">TỔNG SỐ</td>
                    <td></td>
                    <td></td>
                    <td>{tongSoCay:N0}</td>
                    <td></td>
                </tr>");

            var logoUrl = _configuration.GetValue<string>("AppSettings:LogoUrl")
                          ?? "https://report.hoaphatdungquat.vn/img/logoHP.png";
            var logoBase64 = await ConvertImageUrlToBase64Async(logoUrl);

            var signNhan = await FormatChuKyBase64Async(nguoiNhanKip?.ChuKy, nguoiNhanKip?.TinhTrang == 1);
            var signGiao = await FormatChuKyBase64Async(nguoiGiaoKip?.ChuKy, nguoiGiaoKip?.TinhTrang == 1);

            var templatePath = Path.Combine(
                _env.WebRootPath,
                "template_html",
                "BM.05-QT.05.13_Bien_ban_giao_nhan_phoi.html"
            );
            var html = await File.ReadAllTextAsync(templatePath);

            html = html
                .Replace("{{LogoUrl}}", logoBase64)
                .Replace("{{xuong}}", xuong)
                .Replace("{{Ca}}", ca.ToString())
                .Replace("{{Kip}}", kip)
                .Replace("{{TuGio}}", tuGio)
                .Replace("{{TuNgay}}", tuNgay)
                .Replace("{{DenGio}}", denGio)
                .Replace("{{DenNgay}}", denNgay)
                .Replace("{{Rows}}", rows.ToString())
                .Replace("{{Sign_NguoinhanKip}}", signNhan)
                .Replace("{{Sign_NguoigiaoKip}}", signGiao)
                .Replace("{{Name_NguoinhanKip}}", nguoiNhanKip?.HoVaTen ?? "")
                .Replace("{{Name_NguoigiaoKip}}", nguoiGiaoKip?.HoVaTen ?? "");

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
                FileName = $"BM.05-QT.05.13_Bien_ban_giao_nhan_phoi_{ngaySX:yyyyMMdd}_Ca{ca}{kip}_{DateTime.Now:HHmmss}.pdf",
                ContentType = "application/pdf"
            };
        }

        private bool TryGetRowsElement(JsonElement root, out JsonElement rows)
        {
            rows = default;
            return TryGetArray(root, "table1", out rows)
                || TryGetArray(root, "Table1", out rows)
                || TryGetArray(root, "rows", out rows)
                || TryGetArray(root, "Rows", out rows)
                || TryGetArray(root, "data", out rows)
                || TryGetArray(root, "Data", out rows);
        }

        private bool TryGetArray(JsonElement obj, string key, out JsonElement array)
        {
            array = default;
            if (obj.TryGetProperty(key, out var element) && element.ValueKind == JsonValueKind.Array)
            {
                array = element;
                return true;
            }
            return false;
        }

        private string? TryGetString(JsonElement row, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (row.TryGetProperty(key, out var value) && value.ValueKind != JsonValueKind.Null)
                {
                    if (value.ValueKind == JsonValueKind.String)
                        return value.GetString();

                    return value.ToString();
                }
            }

            return null;
        }

        private int? TryGetInt(JsonElement row, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (!row.TryGetProperty(key, out var value) || value.ValueKind == JsonValueKind.Null)
                    continue;

                if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var n))
                    return n;

                if (value.ValueKind == JsonValueKind.String)
                {
                    var raw = value.GetString();
                    if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                        return parsed;
                }
            }

            return null;
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
            {
                return daKy ? "<div style='font-style:italic;color:red'>Đã ký</div>" : "";
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

            return daKy ? "<div style='font-style:italic;color:red'>Đã ký</div>" : "";
        }

        public async Task<ExportFileResult> ExportTongHopExcelByPhieuAsync(DateOnly? fromDate, DateOnly? toDate)
        {
            var maBmList = new[]
            {
                "CTD_BB_GNP",
                "CTD_BB_GiaoNhanPhoi",
                "BM.05-QT.05.13",
                "BM.05/QT.05.13"
            };

            var phieuQuery = _context.BmPhieus
                .AsNoTracking()
                .Where(x => x.IsDelete != 1 && maBmList.Contains(x.MaBm));

            if (fromDate.HasValue)
                phieuQuery = phieuQuery.Where(x => x.NgaySX >= fromDate.Value);

            if (toDate.HasValue)
                phieuQuery = phieuQuery.Where(x => x.NgaySX <= toDate.Value);

            var phieus = await phieuQuery
                .Select(x => new
                {
                    x.Idphieu,
                    x.SoPhieu,
                    x.NgaySX,
                    x.Ca,
                    x.Kip,
                    x.TinhTrang,
                    x.Scope,
                    x.MaBm,
                })
                .ToListAsync();

            var phieuIds = phieus.Select(x => x.Idphieu).ToList();
            var details = await _context.CtdGiaoNhanPhois
                .AsNoTracking()
                .Where(x => x.IdPhieu.HasValue && phieuIds.Contains(x.IdPhieu.Value))
                .ToListAsync();

            var rows = (from p in phieus
                        join d in details on p.Idphieu equals d.IdPhieu
                        orderby p.NgaySX, p.Ca, p.Kip, p.SoPhieu
                        select new
                        {
                            p.SoPhieu,
                            p.NgaySX,
                            p.Ca,
                            p.Kip,
                            d.ViTri,
                            d.MacThep,
                            d.KichThuoc,
                            d.SoCay,
                            d.GhiChu,
                            p.TinhTrang,
                            p.Scope
                        }).ToList();

            var templatePath = Path.Combine(_env.WebRootPath, "templates", "BM_TongHopGiaoNhanPhoi.xlsx");
            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"Không tìm thấy file mẫu Excel: {templatePath}");

            using var workbook = new XLWorkbook(templatePath);
            var ws = workbook.Worksheet(1);

            var fromDateStr = fromDate?.ToString("dd/MM/yyyy") ?? "...";
            var toDateStr = toDate?.ToString("dd/MM/yyyy") ?? "...";
            ws.Cell("G3").Value = $"Từ ngày: {fromDateStr} đến ngày: {toDateStr}";

            var startRow = 9;
            var rowIndex = startRow;
            var stt = 1;

            foreach (var item in rows)
            {
                if (rowIndex > startRow)
                    ws.Row(startRow).CopyTo(ws.Row(rowIndex));

                ws.Cell(rowIndex, 1).Value = stt;
                ws.Cell(rowIndex, 2).Value = item.NgaySX?.ToDateTime(TimeOnly.MinValue);
                ws.Cell(rowIndex, 3).Value = item.Kip;
                ws.Cell(rowIndex, 4).Value = item.Ca;
                ws.Cell(rowIndex, 5).Value = item.ViTri;
                ws.Cell(rowIndex, 6).Value = item.MacThep;
                ws.Cell(rowIndex, 7).Value = item.KichThuoc;
                ws.Cell(rowIndex, 8).Value = item.SoCay;
                ws.Cell(rowIndex, 9).Value = item.GhiChu;
                ws.Cell(rowIndex, 10).Value = item.SoPhieu;

                var statusCell = ws.Cell(rowIndex, 11);
                statusCell.Value = item.TinhTrang switch
                {
                    0 => "Đang lưu",
                    1 => "Đã gửi",
                    2 => "Hoàn thành",
                    3 => "Đã thu hồi",
                    4 => "Không xác nhận",
                    5 => "Đã chốt",
                    6 => "Đang phê duyệt",
                    7 => "Hiệu chỉnh",
                    _ => "Không xác định"
                };

                rowIndex++;
                stt++;
            }

            if (rows.Count > 0)
            {
                ws.Rows(startRow, rowIndex - 1).AdjustToContents();
            }

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            ms.Position = 0;

            var fileName = $"BM_TongHopGiaoNhanPhoi_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}_{DateTime.Now:HHmmss}.xlsx";
            return new ExportFileResult
            {
                Content = ms.ToArray(),
                FileName = fileName,
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            };
        }
    }
}


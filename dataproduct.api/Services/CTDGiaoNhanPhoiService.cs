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
using ClosedXML.Excel.Drawings;
using dataproduct.api.Utils;

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

        public async Task<ExportFileResult> ExportChiTietExcelAsync(Guid phieuId)
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
                throw new Exception("Không có dữ liệu giao nhận phôi để xuất Excel");

            var pheDuyets = await _pheDuyetService.GetPheDuyetPhieuAsync(phieuId);
            var nguoiNhan = pheDuyets.FirstOrDefault(x => x.CapDuyet == 1) ?? pheDuyets.FirstOrDefault();
            var nguoiGiao = pheDuyets.FirstOrDefault(x => x.CapDuyet == 0);

            var ca = phieu.Ca ?? 1;
            var kip = phieu.Kip ?? "";
            var ngaySX = phieu.NgaySX ?? DateOnly.FromDateTime(DateTime.Today);
            var xuong = phieu.Scope?.ToString() ?? "";

            var tuGio = ca == 1 ? "08" : "20";
            var denGio = ca == 1 ? "20" : "08";
            var tuNgay = ngaySX.ToString("dd/MM/yyyy");
            var denNgay = ca == 2 ? ngaySX.AddDays(1).ToString("dd/MM/yyyy") : tuNgay;

            // Logo
            byte[]? logoBytes = null;
            var logoUrl = _configuration.GetValue<string>("AppSettings:LogoUrl") ?? "";
            try
            {
                if (!string.IsNullOrWhiteSpace(logoUrl))
                {
                    using var http = _httpClientFactory.CreateClient();
                    http.Timeout = TimeSpan.FromSeconds(10);
                    logoBytes = await http.GetByteArrayAsync(logoUrl);
                }
            }
            catch { /* logo không bắt buộc */ }

            var htmlPath05 = Path.Combine(_env.WebRootPath, "template_html", "BM.05-QT.05.13_Bien_ban_giao_nhan_phoi.html");
            var bmHeaderText = await HtmlTemplateHelper.GetBmHeaderTextAsync(htmlPath05);

            // Nhóm dữ liệu theo vị trí (giống HTML rowspan)
            var grouped = data
                .GroupBy(x => string.IsNullOrWhiteSpace(x.MaViTri) ? (x.ViTri ?? "") : x.MaViTri!)
                .ToList();

            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("Biên bản giao nhận phôi");

            // 6 cột dữ liệu: STT(1) ViTri(2) MacThep(3) KichThuoc(4) SoCay(5) GhiChu(6)
            const int COLS = 6;
            ws.Style.Font.FontName = "Times New Roman";
            ws.Style.Font.FontSize = 11;

            ws.Column(1).Width = 6;
            ws.Column(2).Width = 28;
            ws.Column(3).Width = 16;
            ws.Column(4).Width = 16;
            ws.Column(5).Width = 10;
            ws.Column(6).Width = 22;

            int row = 1;

            // ── HEADER: Logo (trái, 3 rows) | BM code (phải, merge 3 rows) ──
            ws.Row(row).Height = 36;

            if (logoBytes != null)
            {
                var ext = Path.GetExtension(logoUrl).TrimStart('.').ToLower();
                var fmt = ext == "png" ? XLPictureFormat.Png : XLPictureFormat.Jpeg;
                using var logoMs = new MemoryStream(logoBytes);
                ws.AddPicture(logoMs, fmt)
                    .MoveTo(ws.Cell(row, 1))
                    .Scale(0.38);
            }

            // BM code bên phải — merge 3 rows header
            ws.Cell(row, 4).Value = bmHeaderText;
            ws.Range(row, 4, row + 2, COLS).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(11)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                .Alignment.SetWrapText(true);
            row++;

            // Row 2: "CÔNG TY CỔ PHẦN THÉP" (bên dưới logo)
            ws.Row(row).Height = 16;
            ws.Cell(row, 1).Value = "CÔNG TY CỔ PHẦN THÉP";
            ws.Range(row, 1, row, 3).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(11)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
            row++;

            // Row 3: "HÒA PHÁT DUNG QUẤT"
            ws.Row(row).Height = 16;
            ws.Cell(row, 1).Value = "HÒA PHÁT DUNG QUẤT";
            ws.Range(row, 1, row, 3).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(11)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
            row++;

            // ── TITLE ────────────────────────────────────────────────────────
            ws.Cell(row, 1).Value = "BIÊN BẢN GIAO NHẬN PHÔI";
            ws.Range(row, 1, row, COLS).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            ws.Row(row).Height = 26;
            row++;

            // ── SUB-TITLE ─────────────────────────────────────────────────────
            ws.Cell(row, 1).Value = $"XƯỞNG CÁN {xuong}";
            ws.Range(row, 1, row, COLS).Merge().Style
                .Font.SetFontSize(11)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            ws.Row(row).Height = 16;
            row++;

            ws.Cell(row, 1).Value = $"Kíp: {ca}{kip}   từ {tuGio}h ngày {tuNgay}   đến {denGio}h ngày {denNgay}";
            ws.Range(row, 1, row, COLS).Merge().Style
                .Font.SetFontSize(11)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            ws.Row(row).Height = 16;
            row++;

            // ── TABLE HEADER (2 rows, rowspan/colspan như HTML) ───────────────
            int thRow = row;

            // Row 1 của header:
            // STT (rowspan 2 = merge thRow..thRow+1, col 1)
            ws.Cell(thRow, 1).Value = "STT";
            ws.Range(thRow, 1, thRow + 1, 1).Merge().Style
                .Font.SetBold(true)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#f0f0f0"))
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

            // Vị trí (rowspan 2)
            ws.Cell(thRow, 2).Value = "Vị trí";
            ws.Range(thRow, 2, thRow + 1, 2).Merge().Style
                .Font.SetBold(true)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#f0f0f0"))
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

            // Loại phôi (colspan 2 = col 3-4)
            ws.Cell(thRow, 3).Value = "Loại phôi";
            ws.Range(thRow, 3, thRow, 4).Merge().Style
                .Font.SetBold(true)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#f0f0f0"))
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

            // Số cây (rowspan 2)
            ws.Cell(thRow, 5).Value = "Số cây";
            ws.Range(thRow, 5, thRow + 1, 5).Merge().Style
                .Font.SetBold(true)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#f0f0f0"))
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

            // Ghi chú (rowspan 2)
            ws.Cell(thRow, 6).Value = "Ghi chú";
            ws.Range(thRow, 6, thRow + 1, 6).Merge().Style
                .Font.SetBold(true)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#f0f0f0"))
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

            ws.Row(thRow).Height = 18;
            row++;

            // Row 2 của header: Mác thép | Kích thước
            ws.Cell(row, 3).Value = "Mác thép";
            ws.Cell(row, 3).Style.Font.SetBold(true)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#f0f0f0"))
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

            ws.Cell(row, 4).Value = "Kích thước";
            ws.Cell(row, 4).Style.Font.SetBold(true)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#f0f0f0"))
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

            ws.Row(row).Height = 18;
            row++;

            // ── DATA ROWS (nhóm theo ViTri, merge STT + ViTri như HTML rowspan) ─
            int stt = 1;
            foreach (var group in grouped)
            {
                var items = group.ToList();
                int groupStart = row;

                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];

                    ws.Cell(row, 3).Value = item.MacThep ?? "";
                    ws.Cell(row, 4).Value = item.KichThuoc ?? "";
                    ws.Cell(row, 5).Value = item.SoCay.HasValue ? (XLCellValue)item.SoCay.Value : "";
                    ws.Cell(row, 6).Value = item.GhiChu ?? "";

                    ws.Range(row, 1, row, COLS).Style
                        .Font.SetFontSize(11)
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                        .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                        .Border.SetInsideBorder(XLBorderStyleValues.Thin);

                    ws.Row(row).Height = 20;
                    row++;
                }

                // Merge STT và Vị trí theo số dòng trong nhóm (giống rowspan HTML)
                if (items.Count > 1)
                {
                    ws.Range(groupStart, 1, row - 1, 1).Merge();
                    ws.Range(groupStart, 2, row - 1, 2).Merge();
                }

                ws.Cell(groupStart, 1).Value = stt;
                ws.Cell(groupStart, 1).Style
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

                ws.Cell(groupStart, 2).Value = items[0].ViTri ?? "";
                ws.Cell(groupStart, 2).Style
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left)
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

                stt++;
            }

            // ── TOTAL ROW ────────────────────────────────────────────────────
            var tongSoCay = data.Sum(x => x.SoCay ?? 0);
            ws.Cell(row, 1).Value = "TỔNG SỐ";
            ws.Range(row, 1, row, 2).Merge().Style
                .Font.SetBold(true)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#f5f5f5"))
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                .Border.SetInsideBorder(XLBorderStyleValues.Thin);

            ws.Cell(row, 3).Value = "";
            ws.Cell(row, 4).Value = "";
            ws.Cell(row, 5).Value = tongSoCay;
            ws.Cell(row, 5).Style.Font.SetBold(true)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            ws.Cell(row, 6).Value = "";

            ws.Range(row, 3, row, COLS).Style
                .Font.SetBold(true)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#f5f5f5"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                .Border.SetInsideBorder(XLBorderStyleValues.Thin);

            ws.Row(row).Height = 20;
            row += 2;

            // ── NOTE ─────────────────────────────────────────────────────────
            ws.Cell(row, 1).Value = "Lưu ý: Đầu kíp, 2 tổ trưởng Lò nung của 2 kíp bàn giao cho nhau toàn bộ phôi trong nhà theo mẫu trên.";
            ws.Range(row, 1, row, COLS).Merge().Style
                .Font.SetFontSize(11)
                .Alignment.SetWrapText(true);
            ws.Row(row).Height = 16;
            row += 2;

            // ── CHỮ KÝ ───────────────────────────────────────────────────────
            ws.Cell(row, 1).Value = "Người nhận kíp";
            ws.Range(row, 1, row, 3).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(11)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(row, 4).Value = "Người giao kíp";
            ws.Range(row, 4, row, COLS).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(11)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Row(row).Height = 16;
            row++;

            ws.Row(row).Height = 60; // vùng chữ ký
            row++;

            ws.Cell(row, 1).Value = nguoiNhan?.HoVaTen ?? "";
            ws.Range(row, 1, row, 3).Merge().Style
                .Font.SetFontSize(11)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(row, 4).Value = nguoiGiao?.HoVaTen ?? "";
            ws.Range(row, 4, row, COLS).Merge().Style
                .Font.SetFontSize(11)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // Page setup A4
            ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
            ws.PageSetup.PageOrientation = XLPageOrientation.Portrait;
            ws.PageSetup.Margins.Left = 1.3;
            ws.PageSetup.Margins.Right = 0.8;
            ws.PageSetup.Margins.Top = 0.8;
            ws.PageSetup.Margins.Bottom = 0.6;

            using var ms = new MemoryStream();
            wb.SaveAs(ms);

            return new ExportFileResult
            {
                Content = ms.ToArray(),
                FileName = $"BM.05-QT.05.13_Bien_ban_giao_nhan_phoi_{ngaySX:yyyyMMdd}_Ca{ca}{kip}_{DateTime.Now:HHmmss}.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
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


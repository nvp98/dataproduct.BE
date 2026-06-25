using dataproduct.api.DTOs.CTD_Dto;
using dataproduct.api.DTOs.Export;
using dataproduct.api.Models;
using dataproduct.api.Repositories;
using ClosedXML.Excel;
using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel.Drawings;
using dataproduct.api.Utils;

namespace dataproduct.api.Services
{
    public class CTDPhoiNapNguoiService
    {
        private readonly ICtdPhoiNguoiRepository _repo;
        private readonly IPhieuRepository _repoPhieu;
        private readonly IConverter _pdfConverter;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly PheDuyetService _pheDuyetService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ProductFormContext _context;
        private readonly BmConfigService _bmConfig;

        public CTDPhoiNapNguoiService(
            ICtdPhoiNguoiRepository repo,
            IPhieuRepository repoPhieu,
            IConverter pdfConverter,
            IWebHostEnvironment env,
            IConfiguration configuration,
            PheDuyetService pheDuyetService,
            IHttpClientFactory httpClientFactory,
            ProductFormContext context,
            BmConfigService bmConfig)
        {
            _repo = repo;
            _repoPhieu = repoPhieu;
            _pdfConverter = pdfConverter;
            _env = env;
            _configuration = configuration;
            _pheDuyetService = pheDuyetService;
            _httpClientFactory = httpClientFactory;
            _context = context;
            _bmConfig = bmConfig;
        }

        public async Task<IEnumerable<CtdPhoiNguoi>> GetByPhieuIdAsync(Guid phieuId)
        {
            return await _repo.GetByPhieuIdAsync(phieuId);
        }

        public async Task<int> InsertFromPhieuJsonAsync(BmPhieu phieu)
        {
            if (phieu == null)
                throw new ArgumentNullException(nameof(phieu));
            var entities = new List<CtdPhoiNguoi>();

            if (!string.IsNullOrWhiteSpace(phieu.DataJson))
            {
                using var jsonDoc = JsonDocument.Parse(phieu.DataJson);
                var root = jsonDoc.RootElement;

                JsonElement rowsElement;
                if (TryGetRowsElement(root, out rowsElement))
                {
                    foreach (var row in rowsElement.EnumerateArray())
                    {
                        if (row.ValueKind != JsonValueKind.Object)
                            continue;

                        entities.Add(new CtdPhoiNguoi
                        {
                            PhieuId = phieu.Idphieu,
                            Me = TryGetString(row, "me", "Me"),
                            Mac = TryGetString(row, "mac", "Mac"),
                            KichThuoc = TryGetString(row, "kichThuoc", "KichThuoc"),
                            SoThanh = TryGetInt(row, "tongThanh", "SoThanh"),
                            KhoiLuong = TryGetDouble(row, "khoiLuong", "KhoiLuong"),
                            TongKl = TryGetDouble(row, "tongKL", "TongKl", "tongKhoiLuong", "TongKhoiLuong"),
                            GhiChu = TryGetString(row, "ghiChu", "GhiChu")
                        });
                    }
                }
            }

            // Replace mode: luôn xóa dữ liệu cũ của phiếu trước khi ghi mới.
            await _repo.DeleteByPhieuIdAsync(phieu.Idphieu);

            if (entities.Count == 0)
                return 0;

            await _repo.AddListAsync(entities);

            // ─────────────────────────────────────────────────────────────────────────
            // Insert notes vào BM_Phieu_ChiTiet nếu có key "notes"
            // ─────────────────────────────────────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(phieu.DataJson))
            {
                using var jsonDoc = JsonDocument.Parse(phieu.DataJson);
                var root = jsonDoc.RootElement;

                if (root.TryGetProperty("notes", out var notesElement) &&
                    notesElement.ValueKind != JsonValueKind.Null &&
                    !string.IsNullOrWhiteSpace(notesElement.GetString()))
                {
                    var notesValue = notesElement.GetString();

                    // Xóa record cũ của notes (if any)
                    var oldNotes = await _context.BmPhieuChiTiets
                        .Where(x => x.PhieuId == phieu.Idphieu && x.ThongSo == "notes")
                        .ToListAsync();

                    if (oldNotes.Any())
                    {
                        _context.BmPhieuChiTiets.RemoveRange(oldNotes);
                    }

                    // Insert record mới
                    var notesRecord = new BmPhieuChiTiet
                    {
                        PhieuId = phieu.Idphieu,
                        ThongSo = "notes",
                        GiaTri = notesValue,
                        RowId = null
                    };

                    _context.BmPhieuChiTiets.Add(notesRecord);
                    await _context.SaveChangesAsync();
                }
            }

            return entities.Count;
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
                if (row.TryGetProperty(key, out var value) && value.ValueKind != JsonValueKind.Null)
                {
                    if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var n))
                        return n;

                    if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out var parsed))
                        return parsed;
                }
            }

            return null;
        }

        private double? TryGetDouble(JsonElement row, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (row.TryGetProperty(key, out var value) && value.ValueKind != JsonValueKind.Null)
                {
                    if (value.ValueKind == JsonValueKind.Number)
                        return value.GetDouble();

                    if (value.ValueKind == JsonValueKind.String && double.TryParse(value.GetString(), out var parsed))
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
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting image URL to base64: {imageUrl} - {ex.Message}");
                return "";
            }
        }

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

        public async Task<ExportFileResult> ExportPdfAsync(Guid phieuId)
        {
            var phieu = await _repoPhieu.GetByIdAsync(phieuId);
            if (phieu == null)
                throw new Exception("Không tìm thấy phiếu");

            var data = (await _repo.GetByPhieuIdAsync(phieuId)).ToList();
            if (!data.Any())
                throw new Exception("Không có dữ liệu phôi nạp nguội để xuất PDF");

            var pheDuyets = await _pheDuyetService.GetPheDuyetPhieuAsync(phieuId);

            // Lấy thông tin ca/kíp từ phiếu
            var ca = phieu.Ca ?? 1;
            var kip = phieu.Kip ?? "";
            var ngaySX = phieu.NgaySX ?? DateOnly.FromDateTime(DateTime.Today);
            var xuongCan = phieu.Scope?.ToString() ?? "";

            // Tính khoảng thời gian ca
            string tuGio = "", denGio = "", tuNgay = "", denNgay = "";
            DateOnly ngayBatDau = ngaySX;
            DateOnly ngayKetThuc = ngaySX;

            switch (ca)
            {
                case 1:
                    tuGio = "08h00";
                    denGio = "20h00";
                    break;
                case 2:
                    tuGio = "20h00";
                    denGio = "08h00";
                    ngayKetThuc = ngaySX.AddDays(1);
                    break;
                default:
                    tuGio = "";
                    denGio = "";
                    break;
            }

            tuNgay = ngayBatDau.ToString("dd/MM/yyyy");
            denNgay = ngayKetThuc.ToString("dd/MM/yyyy");

            // Lấy người phê duyệt theo cấp
            var hrc1 = pheDuyets.FirstOrDefault(x => x.CapDuyet == 0);
            var qlcl = pheDuyets.FirstOrDefault(x => x.CapDuyet == 1);
            var ctd = pheDuyets.FirstOrDefault(x => x.CapDuyet == 2);

            // Xây dựng table rows
            var rows = new StringBuilder();
            int stt = 1;
            foreach (var t in data)
            {
                rows.Append($@"
                <tr>
                    <td>{stt++}</td>
                    <td>{t.Me}</td>
                    <td>{t.Mac}</td>
                    <td>{t.KichThuoc}</td>
                    <td>{t.SoThanh}</td>
                    <td>{t.TongKl:N0}</td>
                    <td>{t.GhiChu}</td>
                </tr>");
            }

            var tongST = data.Sum(x => x.SoThanh ?? 0);
            var tongKL = data.Sum(x => x.TongKl ?? 0);

            // Load template HTML
            var templatePath = Path.Combine(
                _env.WebRootPath,
                "template_html",
                "BM.02-QT.05.13_Bien_ban_phoi_nap_nguoi.html"
            );
            var html = await _bmConfig.LoadTemplateAsync(templatePath);

            // Logo
            var logoBase64 = $"data:image/png;base64,{Convert.ToBase64String(await File.ReadAllBytesAsync(Path.Combine(_env.WebRootPath, "imgs", "LogoPDF.png")))}";

            // Format chữ ký
            var signHRC1 = await FormatChuKyBase64Async(hrc1?.ChuKy, hrc1?.TinhTrang == 1);
            var signQLCL = await FormatChuKyBase64Async(qlcl?.ChuKy, qlcl?.TinhTrang == 1);
            var signCTD = await FormatChuKyBase64Async(ctd?.ChuKy, ctd?.TinhTrang == 1);

            // ─────────────────────────────────────────────────────────────────────────
            // Lấy notes từ BmPhieuChiTiet và convert newline thành <br />
            // ─────────────────────────────────────────────────────────────────────────
            var notesRecord = await _context.BmPhieuChiTiets
                .FirstOrDefaultAsync(x => x.PhieuId == phieuId && x.ThongSo == "notes");

            var notesHtml = "";
            if (!string.IsNullOrWhiteSpace(notesRecord?.GiaTri))
            {
                // Escape HTML entities và convert newline thành <br />
                notesHtml = System.Net.WebUtility.HtmlEncode(notesRecord.GiaTri)
                    .Replace("\r\n", "<br />")
                    .Replace("\n", "<br />")
                    .Replace("\r", "<br />");
            }

            html = html
                .Replace("{{LogoUrl}}", logoBase64)
                .Replace("{{XuongCan}}", xuongCan)
                .Replace("{{Ca}}", ca.ToString())
                .Replace("{{Kip}}", kip)
                .Replace("{{TuGio}}", tuGio)
                .Replace("{{TuNgay}}", tuNgay)
                .Replace("{{DenGio}}", denGio)
                .Replace("{{DenNgay}}", denNgay)
                // HRC1
                .Replace("{{NguoiNhanHRC1}}", hrc1?.HoVaTen ?? "")
                .Replace("{{ChucVuNhanHRC1}}", hrc1?.TenViTri ?? "")
                .Replace("{{BPHRC1}}", hrc1?.TenPhongBan ?? "")
                // QLCL
                .Replace("{{NguoiNhanQLCL}}", qlcl?.HoVaTen ?? "")
                .Replace("{{ChucVuNhanQLCL}}", qlcl?.TenViTri ?? "")
                .Replace("{{BPQLCL}}", qlcl?.TenPhongBan ?? "")
                // CTD
                .Replace("{{NguoiNhanCTD}}", ctd?.HoVaTen ?? "")
                .Replace("{{ChucVuNhanCTD}}", ctd?.TenViTri ?? "")
                .Replace("{{BPCTD}}", ctd?.TenPhongBan ?? "")
                // Table
                .Replace("{{Rows}}", rows.ToString())
                .Replace("{{TongST}}", tongST.ToString("N0"))
                .Replace("{{TongKL}}", tongKL.ToString("N0"))
                // Notes
                .Replace("{{Notes}}", notesHtml)
                // Chữ ký
                .Replace("{{ChuKyNguoiNhanHRC1}}", signHRC1)
                .Replace("{{ChuKyNguoiNhanQLCL}}", signQLCL)
                .Replace("{{ChuKyNguoiNhanCTD}}", signCTD);

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
                FileName = $"BM.02-QT.05.13_Bien_ban_phoi_nap_nguoi_{DateTime.Now:yyyyMMdd_HHmm}.pdf",
                ContentType = "application/pdf"
            };
        }

        public async Task<ExportFileResult> ExportExcelByPhieuAsync(Guid phieuId)
        {
            var phieu = await _repoPhieu.GetByIdAsync(phieuId);
            if (phieu == null)
                throw new Exception("Không tìm thấy phiếu");

            var data = (await _repo.GetByPhieuIdAsync(phieuId)).ToList();
            if (!data.Any())
                throw new Exception("Không có dữ liệu phôi nạp nguội để xuất Excel");

            var pheDuyets = await _pheDuyetService.GetPheDuyetPhieuAsync(phieuId);
            var hrc1 = pheDuyets.FirstOrDefault(x => x.CapDuyet == 0);
            var qlcl = pheDuyets.FirstOrDefault(x => x.CapDuyet == 1);
            var ctd = pheDuyets.FirstOrDefault(x => x.CapDuyet == 2);

            var ca = phieu.Ca ?? 1;
            var kip = phieu.Kip ?? "";
            var ngaySX = phieu.NgaySX ?? DateOnly.FromDateTime(DateTime.Today);
            var xuongCan = phieu.Scope?.ToString() ?? "";

            string tuGio, denGio;
            var ngayKetThuc = ngaySX;
            switch (ca)
            {
                case 1: tuGio = "08h00"; denGio = "20h00"; break;
                case 2: tuGio = "20h00"; denGio = "08h00"; ngayKetThuc = ngaySX.AddDays(1); break;
                default: tuGio = ""; denGio = ""; break;
            }

            var notesRecord = await _context.BmPhieuChiTiets
                .FirstOrDefaultAsync(x => x.PhieuId == phieuId && x.ThongSo == "notes");
            var notesText = notesRecord?.GiaTri ?? "";

            // Logo
            var _logoPath = Path.Combine(_env.WebRootPath, "imgs", "LogoPDF.png");
            byte[]? logoBytes = File.Exists(_logoPath) ? await File.ReadAllBytesAsync(_logoPath) : null;

            var htmlPath02 = Path.Combine(_env.WebRootPath, "template_html", "BM.02-QT.05.13_Bien_ban_phoi_nap_nguoi.html");
            var bmHeaderText = await HtmlTemplateHelper.GetBmHeaderTextAsync(htmlPath02);

            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("Biên bản phôi nạp nguội");

            // 7 cột: STT(1) Mẻ(2) Mác(3) KíchThước(4) SốThanh(5) TổngKL(6) GhiChú(7)
            const int COLS = 7;
            ws.Style.Font.FontName = "Times New Roman";
            ws.Style.Font.FontSize = 12;

            ws.Column(1).Width = 6;
            ws.Column(2).Width = 14;
            ws.Column(3).Width = 14;
            ws.Column(4).Width = 14;
            ws.Column(5).Width = 14;
            ws.Column(6).Width = 16;
            ws.Column(7).Width = 20;

            int row = 1;

            // ── HEADER: Logo (trái, 3 rows) | BM code (phải, merge 3 rows) ──
            ws.Row(row).Height = 36;

            if (logoBytes != null)
            {
                var fmt = XLPictureFormat.Png;
                using var logoMs = new MemoryStream(logoBytes);
                ws.AddPicture(logoMs, fmt)
                    .MoveTo(ws.Cell(row, 1))
                    .Scale(0.38);
            }

            ws.Cell(row, 5).Value = bmHeaderText;
            ws.Range(row, 5, row + 2, COLS).Merge().Style
                .Font.SetFontSize(11)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                .Alignment.SetWrapText(true);
            row++;

            ws.Row(row).Height = 16;
            ws.Cell(row, 1).Value = "CÔNG TY CỔ PHẦN THÉP";
            ws.Range(row, 1, row, 4).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(11)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
            row++;

            ws.Row(row).Height = 16;
            ws.Cell(row, 1).Value = "HÒA PHÁT DUNG QUẤT";
            ws.Range(row, 1, row, 4).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(11)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
            row++;

            // ── TITLE ────────────────────────────────────────────────────────
            ws.Cell(row, 1).Value = "BIÊN BẢN";
            ws.Range(row, 1, row, COLS).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            ws.Row(row).Height = 24;
            row++;

            ws.Cell(row, 1).Value = "GIAO NHẬN PHÔI NẠP NGUỘI";
            ws.Range(row, 1, row, COLS).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            ws.Row(row).Height = 24;
            row++;

            // ── SUB-TITLE ─────────────────────────────────────────────────────
            ws.Cell(row, 1).Value = $"Xưởng cán: {xuongCan}";
            ws.Range(row, 1, row, COLS).Merge().Style
                .Font.SetItalic(true).Font.SetFontSize(12)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            ws.Row(row).Height = 16;
            row++;

            ws.Cell(row, 1).Value = $"Kíp: {ca}{kip}   Từ {tuGio} Giờ {ngaySX:dd/MM/yyyy}   Đến {denGio} Giờ ngày {ngayKetThuc:dd/MM/yyyy}";
            ws.Range(row, 1, row, COLS).Merge().Style
                .Font.SetItalic(true).Font.SetFontSize(12)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            ws.Row(row).Height = 16;
            row++;

            // ── NGƯỜI THAM GIA ───────────────────────────────────────────────
            ws.Cell(row, 1).Value = "Chúng tôi gồm:";
            ws.Range(row, 1, row, COLS).Merge();
            ws.Row(row).Height = 15;
            row++;

            void WriteNguoi(int r, int idx, string ten, string chucVu, string bp)
            {
                ws.Cell(r, 1).Value = $"{idx}. Ông/bà: {ten}     Chức vụ: {chucVu}     BP: {bp}";
                ws.Range(r, 1, r, COLS).Merge().Style
                    .Font.SetFontSize(12);
                ws.Row(r).Height = 15;
            }

            WriteNguoi(row, 1, hrc1?.HoVaTen ?? "", hrc1?.TenViTri ?? "", hrc1?.TenPhongBan ?? ""); row++;
            WriteNguoi(row, 2, qlcl?.HoVaTen ?? "", qlcl?.TenViTri ?? "", qlcl?.TenPhongBan ?? ""); row++;
            WriteNguoi(row, 3, ctd?.HoVaTen ?? "", ctd?.TenViTri ?? "", ctd?.TenPhongBan ?? ""); row++;

            // ── TABLE HEADER ─────────────────────────────────────────────────
            string[] headers = ["STT", "Mẻ", "Mác", "Kích thước", "Tổng số thanh", "Tổng khối lượng", "Ghi chú"];
            for (int c = 1; c <= COLS; c++)
            {
                ws.Cell(row, c).Value = headers[c - 1];
                ws.Cell(row, c).Style
                    .Font.SetBold(true).Font.SetFontSize(12)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#f0f0f0"))
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            }
            ws.Row(row).Height = 20;
            row++;

            // ── DATA ROWS ────────────────────────────────────────────────────
            int stt = 1;
            var tongST = 0;
            double tongKL = 0;
            foreach (var item in data)
            {
                ws.Cell(row, 1).Value = stt++;
                ws.Cell(row, 2).Value = item.Me ?? "";
                ws.Cell(row, 3).Value = item.Mac ?? "";
                ws.Cell(row, 4).Value = item.KichThuoc ?? "";
                ws.Cell(row, 5).Value = item.SoThanh.HasValue ? (XLCellValue)item.SoThanh.Value : "";
                ws.Cell(row, 6).Value = item.TongKl.HasValue ? (XLCellValue)item.TongKl.Value : "";
                ws.Cell(row, 7).Value = item.GhiChu ?? "";

                ws.Range(row, 1, row, COLS).Style
                    .Font.SetFontSize(12)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                    .Border.SetInsideBorder(XLBorderStyleValues.Thin);
                ws.Cell(row, 2).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);
                ws.Cell(row, 7).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);

                ws.Row(row).Height = 20;
                tongST += item.SoThanh ?? 0;
                tongKL += item.TongKl ?? 0;
                row++;
            }

            // ── FOOTER: Tổng ─────────────────────────────────────────────────
            ws.Cell(row, 1).Value = "Tổng";
            ws.Range(row, 1, row, 2).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(12)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#f5f5f5"))
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                .Border.SetInsideBorder(XLBorderStyleValues.Thin);

            ws.Cell(row, 3).Value = "";
            ws.Cell(row, 4).Value = "";
            ws.Cell(row, 5).Value = tongST;
            ws.Cell(row, 5).Style.Font.SetBold(true).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            ws.Cell(row, 6).Value = tongKL;
            ws.Cell(row, 6).Style.Font.SetBold(true).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .NumberFormat.SetFormat("#,##0");
            ws.Cell(row, 7).Value = "";

            ws.Range(row, 3, row, COLS).Style
                .Font.SetBold(true)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#f5f5f5"))
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                .Border.SetInsideBorder(XLBorderStyleValues.Thin);
            ws.Row(row).Height = 20;
            row += 2;

            // ── GHI CHÚ ──────────────────────────────────────────────────────
            ws.Cell(row, 1).Value = "Ghi chú:";
            ws.Range(row, 1, row, COLS).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(12);
            ws.Row(row).Height = 15;
            row++;

            if (!string.IsNullOrWhiteSpace(notesText))
            {
                ws.Cell(row, 1).Value = notesText;
                ws.Range(row, 1, row, COLS).Merge().Style
                    .Font.SetFontSize(12)
                    .Alignment.SetWrapText(true);
                ws.Row(row).Height = 30;
                row++;
            }

            row++;

            // ── CHỮ KÝ: 3 cột NM.HRC1 | P.QLCL | NM.CT DAI ─────────────────
            // Chia 7 cols thành 3 phần: cols 1-2 | cols 3-5 | cols 6-7
            ws.Cell(row, 1).Value = "NM.HRC1";
            ws.Range(row, 1, row, 2).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(12)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(row, 3).Value = "P.QLCL";
            ws.Range(row, 3, row, 5).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(12)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(row, 6).Value = "NM.CT DAI";
            ws.Range(row, 6, row, COLS).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(12)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Row(row).Height = 16;
            row++;

            ws.Row(row).Height = 60; // vùng chữ ký
            row++;

            ws.Cell(row, 1).Value = hrc1?.HoVaTen ?? "";
            ws.Range(row, 1, row, 2).Merge().Style
                .Font.SetFontSize(12)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(row, 3).Value = qlcl?.HoVaTen ?? "";
            ws.Range(row, 3, row, 5).Merge().Style
                .Font.SetFontSize(12)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(row, 6).Value = ctd?.HoVaTen ?? "";
            ws.Range(row, 6, row, COLS).Merge().Style
                .Font.SetFontSize(12)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // Page setup A4
            ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
            ws.PageSetup.PageOrientation = XLPageOrientation.Portrait;
            ws.PageSetup.Margins.Left = 0.8;
            ws.PageSetup.Margins.Right = 0.6;
            ws.PageSetup.Margins.Top = 0.8;
            ws.PageSetup.Margins.Bottom = 0.6;

            using var stream = new MemoryStream();
            wb.SaveAs(stream);

            return new ExportFileResult
            {
                Content = stream.ToArray(),
                FileName = $"BM.02-QT.05.13_Bien_ban_phoi_nap_nguoi_{ngaySX:yyyyMMdd}_Ca{ca}{kip}_{DateTime.Now:HHmmss}.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            };
        }

        public async Task<ExportFileResult> ExportTongHopExcelByPhieuAsync(DateOnly? fromDate, DateOnly? toDate)
        {
            var maBmList = new[]
            {
                "CTD_BB_Phoinapnguoi",
                "CTD_BB_Phoinapnguoi",
                "CTD_BB_PhoiNguoi",
                "BM.02-QT.05.13",
                "BM.02/QT.05.13"
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
                    x.MayDuc,
                    x.TinhTrang,
                    x.Scope,
                    x.DataJson,
                    x.MaBm,
                })
                .ToListAsync();

            var phieuIds = phieus.Select(x => x.Idphieu).ToList();
            var details = await _context.CtdPhoiNguois
                .AsNoTracking()
                .Where(x => x.PhieuId.HasValue && phieuIds.Contains(x.PhieuId.Value))
                .ToListAsync();

            var rows = (from p in phieus
                        join d in details on p.Idphieu equals d.PhieuId
                        orderby p.NgaySX, p.Ca, p.Kip, p.SoPhieu
                        select new
                        {
                            p.SoPhieu,
                            p.NgaySX,
                            p.Ca,
                            p.Kip,
                            p.MayDuc,
                            d.Me,
                            d.Mac,
                            d.KichThuoc,
                            d.SoThanh,
                            d.TongKl,
                            d.GhiChu,
                            p.TinhTrang,
                            p.Scope
                        }).ToList();

            var templatePath = Path.Combine(_env.WebRootPath, "templates", "BM_TongHopPhoiNapNguoi.xlsx");
            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"Không tìm thấy file mẫu Excel: {templatePath}");

            using var workbook = new XLWorkbook(templatePath);
            var ws = workbook.Worksheet(1);

            ws.Cell("G3").Value = $"Từ ngày: {fromDate:dd/MM/yyyy} đến ngày: {toDate:dd/MM/yyyy}";

            var startRow = 6;
            var rowIndex = startRow;
            foreach (var item in rows)
            {
                if (rowIndex > startRow)
                    ws.Row(startRow).CopyTo(ws.Row(rowIndex));

                ws.Cell(rowIndex, 1).Value = item.NgaySX?.ToDateTime(TimeOnly.MinValue);
                ws.Cell(rowIndex, 2).Value = item.Kip;
                ws.Cell(rowIndex, 3).Value = item.Ca;
                ws.Cell(rowIndex, 4).Value = item.Scope;
                ws.Cell(rowIndex, 5).Value = item.Me;
                ws.Cell(rowIndex, 6).Value = item.Mac;
                ws.Cell(rowIndex, 7).Value = item.KichThuoc;
                ws.Cell(rowIndex, 8).Value = item.SoThanh;
                ws.Cell(rowIndex, 9).Value = item.TongKl;
                ws.Cell(rowIndex, 10).Value = item.GhiChu;
                ws.Cell(rowIndex, 11).Value = item.SoPhieu;

                var statusCell = ws.Cell(rowIndex, 12);
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
            }

            if (rows.Count > 0)
            {
                var lastRow = rowIndex - 1;
                ws.Range(startRow, 1, lastRow, 1).Style.DateFormat.Format = "dd/MM/yyyy";
                // ws.Range(startRow, 8, lastRow, 9).Style.NumberFormat.Format = "#,##0.##";
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return new ExportFileResult
            {
                Content = stream.ToArray(),
                FileName = $"TongHopPhoiNapNguoi_{DateTime.Now:yyyyMMddHHmmss}.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            };
        }
    }
}

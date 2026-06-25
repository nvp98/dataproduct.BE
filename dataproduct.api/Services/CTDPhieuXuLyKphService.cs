using dataproduct.api.DTOs.Export;
using dataproduct.api.Models;
using dataproduct.api.Repositories;
using DinkToPdf;
using DinkToPdf.Contracts;
using System.Globalization;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using dataproduct.api.Utils;

namespace dataproduct.api.Services
{
    public class CTDPhieuXuLyKphService
    {
        private readonly ICtdPhieuXuLyKphRepository _repo;
        private readonly IConverter _pdfConverter;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly IPhieuRepository _repoPhieu;
        private readonly PheDuyetService _pheDuyetService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly BmConfigService _bmConfig;

        public CTDPhieuXuLyKphService(
            ICtdPhieuXuLyKphRepository repo,
            IConverter pdfConverter,
            IWebHostEnvironment env,
            IConfiguration configuration,
            IPhieuRepository repoPhieu,
            PheDuyetService pheDuyetService,
            IHttpClientFactory httpClientFactory,
            BmConfigService bmConfig)
        {
            _repo = repo;
            _pdfConverter = pdfConverter;
            _env = env;
            _configuration = configuration;
            _repoPhieu = repoPhieu;
            _pheDuyetService = pheDuyetService;
            _httpClientFactory = httpClientFactory;
            _bmConfig = bmConfig;
        }

        public async Task<int> InsertFromPhieuJsonAsync(BmPhieu phieu)
        {
            if (phieu == null)
                throw new ArgumentNullException(nameof(phieu));

            if (string.IsNullOrWhiteSpace(phieu.DataJson))
                return 0;

            // Xóa dữ liệu cũ của phiếu này trước khi insert dữ liệu mới
            await _repo.DeleteByIdPhieuAsync(phieu.Idphieu);

            using var jsonDoc = JsonDocument.Parse(phieu.DataJson);
            var root = jsonDoc.RootElement;

            var rows = new List<JsonElement>();

            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in root.EnumerateArray())
                    rows.Add(item);
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                if (TryGetRows(root, out var arrayRows))
                {
                    foreach (var item in arrayRows.EnumerateArray())
                        rows.Add(item);
                }
                else
                {
                    rows.Add(root);
                }
            }

            var entities = new List<CtdPhieuXuLyKph>();
            var thongtinphieu = ParseSoPhieu(phieu.SoPhieu ?? "");
            foreach (var row in rows)
            {
                if (row.ValueKind != JsonValueKind.Object)
                    continue;

                entities.Add(new CtdPhieuXuLyKph
                {
                    IdPhieu = phieu.Idphieu,
                    InSanPham = TryGetString(row, "InSanPham", "inSanPham"),
                    InMacThep = TryGetString(row, "InMacThep", "inMacThep"),
                    InChieuDai = TryGetString(row, "InChieuDai", "inChieuDai"),
                    InSoMe = TryGetString(row, "InSoMe", "inSoMe", "inMe"),
                    InSoThanh = TryGetInt(row, "InSoThanh", "inSoThanh"),
                    InKhoiLuong = TryGetDecimal(row, "InKhoiLuong", "inKhoiLuong"),
                    InCaNgaySx = TryGetString(row, "InCaNgaySX", "inCaNgaySX", "inCaNgaySx"),
                    InLoai = TryGetString(row, "InLoai", "inLoai"),

                    Reason = TryGetString(row, "Reason", "reason", "LyDo", "lyDo"),
                    Measures = TryGetString(row, "Measures", "measures", "BienPhap", "bienPhap"),

                    NewSanPham = TryGetString(row, "NewSanPham", "newSanPham"),
                    NewMacThep = TryGetString(row, "NewMacThep", "newMacThep"),
                    NewChieuDai = TryGetString(row, "NewChieuDai", "newChieuDai"),
                    NewSoMe = TryGetString(row, "NewSoMe", "newSoMe", "newMe"),
                    NewSoThanh = TryGetInt(row, "NewSoThanh", "newSoThanh"),
                    NewKhoiLuong = TryGetDecimal(row, "NewKhoiLuong", "newKhoiLuong"),
                    NewLoai = TryGetString(row, "NewLoai", "newLoai"),

                    NgayXL = thongtinphieu.NgayXuLy,
                    // CaXL = thongtinphieu.CaXuLy,,
                    KipXL = thongtinphieu.CaXuLy,

                    LenhSanXuat = thongtinphieu.LenhSanXuat,

                    CreatedAt = DateTime.Now
                });
            }

            await _repo.AddRangeAsync(entities);
            return entities.Count;
        }

        public class PhieuInfo
        {
            public DateOnly? NgayXuLy { get; set; }
            public string CaXuLy { get; set; }
            public string LenhSanXuat { get; set; }
        }

        public static PhieuInfo ParseSoPhieu(string soPhieu)
        {
            var parts = soPhieu.Split('_');

            // parts[1] = 202603111C (ngày SX + ca SX)
            // parts[2] = 202603081C (ngày XL + ca XL)
            // parts[4] = lệnh SX

            var ngayCaXL = parts[2]; // 202603081C

            string ngayStr = ngayCaXL.Substring(0, 8); // 20260308
            string caXL = ngayCaXL.Substring(8);       // 1C

            DateTime ngayXL = DateTime.ParseExact(ngayStr, "yyyyMMdd", null);
            DateOnly? ngayXuLy = DateOnly.FromDateTime(ngayXL);

            return new PhieuInfo
            {
                NgayXuLy = ngayXuLy,
                CaXuLy = caXL,
                LenhSanXuat = parts[4]
            };
        }

        public async Task<ExportFileResult> ExportPdfXuLyKphAsync(Guid phieuId)
        {
            var phieu = await _repoPhieu.GetByIdAsync(phieuId);
            if (phieu == null)
                throw new Exception("Không tìm thấy phiếu");

            var data = await _repo.GetByIdPhieuAsync(phieuId);
            if (!data.Any())
                throw new Exception("Không có dữ liệu KPH để xuất PDF");

            var templatePath = Path.Combine(
                _env.WebRootPath,
                "template_html",
                "BM.01C-QT.11_Phieu_xu_ly_ban_thanh_pham_KPH.html"
            );

            var html = await _bmConfig.LoadTemplateAsync(templatePath);

            // Tính toán thời gian ca kíp
            string tuGio = "", denGio = "";
            if (phieu.Ca > 0)
            {
                switch (phieu.Ca)
                {
                    case 1: // Ca 1: 08h - 20h
                        tuGio = "08h00";
                        denGio = "20h00";
                        break;
                    case 2: // Ca 2: 20h - 08h
                        tuGio = "20h00";
                        denGio = "08h00";
                        break;
                    default:
                        tuGio = "";
                        denGio = "";
                        break;
                }
            }

            var rows = new StringBuilder();
            int stt = 0;
            decimal inTongSoThanh = 0, inTongKhoiLuong = 0, newTongSoThanh = 0, newTongKhoiLuong = 0;

            foreach (var item in data)
            {
                stt++;
                inTongSoThanh += item.InSoThanh ?? 0;
                inTongKhoiLuong += item.InKhoiLuong ?? 0;
                newTongSoThanh += item.NewSoThanh ?? 0;
                newTongKhoiLuong += item.NewKhoiLuong ?? 0;

                rows.Append($@"
                <tr>
                    <td>{stt}</td>
                    <td class='text-left'>{item.InSanPham}</td>
                    <td>{item.InMacThep}</td>
                    <td>{item.InChieuDai}</td>
                    <td>{item.InSoMe}</td>
                    <td>{item.InSoThanh}</td>
                    <td>{item.InKhoiLuong:N0}</td>
                    <td>{item.InCaNgaySx}</td>
                    <td>{item.InLoai}</td>
                    <td class='text-left'>{item.Reason}</td>
                    <td class='text-left'>{item.Measures}</td>
                    <td class='text-left'>{item.NewSanPham}</td>
                    <td>{item.NewMacThep}</td>
                    <td>{item.NewChieuDai}</td>
                    <td>{item.NewSoMe}</td>
                    <td>{item.NewSoThanh}</td>
                    <td>{item.NewKhoiLuong:N0}</td>
                    <td>{item.NewLoai}</td>
                </tr>");
            }

            // Lấy pheDuyet data để lấy chữ ký
            var pheDuyets = await _pheDuyetService.GetPheDuyetPhieuAsync(phieuId);
            var qlcl = pheDuyets.FirstOrDefault(x => x.CapDuyet == 3 && x.TinhTrang == 1);
            var log = pheDuyets.FirstOrDefault(x => x.CapDuyet == 2 && x.TinhTrang == 1);
            var bpLienQuan = pheDuyets.FirstOrDefault(x => x.CapDuyet == 1 && x.TinhTrang == 1);
            var nguoiLap = pheDuyets.FirstOrDefault(x => x.CapDuyet == 0 && x.TinhTrang == 1);

            var signQLCL = await FormatChuKyBase64Async(qlcl?.ChuKy, qlcl?.TinhTrang == 1);
            var signLOG = await FormatChuKyBase64Async(log?.ChuKy, log?.TinhTrang == 1);
            var signBPLienQuan = await FormatChuKyBase64Async(bpLienQuan?.ChuKy, bpLienQuan?.TinhTrang == 1);
            var signNguoiLap = await FormatChuKyBase64Async(nguoiLap?.ChuKy, nguoiLap?.TinhTrang == 1);

            var logoUrl = $"data:image/png;base64,{Convert.ToBase64String(await File.ReadAllBytesAsync(Path.Combine(_env.WebRootPath, "imgs", "LogoPDF.png")))}";

            // Parse date from phieu
            var ngaySX = phieu.NgaySX?.ToString("dd/MM/yyyy") ?? "";
            var thangSX = phieu.NgaySX?.Month.ToString("D2") ?? "";
            var namSX = phieu.NgaySX?.Year.ToString() ?? "";
            var ca = phieu.Ca?.ToString() ?? "";
            var kip = phieu.Kip ?? "";
            var mayDuc = phieu.MayDuc?.ToString() ?? "";

            html = html
                .Replace("{{LogoUrl}}", logoUrl)
                .Replace("{{NgaySX}}", ngaySX)
                .Replace("{{ThangSX}}", thangSX)
                .Replace("{{NamSX}}", namSX)
                .Replace("{{Ca}}", ca)
                .Replace("{{Kip}}", kip)
                .Replace("{{MayDuc}}", mayDuc)
                .Replace("{{TuGio}}", tuGio)
                .Replace("{{DenGio}}", denGio)
                .Replace("{{Rows}}", rows.ToString())
                .Replace("{{InTongST}}", inTongSoThanh.ToString("N0"))
                .Replace("{{InTongKL}}", inTongKhoiLuong.ToString("N0"))
                .Replace("{{newTongST}}", newTongSoThanh.ToString("N0"))
                .Replace("{{newTongKL}}", newTongKhoiLuong.ToString("N0"))
                .Replace("{{sx}}", "")
                .Replace("{{luukho}}", "")
                .Replace("{{CaXuLy}}", "")
                .Replace("{{KipXuLy}}", "")
                .Replace("{{NgayXuLy}}", "")
                .Replace("{{ThangXuLy}}", "")
                .Replace("{{NamXuLy}}", "")
                .Replace("{{LenhSanXuat}}", "")
                .Replace("{{Sign_QLCL}}", signQLCL)
                .Replace("{{Name_QLCL}}", qlcl?.HoVaTen ?? "")
                .Replace("{{Sign_LOG}}", signLOG)
                .Replace("{{Name_LOG}}", log?.HoVaTen ?? "")
                .Replace("{{Sign_BPLienQuan}}", signBPLienQuan)
                .Replace("{{Name_BPLienQuan}}", bpLienQuan?.HoVaTen ?? "")
                .Replace("{{Sign_NguoiLap}}", signNguoiLap)
                .Replace("{{Name_NguoiLap}}", nguoiLap?.HoVaTen ?? "");

            var doc = new HtmlToPdfDocument
            {
                GlobalSettings =
                {
                    PaperSize = PaperKind.A4,
                    Orientation = Orientation.Landscape
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
                            UserStyleSheet = ""
                        },
                        LoadSettings =
                        {
                            BlockLocalFileAccess = false,
                            DebugJavascript = false,
                            LoadErrorHandling = ContentErrorHandling.Ignore
                        }
                    }
                }
            };

            var pdfBytes = _pdfConverter.Convert(doc);

            return new ExportFileResult
            {
                Content = pdfBytes,
                FileName = $"BM.01C-QT.11_Phieu_xu_ly_KPH_{DateTime.Now:yyyyMMdd_HHmm}.pdf",
                ContentType = "application/pdf"
            };
        }

        public async Task<ExportFileResult> ExportExcelChiTietKphAsync(Guid phieuId)
        {
            var phieu = await _repoPhieu.GetByIdAsync(phieuId);
            if (phieu == null)
                throw new Exception("Không tìm thấy phiếu");

            var data = (await _repo.GetByIdPhieuAsync(phieuId)).ToList();
            if (!data.Any())
                throw new Exception("Không có dữ liệu KPH để xuất Excel");

            // Parse thông tin từ SoPhieu
            string ngayXL = "", thangXL = "", namXL = "", caXL = "", lenhSX = "";
            try
            {
                var info = ParseSoPhieu(phieu.SoPhieu ?? "");
                ngayXL = info.NgayXuLy?.Day.ToString("D2") ?? "";
                thangXL = info.NgayXuLy?.Month.ToString("D2") ?? "";
                namXL = info.NgayXuLy?.Year.ToString() ?? "";
                caXL = info.CaXuLy ?? "";
                lenhSX = info.LenhSanXuat ?? "";
            }
            catch { /* SoPhieu không đúng format thì bỏ qua */ }

            var ngaySX = phieu.NgaySX?.Day.ToString("D2") ?? "";
            var thangSX = phieu.NgaySX?.Month.ToString("D2") ?? "";
            var namSX = phieu.NgaySX?.Year.ToString() ?? "";
            var ca = phieu.Ca?.ToString() ?? "";
            var kip = phieu.Kip ?? "";
            var mayDuc = phieu.MayDuc?.ToString() ?? "";

            var pheDuyets = await _pheDuyetService.GetPheDuyetPhieuAsync(phieuId);
            var qlcl = pheDuyets.FirstOrDefault(x => x.CapDuyet == 3);
            var log = pheDuyets.FirstOrDefault(x => x.CapDuyet == 2);
            var bpLienQuan = pheDuyets.FirstOrDefault(x => x.CapDuyet == 1);
            var nguoiLap = pheDuyets.FirstOrDefault(x => x.CapDuyet == 0);

            // Logo
            var _logoPath = Path.Combine(_env.WebRootPath, "imgs", "LogoPDF.png");
            byte[]? logoBytes = File.Exists(_logoPath) ? await File.ReadAllBytesAsync(_logoPath) : null;

            var htmlPath01C = Path.Combine(_env.WebRootPath, "template_html", "BM.01C-QT.11_Phieu_xu_ly_ban_thanh_pham_KPH.html");
            var bmHeaderText = await HtmlTemplateHelper.GetBmHeaderTextAsync(htmlPath01C);

            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("Phiếu XL KPH");

            const int COLS = 18;
            ws.Style.Font.FontName = "Times New Roman";
            ws.Style.Font.FontSize = 11;

            // Độ rộng cột
            ws.Column(1).Width = 5;    // TT
            ws.Column(2).Width = 13;   // Sản phẩm (trước)
            ws.Column(3).Width = 10;   // Mác thép
            ws.Column(4).Width = 9;    // Chiều dài
            ws.Column(5).Width = 10;   // Bó số/Mẻ số
            ws.Column(6).Width = 8;    // Số thanh
            ws.Column(7).Width = 10;   // Khối lượng
            ws.Column(8).Width = 11;   // Ca/Ngày SX
            ws.Column(9).Width = 8;    // Phân loại
            ws.Column(10).Width = 16;   // Nguyên nhân
            ws.Column(11).Width = 16;   // Biện pháp
            ws.Column(12).Width = 13;   // Sản phẩm (sau)
            ws.Column(13).Width = 10;   // Mác thép
            ws.Column(14).Width = 9;    // Chiều dài
            ws.Column(15).Width = 10;   // Bó số/Mẻ số
            ws.Column(16).Width = 8;    // Số thanh
            ws.Column(17).Width = 10;   // Khối lượng
            ws.Column(18).Width = 8;    // Phân loại

            int row = 1;

            // ── HEADER: Logo (trái) | BM code (phải, merge rows 1-3) ─────────
            ws.Row(row).Height = 36;

            if (logoBytes != null)
            {
                var fmt = ClosedXML.Excel.Drawings.XLPictureFormat.Png;
                using var logoMs = new MemoryStream(logoBytes);
                ws.AddPicture(logoMs, fmt)
                    .MoveTo(ws.Cell(row, 1))
                    .Scale(0.38);
            }

            ws.Cell(row, 14).Value = bmHeaderText;
            ws.Range(row, 14, row + 2, COLS).Merge().Style
                .Font.SetFontSize(11)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                .Alignment.SetWrapText(true);
            row++;

            ws.Row(row).Height = 16;
            ws.Cell(row, 1).Value = "CÔNG TY CỔ PHẦN THÉP";
            ws.Range(row, 1, row, 13).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(11)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
            row++;

            ws.Row(row).Height = 16;
            ws.Cell(row, 1).Value = "HÒA PHÁT DUNG QUẤT";
            ws.Range(row, 1, row, 13).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(11)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
            row++;

            // ── TITLE ────────────────────────────────────────────────────────
            ws.Cell(row, 1).Value = "PHIẾU XỬ LÝ BÁN THÀNH PHẨM/SẢN PHẨM KHÔNG PHÙ HỢP";
            ws.Range(row, 1, row, COLS).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(14)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
            ws.Row(row).Height = 22;
            row++;

            // ── SUB-TITLE 1: checkbox sx / lưu kho ───────────────────────────
            ws.Cell(row, 1).Value = "☐ Trong quá trình sản xuất      ☐ Trong quá trình lưu kho";
            ws.Range(row, 1, row, COLS).Merge().Style
                .Font.SetFontSize(11)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            ws.Row(row).Height = 15;
            row++;

            // ── SUB-TITLE 2: kíp / ca / ngày ─────────────────────────────────
            ws.Cell(row, 1).Value = $"Kíp: {ca}{kip} ngày {ngaySX} tháng {thangSX} năm {namSX}   xử lý về kíp {caXL} ngày {ngayXL} tháng {thangXL} năm {namXL}   - LSX: {lenhSX}";
            ws.Range(row, 1, row, COLS).Merge().Style
                .Font.SetFontSize(11)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            ws.Row(row).Height = 15;
            row++;

            // ── CONTENT ───────────────────────────────────────────────────────
            ws.Cell(row, 1).Value = $"P.QLCL xác nhận việc xử lý sản phẩm KPH tại công đoạn Phân xưởng cán {mayDuc} như sau:";
            ws.Range(row, 1, row, COLS).Merge().Style
                .Font.SetFontSize(11)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);
            ws.Row(row).Height = 15;
            row++;

            // ── TABLE HEADER ROW 1 ────────────────────────────────────────────
            void SetHeaderCell(IXLRange r, string v)
            {
                r.Merge().Value = v;
                r.Style
                    .Font.SetBold(true).Font.SetFontSize(11)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                    .Alignment.SetWrapText(true)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#d9d9d9"))
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                    .Border.SetInsideBorder(XLBorderStyleValues.Thin);
            }

            SetHeaderCell(ws.Range(row, 1, row, 9), "SẢN PHẨM TRƯỚC SỬ LÝ");
            SetHeaderCell(ws.Range(row, 10, row + 1, 10), "Nguyên nhân");
            SetHeaderCell(ws.Range(row, 11, row + 1, 11), "Biện pháp xử lý");
            SetHeaderCell(ws.Range(row, 12, row, 18), "SẢN PHẨM SAU SỬ LÝ");
            ws.Row(row).Height = 20;
            row++;

            // ── TABLE HEADER ROW 2 ────────────────────────────────────────────
            string[] sub1 = ["TT", "Sản phẩm", "Mác thép", "Chiều dài", "Bó số/Mẻ số", "Số thanh", "Khối lượng\n(kg)", "Ca/Ngày\nsản xuất", "Phân loại"];
            for (int c = 1; c <= 9; c++)
            {
                ws.Cell(row, c).Value = sub1[c - 1];
                ws.Cell(row, c).Style
                    .Font.SetBold(true).Font.SetFontSize(11)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                    .Alignment.SetWrapText(true)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#d9d9d9"))
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            }
            // Cols 10-11 đã được merge (rowspan 2) ở header row 1

            string[] sub2 = ["Sản phẩm", "Mác thép", "Chiều dài", "Bó số/Mẻ số", "Số thanh", "Khối lượng\n(kg)", "Phân loại"];
            for (int c = 12; c <= 18; c++)
            {
                ws.Cell(row, c).Value = sub2[c - 12];
                ws.Cell(row, c).Style
                    .Font.SetBold(true).Font.SetFontSize(11)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                    .Alignment.SetWrapText(true)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#d9d9d9"))
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            }
            ws.Row(row).Height = 28;
            row++;

            // ── DATA ROWS ────────────────────────────────────────────────────
            int stt = 1;
            decimal inTongST = 0, inTongKL = 0, newTongST = 0, newTongKL = 0;

            foreach (var item in data)
            {
                ws.Cell(row, 1).Value = stt++;
                ws.Cell(row, 2).Value = item.InSanPham ?? "";
                ws.Cell(row, 3).Value = item.InMacThep ?? "";
                ws.Cell(row, 4).Value = item.InChieuDai ?? "";
                ws.Cell(row, 5).Value = item.InSoMe ?? "";
                ws.Cell(row, 6).Value = item.InSoThanh.HasValue ? (XLCellValue)item.InSoThanh.Value : "";
                ws.Cell(row, 7).Value = item.InKhoiLuong.HasValue ? (XLCellValue)(double)item.InKhoiLuong.Value : "";
                ws.Cell(row, 8).Value = item.InCaNgaySx ?? "";
                ws.Cell(row, 9).Value = item.InLoai ?? "";
                ws.Cell(row, 10).Value = item.Reason ?? "";
                ws.Cell(row, 11).Value = item.Measures ?? "";
                ws.Cell(row, 12).Value = item.NewSanPham ?? "";
                ws.Cell(row, 13).Value = item.NewMacThep ?? "";
                ws.Cell(row, 14).Value = item.NewChieuDai ?? "";
                ws.Cell(row, 15).Value = item.NewSoMe ?? "";
                ws.Cell(row, 16).Value = item.NewSoThanh.HasValue ? (XLCellValue)item.NewSoThanh.Value : "";
                ws.Cell(row, 17).Value = item.NewKhoiLuong.HasValue ? (XLCellValue)(double)item.NewKhoiLuong.Value : "";
                ws.Cell(row, 18).Value = item.NewLoai ?? "";

                ws.Range(row, 1, row, COLS).Style
                    .Font.SetFontSize(11)
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                    .Border.SetInsideBorder(XLBorderStyleValues.Thin);

                // STT + số lượng / khối lượng: center; còn lại: left
                ws.Cell(row, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Cell(row, 6).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Cell(row, 7).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .NumberFormat.SetFormat("#,##0");
                ws.Cell(row, 9).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Cell(row, 16).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Cell(row, 17).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .NumberFormat.SetFormat("#,##0");
                ws.Cell(row, 18).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                ws.Row(row).Height = 20;
                inTongST += item.InSoThanh ?? 0;
                inTongKL += item.InKhoiLuong ?? 0;
                newTongST += item.NewSoThanh ?? 0;
                newTongKL += item.NewKhoiLuong ?? 0;
                row++;
            }

            // ── FOOTER: Tổng ─────────────────────────────────────────────────
            var footerStyle = new Action<IXLRange>(r => r.Merge().Style
                .Font.SetBold(true).Font.SetFontSize(11)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#f0f0f0"))
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                .Border.SetInsideBorder(XLBorderStyleValues.Thin));

            footerStyle(ws.Range(row, 1, row, 5));
            ws.Cell(row, 1).Value = "Tổng";

            ws.Cell(row, 6).Value = inTongST;
            ws.Cell(row, 6).Style.Font.SetBold(true).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

            ws.Cell(row, 7).Value = inTongKL;
            ws.Cell(row, 7).Style.Font.SetBold(true).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .NumberFormat.SetFormat("#,##0")
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

            footerStyle(ws.Range(row, 8, row, 11));

            footerStyle(ws.Range(row, 12, row, 15));
            ws.Cell(row, 12).Value = "Tổng";

            ws.Cell(row, 16).Value = newTongST;
            ws.Cell(row, 16).Style.Font.SetBold(true).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

            ws.Cell(row, 17).Value = newTongKL;
            ws.Cell(row, 17).Style.Font.SetBold(true).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .NumberFormat.SetFormat("#,##0")
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

            ws.Cell(row, 18).Value = "";
            ws.Cell(row, 18).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);

            ws.Row(row).Height = 20;
            row += 2;

            // ── NOTE ─────────────────────────────────────────────────────────
            ws.Cell(row, 1).Value = "Tổng sản phẩm không phù hợp còn lại........(tấn)";
            ws.Range(row, 1, row, COLS).Merge().Style.Font.SetFontSize(11);
            ws.Row(row).Height = 15;
            row += 2;

            // ── CHỮ KÝ: 4 cột P.QLCL | BP.LOG | BP liên quan | Người lập ───
            // Chia 18 col: 1-4 | 5-9 | 10-14 | 15-18
            var signLabels = new[] { "P.QLCL", "BP.LOG", "BP liên quan", "Người lập" };
            var signRanges = new[] { (1, 4), (5, 9), (10, 14), (15, 18) };
            var signPeople = new[] { qlcl, log, bpLienQuan, nguoiLap };

            for (int i = 0; i < 4; i++)
            {
                var (c1, c2) = signRanges[i];
                ws.Cell(row, c1).Value = signLabels[i];
                ws.Range(row, c1, row, c2).Merge().Style
                    .Font.SetBold(true).Font.SetFontSize(11)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            }
            ws.Row(row).Height = 16;
            row++;

            ws.Row(row).Height = 60; // vùng chữ ký
            row++;

            for (int i = 0; i < 4; i++)
            {
                var (c1, c2) = signRanges[i];
                ws.Cell(row, c1).Value = signPeople[i]?.HoVaTen ?? "";
                ws.Range(row, c1, row, c2).Merge().Style
                    .Font.SetFontSize(11)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            }
            ws.Row(row).Height = 16;

            // ── Page setup A4 Landscape ───────────────────────────────────────
            ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
            ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            ws.PageSetup.Margins.Left = 0.8;
            ws.PageSetup.Margins.Right = 0.6;
            ws.PageSetup.Margins.Top = 0.6;
            ws.PageSetup.Margins.Bottom = 0.6;

            using var stream = new MemoryStream();
            wb.SaveAs(stream);

            return new ExportFileResult
            {
                Content = stream.ToArray(),
                FileName = $"BM.01C-QT.11_Phieu_xu_ly_KPH_{phieu.NgaySX:yyyyMMdd}_Ca{ca}{kip}_{DateTime.Now:HHmmss}.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            };
        }

        private static bool TryGetRows(JsonElement root, out JsonElement rows)
        {
            rows = default;
            return TryGetArray(root, "table1", out rows)
                || TryGetArray(root, "rows", out rows)
                || TryGetArray(root, "data", out rows)
                || TryGetArray(root, "items", out rows)
                || TryGetArray(root, "xuLyKphRows", out rows)
                || TryGetArray(root, "kphRows", out rows)
                || TryGetArray(root, "section1", out rows);
        }

        private static bool TryGetArray(JsonElement obj, string key, out JsonElement array)
        {
            array = default;
            if (obj.TryGetProperty(key, out var element) && element.ValueKind == JsonValueKind.Array)
            {
                array = element;
                return true;
            }

            return false;
        }

        private static string? TryGetString(JsonElement row, params string[] keys)
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

        private static int? TryGetInt(JsonElement row, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (!row.TryGetProperty(key, out var value) || value.ValueKind == JsonValueKind.Null)
                    continue;

                if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var n))
                    return n;

                if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out var parsed))
                    return parsed;
            }

            return null;
        }

        private static decimal? TryGetDecimal(JsonElement row, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (!row.TryGetProperty(key, out var value) || value.ValueKind == JsonValueKind.Null)
                    continue;

                if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var n))
                    return n;

                if (value.ValueKind == JsonValueKind.String)
                {
                    var raw = value.GetString();
                    if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
                        return parsed;
                    if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.CurrentCulture, out parsed))
                        return parsed;
                }
            }

            return null;
        }

        private static DateOnly? TryGetDateOnly(JsonElement row, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (!row.TryGetProperty(key, out var value) || value.ValueKind == JsonValueKind.Null)
                    continue;

                if (value.ValueKind == JsonValueKind.String)
                {
                    var raw = value.GetString();
                    if (DateOnly.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                        return parsed;
                    if (DateOnly.TryParse(raw, CultureInfo.CurrentCulture, DateTimeStyles.None, out parsed))
                        return parsed;
                }
            }

            return null;
        }

        private async Task<string> ConvertImageUrlToBase64Async(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return "";

            // Nếu đã là base64, trả về luôn
            if (imageUrl.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
                return imageUrl;

            try
            {
                using var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10);
                var imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
                var base64 = Convert.ToBase64String(imageBytes);

                // Xác định content type từ extension
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
            // Nếu chưa có chữ ký
            if (string.IsNullOrWhiteSpace(chuKy))
            {
                // Nếu đã ký nhưng chưa có ảnh chữ ký
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

            // Nếu đã là base64 image
            if (chuKy.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
            {
                return $"<img src=\"{chuKy}\" style=\"max-width:150px;max-height:80px;\" />";
            }

            // Nếu là URL
            if (chuKy.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                var base64 = await ConvertImageUrlToBase64Async(chuKy);
                if (!string.IsNullOrEmpty(base64))
                {
                    return $"<img src=\"{base64}\" style=\"max-width:150px;max-height:80px;\" />";
                }
            }
            else if (chuKy.StartsWith("/"))
            {
                var domain = _configuration.GetValue<string>("AppSettings:Domain") ?? "https://report.hoaphatdungquat.vn";
                var fullUrl = domain.TrimEnd('/') + chuKy;

                var base64 = await ConvertImageUrlToBase64Async(fullUrl);
                if (!string.IsNullOrEmpty(base64))
                {
                    return $"<img src=\"{base64}\" style=\"max-width:150px;max-height:80px;\" />";
                }
            }

            return @"
                    <div style='text-align:center'>
                        <div style='font-style:italic;color:red'>Đã ký</div>
                        <div style='font-size:11px;color:red'>(Chưa cập nhật chữ ký)</div>
                    </div>";
        }

        /// <summary>
        /// Lấy dữ liệu phiếu xử lý KPH để xuất Excel tổng hợp
        /// </summary>
        public async Task<List<BmTongHopPhieuSanPhamKphRow>> GetDataExportTongHopKphAsync(DateOnly? fromDate, DateOnly? toDate, string? maBm = null)
        {
            // Nếu không chỉ định maBm, lấy dữ liệu từ cả hai form
            var phieus = new List<BmPhieu>();

            // Lấy từ CTD_KPH_Sanxuat (form mới)
            var dataphieu = (await _repoPhieu.GetAllAsync("CTD_KPH_Sanxuat", null)).ToList();
            phieus.AddRange(dataphieu);

            // Nếu chỉ định maBm cụ thể
            if (!string.IsNullOrWhiteSpace(maBm))
            {
                phieus = phieus.Where(p => p.MaBm == maBm).ToList();
            }

            // Filter by date
            if (fromDate.HasValue)
            {
                phieus = phieus.Where(p => p.NgaySX >= fromDate).ToList();
            }

            if (toDate.HasValue)
            {
                phieus = phieus.Where(p => p.NgaySX <= toDate).ToList();
            }

            var result = new List<BmTongHopPhieuSanPhamKphRow>();

            foreach (var phieu in phieus)
            {
                var kphItems = await _repo.GetByIdPhieuAsync(phieu.Idphieu);

                foreach (var item in kphItems)
                {
                    var row = new BmTongHopPhieuSanPhamKphRow
                    {
                        IdPhieu = phieu.Idphieu,
                        NgaySX = phieu.NgaySX,
                        CaSX = phieu.Ca,
                        KipSX = phieu.Kip,
                        NgayXL = item.NgayXL,
                        CaXL = item.CaXL,
                        KipXL = item.KipXL,
                        LenhSanXuat = item.LenhSanXuat,
                        MayDuc = phieu.MayDuc?.ToString(),

                        InSanPham = item.InSanPham,
                        InMacThep = item.InMacThep,
                        InChieuDai = item.InChieuDai,
                        InSoMe = item.InSoMe,
                        InSoThanh = item.InSoThanh,
                        InKhoiLuong = item.InKhoiLuong,
                        InCaNgaySx = item.InCaNgaySx,
                        InLoai = item.InLoai,

                        Reason = item.Reason,
                        Measures = item.Measures,

                        NewSanPham = item.NewSanPham,
                        NewMacThep = item.NewMacThep,
                        NewChieuDai = item.NewChieuDai,
                        NewSoMe = item.NewSoMe,
                        NewSoThanh = item.NewSoThanh,
                        NewKhoiLuong = item.NewKhoiLuong,
                        NewLoai = item.NewLoai,

                        SoPhieu = phieu.SoPhieu,
                        TinhTrang = phieu.TinhTrang,

                        CreatedAt = item.CreatedAt
                    };

                    // Lấy thông tin người lập và người phê duyệt
                    var pheDuyets = await _pheDuyetService.GetPheDuyetPhieuAsync(phieu.Idphieu);
                    var nguoiLap = pheDuyets.FirstOrDefault(x => x.CapDuyet == 0);
                    var nguoiPheDuyet = pheDuyets.FirstOrDefault(x => x.CapDuyet == 3);

                    row.NguoiLapPhieu = nguoiLap?.HoVaTen;
                    row.NguoiPheDuyet = nguoiPheDuyet?.HoVaTen;

                    result.Add(row);
                }
            }

            return result;
        }

        /// <summary>
        /// Xuất Excel tổng hợp phiếu xử lý KPH
        /// </summary>
        public async Task<ExportFileResult> ExportExcelTongHopKphAsync(DateOnly? fromDate, DateOnly? toDate, string? maBm = null)
        {
            var data = await GetDataExportTongHopKphAsync(fromDate, toDate, maBm);

            var templatePath = Path.Combine(_env.WebRootPath, "templates", "BM_TongHopPhieuSanPhamKPH.xlsx");
            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"Không tìm thấy file mẫu Excel: {templatePath}");

            using var workbook = new XLWorkbook(templatePath);
            var ws = workbook.Worksheet(1);
            try
            {
                var worksheet = workbook.Worksheet(1);

                // Ghi thông tin tiêu đề (ngày bắt đầu - kết thúc)
                var dateStr = $"Từ ngày: {fromDate:dd/MM/yyyy} đến ngày: {toDate:dd/MM/yyyy}";
                worksheet.Cell("A5").Value = dateStr;

                int row = 9; // Dữ liệu bắt đầu từ dòng 9 (dòng 8 là header)
                int stt = 1;

                foreach (var item in data)
                {
                    // Thông tin phiếu
                    worksheet.Cell(row, 1).Value = stt++;
                    worksheet.Cell(row, 2).Value = item.NgaySX?.ToString("dd/MM/yyyy");
                    worksheet.Cell(row, 3).Value = item.KipSX;
                    worksheet.Cell(row, 4).Value = item.CaSX;
                    worksheet.Cell(row, 5).Value = item.NgayXL?.ToString("dd/MM/yyyy");
                    worksheet.Cell(row, 6).Value = item.CaXL;
                    worksheet.Cell(row, 7).Value = item.KipXL;
                    worksheet.Cell(row, 8).Value = item.LenhSanXuat;
                    worksheet.Cell(row, 9).Value = "Cán " + item.MayDuc;

                    // Thông tin sản phẩm đầu vào
                    worksheet.Cell(row, 10).Value = item.InSanPham;
                    worksheet.Cell(row, 11).Value = item.InMacThep;
                    worksheet.Cell(row, 12).Value = item.InChieuDai;
                    worksheet.Cell(row, 13).Value = item.InSoMe;
                    worksheet.Cell(row, 14).Value = item.InSoThanh;
                    worksheet.Cell(row, 15).Value = item.InKhoiLuong;
                    worksheet.Cell(row, 16).Value = item.InCaNgaySx;
                    worksheet.Cell(row, 17).Value = item.InLoai;

                    // Lý do và biện pháp
                    worksheet.Cell(row, 18).Value = item.Reason;
                    worksheet.Cell(row, 19).Value = item.Measures;

                    // Thông tin sản phẩm mới
                    worksheet.Cell(row, 20).Value = item.NewSanPham;
                    worksheet.Cell(row, 21).Value = item.NewMacThep;
                    worksheet.Cell(row, 22).Value = item.NewChieuDai;
                    worksheet.Cell(row, 23).Value = item.NewSoMe;
                    worksheet.Cell(row, 24).Value = item.NewSoThanh;
                    worksheet.Cell(row, 25).Value = item.NewKhoiLuong;
                    worksheet.Cell(row, 26).Value = item.NewLoai;

                    // Phê duyệt
                    worksheet.Cell(row, 27).Value = item.SoPhieu;

                    // Trạng thái
                    var statusCell = worksheet.Cell(row, 28);
                    statusCell.Value = GetTinhTrangText(item.TinhTrang);
                    statusCell.Style.Fill.BackgroundColor = GetTinhTrangColor(item.TinhTrang);

                    row++;
                }
                // thêm border format cho toàn bộ vùng dữ liệu
                var dataRange = worksheet.Range(10, 1, row - 1, 28);
                dataRange.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                dataRange.Style.Border.SetInsideBorder(XLBorderStyleValues.Thin);

                // Lưu vào memory stream
                var ms = new MemoryStream();
                workbook.SaveAs(ms);
                ms.Position = 0;

                return new ExportFileResult
                {
                    Content = ms.ToArray(),
                    FileName = $"TongHopPhieuSanPhamKPH_{DateTime.Now:yyyyMMddHHmmss}.xlsx",
                    ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                };
            }
            finally
            {
                // Giải phóng resources
                workbook?.Dispose();
            }
        }
        private string GetTinhTrangText(int? tinhTrang)
        {
            return tinhTrang switch
            {
                0 => "Đang lưu",
                1 => "Đã gửi",
                2 => "Hoàn thành",
                3 => "Đã thu hồi",
                4 => "Không xác nhận",
                5 => "Đã chốt",
                6 => "Đang phê duyệt",
                _ => "Không xác định"
            };
        }

        private XLColor GetTinhTrangColor(int? tinhTrang)
        {
            return tinhTrang switch
            {
                0 => XLColor.LightGray,
                1 => XLColor.LightBlue,
                2 => XLColor.LightGreen,
                3 => XLColor.Orange,
                4 => XLColor.Red,
                5 => XLColor.DarkGreen,
                6 => XLColor.Yellow,
                _ => XLColor.White
            };
        }
    }
}

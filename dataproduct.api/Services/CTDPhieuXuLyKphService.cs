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

        public CTDPhieuXuLyKphService(
            ICtdPhieuXuLyKphRepository repo,
            IConverter pdfConverter,
            IWebHostEnvironment env,
            IConfiguration configuration,
            IPhieuRepository repoPhieu,
            PheDuyetService pheDuyetService,
            IHttpClientFactory httpClientFactory)
        {
            _repo = repo;
            _pdfConverter = pdfConverter;
            _env = env;
            _configuration = configuration;
            _repoPhieu = repoPhieu;
            _pheDuyetService = pheDuyetService;
            _httpClientFactory = httpClientFactory;
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

            var html = await File.ReadAllTextAsync(templatePath);

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

            var logoUrl = _configuration.GetValue<string>("AppSettings:LogoUrl") ?? "https://report.hoaphatdungquat.vn/img/logoHP.png";

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

                int row = 10; // Dữ liệu bắt đầu từ dòng 9 (dòng 8 là header)
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
                    worksheet.Cell(row, 9).Value = item.MayDuc;

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

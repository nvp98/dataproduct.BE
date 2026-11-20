using System.Globalization;
using System.Net;
using System.Text;
using ClosedXML.Excel;
using dataproduct.api.DTOs.CTD_Dto;
using dataproduct.api.DTOs.Export;
using dataproduct.api.Models;
using dataproduct.api.Repositories;
using DinkToPdf;
using DinkToPdf.Contracts;

namespace dataproduct.api.Services
{
    /// <summary>
    /// Xuất Biên bản chốt số liệu Xưởng ĐPV (BM.14/QT.05.11, MaBM HRC1_BCSX_DPV): PDF 1 phiếu, Excel 1 phiếu, Excel tổng hợp theo khoảng ngày.
    /// Dữ liệu đọc từ 4 bảng HRC1_BC_* (đã ghi khi lưu phiếu). Cột mỗi vùng khai báo 1 lần (CotThepLong…) và dùng chung cho cả 3 file
    /// → thứ tự cột / cách cộng tổng luôn giống nhau. Thứ tự cột phải khớp header trong template_html/HRC1_BCSX_DPV.html.
    /// Ký: P.KH lập (cấp 0) → NM.HRC1 (cấp 1) → P.CNLT (cấp 2); in từ trái sang phải P.CNLT – NM.HRC1 – P.KH như biên bản giấy.
    /// </summary>
    public class HRC1_BCSXExportService
    {
        private const string TenTemplate = "HRC1_BCSX_DPV.html";
        private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

        private readonly IHRC1_BCSXRepository _repo;
        private readonly PheDuyetService _pheDuyet;
        private readonly BmConfigService _bmConfig;
        private readonly IConverter _pdf;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public HRC1_BCSXExportService(IHRC1_BCSXRepository repo, PheDuyetService pheDuyet, BmConfigService bmConfig, IConverter pdf,
            IWebHostEnvironment env, IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _repo = repo;
            _pheDuyet = pheDuyet;
            _bmConfig = bmConfig;
            _pdf = pdf;
            _env = env;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        // ═══ Khai báo cột ═══════════════════════════════════════════════════════

        private enum KieuO { Giua, Trai, So }

        /// <summary>
        /// 1 cột: tiêu đề, cách lấy giá trị (dòng, chỉ số trong nhóm), kiểu, có cộng ở dòng tổng không, độ rộng Excel, nhóm header tầng 1,
        /// số ô ghép trên lưới 18 cột của PDF (cả biên bản là 1 bảng liền; tổng Span mỗi vùng = 18, khớp &lt;colgroup&gt; trong template).
        /// </summary>
        private sealed record Cot<T>(string Ten, Func<T, int, object?> Lay, KieuO Kieu = KieuO.Giua, bool Cong = false, double Rong = 10,
            string? Nhom = null, int Span = 1);

        private const int SoCotLuoiPdf = 18;

        // Lưới 18 cột (theo vùng IV): STT | Mẻ | Mác | KT | Kíp | 5 cột nguội | 5 cột nóng | Tổng | Đúc | Ghi chú
        private static List<Cot<HRC1_BC_ThepLong>> CotThepLong() =>
        [
            new("STT", (_, i) => i + 1, Rong: 5),
            new("Mác thép", (x, _) => x.Mac, KieuO.Trai, Rong: 14, Span: 2),
            new("Kíp", (x, _) => x.Kip, Rong: 6),
            new("Đvt", (x, _) => x.DonViTinh, Rong: 6),
            new("Số mẻ", (x, _) => x.SoLuongMe, Rong: 7),
            new("Đúc", (x, _) => x.MayDuc, Rong: 6),
            new("Tồn đầu", (x, _) => x.TonDau, KieuO.So, true, 12),
            new("Nhập", (x, _) => x.Nhap, KieuO.So, true, 13, Span: 2),
            new("Xuất", (x, _) => x.Xuat, KieuO.So, true, 13, Span: 2),
            new("Tồn cuối", (x, _) => x.TonCuoi, KieuO.So, true, 12, Span: 2),
            new("Lệnh sản xuất", (x, _) => x.LenhSanXuat, Rong: 14, Span: 3),
            new("Ghi chú", (x, _) => x.GhiChu, KieuO.Trai, Rong: 20),
        ];

        private static List<Cot<HRC1_BC_Dien>> CotDien() =>
        [
            new("STT", (_, i) => i + 1, Rong: 5),
            new("Nội dung", (x, _) => x.NoiDung, KieuO.Trai, Rong: 18, Span: 3),
            new("Kíp", (x, _) => x.Kip, Rong: 6),
            new("Đvt", (x, _) => x.DonViTinh, Rong: 7, Span: 2),
            new("Số liệu", (x, _) => x.SoLieu, KieuO.So, true, 14, Span: 3),
            new("Ghi chú", (x, _) => x.GhiChu, KieuO.Trai, Rong: 24, Span: 8),
        ];

        private static List<Cot<HRC1_BC_NguyenVatLieu>> CotNguyenVatLieu() =>
        [
            new("STT", (_, i) => i + 1, Rong: 5),
            new("Nội dung", (x, _) => x.NoiDung, KieuO.Trai, Rong: 18, Span: 3),
            new("Kíp", (x, _) => x.Kip, Rong: 6),
            new("Đvt", (x, _) => x.DonViTinh, Rong: 7, Span: 2),
            new("Số liệu", (x, _) => x.SoLieu, KieuO.So, true, 14, Span: 3),
            new("Số liệu", (x, _) => x.SoLieuBoPhan, KieuO.So, true, 12, "Bộ phận liên quan", Span: 2),
            new("Bộ phận", (x, _) => x.BoPhanLienQuan, Rong: 16, Nhom: "Bộ phận liên quan", Span: 2),
            new("Lệnh sản xuất", (x, _) => x.LenhSanXuat, Rong: 14, Span: 3),
            new("Ghi chú", (x, _) => x.GhiChu, KieuO.Trai, Rong: 20),
        ];

        // Biên bản ghi Phôi nguội trước Phôi nóng
        private static List<Cot<HRC1_BC_ThanhPham>> CotKhoiLuong() =>
        [
            new("Tổng số thanh", (x, _) => x.Nguoi_SoThanh, KieuO.So, true, 8, "Phôi nguội"),
            new("Loại I", (x, _) => x.Nguoi_Loai1, KieuO.So, true, 11, "Phôi nguội"),
            new("Loại II BM", (x, _) => x.Nguoi_Loai2BM, KieuO.So, true, 10, "Phôi nguội"),
            new("Loại II TPHH", (x, _) => x.Nguoi_Loai2TPHH, KieuO.So, true, 10, "Phôi nguội"),
            new("Loại III", (x, _) => x.Nguoi_Loai3, KieuO.So, true, 9, "Phôi nguội"),
            new("Tổng số thanh", (x, _) => x.Nong_SoThanh, KieuO.So, true, 8, "Phôi nóng"),
            new("Loại I", (x, _) => x.Nong_Loai1, KieuO.So, true, 11, "Phôi nóng"),
            new("Loại II BM", (x, _) => x.Nong_Loai2BM, KieuO.So, true, 10, "Phôi nóng"),
            new("Loại II TPHH", (x, _) => x.Nong_Loai2TPHH, KieuO.So, true, 10, "Phôi nóng"),
            new("Loại III", (x, _) => x.Nong_Loai3, KieuO.So, true, 9, "Phôi nóng"),
            new("Tổng", (x, _) => x.Tong ?? TongKhoiLuong(x), KieuO.So, true, 12),
        ];

        private static List<Cot<HRC1_BC_ThanhPham>> CotThanhPham() =>
        [
            new("STT", (_, i) => i + 1, Rong: 5),
            new("Mẻ", (x, _) => x.Me, Rong: 12),
            new("Mác thép", (x, _) => x.Mac, Rong: 10),
            new("Kích thước", (x, _) => x.KichThuoc, Rong: 15),
            new("Kíp", (x, _) => x.Kip, Rong: 5),
            .. CotKhoiLuong(),
            new("Đúc", (x, _) => x.MayDuc, Rong: 5),
            new("Ghi chú", (x, _) => x.GhiChu, KieuO.Trai, Rong: 16),
        ];

        private static List<Cot<HRC1_BC_ThanhPham>> CotTongSanLuong() =>
        [
            new("", (_, _) => null, Rong: 4),
            new("STT", (_, i) => i + 1, Rong: 5),
            new("Mác thép", (x, _) => x.Mac, Rong: 10),
            new("Kích thước", (x, _) => x.KichThuoc, Rong: 15),
            new("Kíp", (x, _) => x.Kip, Rong: 5),
            .. CotKhoiLuong(),
            new("Đúc", (x, _) => x.MayDuc, Rong: 5),
            new("LSX", (x, _) => x.LenhSanXuat, Rong: 14),
        ];

        /// <summary>Excel tổng hợp: bỏ STT, thêm Ngày + Số phiếu ở đầu.</summary>
        private static List<Cot<T>> ThemNgay<T>(List<Cot<T>> cots, Dictionary<Guid, string?> soPhieu) where T : HRC1_BC_DongBase =>
        [
            new("Ngày", (x, _) => x.NgaySX, Rong: 11),
            new("Số phiếu", (x, _) => soPhieu.GetValueOrDefault(x.PhieuID), Rong: 16),
            .. cots.Where(c => c.Ten != "STT" && c.Ten != ""),
        ];

        private static decimal TongKhoiLuong(HRC1_BC_ThanhPham x)
            => (x.Nguoi_Loai1 ?? 0) + (x.Nguoi_Loai2BM ?? 0) + (x.Nguoi_Loai2TPHH ?? 0) + (x.Nguoi_Loai3 ?? 0)
             + (x.Nong_Loai1 ?? 0) + (x.Nong_Loai2BM ?? 0) + (x.Nong_Loai2TPHH ?? 0) + (x.Nong_Loai3 ?? 0);

        /// <summary>Nhóm theo kíp, giữ thứ tự đã sắp (ca 1 trước ca 2).</summary>
        private static List<List<T>> TheoKip<T>(IEnumerable<T> rows) where T : HRC1_BC_DongBase
            => rows.GroupBy(x => (x.NgaySX, x.CaSX, x.Kip)).Select(g => g.ToList()).ToList();

        /// <summary>Bảng "Tổng sản lượng": gộp thành phẩm theo (Ngày, Kíp, Mác, KT, Đúc, LSX), thứ tự xuất hiện.</summary>
        private static List<HRC1_BC_ThanhPham> TongSanLuong(IEnumerable<HRC1_BC_ThanhPham> rows)
            => rows.GroupBy(x => (x.NgaySX, x.CaSX, x.Kip, x.Mac, x.KichThuoc, x.MayDuc, x.LenhSanXuat))
                .Select(g => new HRC1_BC_ThanhPham
                {
                    PhieuID = g.First().PhieuID,
                    NgaySX = g.Key.NgaySX,
                    CaSX = g.Key.CaSX,
                    Kip = g.Key.Kip,
                    Mac = g.Key.Mac,
                    KichThuoc = g.Key.KichThuoc,
                    MayDuc = g.Key.MayDuc,
                    LenhSanXuat = g.Key.LenhSanXuat,
                    Me = "",
                    Nguoi_SoThanh = g.Sum(x => x.Nguoi_SoThanh ?? 0),
                    Nguoi_Loai1 = g.Sum(x => x.Nguoi_Loai1 ?? 0),
                    Nguoi_Loai2BM = g.Sum(x => x.Nguoi_Loai2BM ?? 0),
                    Nguoi_Loai2TPHH = g.Sum(x => x.Nguoi_Loai2TPHH ?? 0),
                    Nguoi_Loai3 = g.Sum(x => x.Nguoi_Loai3 ?? 0),
                    Nong_SoThanh = g.Sum(x => x.Nong_SoThanh ?? 0),
                    Nong_Loai1 = g.Sum(x => x.Nong_Loai1 ?? 0),
                    Nong_Loai2BM = g.Sum(x => x.Nong_Loai2BM ?? 0),
                    Nong_Loai2TPHH = g.Sum(x => x.Nong_Loai2TPHH ?? 0),
                    Nong_Loai3 = g.Sum(x => x.Nong_Loai3 ?? 0),
                    Tong = g.Sum(x => x.Tong ?? TongKhoiLuong(x)),
                })
                .ToList();

        /// <summary>Tổng cột cộng được (null → 0); cột không cộng → null.</summary>
        private static object?[] TinhTong<T>(List<Cot<T>> cots, IReadOnlyList<T> rows)
            => cots.Select(c =>
            {
                if (!c.Cong) return (object?)null;
                decimal s = 0;
                for (int i = 0; i < rows.Count; i++)
                    s += c.Lay(rows[i], i) switch { decimal d => d, int n => n, _ => 0 };
                return s;
            }).ToArray();

        // ═══ PDF ════════════════════════════════════════════════════════════════

        public async Task<ExportFileResult> ExportPdfAsync(Guid idPhieu)
        {
            var phieu = await _repo.GetPhieuByIdAsync(idPhieu) ?? throw new Exception("Không tìm thấy phiếu.");
            var ct = await _repo.GetByPhieuAsync(idPhieu);
            var pheDuyets = await _pheDuyet.GetPheDuyetPhieuAsync(idPhieu);
            var ngay = phieu.NgaySX ?? DateOnly.FromDateTime(DateTime.Today);

            // Cả biên bản là 1 bảng liền trên lưới 18 cột: mỗi vùng = dòng tiêu đề vùng + header cột + dòng dữ liệu (+ Tổng kíp).
            // Riêng vùng I: header cột đứng trước dòng "I Thép lỏng" (giống biên bản giấy).
            var bang = new StringBuilder();
            void VungTheoKip<T>(List<Cot<T>> cots, List<T> rows) where T : HRC1_BC_DongBase
            {
                foreach (var kip in TheoKip(rows))
                {
                    for (int i = 0; i < kip.Count; i++) bang.Append(HtmlDong(cots, kip[i], i));
                    bang.Append(HtmlTong(cots, kip, "Tổng kíp", 1));
                }
            }

            var cotTL = CotThepLong();
            bang.Append(HtmlHeader(cotTL)).Append(HtmlTieuDeVung("I", "Thép lỏng"));
            VungTheoKip(cotTL, ct.ThepLong);

            var cotD = CotDien();
            bang.Append(HtmlTieuDeVung("II", "Điện")).Append(HtmlHeader(cotD));
            VungTheoKip(cotD, ct.Dien);

            var cotN = CotNguyenVatLieu();
            bang.Append(HtmlTieuDeVung("III", "Nguyên vật liệu xuất cho các nhà máy")).Append(HtmlHeader(cotN));
            VungTheoKip(cotN, ct.NguyenVatLieu);

            var cotTP = CotThanhPham();
            bang.Append(HtmlTieuDeVung("IV", "Thành phẩm")).Append(HtmlHeader(cotTP));
            for (int i = 0; i < ct.ThanhPham.Count; i++)
                bang.Append(HtmlDong(cotTP, ct.ThanhPham[i], i, ct.ThanhPham[i].LaNganDai ? "nd" : null));
            bang.Append(HtmlTong(cotTP, ct.ThanhPham, "Tổng cộng", 1));

            var cotTSL = CotTongSanLuong();
            var tongSL = TongSanLuong(ct.ThanhPham);
            bang.Append(HtmlTieuDeVung("", "Tổng sản lượng")).Append(HtmlHeader(cotTSL));
            bang.Append(HtmlTong(cotTSL, tongSL, "", 1));
            foreach (var kip in TheoKip(tongSL))
                for (int i = 0; i < kip.Count; i++) bang.Append(HtmlDong(cotTSL, kip[i], i));

            // Ký: cấp 0 P.KH (người lập — có chữ ký khi phiếu đã gửi), cấp 1 NM.HRC1, cấp 2 P.CNLT (khi đã duyệt)
            var daGui = phieu.TinhTrang is not (null or 0 or 3);
            PheDuyetDto? Cap(int cap) => pheDuyets.FirstOrDefault(x => x.CapDuyet == cap);
            async Task<string> ChuKy(PheDuyetDto? p, bool daKy) => daKy ? await FormatChuKyAsync(p?.ChuKy) : "";
            var kh = Cap(0); var hrc1 = Cap(1); var cnlt = Cap(2);

            var templatePath = Path.Combine(_env.WebRootPath, "template_html", TenTemplate);
            var html = (await _bmConfig.LoadTemplateAsync(templatePath))
                .Replace("{{LogoUrl}}", await LogoAsync())
                .Replace("{{TuNgayText}}", NgayChu(ngay))
                .Replace("{{DenNgayText}}", NgayChu(ngay.AddDays(1)))
                .Replace("{{NgayKyText}}", NgayChu(ngay.AddDays(1)))
                .Replace("{{BangBienBan}}", bang.ToString())
                .Replace("{{Sign_KH}}", await ChuKy(kh, daGui && kh != null))
                .Replace("{{Sign_HRC1}}", await ChuKy(hrc1, hrc1?.TinhTrang == 1))
                .Replace("{{Sign_CNLT}}", await ChuKy(cnlt, cnlt?.TinhTrang == 1))
                .Replace("{{Name_KH}}", Enc(kh?.HoVaTen))
                .Replace("{{Name_HRC1}}", Enc(hrc1?.HoVaTen))
                .Replace("{{Name_CNLT}}", Enc(cnlt?.HoVaTen));

            var doc = new HtmlToPdfDocument
            {
                GlobalSettings =
                {
                    PaperSize = PaperKind.A4,
                    Orientation = Orientation.Landscape,
                    Margins = new MarginSettings { Top = 10, Bottom = 10, Left = 8, Right = 8 },
                },
                Objects =
                {
                    new ObjectSettings
                    {
                        HtmlContent = html,
                        WebSettings = { DefaultEncoding = "utf-8", LoadImages = true, EnableJavascript = false, PrintMediaType = true },
                        LoadSettings = { BlockLocalFileAccess = false, LoadErrorHandling = ContentErrorHandling.Ignore },
                        FooterSettings = { FontSize = 8, Center = "[page]", Spacing = 3 },
                    },
                },
            };

            return new ExportFileResult
            {
                Content = _pdf.Convert(doc),
                ContentType = "application/pdf",
                FileName = $"BBChotSoLieu_DPV_{ngay:yyyyMMdd}_{phieu.SoPhieu ?? idPhieu.ToString("N")}.pdf",
            };
        }

        private static string HtmlDong<T>(List<Cot<T>> cots, T row, int idx, string? cls = null)
        {
            var sb = new StringBuilder(cls == null ? "<tr>" : $"<tr class=\"{cls}\">");
            foreach (var c in cots)
                sb.Append(HtmlO(c.Kieu, c.Lay(row, idx), c.Ten == "Kích thước" ? "kt" : null, c.Span));
            return sb.Append("</tr>").ToString();
        }

        private static string HtmlTong<T>(List<Cot<T>> cots, IReadOnlyList<T> rows, string nhan, int cotNhan)
        {
            var tong = TinhTong(cots, rows);
            var sb = new StringBuilder("<tr class=\"total\">");
            for (int c = 0; c < cots.Count; c++)
                sb.Append(c == cotNhan && nhan != ""
                    ? $"<td class=\"text-left\"{GhepO(cots[c].Span)}>{Enc(nhan)}</td>"
                    : HtmlO(cots[c].Kieu, tong[c], span: cots[c].Span));
            return sb.Append("</tr>").ToString();
        }

        private static string GhepO(int span) => span > 1 ? $" colspan=\"{span}\"" : "";

        private static string HtmlO(KieuO kieu, object? v, string? cls = null, int span = 1)
        {
            var lop = kieu switch { KieuO.So => "text-right", KieuO.Trai => "text-left", _ => "" };
            if (cls != null) lop = (lop + " " + cls).Trim();
            var text = v switch
            {
                null => "",
                decimal d => d.ToString("#,##0.###", Vi),
                int n => kieu == KieuO.So ? n.ToString("#,##0", Vi) : n.ToString(),
                DateOnly dt => dt.ToString("dd/MM/yyyy"),
                _ => Enc(v.ToString()),
            };
            return lop == "" ? $"<td{GhepO(span)}>{text}</td>" : $"<td class=\"{lop}\"{GhepO(span)}>{text}</td>";
        }

        /// <summary>Header cột của 1 vùng (1 hoặc 2 tầng theo Nhom), ghép ô theo Span trên lưới 18 cột.</summary>
        private static string HtmlHeader<T>(List<Cot<T>> cots)
        {
            var haiTang = cots.Any(c => c.Nhom != null);
            var t1 = new StringBuilder("<tr class=\"hdr\">");
            var t2 = new StringBuilder("<tr class=\"hdr\">");
            for (int i = 0; i < cots.Count; i++)
            {
                var c = cots[i];
                var ten = Enc(c.Ten);
                if (!haiTang || c.Nhom == null)
                {
                    t1.Append($"<th{GhepO(c.Span)}{(haiTang ? " rowspan=\"2\"" : "")}>{ten}</th>");
                    continue;
                }
                if (i == 0 || cots[i - 1].Nhom != c.Nhom)
                {
                    var tongSpan = cots.Skip(i).TakeWhile(x => x.Nhom == c.Nhom).Sum(x => x.Span);
                    t1.Append($"<th colspan=\"{tongSpan}\">{Enc(c.Nhom)}</th>");
                }
                t2.Append($"<th{GhepO(c.Span)}>{ten}</th>");
            }
            t1.Append("</tr>");
            return haiTang ? t1.Append(t2).Append("</tr>").ToString() : t1.ToString();
        }

        private static string HtmlTieuDeVung(string so, string ten)
            => $"<tr class=\"section\"><td>{Enc(so)}</td><td colspan=\"{SoCotLuoiPdf - 1}\">{Enc(ten)}</td></tr>";

        private static string NgayChu(DateOnly d) => $"{d.Day:00} tháng {d.Month:00} năm {d.Year}";

        private static string Enc(string? s) => WebUtility.HtmlEncode(s ?? "");

        private async Task<string> LogoAsync()
        {
            var path = Path.Combine(_env.WebRootPath, "imgs", "LogoPDF.png");
            return File.Exists(path) ? $"data:image/png;base64,{Convert.ToBase64String(await File.ReadAllBytesAsync(path))}" : "";
        }

        private async Task<string> FormatChuKyAsync(string? chuKy)
        {
            if (string.IsNullOrWhiteSpace(chuKy)) return "<div style='font-style:italic;color:red'>Đã ký</div>";
            string? src = null;
            if (chuKy.StartsWith("data:image", StringComparison.OrdinalIgnoreCase)) src = chuKy;
            else if (chuKy.StartsWith("http", StringComparison.OrdinalIgnoreCase)) src = await TaiAnhBase64Async(chuKy);
            else if (chuKy.StartsWith('/'))
            {
                var domain = _configuration.GetValue<string>("AppSettings:Domain") ?? "https://report.hoaphatdungquat.vn";
                src = await TaiAnhBase64Async(domain.TrimEnd('/') + chuKy);
            }
            return string.IsNullOrEmpty(src)
                ? "<div style='font-style:italic;color:red'>Đã ký</div>"
                : $"<img src=\"{src}\" style=\"max-width:150px;max-height:65px;\" />";
        }

        private async Task<string?> TaiAnhBase64Async(string url)
        {
            try
            {
                using var http = _httpClientFactory.CreateClient();
                http.Timeout = TimeSpan.FromSeconds(10);
                var bytes = await http.GetByteArrayAsync(url);
                var mime = Path.GetExtension(url).ToLowerInvariant() switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".gif" => "image/gif",
                    ".svg" => "image/svg+xml",
                    _ => "image/png",
                };
                return $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
            }
            catch
            {
                return null;
            }
        }

        // ═══ Excel 1 phiếu (bố cục biên bản) ═══════════════════════════════════

        public async Task<ExportFileResult> ExportDetailExcelAsync(Guid idPhieu)
        {
            var phieu = await _repo.GetPhieuByIdAsync(idPhieu) ?? throw new Exception("Không tìm thấy phiếu.");
            var ct = await _repo.GetByPhieuAsync(idPhieu);
            var pheDuyets = await _pheDuyet.GetPheDuyetPhieuAsync(idPhieu);
            var ngay = phieu.NgaySX ?? DateOnly.FromDateTime(DateTime.Today);
            var bm = await _bmConfig.GetAsync(Path.GetFileNameWithoutExtension(TenTemplate));

            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("BienBan");
            ws.Style.Font.FontName = "Times New Roman";
            ws.Style.Font.FontSize = 11;
            const int soCot = 18;

            ws.Cell(1, 1).Value = "CÔNG TY CỔ PHẦN THÉP HÒA PHÁT DUNG QUẤT";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, soCot).Value = bm?.MaBM ?? "BM.14/QT.05.11";
            ws.Cell(2, soCot).Value = $"Ngày hiệu lực: {bm?.NgayHieuLuc}";
            ws.Cell(3, soCot).Value = $"Lần sửa đổi: {bm?.LanSuaDoi}";
            ws.Range(1, soCot, 3, soCot).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            ws.Range(1, soCot, 3, soCot).Style.Font.Bold = true;
            var tieuDe = ws.Range(4, 1, 4, soCot).Merge();
            tieuDe.Value = "BIÊN BẢN CHỐT SỐ LIỆU XƯỞNG ĐÚC PHÔI VUÔNG";
            tieuDe.Style.Font.Bold = true;
            tieuDe.Style.Font.FontSize = 15;
            tieuDe.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            var kyTxt = ws.Range(5, 1, 5, soCot).Merge();
            kyTxt.Value = $"Từ 08 giờ 00 ngày {NgayChu(ngay)} đến 08 giờ 00 ngày {NgayChu(ngay.AddDays(1))}";
            kyTxt.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            var dong = 7;
            dong = GhiVungTheoKip(ws, dong, "I. Thép lỏng", CotThepLong(), ct.ThepLong);
            dong = GhiVungTheoKip(ws, dong + 1, "II. Điện", CotDien(), ct.Dien);
            dong = GhiVungTheoKip(ws, dong + 1, "III. Nguyên vật liệu xuất cho các nhà máy", CotNguyenVatLieu(), ct.NguyenVatLieu);

            // IV. Thành phẩm
            dong++;
            GhiTieuDeVung(ws, dong++, "IV. Thành phẩm");
            var cotTP = CotThanhPham();
            var dauBang = dong;
            dong = GhiHeader(ws, dong, cotTP);
            for (int i = 0; i < ct.ThanhPham.Count; i++)
            {
                GhiDong(ws, dong, cotTP, ct.ThanhPham[i], i);
                if (ct.ThanhPham[i].LaNganDai) ws.Range(dong, 1, dong, cotTP.Count).Style.Fill.BackgroundColor = XLColor.FromHtml("#E7E6E6");
                dong++;
            }
            GhiTong(ws, dong++, cotTP, ct.ThanhPham, "Tổng cộng", 1);
            KeVien(ws, dauBang, dong - 1, cotTP.Count);

            // Tổng sản lượng
            dong++;
            GhiTieuDeVung(ws, dong++, "Tổng sản lượng");
            var cotTSL = CotTongSanLuong();
            var tongSL = TongSanLuong(ct.ThanhPham);
            dauBang = dong;
            dong = GhiHeader(ws, dong, cotTSL);
            GhiTong(ws, dong++, cotTSL, tongSL, "", 1);
            foreach (var kip in TheoKip(tongSL))
                for (int i = 0; i < kip.Count; i++) GhiDong(ws, dong++, cotTSL, kip[i], i);
            KeVien(ws, dauBang, dong - 1, cotTSL.Count);

            // Ký: P.CNLT – NM.HRC1 – P.KH
            dong++;
            ws.Cell(dong, soCot).Value = $"Quảng Ngãi, ngày {NgayChu(ngay.AddDays(1))}";
            ws.Cell(dong, soCot).Style.Font.Italic = true;
            ws.Cell(dong, soCot).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            dong++;
            var ky = new[] { ("P.CNLT", 2, 2), ("NM.HRC1", 1, 9), ("P.KH", 0, 15) };
            foreach (var (ten, cap, cot) in ky)
            {
                ws.Cell(dong, cot).Value = ten;
                ws.Cell(dong, cot).Style.Font.Bold = true;
                ws.Cell(dong + 4, cot).Value = pheDuyets.FirstOrDefault(x => x.CapDuyet == cap)?.HoVaTen ?? "";
            }

            for (int c = 1; c <= soCot; c++)
                ws.Column(c).Width = Math.Max(cotTP.ElementAtOrDefault(c - 1)?.Rong ?? 10, 6);
            ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
            ws.PageSetup.FitToPages(1, 0);

            return KetQuaExcel(wb, $"BBChotSoLieu_DPV_{ngay:yyyyMMdd}_{phieu.SoPhieu ?? idPhieu.ToString("N")}.xlsx");
        }

        /// <summary>1 vùng có dòng "Tổng kíp" sau mỗi kíp (Thép lỏng, Điện, NVL).</summary>
        private static int GhiVungTheoKip<T>(IXLWorksheet ws, int dong, string tieuDe, List<Cot<T>> cots, List<T> rows) where T : HRC1_BC_DongBase
        {
            GhiTieuDeVung(ws, dong++, tieuDe);
            var dau = dong;
            dong = GhiHeader(ws, dong, cots);
            foreach (var kip in TheoKip(rows))
            {
                for (int i = 0; i < kip.Count; i++) GhiDong(ws, dong++, cots, kip[i], i);
                GhiTong(ws, dong++, cots, kip, "Tổng kíp", 1);
            }
            KeVien(ws, dau, dong - 1, cots.Count);
            return dong;
        }

        private static void GhiTieuDeVung(IXLWorksheet ws, int dong, string tieuDe)
        {
            ws.Cell(dong, 1).Value = tieuDe;
            ws.Cell(dong, 1).Style.Font.Bold = true;
        }

        // ═══ Excel tổng hợp theo khoảng ngày ═══════════════════════════════════

        public async Task<ExportFileResult> ExportTongHopExcelAsync(DateOnly? tuNgay, DateOnly? denNgay)
        {
            var den = denNgay ?? DateOnly.FromDateTime(DateTime.Today);
            var tu = tuNgay ?? new DateOnly(den.Year, den.Month, 1);
            if (tu > den) (tu, den) = (den, tu);

            var (data, soPhieu) = await _repo.GetTheoKhoangNgayAsync(HRC1_BCSXService.MaBM, tu, den);

            using var wb = new XLWorkbook();
            var tieuDe = $"BIÊN BẢN CHỐT SỐ LIỆU XƯỞNG ĐÚC PHÔI VUÔNG — TỔNG HỢP TỪ {tu:dd/MM/yyyy} ĐẾN {den:dd/MM/yyyy}";
            SheetTongHop(wb, "TongSanLuong", tieuDe, ThemNgay(CotTongSanLuong(), soPhieu), TongSanLuong(data.ThanhPham));
            SheetTongHop(wb, "ThanhPham", tieuDe, ThemNgay(CotThanhPham(), soPhieu), data.ThanhPham);
            SheetTongHop(wb, "ThepLong", tieuDe, ThemNgay(CotThepLong(), soPhieu), data.ThepLong);
            SheetTongHop(wb, "Dien", tieuDe, ThemNgay(CotDien(), soPhieu), data.Dien);
            SheetTongHop(wb, "NguyenVatLieu", tieuDe, ThemNgay(CotNguyenVatLieu(), soPhieu), data.NguyenVatLieu);

            return KetQuaExcel(wb, $"BBChotSoLieu_DPV_TongHop_{tu:yyyyMMdd}_{den:yyyyMMdd}.xlsx");
        }

        private static void SheetTongHop<T>(XLWorkbook wb, string ten, string tieuDe, List<Cot<T>> cots, List<T> rows)
        {
            var ws = wb.AddWorksheet(ten);
            ws.Style.Font.FontName = "Times New Roman";
            ws.Cell(1, 1).Value = tieuDe;
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 13;

            const int dau = 3;
            var dong = GhiHeader(ws, dau, cots);
            var dongDuLieu = dong;
            for (int i = 0; i < rows.Count; i++) GhiDong(ws, dong++, cots, rows[i], i);
            if (rows.Count == 0) ws.Cell(dong++, 1).Value = "Không có phiếu nào trong khoảng ngày.";
            else GhiTong(ws, dong++, cots, rows, "Tổng", 0);
            KeVien(ws, dau, dong - 1, cots.Count);

            for (int c = 0; c < cots.Count; c++) ws.Column(c + 1).Width = cots[c].Rong;
            ws.SheetView.Freeze(dongDuLieu - 1, 2);
            if (rows.Count > 0) ws.Range(dongDuLieu - 1, 1, dong - 2, cots.Count).SetAutoFilter();
        }

        // ═══ Ghi ô Excel ═══════════════════════════════════════════════════════

        /// <summary>Header 1 hoặc 2 tầng (cột có Nhom → tầng 1 gộp ngang, cột không nhóm → gộp dọc). Trả dòng kế tiếp.</summary>
        private static int GhiHeader<T>(IXLWorksheet ws, int dong, List<Cot<T>> cots)
        {
            var haiTang = cots.Any(c => c.Nhom != null);
            for (int c = 0; c < cots.Count; c++)
            {
                var cot = cots[c];
                if (!haiTang) { ws.Cell(dong, c + 1).Value = cot.Ten; continue; }
                if (cot.Nhom == null)
                {
                    ws.Range(dong, c + 1, dong + 1, c + 1).Merge().Value = cot.Ten;
                    continue;
                }
                ws.Cell(dong + 1, c + 1).Value = cot.Ten;
                if (c == 0 || cots[c - 1].Nhom != cot.Nhom)
                {
                    var cuoi = c;
                    while (cuoi + 1 < cots.Count && cots[cuoi + 1].Nhom == cot.Nhom) cuoi++;
                    ws.Range(dong, c + 1, dong, cuoi + 1).Merge().Value = cot.Nhom;
                }
            }
            var soDong = haiTang ? 2 : 1;
            var vung = ws.Range(dong, 1, dong + soDong - 1, cots.Count);
            vung.Style.Font.Bold = true;
            vung.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9E1F2");
            vung.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            vung.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            vung.Style.Alignment.WrapText = true;
            return dong + soDong;
        }

        private static void GhiDong<T>(IXLWorksheet ws, int dong, List<Cot<T>> cots, T row, int idx)
        {
            for (int c = 0; c < cots.Count; c++)
                GhiO(ws.Cell(dong, c + 1), cots[c].Kieu, cots[c].Lay(row, idx));
        }

        private static void GhiTong<T>(IXLWorksheet ws, int dong, List<Cot<T>> cots, IReadOnlyList<T> rows, string nhan, int cotNhan)
        {
            var tong = TinhTong(cots, rows);
            for (int c = 0; c < cots.Count; c++)
                if (c == cotNhan && nhan != "") ws.Cell(dong, c + 1).Value = nhan;
                else GhiO(ws.Cell(dong, c + 1), cots[c].Kieu, tong[c]);
            ws.Range(dong, 1, dong, cots.Count).Style.Font.Bold = true;
            ws.Range(dong, 1, dong, cots.Count).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF2CC");
        }

        private static void GhiO(IXLCell o, KieuO kieu, object? v)
        {
            switch (v)
            {
                case null: break;
                case decimal d:
                    o.Value = (double)d;
                    o.Style.NumberFormat.Format = d == decimal.Truncate(d) ? "#,##0" : "#,##0.###";
                    break;
                case int n:
                    o.Value = n;
                    if (kieu == KieuO.So) o.Style.NumberFormat.Format = "#,##0";
                    break;
                case DateOnly dt:
                    o.Value = dt.ToDateTime(TimeOnly.MinValue);
                    o.Style.DateFormat.Format = "dd/MM/yyyy";
                    break;
                default:
                    o.Value = v.ToString();
                    break;
            }
            o.Style.Alignment.Horizontal = kieu switch
            {
                KieuO.So => XLAlignmentHorizontalValues.Right,
                KieuO.Trai => XLAlignmentHorizontalValues.Left,
                _ => XLAlignmentHorizontalValues.Center,
            };
        }

        private static void KeVien(IXLWorksheet ws, int tu, int den, int soCot)
        {
            if (den < tu) return;
            var vung = ws.Range(tu, 1, den, soCot);
            vung.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            vung.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        }

        private static ExportFileResult KetQuaExcel(XLWorkbook wb, string tenFile)
        {
            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return new ExportFileResult
            {
                Content = ms.ToArray(),
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileName = tenFile,
            };
        }
    }
}

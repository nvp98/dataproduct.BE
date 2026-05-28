using ClosedXML.Excel;
using dataproduct.api.DTOs.CTD_Dto;
using dataproduct.api.DTOs.Export;
using dataproduct.api.Models;
using dataproduct.api.Repositories;
using DinkToPdf;
using DinkToPdf.Contracts;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NuGet.Protocol.Core.Types;
using System;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Text.Json;
using static dataproduct.api.DTOs.CTD_Dto.PhoinhapkhoDto;

namespace dataproduct.api.Services
{
    public class BMDucCTDService
    {
        private readonly ICtdBMDucCTDRepository _repo;
        private readonly IConverter _pdfConverter;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly PheDuyetService _pheDuyetService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IPhieuRepository _repoPhieu;
        private readonly ProductFormContext _context;


        public BMDucCTDService(ICtdBMDucCTDRepository repo, IConverter pdfConverter, IWebHostEnvironment env, IConfiguration configuration, PheDuyetService pheDuyetService, IHttpClientFactory httpClientFactory, IPhieuRepository repoPhieu, ProductFormContext context)
        {
            _repo = repo;
            _pdfConverter = pdfConverter;
            _env = env;
            _configuration = configuration;
            _pheDuyetService = pheDuyetService;
            _httpClientFactory = httpClientFactory;
            _repoPhieu = repoPhieu;
            _context = context;
        }

        private static string NormalizeKeyValue(string? value)
        {
            return (value ?? string.Empty).Trim().ToUpperInvariant();
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


        public async Task<List<SanLuongPhoiDto>> GetByKipNgayAsync(string ca, string kip, DateTime ngaySX)
        {
            return await _repo.GetSanLuongPhoiAsync(ca, kip, ngaySX);
        }

        public async Task<int> InsertSanLuongPhoiFromPhieuJsonAsync(BmPhieu phieu)
        {
            if (phieu == null)
                throw new ArgumentNullException(nameof(phieu));

            var entities = new List<BM_SanLuongPhoi>();

            if (!string.IsNullOrWhiteSpace(phieu.DataJson))
            {
                using var jsonDoc = JsonDocument.Parse(phieu.DataJson);
                var root = jsonDoc.RootElement;

                if (TryGetRowsElement(root, out var rowsElement))
                {
                    foreach (var row in rowsElement.EnumerateArray())
                    {
                        if (row.ValueKind != JsonValueKind.Object)
                            continue;

                        entities.Add(new BM_SanLuongPhoi
                        {
                            IdPhieu = phieu.Idphieu,
                            SoPhieu = phieu.SoPhieu ?? string.Empty,
                            NgaySX = (phieu.NgaySX ?? DateOnly.FromDateTime(DateTime.Today)).ToDateTime(TimeOnly.MinValue),
                            Kip = phieu.Kip ?? string.Empty,
                            Ca = phieu.Ca ?? 0,
                            MayDuc = phieu.MayDuc ?? 0,
                            MacThep = TryGetString(row, "macThep", "MacThep") ?? string.Empty,
                            KichThuoc = TryGetString(row, "kichThuoc", "KichThuoc") ?? string.Empty,
                            StLoai1 = TryGetInt(row, "stLoai1", "StLoai1"),
                            KlLoai1 = TryGetDecimal(row, "klLoai1", "KlLoai1"),
                            StPhoiNgan = TryGetInt(row, "stPhoiNgan", "StPhoiNgan"),
                            KlPhoiNgan = TryGetDecimal(row, "klPhoiNgan", "KlPhoiNgan"),
                            StLoai2 = TryGetInt(row, "stLoai2", "StLoai2"),
                            KlLoai2 = TryGetDecimal(row, "klLoai2", "KlLoai2"),
                            StLoai3 = TryGetInt(row, "stLoai3", "StLoai3"),
                            KlLoai3 = TryGetDecimal(row, "klLoai3", "KlLoai3"),
                            TongSoThanh = TryGetInt(row, "tongSoThanh", "TongSoThanh"),
                            TongKhoiLuong = TryGetDecimal(row, "tongKhoiLuong", "TongKhoiLuong"),
                            NguoiTaoId = phieu.NguoiTaoId,
                            ThoiGianTao = DateTime.Now,
                            TTHD = true
                        });
                    }
                }
            }

            await _repo.DeleteSanLuongPhoiByPhieuAsync(phieu.Idphieu);

            // Khi phiếu là bản hiệu chỉnh (clone), xóa luôn dữ liệu chi tiết của phiếu gốc
            // để tránh còn song song dữ liệu "phiếu cũ".
            if (phieu.ID_PhieuGoc.HasValue
                && phieu.ID_PhieuGoc.Value != Guid.Empty
                && phieu.ID_PhieuGoc.Value != phieu.Idphieu)
            {
                await _repo.DeleteSanLuongPhoiByPhieuAsync(phieu.ID_PhieuGoc.Value);
            }



            if (entities.Count == 0)
                return 0;

            await _repo.AddSanLuongPhoiListAsync(entities);
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

        private decimal? TryGetDecimal(JsonElement row, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (row.TryGetProperty(key, out var value) && value.ValueKind != JsonValueKind.Null)
                {
                    if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var n))
                        return n;

                    if (value.ValueKind == JsonValueKind.String && decimal.TryParse(value.GetString(), out var parsed))
                        return parsed;
                }
            }

            return null;
        }

        public async Task<List<PhoinhapkhoNhanPhoiDto>> GetPhoiNhapKhoAsync(string ca, string kip, DateTime ngaySX, int mayduc)
        {
            return await _repo.GetPhoiNhapKhoAsync(ca, kip, ngaySX, mayduc);
        }

        public async Task<(List<PhoiNhapKhoListItemDto> Data, int Total)> GetPhoiNhapKhoListAsync(
            Guid? idPhieu,
            DateTime? fromDate,
            DateTime? toDate,
            string? kip,
            int? ca,
            int? mayDuc,
            string? soPhieu,
            int page = 1,
            int pageSize = 200)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 200;

            var query = _context.BM_PhoiNhapKho.AsNoTracking().AsQueryable();

            if (idPhieu.HasValue && idPhieu.Value != Guid.Empty)
                query = query.Where(x => x.IdPhieu == idPhieu.Value);

            if (fromDate.HasValue)
                query = query.Where(x => x.NgaySX.Date >= fromDate.Value.Date);

            if (toDate.HasValue)
                query = query.Where(x => x.NgaySX.Date <= toDate.Value.Date);

            if (!string.IsNullOrWhiteSpace(kip))
                query = query.Where(x => x.Kip == kip);

            if (ca.HasValue)
                query = query.Where(x => x.Ca == ca.Value);

            if (mayDuc.HasValue)
                query = query.Where(x => x.MayDuc == mayDuc.Value);

            if (!string.IsNullOrWhiteSpace(soPhieu))
                query = query.Where(x => x.SoPhieu.Contains(soPhieu));

            var total = await query.CountAsync();

            var data = await query
                .OrderByDescending(x => x.ThoiGianTao)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new PhoiNhapKhoListItemDto
                {
                    Id = x.Id,
                    IdPhieu = x.IdPhieu,
                    SoPhieu = x.SoPhieu,
                    NgaySX = x.NgaySX, // ngày đúc -> lấy từ BK_PhoiThep
                    Kip = x.Kip,
                    Ca = x.Ca,
                    MayDuc = x.MayDuc,
                    Me = x.Me,
                    Mac = x.Mac,
                    KichThuoc = x.KichThuoc,
                    StLoai1 = x.StLoai1,
                    KlLoai1 = x.KlLoai1,
                    StPhoiNgan = x.StPhoiNgan,
                    KlPhoiNgan = x.KlPhoiNgan,
                    CdPhoiNgan = x.CdPhoiNgan,
                    StLoai2 = x.StLoai2,
                    KlLoai2 = x.KlLoai2,
                    StLoai2TP = x.StLoai2TP,
                    KlLoai2TP = x.KlLoai2TP,
                    StLoai3 = x.StLoai3,
                    KlLoai3 = x.KlLoai3,
                    TongSoThanh = x.TongSoThanh,
                    TongKhoiLuong = x.TongKhoiLuong,
                    TTHD = x.TTHD,
                    ThoiGianTao = x.ThoiGianTao,
                    NgayGiao = x.NgaySX,
                    ID_Chot = x.ID_Chot,
                    ID_NguoiCap0 = x.ID_NguoiCap0,
                    ID_NguoiCap1 = x.ID_NguoiCap1,
                    ID_NguoiCap2 = x.ID_NguoiCap2,
                    TinhTrangCap0 = x.TinhTrangCap0,
                    TinhTrangCap1 = x.TinhTrangCap1,
                    TinhTrangCap2 = x.TinhTrangCap2,
                    TinhTrang = x.TinhTrang
                })
                .ToListAsync();

            if (data.Count > 0)
            {
                var meSet = data.Select(x => NormalizeKeyValue(x.Me)).Distinct().ToList();
                var macSet = data.Select(x => NormalizeKeyValue(x.Mac)).Distinct().ToList();
                var kichThuocSet = data.Select(x => NormalizeKeyValue(x.KichThuoc)).Distinct().ToList();

                var bkRows = await _context.BkPhoiThep
                    .AsNoTracking()
                    .Where(x => meSet.Contains((x.Me ?? string.Empty).Trim().ToUpper())
                        && macSet.Contains((x.Mac ?? string.Empty).Trim().ToUpper())
                        && kichThuocSet.Contains((x.KichThuoc ?? string.Empty).Trim().ToUpper()))
                    .Select(x => new
                    {
                        x.Ca,
                        Kip = (x.Kip ?? string.Empty).Trim().ToUpper(),
                        MayDuc = x.MayDuc ?? 0,
                        Me = (x.Me ?? string.Empty).Trim().ToUpper(),
                        Mac = (x.Mac ?? string.Empty).Trim().ToUpper(),
                        KichThuoc = (x.KichThuoc ?? string.Empty).Trim().ToUpper(),
                        x.NgaySx
                    })
                    .ToListAsync();

                var bkLookup = bkRows
                    .GroupBy(x => (x.Me, x.Mac, x.KichThuoc))
                    .ToDictionary(g => g.Key, g => g.Max(v => v.NgaySx));

                foreach (var item in data)
                {
                    var key = (
                        NormalizeKeyValue(item.Me),
                        NormalizeKeyValue(item.Mac),
                        NormalizeKeyValue(item.KichThuoc)
                    );

                    if (bkLookup.TryGetValue(key, out var ngaySxFromBk))
                    {
                        item.NgaySX = ngaySxFromBk.ToDateTime(TimeOnly.MinValue);
                    }
                }
            }

            return (data, total);
        }
        public async Task<ExportFileResult> ExportPdfSanLuongAsync(DateOnly? NgaySX, int? Ca, string? Kip, Guid? idPhieu, List<PheDuyetDto> pheDuyets)
        {
            if (!NgaySX.HasValue || !Ca.HasValue || string.IsNullOrEmpty(Kip))
                throw new ArgumentException("Thiếu tham số Ngày / Ca / Kíp");

            if (!idPhieu.HasValue)
                throw new ArgumentException("Thiếu IdPhiếu");

            var items = await _repo.GetSanLuongPhoiChiTietAsync(
                ca: Ca.Value,
                kip: Kip,
                ngaySX: NgaySX.Value.ToDateTime(TimeOnly.MinValue),
                idPhieu: idPhieu
            );

            var data = items.ToList();

            if (!data.Any())
                throw new Exception("Không có dữ liệu sản lượng để xuất PDF");

            // Tính toán thời gian ca kíp
            string tuGio = "", denGio = "", tuNgay = "", denNgay = "";

            if (NgaySX.HasValue && Ca.HasValue)
            {
                DateOnly ngayBatDau = NgaySX.Value;
                DateOnly ngayKetThuc = NgaySX.Value;

                switch (Ca.Value)
                {
                    case 1: // Ca 1: 08h - 20h
                        tuGio = "08";
                        denGio = "20";
                        break;
                    case 2: // Ca 2: 20h - 08h
                        tuGio = "20";
                        denGio = "08";
                        ngayKetThuc = NgaySX.Value.AddDays(1);
                        break;
                    default:
                        tuGio = "";
                        denGio = "";
                        break;
                }

                tuNgay = ngayBatDau.ToString("dd/MM/yyyy");
                denNgay = ngayKetThuc.ToString("dd/MM/yyyy");
            }
            var templatePath = Path.Combine(
                _env.WebRootPath,
                "template_html",
                "BM.11-QT.05.11_Bien_ban_xac_nhan_san_luong_phoi.html"
            );

            var html = await File.ReadAllTextAsync(templatePath);



            var rows = new StringBuilder();
            int tongSoThanh = 0;
            decimal tongKhoiLuong = 0;

            int tongStLoai1 = 0;
            decimal tongKlLoai1 = 0;

            int tongStPhoiNgan = 0;
            decimal tongKlPhoiNgan = 0;

            int tongStLoai2 = 0;
            decimal tongKlLoai2 = 0;

            int tongStLoai3 = 0;
            decimal tongKlLoai3 = 0;

            foreach (var t in data)
            {
                tongSoThanh += t.TongSoThanh ?? 0;
                tongKhoiLuong += t.TongKhoiLuong ?? 0;

                tongStLoai1 += t.StLoai1 ?? 0;
                tongKlLoai1 += t.KlLoai1 ?? 0;

                tongStLoai2 += t.StLoai2 ?? 0;
                tongKlLoai2 += t.KlLoai2 ?? 0;

                tongStLoai3 += t.StLoai3 ?? 0;
                tongKlLoai3 += t.KlLoai3 ?? 0;

                tongStPhoiNgan += t.StPhoiNgan ?? 0;
                tongKlPhoiNgan += t.KlPhoiNgan ?? 0;

                rows.Append($@"
                <tr>
                    <td>{t.KipNgay}</td>
                    <td>{t.MacThep}</td>
                    <td>{t.KichThuoc}</td>

                    <td>{t.StLoai1}</td>
                    <td>{t.KlLoai1:N0}</td>

                    <td>{t.StPhoiNgan}</td>
                    <td>{t.KlPhoiNgan:N0}</td>

                    <td>{t.StLoai2}</td>
                    <td>{t.KlLoai2:N0}</td>

                    <td>{t.StLoai3}</td>
                    <td>{t.KlLoai3:N0}</td>

                    <td>{t.TongSoThanh}</td>
                    <td>{t.TongKhoiLuong:N0}</td>
                </tr>");
            }

            var xuongDuc = pheDuyets.FirstOrDefault(x => x.CapDuyet == 0 && x.TinhTrang == 1);
            var qlcl = pheDuyets.FirstOrDefault(x => x.CapDuyet == 1 && x.TinhTrang == 1);
            var khoPhoi = pheDuyets.FirstOrDefault(x => x.CapDuyet == 2 && x.TinhTrang == 1);

            // Convert logo và chữ ký sang base64
            var logoUrl = _configuration.GetValue<string>("AppSettings:LogoUrl") ?? "https://report.hoaphatdungquat.vn/img/logoHP.png";
            var logoBase64 = await ConvertImageUrlToBase64Async(logoUrl);


            var nguoiGiao = xuongDuc?.HoVaTen;
            var chucVuGiao = xuongDuc?.TenViTri;
            var bPhanGiao = xuongDuc?.TenPhongBan;


            var nguoiqlclo = qlcl?.HoVaTen;
            var chucVuqlcl = qlcl?.TenViTri;
            var bPhanqlcl = qlcl?.TenPhongBan;

            var nguoiNhan = khoPhoi?.HoVaTen;
            var chucVuNhan = khoPhoi?.TenViTri;
            var bPhanNhan = khoPhoi?.TenPhongBan;


            var signXuongDuc = await FormatChuKyBase64Async(
                  xuongDuc?.ChuKy,
                  xuongDuc?.TinhTrang == 1
              );

            var signQLCL = await FormatChuKyBase64Async(
                qlcl?.ChuKy,
                qlcl?.TinhTrang == 1
            );

            var signKhoPhoi = await FormatChuKyBase64Async(
                khoPhoi?.ChuKy,
                khoPhoi?.TinhTrang == 1
            );

            html = html
                // Header
                .Replace("{{LogoUrl}}", logoBase64)
                .Replace("{{NgaySX}}", NgaySX.Value.ToString("dd/MM/yyyy"))
                .Replace("{{Ca}}", Ca.Value.ToString())
                .Replace("{{Kip}}", Kip)

                // Content
                //.Replace("{{NguoiThamGia}}", nguoiThamGia.ToString())

                .Replace("{{TongStLoai1}}", tongStLoai1.ToString("N0"))
                .Replace("{{TongKlLoai1}}", tongKlLoai1.ToString("N0"))

                .Replace("{{TongStPhoiNgan}}", tongStPhoiNgan.ToString("N0"))
                .Replace("{{TongKlPhoiNgan}}", tongKlPhoiNgan.ToString("N0"))

                .Replace("{{TongStLoai2}}", tongStLoai2.ToString("N0"))
                .Replace("{{TongKlLoai2}}", tongKlLoai2.ToString("N0"))

                .Replace("{{TongStLoai3}}", tongStLoai3.ToString("N0"))
                .Replace("{{TongKlLoai3}}", tongKlLoai3.ToString("N0"))

                .Replace("{{TongSoThanh}}", tongSoThanh.ToString("N0"))
                .Replace("{{TongKhoiLuong}}", tongKhoiLuong.ToString("N0"))

                // ===== XƯỞNG ĐÚC =====
                .Replace("{{NguoiGiao}}", nguoiGiao)
                .Replace("{{ChucVuGiao}}", chucVuGiao)
                .Replace("{{BoPhanGiao}}", bPhanGiao)

                // ===== QLCL =====
                .Replace("{{NguoiQLCL}}", nguoiqlclo)
                .Replace("{{ChucVuQLCL}}", chucVuqlcl)
                .Replace("{{BoPhanQLCL}}", bPhanqlcl)

                // ===== KHO PHÔI =====
                .Replace("{{NguoiNhan}}", nguoiNhan)
                .Replace("{{ChucVuNhan}}", chucVuNhan)
                .Replace("{{BoPhanNhan}}", bPhanNhan)

                // Table
                .Replace("{{Rows}}", rows.ToString())
                .Replace("{{TongSoThanh}}", tongSoThanh.ToString("N0"))
                .Replace("{{TongKhoiLuong}}", tongKhoiLuong.ToString("N0"))

                // ===== XƯỞNG ĐÚC =====
                .Replace("{{Sign_XuongDuc}}", signXuongDuc)
                .Replace("{{Name_XuongDuc}}", xuongDuc?.HoVaTen ?? "")

                // ===== QLCL =====
                .Replace("{{Sign_QLCL}}", signQLCL)
                .Replace("{{Name_QLCL}}", qlcl?.HoVaTen ?? "")

                // ===== KHO PHÔI =====
                .Replace("{{Sign_KhoPhoi}}", signKhoPhoi)
                .Replace("{{Name_KhoPhoi}}", khoPhoi?.HoVaTen ?? "");


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
                FileName = $"BM.06_Bien_ban_san_luong_phoi_{DateTime.Now:yyyyMMdd_HHmm}.pdf",
                ContentType = "application/pdf"
            };
        }

        public async Task DeleteSanLuongPhoiByPhieuAsync(Guid idPhieu)
        {
            if (idPhieu == Guid.Empty)
                throw new ArgumentException("IdPhieu không hợp lệ");

            await _repo.DeleteSanLuongPhoiByPhieuAsync(idPhieu);
        }
        //=== PHÔI NHẬP KHO
        public async Task<int> InsertPhoiNhapKhoFromPhieuJsonAsync(BmPhieu phieu)
        {
            if (phieu == null)
                throw new ArgumentNullException(nameof(phieu));

            var entities = new List<BM_PhoiNhapKho>();

            if (!string.IsNullOrWhiteSpace(phieu.DataJson))
            {
                using var jsonDoc = JsonDocument.Parse(phieu.DataJson);
                var root = jsonDoc.RootElement;

                if (TryGetRowsElement(root, out var rowsElement))
                {
                    foreach (var row in rowsElement.EnumerateArray())
                    {
                        if (row.ValueKind != JsonValueKind.Object)
                            continue;

                        entities.Add(new BM_PhoiNhapKho
                        {
                            IdPhieu = phieu.Idphieu,
                            SoPhieu = phieu.SoPhieu ?? string.Empty,
                            NgaySX = (phieu.NgaySX ?? DateOnly.FromDateTime(DateTime.Today)).ToDateTime(TimeOnly.MinValue),
                            Kip = phieu.Kip ?? string.Empty,
                            Ca = phieu.Ca ?? 0,
                            MayDuc = phieu.MayDuc ?? 0,
                            Me = TryGetString(row, "me", "Me") ?? string.Empty,
                            Mac = TryGetString(row, "mac", "Mac") ?? string.Empty,
                            KichThuoc = TryGetString(row, "kichThuoc", "KichThuoc") ?? string.Empty,
                            StLoai1 = TryGetInt(row, "stLoai1", "StLoai1"),
                            KlLoai1 = TryGetDecimal(row, "klLoai1", "KlLoai1"),
                            StPhoiNgan = TryGetInt(row, "stPhoiNgan", "StPhoiNgan"),
                            KlPhoiNgan = TryGetDecimal(row, "klPhoiNgan", "KlPhoiNgan"),
                            CdPhoiNgan = TryGetDecimal(row, "cdPhoiNgan", "CdPhoiNgan"),
                            StLoai2 = TryGetInt(row, "stLoai2", "StLoai2"),
                            KlLoai2 = TryGetDecimal(row, "klLoai2", "KlLoai2"),
                            StLoai2TP = TryGetInt(row, "stLoai2tp", "StLoai2TP", "StLoai2tp"),
                            KlLoai2TP = TryGetDecimal(row, "klLoai2tp", "KlLoai2TP", "KlLoai2tp"),
                            StLoai3 = TryGetInt(row, "stLoai3", "StLoai3"),
                            KlLoai3 = TryGetDecimal(row, "klLoai3", "KlLoai3"),
                            TongSoThanh = TryGetInt(row, "tongSoThanh", "TongSoThanh"),
                            TongKhoiLuong = TryGetDecimal(row, "tongKhoiLuong", "TongKhoiLuong"),
                            NguoiTaoId = phieu.NguoiTaoId,
                            ThoiGianTao = DateTime.Now,
                            TTHD = true
                        });
                    }
                }
            }

            await _repo.DeletePhoiNhapKhoByPhieuAsync(phieu.Idphieu);

            // Khi phiếu là bản hiệu chỉnh (clone), xóa luôn dữ liệu chi tiết của phiếu gốc
            // để tránh còn song song dữ liệu "phiếu cũ".
            if (phieu.ID_PhieuGoc.HasValue
                && phieu.ID_PhieuGoc.Value != Guid.Empty
                && phieu.ID_PhieuGoc.Value != phieu.Idphieu)
            {
                await _repo.DeletePhoiNhapKhoByPhieuAsync(phieu.ID_PhieuGoc.Value);
            }

            if (entities.Count == 0)
                return 0;

            await _repo.AddPhoiNhapKhoListAsync(entities);
            return entities.Count;
        }

        public async Task<int> UpsertPhoiNhapKhoFromAsync(InsertPhoiNhapKhoRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.NgaySX == default || request.Ca <= 0)
                throw new ArgumentException("Thiếu thông tin NgàySX/Ca");
            var ngay = DateOnly.FromDateTime(request.NgaySX);

            // ★ Tìm phiếu theo Ngày sx, ca sx, máy đúc nếu phiếu đã chốt k cho chuyển thanh vào nữa
            var phieu = await _context.BmPhieus.FirstOrDefaultAsync(x =>
                x.NgaySX == ngay &&
                x.Ca == request.Ca &&
                x.MayDuc == request.MayDuc);

            // ★ Kiểm tra nếu phiếu đã chốt (TinhTrang = 5) thì không cho phép chuyển thanh
            if (phieu != null && phieu.TinhTrang == 5)
                throw new Exception($"Phiếu đã chốt! Không thể chuyển thanh vào phiếu ngày {request.NgaySX:dd/MM/yyyy}, ca {request.Ca}, máy đúc {request.MayDuc}.");

            var incomingRows = (request.Table1 ?? new List<InsertPhoiNhapKhoDto>())
                .Where(x => !string.IsNullOrWhiteSpace(x.Me) && !string.IsNullOrWhiteSpace(x.Mac))
                .GroupBy(x => new
                {
                    NgaySX = request.NgaySX.Date,
                    Ca = request.Ca,
                    Me = NormalizeKeyValue(x.Me),
                    Mac = NormalizeKeyValue(x.Mac)
                })
                .Select(g => g.Last())
                .ToList();

            if (incomingRows.Count == 0)
                return 0;

            await using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                var existingRows = await _context.BM_PhoiNhapKho
                    .Where(x => x.NgaySX.Date == request.NgaySX.Date && x.Ca == request.Ca)
                    .ToListAsync();

                var inserted = 0;
                var updated = 0;

                foreach (var row in incomingRows)
                {
                    var match = existingRows.FirstOrDefault(x =>
                        x.NgaySX.Date == request.NgaySX.Date &&
                        x.Ca == request.Ca &&
                        NormalizeKeyValue(x.Me) == NormalizeKeyValue(row.Me) &&
                        NormalizeKeyValue(x.Mac) == NormalizeKeyValue(row.Mac) &&
                        NormalizeKeyValue(x.KichThuoc) == NormalizeKeyValue(row.KichThuoc));

                    if (match == null)
                    {
                        var entity = new BM_PhoiNhapKho
                        {
                            IdPhieu = request.IdPhieu,
                            SoPhieu = request.SoPhieu ?? string.Empty,
                            NgaySX = request.NgaySX.Date,
                            Kip = request.Kip ?? string.Empty,
                            Ca = request.Ca,
                            MayDuc = request.MayDuc,
                            Me = row.Me ?? string.Empty,
                            Mac = row.Mac ?? string.Empty,
                            KichThuoc = row.KichThuoc ?? string.Empty,
                            StLoai1 = row.StLoai1,
                            KlLoai1 = row.KlLoai1,
                            StPhoiNgan = row.StPhoiNgan,
                            KlPhoiNgan = row.KlPhoiNgan,
                            CdPhoiNgan = row.CdPhoiNgan,
                            StLoai2 = row.StLoai2,
                            KlLoai2 = row.KlLoai2,
                            StLoai2TP = row.StLoai2TP,
                            KlLoai2TP = row.KlLoai2TP,
                            StLoai3 = row.StLoai3,
                            KlLoai3 = row.KlLoai3,
                            TongSoThanh = row.TongSoThanh,
                            TongKhoiLuong = row.TongKhoiLuong,
                            NguoiTaoId = request.NguoiTaoId,
                            ThoiGianTao = DateTime.Now,
                            TTHD = true,
                            TinhTrangCap0 = 1, // da giao
                            ID_NguoiCap0 = request.NguoiTaoId,  // ID nguoi giao
                        };

                        await _context.BM_PhoiNhapKho.AddAsync(entity);
                        existingRows.Add(entity);
                        inserted++;
                        continue;
                    }

                    match.IdPhieu = request.IdPhieu;
                    match.SoPhieu = request.SoPhieu ?? string.Empty;
                    match.Kip = request.Kip ?? string.Empty;
                    match.MayDuc = request.MayDuc;
                    match.KichThuoc = row.KichThuoc ?? string.Empty;
                    match.StLoai1 += row.StLoai1;
                    match.KlLoai1 += row.KlLoai1;
                    match.StPhoiNgan += row.StPhoiNgan;
                    match.KlPhoiNgan += row.KlPhoiNgan;
                    match.CdPhoiNgan += row.CdPhoiNgan;
                    match.StLoai2 += row.StLoai2;
                    match.KlLoai2 += row.KlLoai2;
                    match.StLoai2TP += row.StLoai2TP;
                    match.KlLoai2TP += row.KlLoai2TP;
                    match.StLoai3 += row.StLoai3;
                    match.KlLoai3 += row.KlLoai3;
                    match.TongSoThanh += row.TongSoThanh;
                    match.TongKhoiLuong += row.TongKhoiLuong;
                    match.NguoiTaoId = request.NguoiTaoId;
                    match.ThoiGianTao = DateTime.Now;
                    match.TTHD = true;
                    match.TinhTrangCap0 = 1; // da giao
                    match.ID_NguoiCap0 = request.NguoiTaoId;  // ID nguoi giao

                    updated++;
                }

                await _context.SaveChangesAsync();

                //var ngaySx = DateOnly.FromDateTime(request.NgaySX.Date);
                //var bkRows = await _context.BkPhoiThep
                //    .Where(x => x.Me ==  && x.Ca == request.Ca)
                //    .ToListAsync();

                foreach (var key in incomingRows)
                {
                    //var tongDaChuyen = existingRows
                    //    .Where(x =>
                    //        x.NgaySX.Date == request.NgaySX.Date &&
                    //        x.Ca == request.Ca &&
                    //        NormalizeKeyValue(x.Me) == key.Me &&
                    //        NormalizeKeyValue(x.Mac) == key.Mac)
                    //    .Sum(x => x.TongSoThanh ?? 0);

                    //foreach (var bk in bkRows.Where(x =>
                    //             NormalizeKeyValue(x.Me) == key.Me &&
                    //             NormalizeKeyValue(x.Mac) == key.Mac))
                    //{
                    //    bk.StDaChuyen = tongDaChuyen;
                    //}

                    var json = JsonSerializer.Serialize(key);

                    await _context.Database.ExecuteSqlRawAsync(
                        "EXEC sp_CTD_Update_ST_DaChuyen_FromJson @json",
                        new SqlParameter("@json", json)
                    );
                }

                //await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return inserted + updated;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
        public async Task DeletePhoiNhapKhoByPhieuAsync(Guid idPhieu)
        {
            if (idPhieu == Guid.Empty)
                throw new ArgumentException("IdPhieu không hợp lệ");

            await _repo.DeletePhoiNhapKhoByPhieuAsync(idPhieu);
        }

        public async Task<int> XacNhanPhoiNhapKhoRowsAsync(List<int> ids, int nguoiXacNhanId, int? CapXacNhan, int? TinhTrangCap, Guid idPhieu)
        {
            // logic: cấp xác nhận nào thì cập nhật trường tương ứng (TinhTrangCap0, TinhTrangCap1, TinhTrangCap2) và ID_Cap0, ID_Cap1, ID_Cap2
            if (ids == null || ids.Count == 0)
                throw new ArgumentException("Danh sách dòng xác nhận không hợp lệ");

            if (nguoiXacNhanId <= 0)
                throw new ArgumentException("ID người xác nhận không hợp lệ");

            if (!CapXacNhan.HasValue || CapXacNhan < 0 || CapXacNhan > 2)
                throw new ArgumentException("Cấp xác nhận không hợp lệ");

            try
            {
                // update BM_PhoiNhapKho set TinhTrangCap{CapXacNhan} = 1, ID_Cap{Cap
                //XacNhan} = @nguoiXacNhanId where Id in (@ids)
                // chạy qua BM_PhoiNhapKho update qua từng dòng code chứ k qua store để đảm bảo trigger hoạt động
                var rowsToUpdate = await _context.BM_PhoiNhapKho.Where(x => ids.Contains(x.Id)).ToListAsync();
                foreach (var row in rowsToUpdate)
                {
                    switch (CapXacNhan.Value)
                    {
                        case 0:
                            row.TinhTrangCap0 = TinhTrangCap;
                            row.ID_NguoiCap0 = nguoiXacNhanId;
                            break;
                        case 1:
                            row.TinhTrangCap1 = TinhTrangCap;
                            row.ID_NguoiCap1 = nguoiXacNhanId;
                            break;
                        case 2:
                            row.TinhTrangCap2 = TinhTrangCap;
                            row.ID_NguoiCap2 = nguoiXacNhanId;
                            break;
                    }
                }
                await _context.SaveChangesAsync();
                var checkpheduyet = await _pheDuyetService.GetPheDuyetPhieuAsync(idPhieu);
                if (checkpheduyet.Count == 0)
                {
                    // Khoi tao cap duyet = 0
                    var IDNguoiTao = rowsToUpdate.Where(x => x.NguoiTaoId != null).Select(x => x.NguoiTaoId.Value).Distinct().FirstOrDefault();
                    if (IDNguoiTao != 0)
                    {
                        // call đến khơi tạo PheDuyet
                        await _pheDuyetService.InitializePheDuyetAsync(idPhieu, 0, IDNguoiTao);
                    }
                }
                // call đến khơi tạo PheDuyet
                await _pheDuyetService.InitializePheDuyetAsync(idPhieu, CapXacNhan.Value, nguoiXacNhanId);

                // Update BM_Phieu TinhTrang -> Dang phe duyet
                var phieu = _context.BmPhieus.FirstOrDefault(x => x.Idphieu == idPhieu);
                if (phieu.TinhTrang == 0)
                {
                    phieu.TinhTrang = 2; // Hoàn thành
                    await _context.SaveChangesAsync();
                }


                return rowsToUpdate.Count;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }
        public async Task<int> ChotPhoiNhapKhoRowsAsync(Guid idPhieu, int? tinhTrangChot)
        {
            try
            {
                // Bước 1: Tìm BM_Phieu theo idPhieu
                var phieu = await _context.BmPhieus.FirstOrDefaultAsync(x => x.Idphieu == idPhieu);
                if (phieu == null)
                    throw new Exception($"Không tìm thấy phiếu với ID: {idPhieu}");

                // Bước 2: Lấy thông tin ngày sản xuất, ca từ phiếu
                if (!phieu.NgaySX.HasValue || phieu.Ca <= 0)
                    throw new ArgumentException("Phiếu thiếu thông tin Ngày SX hoặc Ca");

                // Bước 3: Tìm tất cả rows trong BM_PhoiNhapKho theo IdPhieu và ngày/ca
                var rowsToChot = await _context.BM_PhoiNhapKho
                    .Where(x =>
                                x.NgaySX.Date == phieu.NgaySX.Value.ToDateTime(TimeOnly.MinValue).Date &&
                                x.Ca == phieu.Ca.Value && x.MayDuc == phieu.MayDuc)
                    .ToListAsync();

                if (rowsToChot.Count == 0)
                    throw new Exception("Không tìm thấy dữ liệu phôi nhập kho để chốt");

                // Bước 4: Kiểm tra xem tất cả cấp đã duyệt chưa
                var allCapsApproved = rowsToChot.All(x =>
                    x.TinhTrangCap0 == 1 &&
                    x.TinhTrangCap1 == 1 &&
                    x.TinhTrangCap2 == 1
                );

                if (!allCapsApproved && tinhTrangChot == 1) // Nếu đang chốt mà chưa duyệt hết thì không cho chốt
                    throw new Exception("Không thể chốt! Vui lòng đảm bảo tất cả cấp đều đã duyệt");

                // Bước 5: Khi chốt, cập nhật tình trạng rows = 1
                foreach (var row in rowsToChot)
                {
                    row.TinhTrang = tinhTrangChot; // Đã chốt
                }
                if (tinhTrangChot == 1)
                {
                    // Bước 6: Cập nhật TinhTrang của BM_Phieu = 5 (Chốt)
                    phieu.TinhTrang = 5;
                }
                else // thu hồi
                {
                    // Bước 6: Cập nhật TinhTrang của BM_Phieu = 1( hủy Chốt)
                    phieu.TinhTrang = 1;
                }


                await _context.SaveChangesAsync();

                return rowsToChot.Count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ChotPhoiNhapKhoRowsAsync Error: {ex.Message}");
                throw;
            }
        }

        public async Task<int> ThuHoiPhoiNhapKhoRowsAsync(List<int> ids)
        {
            if (ids == null || ids.Count == 0)
                throw new ArgumentException("Danh sách dòng thu hồi không hợp lệ");

            var validIds = ids.Where(x => x > 0).Distinct().ToList();
            if (validIds.Count == 0)
                throw new ArgumentException("Danh sách dòng thu hồi không hợp lệ");

            try
            {
                var json = JsonSerializer.Serialize(validIds.Select(id => new { id }));

                var affected = await _context.Database.ExecuteSqlRawAsync(
                    "EXEC sp_CTD_ThuHoi_PhoiNhapKho_ByIds @json",
                    new SqlParameter("@json", json)
                );

                return affected;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }
        public async Task<ExportFileResult> ExportPdfPhoiNhapKhoAsync(PhoiNhapKhoPdfDTOReq request)
        {
            if (request.NgaySX == default || request.Ca == 0 || string.IsNullOrEmpty(request.Kip))
                throw new ArgumentException("Thiếu tham số Ngày / Ca / Kíp");

            if (request.IdPhieu == Guid.Empty)
                throw new ArgumentException("Thiếu IdPhiếu");

            var phieu = await _repoPhieu.GetByIdAsync(request.IdPhieu);
            if (phieu == null)
                throw new Exception("Không tìm thấy phiếu");

            var items = await _repo.GetPhoiNhapKhoChiTietAsync(
                ca: request.Ca,
                kip: request.Kip,
                ngaySX: request.NgaySX,
                idPhieu: request.IdPhieu
            );
            var data = items.ToList();

            if (!data.Any())
                throw new Exception("Không có dữ liệu sản lượng để xuất PDF");

            int mayDuc = (int)phieu.MayDuc;
            // Tính toán thời gian ca kíp
            string tuGio = "", denGio = "", tuNgay = "", denNgay = "";

            if (request.NgaySX != default && request.Ca > 0)
            {
                var ngayBatDau = DateOnly.FromDateTime(request.NgaySX);
                var ngayKetThuc = DateOnly.FromDateTime(request.NgaySX);

                switch (request.Ca)
                {
                    case 1: // Ca 1: 08h - 20h
                        tuGio = "08h00";
                        denGio = "20h00";
                        break;
                    case 2: // Ca 2: 20h - 08h
                        tuGio = "20h00";
                        denGio = "08h00";
                        ngayKetThuc = DateOnly.FromDateTime(request.NgaySX).AddDays(1);
                        break;
                    default:
                        tuGio = "";
                        denGio = "";
                        break;
                }

                tuNgay = ngayBatDau.ToString("dd/MM/yyyy");
                denNgay = ngayKetThuc.ToString("dd/MM/yyyy");
            }
            var templatePath = Path.Combine(
                _env.WebRootPath,
                "template_html",
                "BM12-QT.05.11_Bien_ban_giao_nhan_phoi_nhap_kho.html"
            );

            var html = await File.ReadAllTextAsync(templatePath);
            var nguoiThamGia = new StringBuilder();
            int indexNguoi = 1;

            var pheDuyets = await _pheDuyetService.GetPheDuyetPhieuAsync(request.IdPhieu) ?? new List<PheDuyetDto>();



            var rows = new StringBuilder();

            int tongSoThanh = 0;
            int stt = 0;
            decimal tongKhoiLuong = 0;
            int tongStLoai1 = 0;
            decimal tongKlLoai1 = 0;

            int tongStLoai2 = 0;
            decimal tongKlLoai2 = 0;

            int tongStLoai2TP = 0;
            decimal tongKlLoai2TP = 0;

            int tongStPhoiNgan = 0;
            decimal tongKlPhoiNgan = 0;
            decimal tongCdPhoiNgan = 0;

            int tongStLoai3 = 0;
            decimal tongKlLoai3 = 0;

            foreach (var t in data)
            {
                tongSoThanh += t.TongSoThanh ?? 0;
                tongKhoiLuong += t.TongKhoiLuong ?? 0;

                tongStLoai1 += t.StLoai1 ?? 0;
                tongKlLoai1 += t.KlLoai1 ?? 0;

                tongStLoai2 += t.StLoai2 ?? 0;
                tongKlLoai2 += t.KlLoai2 ?? 0;

                tongStLoai2TP += t.StLoai2TP ?? 0;
                tongKlLoai2TP += t.KlLoai2TP ?? 0;

                tongStPhoiNgan += t.StPhoiNgan ?? 0;
                tongKlPhoiNgan += t.KlPhoiNgan ?? 0;
                tongCdPhoiNgan += t.CdPhoiNgan ?? 0;

                tongStLoai3 += t.StLoai3 ?? 0;
                tongKlLoai3 += t.KlLoai3 ?? 0;
                stt += 1;
                rows.Append($@"
                <tr>
                    <td>{stt}</td>
                    <td>{t.Me}</td>
                    <td>{t.Mac}</td>
                    <td>{t.KichThuoc}</td>

                    <td>{t.StLoai1}</td>
                    <td>{t.KlLoai1:N0}</td>

                    <td>{t.StLoai2}</td>
                    <td>{t.KlLoai2:N0}</td>

                    <td>{t.StLoai2TP}</td>
                    <td>{t.KlLoai2TP:N0}</td>

                    <td>{t.StPhoiNgan}</td>
                    <td>{t.CdPhoiNgan:N2}</td>
                    <td>{t.KlPhoiNgan:N0}</td>

                    <td>{t.StLoai3}</td>
                    <td>{t.KlLoai3:N0}</td>

                    <td>{t.TongKhoiLuong:N0}</td>
                </tr>");
            }

            // Kiểm tra xem ds data nếu đã duyệt hết thì update tinh trang duyet, nếu chưa thì để nguyên
            if (data.All(x => x.TinhTrangCap0 == 1))
            {
                // update tinh trang duyet cap 0
                pheDuyets.Where(x => x.CapDuyet == 0).ToList().ForEach(x => x.TinhTrang = 1);
            }
            if (data.All(x => x.TinhTrangCap1 == 1))
            {
                // update tinh trang duyet cap 1
                pheDuyets.Where(x => x.CapDuyet == 1).ToList().ForEach(x => x.TinhTrang = 1);
            }
            if (data.All(x => x.TinhTrangCap2 == 1))
            {
                // update tinh trang duyet cap 2
                pheDuyets.Where(x => x.CapDuyet == 2).ToList().ForEach(x => x.TinhTrang = 1);
            }
            await _context.SaveChangesAsync();


            var xuongDuc = pheDuyets.FirstOrDefault(x => x.CapDuyet == 0 && x.TinhTrang == 1);
            var qlcl = pheDuyets.FirstOrDefault(x => x.CapDuyet == 1 && x.TinhTrang == 1);
            var khoPhoi = pheDuyets.FirstOrDefault(x => x.CapDuyet == 2 && x.TinhTrang == 1);


            // Convert logo và chữ ký sang base64
            var logoUrl = _configuration.GetValue<string>("AppSettings:LogoUrl") ?? "https://report.hoaphatdungquat.vn/img/logoHP.png";
            var logoBase64 = await ConvertImageUrlToBase64Async(logoUrl);

            var nguoiGiao = xuongDuc?.HoVaTen;
            var chucVuGiao = xuongDuc?.TenViTri;
            var bPhanGiao = xuongDuc?.TenPhongBan;


            var nguoiqlclo = qlcl?.HoVaTen;
            var chucVuqlcl = qlcl?.TenViTri;
            var bPhanqlcl = qlcl?.TenPhongBan;

            var nguoiNhan = khoPhoi?.HoVaTen;
            var chucVuNhan = khoPhoi?.TenViTri;
            var bPhanNhan = khoPhoi?.TenPhongBan;

            var signXuongDuc = await FormatChuKyBase64Async(xuongDuc?.ChuKy, xuongDuc?.TinhTrang == 1);
            var signQLCL = await FormatChuKyBase64Async(qlcl?.ChuKy, qlcl?.TinhTrang == 1);
            var signKhoPhoi = await FormatChuKyBase64Async(khoPhoi?.ChuKy, khoPhoi?.TinhTrang == 1);

            html = html
                // Header
                .Replace("{{LogoUrl}}", logoBase64)
                .Replace("{{NgaySX}}", request.NgaySX.ToString("dd/MM/yyyy"))
                .Replace("{{Ca}}", request.Ca.ToString())
                .Replace("{{Kip}}", request.Kip)
                .Replace("{{MayDuc}}", mayDuc.ToString())

                // ===== Thời gian ca =====
                .Replace("{{TuGio}}", tuGio)
                .Replace("{{DenGio}}", denGio)
                .Replace("{{TuNgay}}", tuNgay)
                .Replace("{{DenNgay}}", denNgay)

                .Replace("{{TongStLoai1}}", tongStLoai1.ToString("N0"))
                .Replace("{{TongKlLoai1}}", tongKlLoai1.ToString("N0"))

                .Replace("{{TongStLoai2}}", tongStLoai2.ToString("N0"))
                .Replace("{{TongKlLoai2}}", tongKlLoai2.ToString("N0"))

                .Replace("{{TongStLoai2TP}}", tongStLoai2TP.ToString("N0"))
                .Replace("{{TongKlLoai2TP}}", tongKlLoai2TP.ToString("N0"))

                .Replace("{{TongStPhoiNgan}}", tongStPhoiNgan.ToString("N0"))
                .Replace("{{TongCdPhoiNgan}}", tongCdPhoiNgan.ToString("N2"))
                .Replace("{{TongKlPhoiNgan}}", tongKlPhoiNgan.ToString("N0"))

                .Replace("{{TongStLoai3}}", tongStLoai3.ToString("N0"))
                .Replace("{{TongKlLoai3}}", tongKlLoai3.ToString("N0"))
                // Content
                // ===== XƯỞNG ĐÚC =====
                .Replace("{{NguoiGiao}}", nguoiGiao)
                .Replace("{{ChucVuGiao}}", chucVuGiao)
                .Replace("{{BoPhanGiao}}", bPhanGiao)

                // ===== QLCL =====
                .Replace("{{NguoiQLCL}}", nguoiqlclo)
                .Replace("{{ChucVuQLCL}}", chucVuqlcl)
                .Replace("{{BoPhanQLCL}}", bPhanqlcl)

                // ===== KHO PHÔI =====
                .Replace("{{NguoiNhan}}", nguoiNhan)
                .Replace("{{ChucVuNhan}}", chucVuNhan)
                .Replace("{{BoPhanNhan}}", bPhanNhan)

                // Table
                .Replace("{{Rows}}", rows.ToString())
                .Replace("{{TongSoThanh}}", tongSoThanh.ToString("N0"))
                .Replace("{{TongKhoiLuong}}", tongKhoiLuong.ToString("N0"))

                // ===== XƯỞNG ĐÚC =====
                .Replace("{{Sign_XuongDuc}}", signXuongDuc)
                .Replace("{{Name_XuongDuc}}", xuongDuc?.HoVaTen ?? "")

                // ===== QLCL =====
                .Replace("{{Sign_QLCL}}", signQLCL)
                .Replace("{{Name_QLCL}}", qlcl?.HoVaTen ?? "")

                // ===== KHO PHÔI =====
                .Replace("{{Sign_KhoPhoi}}", signKhoPhoi)
                .Replace("{{Name_KhoPhoi}}", khoPhoi?.HoVaTen ?? "");


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
                FileName = $"BM12-QT.05.11_Bien_ban_giao_nhan_phoi_nhap_kho_{DateTime.Now:yyyyMMdd_HHmm}.pdf",
                ContentType = "application/pdf"
            };
        }

        public async Task<ExportFileResult> ExportPdfPhoiNhapKhoByPhieuAsync(Guid phieuId)
        {
            if (phieuId == Guid.Empty)
                throw new ArgumentException("Thiếu IdPhiếu");

            var phieu = await _repoPhieu.GetByIdAsync(phieuId);
            if (phieu == null)
                throw new Exception("Không tìm thấy phiếu");
            phieu.Kip = "A"; // Mặc định kíp A nếu chưa có, vì kíp là bắt buộc để xuất
            if (!phieu.NgaySX.HasValue || !phieu.Ca.HasValue || string.IsNullOrWhiteSpace(phieu.Kip))
                throw new ArgumentException("Phiếu thiếu thông tin Ngày / Ca / Kíp");

            var pheDuyets = await _pheDuyetService.GetPheDuyetPhieuAsync(phieuId);

            var request = new PhoiNhapKhoPdfDTOReq
            {
                IdPhieu = phieuId,
                NgaySX = phieu.NgaySX.Value.ToDateTime(TimeOnly.MinValue),
                Ca = phieu.Ca.Value,
                Kip = phieu.Kip,
                MayDuc = phieu.MayDuc ?? 0,
                listNguoiPheDuyet = pheDuyets
            };

            return await ExportPdfPhoiNhapKhoAsync(request);
        }
        public async Task<List<BmPhieuExportRow>> GetDataExportExcelByBmPhieuAsync(DateOnly? fromDate, DateOnly? toDate)
        {
            var query = _context.BM_PhoiNhapKho.AsNoTracking().AsQueryable();


            if (fromDate.HasValue)
                query = query.Where(x => x.NgaySX.Date >= fromDate.Value.ToDateTime(TimeOnly.MinValue).Date);

            if (toDate.HasValue)
                query = query.Where(x => x.NgaySX.Date <= toDate.Value.ToDateTime(TimeOnly.MinValue).Date);

            var rows = await (
                from x in query
                join p in _context.BmPhieus on x.IdPhieu equals p.Idphieu
                orderby x.NgaySX descending, x.ThoiGianTao descending
                select new BmPhieuExportRow
                {
                    SoPhieu = x.SoPhieu,
                    NgaySX = DateOnly.FromDateTime(x.NgaySX),
                    MayDuc = x.MayDuc,
                    Kip = x.Kip,
                    Ca = x.Ca,
                    Me = x.Me,
                    Mac = x.Mac,
                    KichThuoc = x.KichThuoc,
                    StLoai1 = x.StLoai1,
                    KlLoai1 = x.KlLoai1,
                    StLoai2 = x.StLoai2,
                    KlLoai2 = x.KlLoai2,
                    StLoai2tp = x.StLoai2TP,
                    KlLoai2tp = x.KlLoai2TP,
                    StPhoiNgan = x.StPhoiNgan,
                    CdPhoiNgan = x.CdPhoiNgan,
                    KlPhoiNgan = x.KlPhoiNgan,
                    StLoai3 = x.StLoai3,
                    KlLoai3 = x.KlLoai3,
                    TongSoThanh = x.TongSoThanh,
                    TongKhoiLuong = x.TongKhoiLuong,
                    // 👉 lấy từ bảng Phiếu
                    TinhTrang = p.TinhTrang
                }
                ).ToListAsync();
            return rows;
        }
        public async Task<List<BmPhieuExportRow>> GetDataExportExcelByBmPhieuPKHAsync(DateOnly? fromDate, DateOnly? toDate)
        {
            // ★ Nguồn BKMis (dữ liệu gốc sản xuất)
            var dataBKMis = await _repo.GetPhoiNhapKhoExportRangeAsync(fromDate, toDate);

            // ★ Dữ liệu nhập kho từ query (để so sánh với BKMis)
            var query = _context.BM_PhoiNhapKho.AsNoTracking().AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(x => x.NgaySX.Date >= fromDate.Value.ToDateTime(TimeOnly.MinValue).Date);

            if (toDate.HasValue)
                query = query.Where(x => x.NgaySX.Date <= toDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue).Date);

            var queryData = await query.ToListAsync();

            // ★ Lần 1 Lookup: Nhập kho cùng ngày ca - so sánh dựa trên (NgaySX, Ca, Me, Mac, KichThuoc)
            var lan1Lookup = queryData
                .GroupBy(x => new
                {
                    NgaySX = DateOnly.FromDateTime(x.NgaySX),
                    Ca = x.Ca,
                    Me = NormalizeKeyValue(x.Me),
                    Mac = NormalizeKeyValue(x.Mac),
                    KichThuoc = NormalizeKeyValue(x.KichThuoc),
                })
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        StLoai1 = g.Sum(x => x.StLoai1),
                        KlLoai1 = g.Sum(x => Convert.ToDecimal(x.KlLoai1)),
                        StLoai2 = g.Sum(x => x.StLoai2),
                        KlLoai2 = g.Sum(x => Convert.ToDecimal(x.KlLoai2)),
                        StLoai2tp = g.Sum(x => x.StLoai2TP),
                        KlLoai2tp = g.Sum(x => Convert.ToDecimal(x.KlLoai2TP)),
                        StPhoiNgan = g.Sum(x => x.StPhoiNgan),
                        CdPhoiNgan = g.Sum(x => Convert.ToDecimal(x.CdPhoiNgan)),
                        KlPhoiNgan = g.Sum(x => Convert.ToDecimal(x.KlPhoiNgan)),
                        StLoai3 = g.Sum(x => x.StLoai3),
                        KlLoai3 = g.Sum(x => Convert.ToDecimal(x.KlLoai3)),
                        TongSoThanh = g.Sum(x => x.TongSoThanh),
                        TongKhoiLuong = g.Sum(x => Convert.ToDecimal(x.TongKhoiLuong)),
                        // 🔥 FIX trạng thái
                        TinhTrangHRC = g.All(x => x.TinhTrangCap2 == 1) ? 1 : 0,
                        TinhTrangQLCL = g.All(x => x.TinhTrangCap1 == 1) ? 1 : 0,
                        TinhTrangChot = g.All(x => x.TinhTrang == 1) ? 1 : 0,
                        SoPhieu = g.Select(x => x.SoPhieu).FirstOrDefault(),
                    });

            // ★ Lần 2 Lookup: Nhập kho ca tiếp theo - lookup bình thường, tìm kiếm bằng shifted key
            var lan2Lookup = queryData
                .GroupBy(x => new
                {
                    NgaySX = DateOnly.FromDateTime(x.NgaySX),
                    Ca = x.Ca,
                    Me = NormalizeKeyValue(x.Me),
                    Mac = NormalizeKeyValue(x.Mac),
                    KichThuoc = NormalizeKeyValue(x.KichThuoc),
                })
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        StLoai1 = g.Sum(x => x.StLoai1),
                        KlLoai1 = g.Sum(x => Convert.ToDecimal(x.KlLoai1)),
                        StLoai2 = g.Sum(x => x.StLoai2),
                        KlLoai2 = g.Sum(x => Convert.ToDecimal(x.KlLoai2)),
                        StLoai2tp = g.Sum(x => x.StLoai2TP),
                        KlLoai2tp = g.Sum(x => Convert.ToDecimal(x.KlLoai2TP)),
                        StPhoiNgan = g.Sum(x => x.StPhoiNgan),
                        CdPhoiNgan = g.Sum(x => Convert.ToDecimal(x.CdPhoiNgan)),
                        KlPhoiNgan = g.Sum(x => Convert.ToDecimal(x.KlPhoiNgan)),
                        StLoai3 = g.Sum(x => x.StLoai3),
                        KlLoai3 = g.Sum(x => Convert.ToDecimal(x.KlLoai3)),
                        TongSoThanh = g.Sum(x => x.TongSoThanh),
                        TongKhoiLuong = g.Sum(x => Convert.ToDecimal(x.TongKhoiLuong)),
                        // 🔥 FIX trạng thái
                        TinhTrangHRC = g.All(x => x.TinhTrangCap2 == 1) ? 1 : 0,
                        TinhTrangQLCL = g.All(x => x.TinhTrangCap1 == 1) ? 1 : 0,
                        TinhTrangChot = g.All(x => x.TinhTrang == 1) ? 1 : 0,
                    });

            // ★ Tạo rows từ BKMis (nguồn gốc sản xuất) - so sánh dựa trên số mẻ (Me, Mac, KichThuoc)
            var rows = dataBKMis
                .GroupBy(x => new
                {
                    x.NgaySX,
                    x.Ca,
                    Me = NormalizeKeyValue(x.Me),
                    Mac = NormalizeKeyValue(x.Mac),
                    KichThuoc = NormalizeKeyValue(x.KichThuoc),
                })
                .Select(g => new
                {
                    Row = new BmPhieuExportRow
                    {
                        NgaySX = g.Key.NgaySX,
                        Ca = g.Key.Ca,
                        Me = g.FirstOrDefault()?.Me ?? "",
                        Mac = g.FirstOrDefault()?.Mac ?? "",
                        KichThuoc = g.FirstOrDefault()?.KichThuoc ?? "",


                        // BK: dữ liệu gốc sản xuất từ BKMis
                        StLoai1_BK = g.Sum(x => x.StLoai1),
                        KlLoai1_BK = g.Sum(x => Convert.ToDecimal(x.KlLoai1)),
                        StLoai2_BK = g.Sum(x => x.StLoai2),
                        KlLoai2_BK = g.Sum(x => Convert.ToDecimal(x.KlLoai2)),
                        StLoai2tp_BK = g.Sum(x => x.stLoai2tp),
                        KlLoai2tp_BK = g.Sum(x => Convert.ToDecimal(x.klLoai2tp)),
                        StPhoiNgan_BK = g.Sum(x => x.StPhoiNgan),
                        CdPhoiNgan_BK = g.Sum(x => Convert.ToDecimal(x.CdPhoiNgan)),
                        KlPhoiNgan_BK = g.Sum(x => Convert.ToDecimal(x.KlPhoiNgan)),
                        StLoai3_BK = g.Sum(x => x.StLoai3),
                        KlLoai3_BK = g.Sum(x => Convert.ToDecimal(x.KlLoai3)),
                        TongSoThanh_BK = g.Sum(x => x.TongSoThanh),
                        TongKhoiLuong_BK = g.Sum(x => Convert.ToDecimal(x.TongKhoiLuong)),
                    },
                    Key = g.Key
                })
                .OrderByDescending(x => x.Key.NgaySX)
                .ThenBy(x => x.Key.Ca)
                .ToList();

            // ★ Populate dữ liệu nhập kho lần 1 và lần 2 dựa trên trùng số mẻ (Me, Mac, KichThuoc)
            foreach (var item in rows)
            {
                // ★ Lần 1: Nhập kho cùng ngày ca - nếu cùng mẻ thì hiển thị
                if (lan1Lookup.TryGetValue(item.Key, out var lan1))
                {
                    item.Row.SoPhieu = lan1.SoPhieu;
                    item.Row.StLoai1 = lan1.StLoai1;
                    item.Row.KlLoai1 = lan1.KlLoai1;
                    item.Row.StLoai2 = lan1.StLoai2;
                    item.Row.KlLoai2 = lan1.KlLoai2;
                    item.Row.StLoai2tp = lan1.StLoai2tp;
                    item.Row.KlLoai2tp = lan1.KlLoai2tp;
                    item.Row.StPhoiNgan = lan1.StPhoiNgan;
                    item.Row.CdPhoiNgan = lan1.CdPhoiNgan;
                    item.Row.KlPhoiNgan = lan1.KlPhoiNgan;
                    item.Row.StLoai3 = lan1.StLoai3;
                    item.Row.KlLoai3 = lan1.KlLoai3;
                    item.Row.TongSoThanh = lan1.TongSoThanh;
                    item.Row.TongKhoiLuong = lan1.TongKhoiLuong;
                    item.Row.TinhTrang_HRC = lan1.TinhTrangHRC;
                    item.Row.TinhTrang_QLCL = lan1.TinhTrangQLCL;
                    item.Row.TinhTrang_Chot = lan1.TinhTrangChot;
                }

                // ★ Lần 2: Nhập kho ngày ca tiếp theo - nếu cùng mẻ thì hiển thị
                var key2 = new
                {
                    NgaySX = item.Key.Ca == 2 ? item.Key.NgaySX.AddDays(1) : item.Key.NgaySX,
                    Ca = item.Key.Ca == 1 ? 2 : 1,
                    Me = item.Key.Me,
                    Mac = item.Key.Mac,
                    KichThuoc = item.Key.KichThuoc,
                };
                if (lan2Lookup.TryGetValue(key2, out var lan2))
                {
                    item.Row.StLoai1_Lan2 = lan2.StLoai1;
                    item.Row.KlLoai1_Lan2 = lan2.KlLoai1;
                    item.Row.StLoai2_Lan2 = lan2.StLoai2;
                    item.Row.KlLoai2_Lan2 = lan2.KlLoai2;
                    item.Row.StLoai2tp_Lan2 = lan2.StLoai2tp;
                    item.Row.KlLoai2tp_Lan2 = lan2.KlLoai2tp;
                    item.Row.StPhoiNgan_Lan2 = lan2.StPhoiNgan;
                    item.Row.CdPhoiNgan_Lan2 = lan2.CdPhoiNgan;
                    item.Row.KlPhoiNgan_Lan2 = lan2.KlPhoiNgan;
                    item.Row.StLoai3_Lan2 = lan2.StLoai3;
                    item.Row.KlLoai3_Lan2 = lan2.KlLoai3;
                    item.Row.TongSoThanh_Lan2 = lan2.TongSoThanh;
                    item.Row.TongKhoiLuong_Lan2 = lan2.TongKhoiLuong;
                    item.Row.TinhTrang_HRC2 = lan2.TinhTrangHRC;
                    item.Row.TinhTrang_QLCL2 = lan2.TinhTrangQLCL;
                    item.Row.TinhTrang_Chot2 = lan2.TinhTrangChot;
                }
            }

            return rows.Select(x => x.Row).ToList();
        }

        public async Task<byte[]> ExportExcelByBmPhieuAsync(DateOnly? fromDate, DateOnly? toDate)
        {
            var data = await GetDataExportExcelByBmPhieuAsync(fromDate, toDate);


            var templatePath = Path.Combine(
                _env.WebRootPath,
                "templates",
                "BM_TongHopGiaoNhanPhoiNhapKho.xlsx"
            );

            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"Không tìm thấy file mẫu Excel: {templatePath}");

            using var workbook = new XLWorkbook(templatePath);
            var ws = workbook.Worksheet(1);

            ws.Cell("A4").Value = $"Từ ngày: {fromDate:dd/MM/yyyy} đến ngày: {toDate:dd/MM/yyyy}";

            int row = 9;
            int stt = 1;
            foreach (var item in data)
            {
                // copy format của dòng mẫu
                if (row > 9)
                {
                    ws.Row(9).CopyTo(ws.Row(row));
                }

                ws.Cell(row, 1).Value = stt++;
                ws.Cell(row, 2).Value = item.NgaySX?.ToString("dd/MM/yyyy");

                ws.Cell(row, 3).Value = item.Kip;
                ws.Cell(row, 4).Value = item.Ca;
                ws.Cell(row, 5).Value = item.MayDuc;

                ws.Cell(row, 6).Value = item.Me;
                ws.Cell(row, 7).Value = item.Mac;
                ws.Cell(row, 8).Value = item.KichThuoc;

                ws.Cell(row, 9).Value = item.StLoai1;
                ws.Cell(row, 10).Value = item.KlLoai1;

                ws.Cell(row, 11).Value = item.StLoai2;
                ws.Cell(row, 12).Value = item.KlLoai2;

                ws.Cell(row, 13).Value = item.StLoai2tp;
                ws.Cell(row, 14).Value = item.KlLoai2tp;

                ws.Cell(row, 15).Value = item.StPhoiNgan;
                ws.Cell(row, 16).Value = item.CdPhoiNgan;
                ws.Cell(row, 17).Value = item.KlPhoiNgan;

                ws.Cell(row, 18).Value = item.StLoai3;
                ws.Cell(row, 19).Value = item.KlLoai3;

                ws.Cell(row, 20).Value = item.TongKhoiLuong;

                ws.Cell(row, 21).Value = "";
                ws.Cell(row, 22).Value = item.SoPhieu;
                var cell = ws.Cell(row, 23);
                switch (item.TinhTrang)
                {
                    case 0:
                        cell.Value = "Đang lưu";
                        cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                        break;

                    case 1:
                        cell.Value = "Đã gửi";
                        cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
                        break;

                    case 2:
                        cell.Value = "Hoàn thành";
                        cell.Style.Fill.BackgroundColor = XLColor.LightGreen;
                        break;

                    case 3:
                        cell.Value = "Đã thu hồi";
                        cell.Style.Fill.BackgroundColor = XLColor.Orange;
                        break;

                    case 4:
                        cell.Value = "Không xác nhận";
                        cell.Style.Fill.BackgroundColor = XLColor.Red;
                        cell.Style.Font.FontColor = XLColor.White;
                        break;

                    case 5:
                        cell.Value = "Đã chốt";
                        cell.Style.Fill.BackgroundColor = XLColor.DarkGreen;
                        cell.Style.Font.FontColor = XLColor.White;
                        break;

                    case 6:
                        cell.Value = "Đang phê duyệt";
                        cell.Style.Fill.BackgroundColor = XLColor.Yellow;
                        break;

                    case 7:
                        cell.Value = "Hiệu chỉnh";
                        cell.Style.Fill.BackgroundColor = XLColor.MediumPurple;
                        cell.Style.Font.FontColor = XLColor.White;
                        break;

                    default:
                        cell.Value = "Không xác định";
                        break;
                }
                row++;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return stream.ToArray();
        }

        public async Task<byte[]> ExportExcelByBmPhieuPKHAsync(DateOnly? fromDate, DateOnly? toDate)
        {
            var data = await GetDataExportExcelByBmPhieuPKHAsync(fromDate, toDate);


            var templatePath = Path.Combine(
                _env.WebRootPath,
                "templates",
                "BM_TongHopGiaoNhanPhoiNhapKhoPKH.xlsx"
            );

            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"Không tìm thấy file mẫu Excel: {templatePath}");

            using var workbook = new XLWorkbook(templatePath);
            var ws = workbook.Worksheet(1);

            ws.Cell("A4").Value = $"Từ ngày: {fromDate:dd/MM/yyyy} đến ngày: {toDate:dd/MM/yyyy}";

            static int ToInt(int? value) => value ?? 0;
            static decimal ToDecimal(decimal? value) => value ?? 0m;

            int row = 9;
            int stt = 1;
            foreach (var item in data)
            {
                // copy format của dòng mẫu
                if (row > 9)
                {
                    ws.Row(9).CopyTo(ws.Row(row));
                }

                ws.Cell(row, 1).Value = stt++;
                ws.Cell(row, 2).Value = item.NgaySX?.ToString("dd/MM/yyyy");

                ws.Cell(row, 3).Value = item.Kip;
                ws.Cell(row, 4).Value = item.Ca;
                ws.Cell(row, 5).Value = item.MayDuc;

                ws.Cell(row, 6).Value = item.Me;
                ws.Cell(row, 7).Value = item.Mac;
                ws.Cell(row, 8).Value = item.KichThuoc;

                // Số liệu BK mis

                ws.Cell(row, 9).Value = item.StLoai1_BK;
                ws.Cell(row, 10).Value = item.KlLoai1_BK;

                ws.Cell(row, 11).Value = item.StLoai2_BK;
                ws.Cell(row, 12).Value = item.KlLoai2_BK;

                ws.Cell(row, 13).Value = item.StLoai2tp_BK;
                ws.Cell(row, 14).Value = item.KlLoai2tp_BK;

                ws.Cell(row, 15).Value = item.StPhoiNgan_BK;
                ws.Cell(row, 16).Value = item.CdPhoiNgan_BK;
                ws.Cell(row, 17).Value = item.KlPhoiNgan_BK;

                ws.Cell(row, 18).Value = item.StLoai3_BK;
                ws.Cell(row, 19).Value = item.KlLoai3_BK;

                ws.Cell(row, 20).Value = item.TongKhoiLuong;

                ws.Cell(row, 21).Value = "";
                ws.Cell(row, 22).Value = item.SoPhieu;

                // Vùng chuyển lần 1 (X -> AI)
                ws.Cell(row, 24).Value = item.StLoai1;
                ws.Cell(row, 25).Value = item.KlLoai1;
                ws.Cell(row, 26).Value = item.StLoai2;
                ws.Cell(row, 27).Value = item.KlLoai2;
                ws.Cell(row, 28).Value = item.StLoai2tp;
                ws.Cell(row, 29).Value = item.KlLoai2tp;
                ws.Cell(row, 30).Value = item.StPhoiNgan;
                ws.Cell(row, 31).Value = item.CdPhoiNgan;
                ws.Cell(row, 32).Value = item.KlPhoiNgan;
                ws.Cell(row, 33).Value = item.StLoai3;
                ws.Cell(row, 34).Value = item.KlLoai3;
                // Bổ sung thêm 3 tình trạng HRC, QLCL, Chốt để dễ theo dõi
                // Thêm màu sắc cho dễ nhìn
                ws.Cell(row, 35).Value = item.TinhTrang_HRC == 1 ? "Đã xác nhận" : "";
                ws.Cell(row, 35).Style.Fill.BackgroundColor = item.TinhTrang_HRC == 1 ? XLColor.LightGreen : XLColor.Yellow;
                ws.Cell(row, 36).Value = item.TinhTrang_QLCL == 1 ? "Đã xác nhận" : "";
                ws.Cell(row, 36).Style.Fill.BackgroundColor = item.TinhTrang_QLCL == 1 ? XLColor.LightGreen : XLColor.Yellow;
                ws.Cell(row, 37).Value = item.TinhTrang_Chot == 1 ? "Đã xác nhận" : "";
                ws.Cell(row, 37).Style.Fill.BackgroundColor = item.TinhTrang_Chot == 1 ? XLColor.LightGreen : XLColor.Yellow;

                //ws.Cell(row, 35).Value = item.Ti;

                // Vùng Chuyển lần 2
                ws.Cell(row, 38).Value = item.StLoai1_Lan2;
                ws.Cell(row, 39).Value = item.KlLoai1_Lan2;
                ws.Cell(row, 40).Value = item.StLoai2_Lan2;
                ws.Cell(row, 41).Value = item.KlLoai2_Lan2;
                ws.Cell(row, 42).Value = item.StLoai2tp_Lan2;
                ws.Cell(row, 43).Value = item.KlLoai2tp_Lan2;
                ws.Cell(row, 44).Value = item.StPhoiNgan_Lan2;
                ws.Cell(row, 45).Value = item.CdPhoiNgan_Lan2;
                ws.Cell(row, 46).Value = item.KlPhoiNgan_Lan2;
                ws.Cell(row, 47).Value = item.StLoai3_Lan2;
                ws.Cell(row, 48).Value = item.KlLoai3_Lan2;
                // Bổ sung thêm 3 tình trạng HRC, QLCL, Chốt để dễ theo dõi
                // Thêm màu sắc cho dễ nhìn
                ws.Cell(row, 49).Value = item.TinhTrang_HRC2 == 1 ? "Đã xác nhận" : "";
                ws.Cell(row, 49).Style.Fill.BackgroundColor = item.TinhTrang_HRC2 == 1 ? XLColor.LightGreen : XLColor.Yellow;
                ws.Cell(row, 50).Value = item.TinhTrang_QLCL2 == 1 ? "Đã xác nhận" : "";
                ws.Cell(row, 50).Style.Fill.BackgroundColor = item.TinhTrang_QLCL2 == 1 ? XLColor.LightGreen : XLColor.Yellow;
                ws.Cell(row, 51).Value = item.TinhTrang_Chot2 == 1 ? "Đã xác nhận" : "";
                ws.Cell(row, 51).Style.Fill.BackgroundColor = item.TinhTrang_Chot2 == 1 ? XLColor.LightGreen : XLColor.Yellow;

                var cell = ws.Cell(row, 23);
                switch (item.TinhTrang)
                {
                    case 0:
                        cell.Value = "Đang lưu";
                        cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                        break;

                    case 1:
                        cell.Value = "Đã gửi";
                        cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
                        break;

                    case 2:
                        cell.Value = "Hoàn thành";
                        cell.Style.Fill.BackgroundColor = XLColor.LightGreen;
                        break;

                    case 3:
                        cell.Value = "Đã thu hồi";
                        cell.Style.Fill.BackgroundColor = XLColor.Orange;
                        break;

                    case 4:
                        cell.Value = "Không xác nhận";
                        cell.Style.Fill.BackgroundColor = XLColor.Red;
                        cell.Style.Font.FontColor = XLColor.White;
                        break;

                    case 5:
                        cell.Value = "Đã chốt";
                        cell.Style.Fill.BackgroundColor = XLColor.DarkGreen;
                        cell.Style.Font.FontColor = XLColor.White;
                        break;

                    case 6:
                        cell.Value = "Đang phê duyệt";
                        cell.Style.Fill.BackgroundColor = XLColor.Yellow;
                        break;

                    case 7:
                        cell.Value = "Hiệu chỉnh";
                        cell.Style.Fill.BackgroundColor = XLColor.MediumPurple;
                        cell.Style.Font.FontColor = XLColor.White;
                        break;

                    default:
                        cell.Value = "Không xác định";
                        break;
                }
                row++;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return stream.ToArray();
        }

        public async Task<List<BmSanLuongPhoiRow>> GetDataExportExcelSanLuongPhoiAsync(DateOnly? fromDate, DateOnly? toDate)
        {
            var phieuList = await _repo.GetDataSanLuongPhoiAsync(fromDate, toDate);

            var result = new List<BmSanLuongPhoiRow>();

            foreach (var item in phieuList)
            {
                if (string.IsNullOrEmpty(item.DataJson))
                    continue;

                var json = JsonSerializer.Deserialize<BmPhieuSLPJson>(
                    item.DataJson,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (json?.table1 == null)
                    continue;

                foreach (var row in json.table1)
                {
                    result.Add(new BmSanLuongPhoiRow
                    {
                        SoPhieu = item.SoPhieu,
                        NgaySX = json.NgaySX,
                        Kip = json.kip,
                        Ca = json.ca,

                        MacThep = row.macThep,
                        KichThuoc = row.kichThuoc,

                        StLoai1 = row.stLoai1,
                        KlLoai1 = row.klLoai1,

                        StPhoiNgan = row.stPhoiNgan,
                        KlPhoiNgan = row.klPhoiNgan,

                        StLoai2 = row.stLoai2,
                        KlLoai2 = row.klLoai2,

                        StLoai3 = row.stLoai3,
                        KlLoai3 = row.klLoai3,

                        TongSoThanh = row.tongSoThanh,
                        TongKhoiLuong = row.tongKhoiLuong,

                        TinhTrang = item.TinhTrang
                    });
                }
            }

            return result;
        }
        public async Task<byte[]> ExportExcelSanLuongPhoiAsync(DateOnly? fromDate, DateOnly? toDate)
        {
            var data = await GetDataExportExcelSanLuongPhoiAsync(fromDate, toDate);

            var templatePath = Path.Combine(
                _env.WebRootPath,
                "templates",
                "BM_TongHopSanLuongPhoi.xlsx"
            );

            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"Không tìm thấy file mẫu Excel: {templatePath}");

            using var workbook = new XLWorkbook(templatePath);
            var ws = workbook.Worksheet(1);

            ws.Cell("A4").Value = $"Từ ngày: {fromDate:dd/MM/yyyy} đến ngày: {toDate:dd/MM/yyyy}";

            int startRow = 9;

            if (data.Count > 1)
                ws.Row(startRow).InsertRowsBelow(data.Count - 1);

            int row = startRow;
            int stt = 1;

            foreach (var item in data)
            {
                ws.Cell(row, 1).Value = stt++;
                ws.Cell(row, 2).Value = item.NgaySX?.ToDateTime(TimeOnly.MinValue);
                ws.Cell(row, 3).Value = item.Kip;
                ws.Cell(row, 4).Value = item.Ca;

                ws.Cell(row, 5).Value = item.MacThep;
                ws.Cell(row, 6).Value = item.KichThuoc;

                ws.Cell(row, 7).Value = item.StLoai1;
                ws.Cell(row, 8).Value = item.KlLoai1;

                ws.Cell(row, 9).Value = item.StPhoiNgan;
                ws.Cell(row, 10).Value = item.KlPhoiNgan;

                ws.Cell(row, 11).Value = item.StLoai2;
                ws.Cell(row, 12).Value = item.KlLoai2;

                ws.Cell(row, 13).Value = item.StLoai3;
                ws.Cell(row, 14).Value = item.KlLoai3;

                ws.Cell(row, 15).Value = item.TongSoThanh;
                ws.Cell(row, 16).Value = item.TongKhoiLuong;

                ws.Cell(row, 17).Value = "";
                ws.Cell(row, 18).Value = item.SoPhieu;

                var cell = ws.Cell(row, 19);

                switch (item.TinhTrang)
                {
                    case 0:
                        cell.Value = "Đang lưu";
                        cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                        break;

                    case 1:
                        cell.Value = "Đã gửi";
                        cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
                        break;

                    case 2:
                        cell.Value = "Hoàn thành";
                        cell.Style.Fill.BackgroundColor = XLColor.LightGreen;
                        break;

                    case 3:
                        cell.Value = "Đã thu hồi";
                        cell.Style.Fill.BackgroundColor = XLColor.Orange;
                        break;

                    case 4:
                        cell.Value = "Không xác nhận";
                        cell.Style.Fill.BackgroundColor = XLColor.Red;
                        cell.Style.Font.FontColor = XLColor.White;
                        break;

                    case 5:
                        cell.Value = "Đã chốt";
                        cell.Style.Fill.BackgroundColor = XLColor.DarkGreen;
                        cell.Style.Font.FontColor = XLColor.White;
                        break;

                    case 6:
                        cell.Value = "Đang phê duyệt";
                        cell.Style.Fill.BackgroundColor = XLColor.Yellow;
                        break;

                    case 7:
                        cell.Value = "Hiệu chỉnh";
                        cell.Style.Fill.BackgroundColor = XLColor.MediumPurple;
                        cell.Style.Font.FontColor = XLColor.White;
                        break;

                    default:
                        cell.Value = "Không xác định";
                        break;
                }

                row++;
            }
            int lastRow = row - 1;

            // format ngày
            ws.Range(9, 2, lastRow, 2).Style.DateFormat.Format = "dd/MM/yyyy";

            // format số
            ws.Range(9, 7, lastRow, 16).Style.NumberFormat.Format = "#,##0";

            // căn giữa
            ws.Range(9, 1, lastRow, 4)
                .Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // căn phải số
            ws.Range(9, 7, lastRow, 16)
                .Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            // border bảng
            var range = ws.Range(9, 1, lastRow, 19);

            range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // chiều cao dòng
            ws.Rows(9, lastRow).Height = 35;

            // freeze header
            ws.SheetView.FreezeRows(8);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return stream.ToArray();
        }

        /// <summary>
        /// Export Excel chi tiết dữ liệu phôi nhập kho theo phiếu
        /// Sử dụng template BM_GiaoNhanPhoiNhapKho.xlsx
        /// Bao gồm tất cả dòng phôi trong phiếu với đầy đủ thông tin chi tiết
        /// </summary>
        public async Task<byte[]> ExportExcelPhoiNhapKhoByPhieuAsync(Guid phieuId)
        {
            try
            {
                // ★ Lấy thông tin phiếu
                var phieu = await _repoPhieu.GetByIdAsync(phieuId);
                if (phieu == null)
                    throw new ArgumentException($"Không tìm thấy phiếu với ID: {phieuId}");

                // ★ Lấy dữ liệu phôi nhập kho từ phiếu
                var phoiNhapKhoData = await _context.BM_PhoiNhapKho
                    .AsNoTracking()
                    .Where(x => x.IdPhieu == phieuId)
                    .OrderBy(x => x.NgaySX)
                    .ThenBy(x => x.Ca)
                    .ThenBy(x => x.Me)
                    .ThenBy(x => x.Mac)
                    .ToListAsync();

                if (!phoiNhapKhoData.Any())
                    throw new ArgumentException($"Phiếu {phieuId} không có dữ liệu phôi nhập kho");

                // ★ Load template
                var templatePath = Path.Combine(
                    _env.WebRootPath,
                    "templates",
                    "BM_GiaoNhanPhoiNhapKho.xlsx"
                );

                if (!File.Exists(templatePath))
                    throw new FileNotFoundException($"Không tìm thấy file mẫu Excel: {templatePath}");

                using var workbook = new XLWorkbook(templatePath);
                var ws = workbook.Worksheet(1);

                // ★ Cập nhật thông tin phiếu ở dòng 4 (nếu có ô dành cho tiêu đề chi tiết)
                // Ví dụ: "Chi tiết phôi nhập kho - Số phiếu: xxxxx"
                ws.Cell("A4").Value = $"Chi tiết phôi nhập kho - Số phiếu: {phieu.SoPhieu} (Ngày: {DateTime.Now:dd/MM/yyyy})";

                // ★ Điền dữ liệu (bắt đầu từ dòng 8, giống template)
                int dataRow = 8;
                int stt = 1;
                decimal totalTongSoThanh = 0;
                decimal totalTongKhoiLuong = 0;

                foreach (var row in phoiNhapKhoData)
                {
                    // ★ Copy format từ dòng 8 nếu cần thêm dòng
                    if (dataRow > 8)
                    {
                        ws.Row(8).CopyTo(ws.Row(dataRow));
                    }

                    // ★ Điền dữ liệu theo cấu trúc template
                    ws.Cell(dataRow, 1).Value = stt++;
                    ws.Cell(dataRow, 2).Value = row.NgaySX.ToString("dd/MM/yyyy");
                    ws.Cell(dataRow, 3).Value = row.Me;
                    ws.Cell(dataRow, 4).Value = row.Mac;
                    ws.Cell(dataRow, 5).Value = row.KichThuoc;

                    // Loại I
                    ws.Cell(dataRow, 6).Value = row.StLoai1;
                    ws.Cell(dataRow, 7).Value = row.KlLoai1;

                    // Loại II BM
                    ws.Cell(dataRow, 8).Value = row.StLoai2;
                    ws.Cell(dataRow, 9).Value = row.KlLoai2;

                    // Loại II TP
                    ws.Cell(dataRow, 10).Value = row.StLoai2TP;
                    ws.Cell(dataRow, 11).Value = row.KlLoai2TP;

                    // Phôi ngắn (11m - 9m - 6m)
                    ws.Cell(dataRow, 12).Value = row.StPhoiNgan;
                    ws.Cell(dataRow, 13).Value = row.CdPhoiNgan;
                    ws.Cell(dataRow, 14).Value = row.KlPhoiNgan;

                    // Loại III
                    ws.Cell(dataRow, 15).Value = row.StLoai3;
                    ws.Cell(dataRow, 16).Value = row.KlLoai3;

                    // Tổng
                    ws.Cell(dataRow, 17).Value = row.TongSoThanh;
                    ws.Cell(dataRow, 18).Value = row.TongKhoiLuong;

                    // ★ Trạng thái xác nhận (thêm vào cột sau)
                    // QLCL
                    var qlclStatus = row.TinhTrangCap1 == 1 ? "✓" : "";
                    ws.Cell(dataRow, 19).Value = qlclStatus;
                    if (row.TinhTrangCap1 == 1)
                        ws.Cell(dataRow, 19).Style.Fill.BackgroundColor = XLColor.LightGreen;

                    // Đúc
                    var ducStatus = row.TinhTrangCap2 == 1 ? "✓" : "";
                    ws.Cell(dataRow, 20).Value = ducStatus;
                    if (row.TinhTrangCap2 == 1)
                        ws.Cell(dataRow, 20).Style.Fill.BackgroundColor = XLColor.LightGreen;

                    // Chốt
                    var chotStatus = row.TinhTrang == 1 ? "✓" : "";
                    ws.Cell(dataRow, 21).Value = chotStatus;
                    if (row.TinhTrang == 1)
                        ws.Cell(dataRow, 21).Style.Fill.BackgroundColor = XLColor.DarkGreen;

                    totalTongSoThanh += row.TongSoThanh ?? 0;
                    totalTongKhoiLuong += row.TongKhoiLuong ?? 0;

                    dataRow++;
                }

                // ★ Dòng tổng cộng
                int totalRow = dataRow;
                ws.Row(8).CopyTo(ws.Row(totalRow));

                ws.Cell(totalRow, 1).Value = "";
                ws.Cell(totalRow, 2).Value = "";
                ws.Cell(totalRow, 3).Value = "TỔNG CỘNG";
                ws.Cell(totalRow, 3).Style.Font.Bold = true;
                ws.Cell(totalRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                ws.Cell(totalRow, 4).Value = "";
                ws.Cell(totalRow, 5).Value = "";

                // Sum từng loại
                ws.Cell(totalRow, 6).Value = phoiNhapKhoData.Sum(x => x.StLoai1);
                ws.Cell(totalRow, 7).Value = phoiNhapKhoData.Sum(x => x.KlLoai1);
                ws.Cell(totalRow, 8).Value = phoiNhapKhoData.Sum(x => x.StLoai2);
                ws.Cell(totalRow, 9).Value = phoiNhapKhoData.Sum(x => x.KlLoai2);
                ws.Cell(totalRow, 10).Value = phoiNhapKhoData.Sum(x => x.StLoai2TP);
                ws.Cell(totalRow, 11).Value = phoiNhapKhoData.Sum(x => x.KlLoai2TP);
                ws.Cell(totalRow, 12).Value = phoiNhapKhoData.Sum(x => x.StPhoiNgan);
                ws.Cell(totalRow, 13).Value = phoiNhapKhoData.Sum(x => x.CdPhoiNgan);
                ws.Cell(totalRow, 14).Value = phoiNhapKhoData.Sum(x => x.KlPhoiNgan);
                ws.Cell(totalRow, 15).Value = phoiNhapKhoData.Sum(x => x.StLoai3);
                ws.Cell(totalRow, 16).Value = phoiNhapKhoData.Sum(x => x.KlLoai3);

                // Tổng số thanh và khối lượng
                ws.Cell(totalRow, 17).Value = totalTongSoThanh;
                ws.Cell(totalRow, 17).Style.Font.Bold = true;
                ws.Cell(totalRow, 17).Style.Fill.BackgroundColor = XLColor.Yellow;

                ws.Cell(totalRow, 18).Value = totalTongKhoiLuong;
                ws.Cell(totalRow, 18).Style.Font.Bold = true;
                ws.Cell(totalRow, 18).Style.Fill.BackgroundColor = XLColor.Yellow;

                // ★ Format các cột số
                for (int col = 6; col <= 18; col++)
                {
                    ws.Range(8, col, totalRow, col).Style.NumberFormat.Format = "0.00";
                    ws.Range(8, col, totalRow, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                }

                // ★ Đóng băng dòng header
                ws.SheetView.FreezeRows(7);

                // ★ Xuất file
                using var stream = new MemoryStream();
                workbook.SaveAs(stream);

                return stream.ToArray();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Lỗi khi export Excel phôi nhập kho: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Helper để hiển thị trạng thái phiếu
        /// </summary>
        private static string GetTinhTrangDisplay(int? tinhTrang)
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
                7 => "Hiệu chỉnh",
                _ => "Không xác định"
            };
        }
    }
}

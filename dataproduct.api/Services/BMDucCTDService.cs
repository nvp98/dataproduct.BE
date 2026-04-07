using ClosedXML.Excel;
using dataproduct.api.DTOs.CTD_Dto;
using dataproduct.api.DTOs.Export;
using dataproduct.api.Models;
using dataproduct.api.Repositories;
using DinkToPdf;
using DinkToPdf.Contracts;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Extensions.Configuration;
using NuGet.Protocol.Core.Types;
using System;
using System.Configuration;
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


        public BMDucCTDService(ICtdBMDucCTDRepository repo, IConverter pdfConverter, IWebHostEnvironment env, IConfiguration configuration, PheDuyetService pheDuyetService, IHttpClientFactory httpClientFactory, IPhieuRepository repoPhieu)
        {
            _repo = repo;
            _pdfConverter = pdfConverter;
            _env = env;
            _configuration = configuration;
            _pheDuyetService = pheDuyetService;
            _httpClientFactory = httpClientFactory;
            _repoPhieu = repoPhieu;
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

        public async Task<List<PhoinhapkhoDto>> GetPhoiNhapKhoAsync(string ca, string kip, DateTime ngaySX, int mayduc)
        {
            return await _repo.GetPhoiNhapKhoAsync(ca, kip, ngaySX, mayduc);
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


            var nguoiGiao = xuongDuc.HoVaTen;
            var chucVuGiao = xuongDuc.TenViTri;
            var bPhanGiao = xuongDuc.TenPhongBan;


            var nguoiqlclo = qlcl.HoVaTen;
            var chucVuqlcl = qlcl.TenViTri;
            var bPhanqlcl = qlcl.TenPhongBan;

            var nguoiNhan = khoPhoi.HoVaTen;
            var chucVuNhan = khoPhoi.TenViTri;
            var bPhanNhan = khoPhoi.TenPhongBan;


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
        public async Task DeletePhoiNhapKhoByPhieuAsync(Guid idPhieu)
        {
            if (idPhieu == Guid.Empty)
                throw new ArgumentException("IdPhieu không hợp lệ");

            await _repo.DeletePhoiNhapKhoByPhieuAsync(idPhieu);
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

            var pheDuyets = request.listNguoiPheDuyet ?? new List<PheDuyetDto>();



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

            var xuongDuc = pheDuyets.FirstOrDefault(x => x.CapDuyet == 0 && x.TinhTrang == 1);
            var qlcl = pheDuyets.FirstOrDefault(x => x.CapDuyet == 1 && x.TinhTrang == 1);
            var khoPhoi = pheDuyets.FirstOrDefault(x => x.CapDuyet == 2 && x.TinhTrang == 1);


            // Convert logo và chữ ký sang base64
            var logoUrl = _configuration.GetValue<string>("AppSettings:LogoUrl") ?? "https://report.hoaphatdungquat.vn/img/logoHP.png";
            var logoBase64 = await ConvertImageUrlToBase64Async(logoUrl);

            var nguoiGiao = xuongDuc.HoVaTen;
            var chucVuGiao = xuongDuc.TenViTri;
            var bPhanGiao = xuongDuc.TenPhongBan;


            var nguoiqlclo = qlcl.HoVaTen;
            var chucVuqlcl = qlcl.TenViTri;
            var bPhanqlcl = qlcl.TenPhongBan;

            var nguoiNhan = khoPhoi.HoVaTen;
            var chucVuNhan = khoPhoi.TenViTri;
            var bPhanNhan = khoPhoi.TenPhongBan;

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
            var phieuList = await _repo.GetDataAsync(fromDate, toDate);

            var result = new List<BmPhieuExportRow>();

            foreach (var item in phieuList)
            {
                if (string.IsNullOrEmpty(item.DataJson))
                    continue;

                var json = JsonSerializer.Deserialize<BmPhieuJson>(
                    item.DataJson,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (json?.table1 == null)
                    continue;

                foreach (var row in json.table1)
                {
                    result.Add(new BmPhieuExportRow
                    {
                        SoPhieu = item.SoPhieu,
                        NgaySX = json.NgaySX,
                        MayDuc = json.mayduc,
                        Kip = json.kip,
                        Ca = json.ca,

                        Me = row.me,
                        Mac = row.mac,
                        KichThuoc = row.kichThuoc,

                        StLoai1 = row.stLoai1,
                        KlLoai1 = row.klLoai1,

                        StLoai2 = row.stLoai2,
                        KlLoai2 = row.klLoai2,

                        StLoai2tp = row.stLoai2tp,
                        KlLoai2tp = row.klLoai2tp,

                        StPhoiNgan = row.stPhoiNgan,
                        CdPhoiNgan = row.cdPhoiNgan,
                        KlPhoiNgan = row.klPhoiNgan,

                        StLoai3 = row.stLoai3,
                        KlLoai3 = row.klLoai3,

                        TongSoThanh = row.tongSoThanh,
                        TongKhoiLuong = row.tongKhoiLuong,
                        TinhTrang = item.TinhTrang,
                    });
                }
            }

            return result;
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
    }
}

using dataproduct.api.DTOs.CTD_Dto;
using dataproduct.api.DTOs.Export;
using dataproduct.api.Models;
using dataproduct.api.Repositories;
using DinkToPdf;
using DinkToPdf.Contracts;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Extensions.Configuration;
using System;
using System.Configuration;
using System.Text;
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

        private async Task<string> FormatChuKyBase64Async(string? chuKy)
        {
            if (string.IsNullOrWhiteSpace(chuKy))
                return "";

            // Nếu đã là base64 image (bắt đầu bằng data:image)
            if (chuKy.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
            {
                return $"<img src=\"{chuKy}\" style=\"max-width: 150px; max-height: 80px;\" />";
            }

            // Nếu là URL (http/https) hoặc relative path
            if (chuKy.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                var base64 = await ConvertImageUrlToBase64Async(chuKy);
                if (!string.IsNullOrEmpty(base64))
                {
                    return $"<img src=\"{base64}\" style=\"max-width: 150px; max-height: 80px;\" />";
                }
            }
            else if (chuKy.StartsWith("/"))
            {
                // Nếu là đường dẫn relative, ghép với domain
                var domain = _configuration.GetValue<string>("AppSettings:Domain") ?? "https://report.hoaphatdungquat.vn";
                var fullUrl = domain.TrimEnd('/') + chuKy;
                var base64 = await ConvertImageUrlToBase64Async(fullUrl);
                if (!string.IsNullOrEmpty(base64))
                {
                    return $"<img src=\"{base64}\" style=\"max-width: 150px; max-height: 80px;\" />";
                }
            }

            // Nếu không phải là link/base64, trả về text gốc
            return chuKy;
        }
        
        public async Task<List<SanLuongPhoiDto>> GetByKipNgayAsync( string ca, string kip,DateTime ngaySX)
        {
            return await _repo.GetSanLuongPhoiAsync(ca,kip, ngaySX);
        }
        public async Task<List<PhoinhapkhoDto>> GetPhoiNhapKhoAsync(string ca,string kip,DateTime ngaySX, int mayduc)
        {
            return await _repo.GetPhoiNhapKhoAsync(ca, kip, ngaySX,mayduc);
        }
        public async Task<ExportFileResult> ExportPdfSanLuongAsync( DateOnly? NgaySX,int? Ca, string? Kip, Guid? idPhieu,List<PheDuyetDto> pheDuyets)
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

            foreach (var t in data)
            {
                tongSoThanh += t.TongSoThanh ?? 0;
                tongKhoiLuong += t.TongKhoiLuong ?? 0;

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


            var signXuongDuc = await FormatChuKyBase64Async(xuongDuc?.ChuKy);
            var signQLCL = await FormatChuKyBase64Async(qlcl?.ChuKy);
            var signKhoPhoi = await FormatChuKyBase64Async(khoPhoi?.ChuKy);

            html = html
                // Header
                .Replace("{{LogoUrl}}", logoBase64)
                .Replace("{{NgaySX}}", NgaySX.Value.ToString("dd/MM/yyyy"))
                .Replace("{{Ca}}", Ca.Value.ToString())
                .Replace("{{Kip}}", Kip)

                // Content
                //.Replace("{{NguoiThamGia}}", nguoiThamGia.ToString())

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

        public async Task InsertSanLuongPhoiAsync(SaveSanLuongPhoiDto dto)
        {
            try
            {
                var entities = dto.Table1.Select(r => new BM_SanLuongPhoi
                {
                    IdPhieu = dto.IdPhieu,
                    SoPhieu = dto.SoPhieu,
                    NgaySX = dto.NgaySX.Date,
                    Kip = dto.Kip,
                    Ca = dto.Ca,
                    MayDuc = dto.MayDuc,
                    MacThep = r.MacThep,
                    KichThuoc = r.KichThuoc,
                    StLoai1 = r.StLoai1,
                    KlLoai1 = r.KlLoai1,
                    StPhoiNgan = r.StPhoiNgan,
                    KlPhoiNgan = r.KlPhoiNgan,
                    StLoai2 = r.StLoai2,
                    KlLoai2 = r.KlLoai2,
                    StLoai3 = r.StLoai3,
                    KlLoai3 = r.KlLoai3,
                    TongSoThanh = r.TongSoThanh,
                    TongKhoiLuong = r.TongKhoiLuong,
                    NguoiTaoId = null
                }).ToList();
                await _repo.InsertSanLuongPhoiAsync(entities);
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi lưu sản lượng phôi", ex);
            }
        }
        public async Task DeleteSanLuongPhoiByPhieuAsync(Guid idPhieu)
        {
            if (idPhieu == Guid.Empty)
                throw new ArgumentException("IdPhieu không hợp lệ");

            await _repo.DeleteSanLuongPhoiByPhieuAsync(idPhieu);
        }
        //=== PHÔI NHẬP KHO
        public async Task InsertPhoiNhapKhoAsync(SavePhoiNhapKhoDto dto)
        {
            try
            {
                var entities = dto.Table1.Select(r => new BM_PhoiNhapKho
                {
                    IdPhieu = dto.IdPhieu,
                    SoPhieu = dto.SoPhieu,
                    NgaySX = dto.NgaySX.Date,
                    Kip = dto.Kip,
                    Ca = dto.Ca,
                    MayDuc = dto.MayDuc,
                    Me = r.Me,
                    Mac = r.Mac,
                    KichThuoc = r.KichThuoc,
                    StLoai1 = r.StLoai1,
                    KlLoai1 = r.KlLoai1,
                    StPhoiNgan = r.StPhoiNgan,
                    KlPhoiNgan = r.KlPhoiNgan,
                    StLoai2 = r.StLoai2,
                    KlLoai2 = r.KlLoai2,
                    StLoai2TP = r.StLoai2TP,
                    KlLoai2TP = r.KlLoai2TP,
                    StLoai3 = r.StLoai3,
                    KlLoai3 = r.KlLoai3,
                    TongSoThanh = r.TongSoThanh,
                    TongKhoiLuong = r.TongKhoiLuong,
                    NguoiTaoId = null
                }).ToList();
                await _repo.InsertPhoiNhapKhoAsync(entities);
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi lưu sản lượng phôi", ex);
            }
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

            foreach (var t in data)
            {
                tongSoThanh += t.TongSoThanh ?? 0;
                tongKhoiLuong += t.TongKhoiLuong ?? 0;
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
                    <td>{t.KlPhoiNgan:N0}</td>

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

            var signXuongDuc = await FormatChuKyBase64Async(xuongDuc?.ChuKy);
            var signQLCL = await FormatChuKyBase64Async(qlcl?.ChuKy);
            var signKhoPhoi = await FormatChuKyBase64Async(khoPhoi?.ChuKy);

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
    }
}

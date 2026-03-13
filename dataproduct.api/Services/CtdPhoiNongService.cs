using ClosedXML.Excel;
using dataproduct.api.DTOs;
using dataproduct.api.DTOs.Export;
using dataproduct.api.Models;
using dataproduct.api.Models.MasterData;
using dataproduct.api.Repositories;
using DinkToPdf;
using DinkToPdf.Contracts;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Drawing.Printing;
using System.Text;
using PaperKind = DinkToPdf.PaperKind;

namespace dataproduct.api.Services
{
    public class CtdPhoiNongService
    {
        private readonly ICtdPhoiNongRepository _repo;
        private readonly IPhieuRepository _repoPhieu;
        private readonly IBMPheDuyetRepository _repoBmPheDuyet;
        private readonly IConverter _pdfConverter;
        private readonly IWebHostEnvironment _env;
        private readonly IBKPhoiThepRepository _repoBkPhoi;
        private readonly PheDuyetService _pdservice;
        private readonly IConfiguration _configuration;

        public CtdPhoiNongService(ICtdPhoiNongRepository repo, IPhieuRepository repoPhieu, IBMPheDuyetRepository repoBmPheDuyet, IConverter pdfConverter, IWebHostEnvironment env, IBKPhoiThepRepository repoBkPhoi, PheDuyetService pdservice, IConfiguration configuration)
        {
            _repo = repo;
            _pdfConverter = pdfConverter;
            _env = env;
            _repoBkPhoi = repoBkPhoi;
            _repoPhieu = repoPhieu;
            _repoBmPheDuyet = repoBmPheDuyet;
            _pdservice = pdservice;
            _configuration = configuration;
        }

        public Task<IEnumerable<CtdPhoiNong>> GetAllAsync(DateOnly? NgaySX, int? Ca, string? Kip, int? Xuong, string? Me)
        {
            return _repo.GetAllAsync(NgaySX, Ca, Kip, Xuong, Me);
        }

        public Task<CtdPhoiNong?> GetByIdAsync(int id)
        {
            return _repo.GetByIdAsync(id);
        }

        public async Task<CtdPhoiNong> CreateAsync(CtdPhoiNong entity)
        {
            await _repo.AddAsync(entity);
            return entity;
        }

        public async Task<IEnumerable<CtdPhoiNong>> CreateListAsync(List<CtdPhoiNong> entities)
        {
            await _repo.AddListAsync(entities);
            return entities;
        }

        public async Task<bool> UpdateAsync(int id, CtdPhoiNong entity)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return false;
            entity.Id = id;
            await _repo.UpdateAsync(entity);
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return false;
            await _repo.DeleteAsync(id);
            return true;
        }

        public Task<int> UpdateStatusesAsync(List<CtdPhoiNongStatusUpdate> items)
        {
            return _repo.UpdateStatusRangeAsync(items);
        }

        public Task<int> UpdateStatusDone(DateOnly? NgaySX, int? Ca, string? Kip, int? Xuong, string? Me, int? status)
        {
            return _repo.UpdateStatusDone(NgaySX, Ca, Kip, Xuong, Me, status);
        }

        public Task<IEnumerable<CtdPhoiNong>> GetByPhieuIdAsync(Guid phieuId)
        {
            return _repo.GetByPhieuIdAsync(phieuId);
        }

        public Task<(int Created, int Updated)> UpsertListAsync(List<CtdPhoiNong> entities)
        {
            return _repo.UpsertListAsync(entities);
        }

        /// <summary>
        /// Helper method để format chữ ký thành thẻ img nếu là link/base64
        /// </summary>
        private string FormatChuKy(string? chuKy)
        {
            if (string.IsNullOrWhiteSpace(chuKy))
                return "";

            // Nếu là base64 image (bắt đầu bằng data:image)
            if (chuKy.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
            {
                return $"<img src=\"{chuKy}\" style=\"max-width: 150px; max-height: 80px;\" />";
            }

            // Nếu là URL (http/https)
            if (chuKy.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return $"<img src=\"{chuKy}\" style=\"max-width: 150px; max-height: 80px;\" />";
            }

            // Nếu là đường dẫn relative (ví dụ: /uploads/chuky/xxx.png)
            if (chuKy.StartsWith("/"))
            {
                // Lấy domain từ config
                var domain = _configuration.GetValue<string>("AppSettings:Domain") ?? "https://report.hoaphatdungquat.vn";

                // Ghép domain với relative path
                var fullUrl = domain.TrimEnd('/') + chuKy;

                return $"<img src=\"{fullUrl}\" style=\"max-width: 150px; max-height: 80px;\" />";
            }

            // Nếu không phải là link/base64, trả về text gốc
            return chuKy;
        }

        public async Task<ExportFileResult> ExportExcelAsync(DateOnly? NgaySX, int? Ca, string? Kip, int? Xuong, string? Me)
        {
            // 1️⃣ Query dữ liệu
            var query = await _repo.GetAllAsync(NgaySX, Ca, Kip, Xuong, Me);

            // ==== 2. Đường dẫn đến template ====
            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "templates", "BM.06-QT.05.11 Bien ban giao nhan phoi nong.xlsx");
            if (!System.IO.File.Exists(templatePath))
                throw new FileNotFoundException($"Không tìm thấy file mẫu Excel: {templatePath}");

            // ==== 3. Tạo workbook từ file mẫu ====
            using var workbook = new ClosedXML.Excel.XLWorkbook(templatePath);
            var ws = workbook.Worksheet(1); // worksheet đầu tiên

            // ==== 4. Ghi dữ liệu bắt đầu từ dòng 6 ====
            int startRow = 16;
            int currentRow = startRow;

            foreach (var t in query.Where(x => x.TinhTrang == 1))
            {
                ws.Cell(currentRow, 1).Value = currentRow - 2;
                ws.Cell(currentRow, 2).Value = t.Me;
                ws.Cell(currentRow, 3).Value = t.Mac;
                ws.Cell(currentRow, 4).Value = t.KichThuoc;
                ws.Cell(currentRow, 5).Value = t.SoThanhLoai1;
                ws.Cell(currentRow, 6).Value = t.KhoiLuongLoai1;
                ws.Cell(currentRow, 7).Value = t.SoThanhLoai2;
                ws.Cell(currentRow, 8).Value = t.KhoiLuongLoai2;
                ws.Cell(currentRow, 9).Value = t.SoThanhLoai2;
                ws.Cell(currentRow, 10).Value = t.KhoiLuongLoai2;
                ws.Cell(currentRow, 11).Value = t.SoThanhLoai3;
                ws.Cell(currentRow, 12).Value = t.KhoiLuongLoai3;
                ws.Cell(currentRow, 13).Value = t.TongKl;
                currentRow++;
            }



            // 4️⃣ Trả dữ liệu file (KHÔNG return File)
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return new ExportFileResult
            {
                Content = stream.ToArray(),
                FileName = $"BM.06-QT.05.11_Bien_ban_giao_nhan_phoi_nong_{DateTime.Now:yyyyMMdd_HHmm}.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            };
        }

        public async Task<ExportFileResult> ExportPdfAsync(
    DateOnly? NgaySX, int? Ca, string? Kip, int? Xuong, string? Me, Guid id)
        {
            var items = await _repo.GetAllAsync(NgaySX, Ca, Kip, Xuong, Me);
            var data = items.Where(x => x.TinhTrang == 1).ToList();

            // Lấy thông tin người ký từ phiếu đầu tiên (nếu có)
            string nguoiLapPhieu = "";
            string nguoiNhanCTD = "";
            string nguoiNhanQLCL = "";
            string chuKyNguoiLapPhieu = "";
            string chuKyNguoiCTD = "";
            string chuKyNguoiNhanQLCL = "";
            string phongBanNguoiLap = "";
            string phongBanNguoiCTD = "";
            string phongBanNguoiQLCL = "";
            string chucVuNguoiLap = "";
            string chucVuNguoiCTD = "";
            string chucVuNguoiQLCL = "";

            //var firstItem = data.FirstOrDefault();
            if (id != null)
            {
                // Lấy phiếu từ repository
                var phieu = await _repoPhieu.GetByIdAsync(id);

                if (phieu != null)
                {
                    // 1. Lấy danh sách phê duyệt + chữ ký (đã bao gồm thông tin đầy đủ)
                    var pheDuyets = await _pdservice.GetPheDuyetPhieuAsync(phieu.Idphieu);

                    if (pheDuyets != null && pheDuyets.Any())
                    {
                        // Người lập biểu (cấp duyệt = 0 - người tạo)
                        var nguoiLap = pheDuyets.FirstOrDefault(x => x.CapDuyet == 0);
                        if (nguoiLap != null)
                        {
                            nguoiLapPhieu = nguoiLap.HoVaTen ?? "";
                            chuKyNguoiLapPhieu = FormatChuKy(nguoiLap.ChuKy);
                            phongBanNguoiLap = nguoiLap.TenPhongBan ?? "";
                            chucVuNguoiLap = nguoiLap.TenViTri ?? "";
                        }

                        // Người CTD (cấp duyệt = 2)
                        var nguoiCTD = pheDuyets.FirstOrDefault(x => x.CapDuyet == 2);
                        if (nguoiCTD != null)
                        {
                            nguoiNhanCTD = nguoiCTD.HoVaTen ?? "";
                            chuKyNguoiCTD = FormatChuKy(nguoiCTD.ChuKy);
                            phongBanNguoiCTD = nguoiCTD.TenPhongBan ?? "";
                            chucVuNguoiCTD = nguoiCTD.TenViTri ?? "";
                        }

                        // Người QLCL (cấp duyệt = 1)
                        var nguoiQLCL = pheDuyets.FirstOrDefault(x => x.CapDuyet == 1);
                        if (nguoiQLCL != null)
                        {
                            nguoiNhanQLCL = nguoiQLCL.HoVaTen ?? "";
                            chuKyNguoiNhanQLCL = FormatChuKy(nguoiQLCL.ChuKy);
                            phongBanNguoiQLCL = nguoiQLCL.TenPhongBan ?? "";
                            chucVuNguoiQLCL = nguoiQLCL.TenViTri ?? "";
                        }
                    }
                }
            }

            // 1️⃣ Load HTML template
            var templatePath = Path.Combine(
                  _env.WebRootPath,
                "template_html",
                "BM.05-QT.05.11_Bien_ban_phoi_nong.html"
            );

            var html = await File.ReadAllTextAsync(templatePath);

            // 2️⃣ Build table rows
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
                  <td>{t.SoThanhLoai1}</td>
                  <td>{t.KhoiLuongLoai1}</td>
                  <td>{t.SoThanhLoai2}</td>
                  <td>{t.KhoiLuongLoai2}</td>
                  <td>{t.SoThanhLoai2}</td>
                  <td>{t.KhoiLuongLoai2}</td>
                  <td>{t.SoThanhLoai3}</td>
                  <td>{t.KhoiLuongLoai3}</td>
                  <td>{t.TongKl}</td>
                </tr>");
            }
            var tongKLChung = data.Sum(x => x.TongKl);

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

            // 3️⃣ Replace placeholder
            var logoUrl = _configuration.GetValue<string>("AppSettings:LogoUrl") ?? "https://report.hoaphatdungquat.vn/img/logoHP.png";

            html = html
                .Replace("{{LogoUrl}}", logoUrl)
                .Replace("{{NgaySX}}", NgaySX?.ToString("dd/MM/yyyy") ?? "")
                .Replace("{{Ca}}", Ca?.ToString() ?? "")
                .Replace("{{Kip}}", Ca?.ToString() ?? "")
                .Replace("{{MayDuc}}", Xuong.ToString() ?? "")
                .Replace("{{TongKhoiLuong}}", tongKLChung.ToString() ?? "")
                .Replace("{{TuGio}}", tuGio)
                .Replace("{{TuNgay}}", tuNgay)
                .Replace("{{DenGio}}", denGio)
                .Replace("{{DenNgay}}", denNgay)
                .Replace("{{Rows}}", rows.ToString())
                .Replace("{{NguoiLapPhieu}}", nguoiLapPhieu)
                .Replace("{{NguoiNhanCTD}}", nguoiNhanCTD)
                .Replace("{{NguoiNhanQLCL}}", nguoiNhanQLCL)
                .Replace("{{ChuKyNguoiLap}}", chuKyNguoiLapPhieu)
                .Replace("{{ChuKyNguoiNhanCTD}}", chuKyNguoiCTD)
                .Replace("{{ChuKyNguoiNhanQLCL}}", chuKyNguoiNhanQLCL)
                .Replace("{{BPNguoiLap}}", phongBanNguoiLap)
                .Replace("{{BPCTD}}", phongBanNguoiCTD)
                .Replace("{{BPQLCL}}", phongBanNguoiQLCL)
                .Replace("{{ChucVuNguoiLap}}", chucVuNguoiLap)
                .Replace("{{ChucVuCTD}}", chucVuNguoiCTD)
                .Replace("{{ChucVuQLCL}}", chucVuNguoiQLCL);

            // 4️⃣ Convert HTML → PDF
            var doc = new HtmlToPdfDocument()
            {
                GlobalSettings =
                {
                    PaperSize = PaperKind.A4,
                    Orientation = DinkToPdf.Orientation.Portrait
                },
                Objects =
                {
                    new ObjectSettings
                    {
                        HtmlContent = html,
                        WebSettings = { DefaultEncoding = "utf-8" }
                    }
                }
            };

            var pdfBytes = _pdfConverter.Convert(doc);

            return new ExportFileResult
            {
                Content = pdfBytes,
                FileName = $"BM.06-QT.05.11_Bien_ban_giao_nhan_phoi_nong_{DateTime.Now:yyyyMMdd_HHmm}.pdf",
                ContentType = "application/pdf"
            };
        }


        public async Task<ExportFileResult> PKH_ExportExcelAsync(DateOnly? NgaySX, DateOnly? TuNgay, DateOnly? DenNgay, int? Ca, string? Kip, int? Xuong, string? Me)
        {

            // Lấy dữ liệu từ BK PhoiThep
            var dataBkPhoi = await _repoBkPhoi.GetAllAsync(NgaySX, TuNgay, DenNgay, Ca, Kip, 1, Xuong);

            // 1️⃣ Query dữ liệu
            var query = await _repo.GetAllAsync(null, null, null, Xuong, null);

            // ==== 2. Đường dẫn đến template ====
            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "templates", "PKH_BM_PhoiNong.xlsx");
            if (!System.IO.File.Exists(templatePath))
                throw new FileNotFoundException($"Không tìm thấy file mẫu Excel: {templatePath}");

            // ==== 3. Tạo workbook từ file mẫu ====
            using var workbook = new ClosedXML.Excel.XLWorkbook(templatePath);
            var ws = workbook.Worksheet(1); // worksheet đầu tiên

            // ==== 4. Ghi dữ liệu bắt đầu từ dòng 6 ====
            int startRow = 6;
            int currentRow = startRow;

            foreach (var t in dataBkPhoi)
            {
                ws.Cell(currentRow, 1).Value = currentRow - 5;
                ws.Cell(currentRow, 2).Value = t.NgaySx.ToString("dd/MM/yyyy");
                ws.Cell(currentRow, 3).Value = t.Ca + t.Kip;
                ws.Cell(currentRow, 4).Value = t.Me;
                ws.Cell(currentRow, 5).Value = t.Mac;
                ws.Cell(currentRow, 6).Value = t.KichThuoc;
                ws.Cell(currentRow, 7).Value = t.MayDuc;
                ws.Cell(currentRow, 8).Value = "Cán" + t.MayDuc;
                ws.Cell(currentRow, 9).Value = t.LoaiChatLuong == 1 ? t.SoThanh : "";
                ws.Cell(currentRow, 10).Value = t.LoaiChatLuong == 1 ? t.TongKhoiLuog : "";
                ws.Cell(currentRow, 11).Value = t.LoaiChatLuong == 2 ? t.SoThanh : "";
                ws.Cell(currentRow, 12).Value = t.LoaiChatLuong == 2 ? t.TongKhoiLuog : "";
                ws.Cell(currentRow, 13).Value = t.LoaiChatLuong == 2 ? t.SoThanh : "";
                ws.Cell(currentRow, 14).Value = t.LoaiChatLuong == 2 ? t.TongKhoiLuog : "";
                ws.Cell(currentRow, 15).Value = t.LoaiChatLuong == 3 ? t.SoThanh : "";
                ws.Cell(currentRow, 16).Value = t.LoaiChatLuong == 3 ? t.TongKhoiLuog : "";
                ws.Cell(currentRow, 17).Value = t.SoThanh;
                ws.Cell(currentRow, 18).Value = t.TongKhoiLuog;
                ws.Cell(currentRow, 19).Value = t.GhiChu;
                // Tìm dữ liệu tương ứng từ CtdPhoiNong
                var ctdPhoi = query.Where(x => x.IdBkPhoiThep == t.Id);
                int sttCtd = 0;
                foreach (var item in ctdPhoi)
                {
                    if (item.Ca == t.Ca && item.NgaySx == t.NgaySx)
                    {
                        sttCtd = 1;
                    }
                    else
                    {
                        sttCtd = 2;
                    }
                    if (sttCtd == 1)
                    {
                        ws.Cell(currentRow, 20).Value = item.TinhTrang == 1 ? "Đã chốt" : "Chưa chốt";
                        ws.Cell(currentRow, 20).Style.Fill.BackgroundColor = item.TinhTrang == 1 ? XLColor.Green : XLColor.Yellow; // Màu nền (ví dụ: vàng nhạt)
                        ws.Cell(currentRow, 21).Value = item.TinhTrangQLCL == 1 ? "Đã xác nhận" : "Chưa xác nhận";
                        ws.Cell(currentRow, 21).Style.Fill.BackgroundColor = item.TinhTrangQLCL == 1 ? XLColor.Green : XLColor.Yellow; // Màu nền (ví dụ: vàng nhạt)
                        ws.Cell(currentRow, 22).Value = item.TinhTrangCTD == 1 ? "Đã xác nhận" : "Chưa xác nhận";
                        ws.Cell(currentRow, 22).Style.Fill.BackgroundColor = item.TinhTrangQLCL == 1 ? XLColor.Green : XLColor.Yellow;  // Màu nền (ví dụ: vàng nhạt)
                        ws.Cell(currentRow, 23).Value = item.NgaySx?.ToString("dd/MM/yyyy");
                        ws.Cell(currentRow, 24).Value = item.Ca;
                        ws.Cell(currentRow, 25).Value = item.TongSt;
                        ws.Cell(currentRow, 26).Value = item.TongKl;
                    }
                    else if (sttCtd == 2)
                    {
                        ws.Cell(currentRow, 27).Value = item.TinhTrang == 1 ? "Đã chốt" : "Chưa chốt";
                        ws.Cell(currentRow, 27).Style.Fill.BackgroundColor = item.TinhTrang == 1 ? XLColor.Green : XLColor.Yellow; // Màu nền (ví dụ: vàng nhạt)
                        ws.Cell(currentRow, 28).Value = item.TinhTrangQLCL == 1 ? "Đã xác nhận" : "Chưa xác nhận";
                        ws.Cell(currentRow, 28).Style.Fill.BackgroundColor = item.TinhTrang == 1 ? XLColor.Green : XLColor.Yellow; // Màu nền (ví dụ: vàng nhạt)
                        ws.Cell(currentRow, 29).Value = item.TinhTrangCTD == 1 ? "Đã xác nhận" : "Chưa xác nhận";
                        ws.Cell(currentRow, 29).Style.Fill.BackgroundColor = item.TinhTrang == 1 ? XLColor.Green : XLColor.Yellow; // Màu nền (ví dụ: vàng nhạt)
                        ws.Cell(currentRow, 30).Value = item.NgaySx?.ToString("dd/MM/yyyy");
                        ws.Cell(currentRow, 31).Value = item.Ca;
                        ws.Cell(currentRow, 32).Value = item.TongSt;
                        ws.Cell(currentRow, 33).Value = item.TongKl;
                    }
                    // sttCtd++;
                }
                currentRow++;
            }

            // 4️⃣ Trả dữ liệu file (KHÔNG return File)
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return new ExportFileResult
            {
                Content = stream.ToArray(),
                FileName = $"TongHop_Bien_ban_giao_nhan_phoi_nong_{DateTime.Now:yyyyMMdd_HHmm}.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            };
        }

    }
}
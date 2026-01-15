using ClosedXML.Excel;
using dataproduct.api.DTOs;
using dataproduct.api.DTOs.Export;
using dataproduct.api.Models;
using dataproduct.api.Repositories;
using DinkToPdf;
using DinkToPdf.Contracts;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Drawing.Printing;
using System.Text;
using PaperKind = DinkToPdf.PaperKind;

namespace dataproduct.api.Services
{
    public class CtdPhoiNongService
    {
        private readonly ICtdPhoiNongRepository _repo;
        private readonly IConverter _pdfConverter;
        private readonly IWebHostEnvironment _env;
        private readonly IBKPhoiThepRepository _repoBkPhoi;

        public CtdPhoiNongService(ICtdPhoiNongRepository repo, IConverter pdfConverter, IWebHostEnvironment env, IBKPhoiThepRepository repoBkPhoi)
        {
            _repo = repo;
            _pdfConverter = pdfConverter;
            _env = env;
            _repoBkPhoi = repoBkPhoi;
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

        public Task<int> UpdateStatusDone(DateOnly? NgaySX, int? Ca, string? Kip, int? Xuong, string? Me)
        {
            return _repo.UpdateStatusDone(NgaySX, Ca, Kip, Xuong, Me);
        }

        public Task<IEnumerable<CtdPhoiNong>> GetByPhieuIdAsync(Guid phieuId)
        {
            return _repo.GetByPhieuIdAsync(phieuId);
        }

        public Task<(int Created, int Updated)> UpsertListAsync(List<CtdPhoiNong> entities)
        {
            return _repo.UpsertListAsync(entities);
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
    DateOnly? NgaySX, int? Ca, string? Kip, int? Xuong, string? Me)
        {
            var items = await _repo.GetAllAsync(NgaySX, Ca, Kip, Xuong, Me);
            var data = items.Where(x => x.TinhTrang == 1).ToList();

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

            // 3️⃣ Replace placeholder
            html = html
                .Replace("{{NgaySX}}", NgaySX?.ToString("dd/MM/yyyy") ?? "")
                .Replace("{{Ca}}", Ca?.ToString() ?? "")
                .Replace("{{Kip}}", Kip ?? "")
                .Replace("{{Rows}}", rows.ToString());

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


        public async Task<ExportFileResult> PKH_ExportExcelAsync(DateOnly? NgaySX, int? Ca, string? Kip, int? Xuong, string? Me)
        {

            // Lấy dữ liệu từ BK PhoiThep
            var dataBkPhoi = await _repoBkPhoi.GetAllAsync(NgaySX, Ca, Kip, 1, Xuong);

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
                ws.Cell(currentRow, 3).Value = t.Ca;
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
                int sttCtd = 1;
                foreach (var item in ctdPhoi)
                {
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
                    sttCtd++;
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
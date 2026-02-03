using dataproduct.api.Models;
using dataproduct.api.Repositories;
using System;
using DinkToPdf;
using DinkToPdf.Contracts;
using dataproduct.api.DTOs.Export;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Text;
using dataproduct.api.DTOs.CTD_Dto;

namespace dataproduct.api.Services
{
    public class BMDucCTDService
    {
        private readonly ICtdBMDucCTDRepository _repo;
        private readonly IConverter _pdfConverter;
        private readonly IWebHostEnvironment _env;

        public BMDucCTDService(ICtdBMDucCTDRepository repo, IConverter pdfConverter, IWebHostEnvironment env)
        {
            _repo = repo;
            _pdfConverter = pdfConverter;
            _env = env;
        }
        public async Task<List<SanLuongPhoiDto>> GetByKipNgayAsync( string ca, string kip,DateTime ngaySX)
        {
            return await _repo.GetSanLuongPhoiAsync(ca,kip, ngaySX);
        }
        public async Task<List<PhoinhapkhoDto>> GetPhoiNhapKhoAsync(string ca,string kip,DateTime ngaySX, int mayduc)
        {
            return await _repo.GetPhoiNhapKhoAsync(ca, kip, ngaySX,mayduc);
        }


        public async Task<ExportFileResult> ExportPdfSanLuongAsync(DateOnly? NgaySX, int? Ca, string? Kip, Guid? idPhieu, List<PheDuyetDto> pheDuyets)
        {

            if (!NgaySX.HasValue || !Ca.HasValue || string.IsNullOrEmpty(Kip))
                throw new ArgumentException("Thiếu tham số xuất PDF");

            var items = await _repo.GetSanLuongPhoiAsync(
                Ca.Value.ToString(),
                Kip,
                NgaySX.Value.ToDateTime(TimeOnly.MinValue)
            );

            var data = items.ToList();

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

    }
}

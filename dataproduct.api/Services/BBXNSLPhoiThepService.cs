using ClosedXML.Excel;
using ClosedXML.Excel.Drawings;
using dataproduct.api.DTOs.CTD_Dto;
using dataproduct.api.DTOs.Export;
using dataproduct.api.Repositories;
using dataproduct.api.Utils;
using System.Text.Json;

namespace dataproduct.api.Services
{
    public class BBXNSLPhoiThepService
    {
        private readonly ICtdBMDucCTDRepository _repo;
        private readonly BMDucCTDService _service;
        private readonly IPhieuRepository _repoPhieu;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly PheDuyetService _pheDuyetService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly BmConfigService _bmConfig;

        public BBXNSLPhoiThepService(
            ICtdBMDucCTDRepository repo,
            BMDucCTDService service,
            IPhieuRepository repoPhieu,
            IWebHostEnvironment env,
            IConfiguration configuration,
            PheDuyetService pheDuyetService,
            IHttpClientFactory httpClientFactory,
            BmConfigService bmConfig)
        {
            _repo = repo;
            _repoPhieu = repoPhieu;
            _env = env;
            _configuration = configuration;
            _pheDuyetService = pheDuyetService;
            _httpClientFactory = httpClientFactory;
            _service = service;
            _bmConfig = bmConfig;
        }

        public async Task<ExportFileResult> ExportChiTietExcelAsync(Guid phieuId)
        {
            var phieu = await _repoPhieu.GetByIdAsync(phieuId);
            if (phieu == null)
                throw new ArgumentException($"Không tìm thấy phiếu với ID: {phieuId}");

            var data = await _repo.GetSanLuongPhoiChiTietAsync(
                ca: phieu.Ca ?? 1,
                kip: phieu.Kip ?? "",
                ngaySX: phieu.NgaySX!.Value.ToDateTime(TimeOnly.MinValue),
                idPhieu: phieu.Idphieu
            );

            if (!data.Any())
                throw new ArgumentException("Không có dữ liệu sản lượng phôi để xuất Excel");

            var pheDuyets = await _pheDuyetService.GetPheDuyetPhieuAsync(phieuId) ?? new List<PheDuyetDto>();
            var xuongDuc = pheDuyets.FirstOrDefault(x => x.CapDuyet == 1 && x.TinhTrang == 1);
            var qlcl = pheDuyets.FirstOrDefault(x => x.CapDuyet == 0 && x.TinhTrang == 1);
            var khoPhoi = pheDuyets.FirstOrDefault(x => x.CapDuyet == 2 && x.TinhTrang == 1);

            // Logo
            var _logoPath = Path.Combine(_env.WebRootPath, "imgs", "LogoPDF.png");
            byte[]? logoBytes = File.Exists(_logoPath) ? await File.ReadAllBytesAsync(_logoPath) : null;

            // BM header từ HTML template
            var htmlPath = Path.Combine(_env.WebRootPath, "template_html",
                "BM.11-QT.05.11_Bien_ban_xac_nhan_san_luong_phoi.html");
            var bmHeaderText = await HtmlTemplateHelper.GetBmHeaderTextAsync(htmlPath);

            var ca = phieu.Ca ?? 1;
            var kip = phieu.Kip ?? "";
            var ngaySX = phieu.NgaySX ?? DateOnly.FromDateTime(DateTime.Today);
            var mayDuc = phieu.MayDuc?.ToString() ?? "";

            // ── BUILD EXCEL ──────────────────────────────────────────────────
            const int COLS = 13;
            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("SanLuongPhoi");

            ws.Style.Font.FontName = "Times New Roman";
            ws.Style.Font.FontSize = 11;

            ws.Column(1).Width = 18; // Kíp - ngày
            ws.Column(2).Width = 12; // Mác thép
            ws.Column(3).Width = 11; // Kích thước
            ws.Column(4).Width = 8; // St Loại I
            ws.Column(5).Width = 12; // Kl Loại I
            ws.Column(6).Width = 8; // St Phôi ngắn
            ws.Column(7).Width = 12; // Kl Phôi ngắn
            ws.Column(8).Width = 8; // St Loại II
            ws.Column(9).Width = 12; // Kl Loại II
            ws.Column(10).Width = 8; // St Loại III
            ws.Column(11).Width = 12; // Kl Loại III
            ws.Column(12).Width = 10; // Tổng số thanh
            ws.Column(13).Width = 13; // Tổng khối lượng

            int row = 1;

            // ── HEADER: Logo trái | BM code phải (rows 1-3) ──────────────────
            ws.Row(row).Height = 36;
            if (logoBytes != null)
            {
                var fmt = XLPictureFormat.Png;
                using var logoMs = new MemoryStream(logoBytes);
                ws.AddPicture(logoMs, fmt).MoveTo(ws.Cell(row, 1)).Scale(0.38);
            }

            ws.Cell(row, 9).Value = bmHeaderText;
            ws.Range(row, 9, row + 2, COLS).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(11)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                .Alignment.SetWrapText(true);
            row++;

            ws.Row(row).Height = 16;
            ws.Cell(row, 1).Value = "CÔNG TY CỔ PHẦN THÉP";
            ws.Range(row, 1, row, 6).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(11)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
            row++;

            ws.Row(row).Height = 16;
            ws.Cell(row, 1).Value = "HÒA PHÁT DUNG QUẤT";
            ws.Range(row, 1, row, 6).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(11)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
            row++;

            // ── TITLE ─────────────────────────────────────────────────────────
            ws.Cell(row, 1).Value = "BIÊN BẢN XÁC NHẬN SẢN LƯỢNG PHÔI THÉP";
            ws.Range(row, 1, row, COLS).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            ws.Row(row).Height = 28;
            row++;

            // ── SUB-TITLE ─────────────────────────────────────────────────────
            ws.Cell(row, 1).Value = $"Máy đúc: {mayDuc}   Kíp: {ca}{kip}   Ngày: {ngaySX:dd/MM/yyyy}";
            ws.Range(row, 1, row, COLS).Merge().Style
                .Font.SetFontSize(11)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            ws.Row(row).Height = 18;
            row++;

            // ── NGƯỜI THAM GIA ────────────────────────────────────────────────
            ws.Cell(row, 1).Value = "Chúng tôi gồm:";
            ws.Range(row, 1, row, COLS).Merge();
            ws.Row(row).Height = 16;
            row++;

            void WriteNguoiRow(string stt, string nguoi, string chucVu, string boPhan)
            {
                ws.Cell(row, 1).Value = $"{stt}. Ông/bà: {nguoi}   Chức vụ: {chucVu}   BP: {boPhan}";
                ws.Range(row, 1, row, COLS).Merge();
                ws.Row(row).Height = 16;
                row++;
            }

            WriteNguoiRow("1", xuongDuc?.HoVaTen ?? "", xuongDuc?.TenViTri ?? "", xuongDuc?.TenPhongBan ?? "");
            WriteNguoiRow("2", qlcl?.HoVaTen ?? "", qlcl?.TenViTri ?? "", qlcl?.TenPhongBan ?? "");
            WriteNguoiRow("3", khoPhoi?.HoVaTen ?? "", khoPhoi?.TenViTri ?? "", khoPhoi?.TenPhongBan ?? "");

            ws.Cell(row, 1).Value = "Cùng nhau thống nhất lập \"Biên bản xác nhận sản lượng phôi thép\" chi tiết như sau:";
            ws.Range(row, 1, row, COLS).Merge();
            ws.Row(row).Height = 16;
            row++;

            // ── TABLE HEADER (2 rows, mirror HTML colspan/rowspan) ────────────
            int thRow = row;

            void MakeHeaderCell(int r, int c1, int c2, string text, bool rowspan2 = false)
            {
                ws.Cell(r, c1).Value = text;
                var rng = rowspan2
                    ? ws.Range(r, c1, r + 1, c2)
                    : ws.Range(r, c1, r, c2);
                if (c1 != c2 || rowspan2) rng.Merge();
                rng.Style
                    .Font.SetBold(true)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                    .Alignment.SetWrapText(true)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#f0f0f0"))
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                    .Border.SetInsideBorder(XLBorderStyleValues.Thin);
            }

            // Row 1: rowspan=2
            MakeHeaderCell(thRow, 1, 1, "Kíp - ngày", rowspan2: true);
            MakeHeaderCell(thRow, 2, 2, "Mác thép", rowspan2: true);
            MakeHeaderCell(thRow, 3, 3, "Kích thước", rowspan2: true);
            // colspan=2 groups
            MakeHeaderCell(thRow, 4, 5, "Loại I");
            MakeHeaderCell(thRow, 6, 7, "Phôi ngắn - dài\n(11m - 9m - 6m)");
            MakeHeaderCell(thRow, 8, 9, "Loại II");
            MakeHeaderCell(thRow, 10, 11, "Loại III");
            // rowspan=2 cuối
            MakeHeaderCell(thRow, 12, 12, "Tổng số\nthanh", rowspan2: true);
            MakeHeaderCell(thRow, 13, 13, "Tổng khối\nlượng (kg)", rowspan2: true);

            ws.Row(thRow).Height = 22;
            row++;

            // Row 2: Số thanh / Khối lượng cho 4 nhóm (cols 4-11)
            int[] subCols = { 4, 5, 6, 7, 8, 9, 10, 11 };
            string[] subLabels = { "Số\nthanh", "Khối lượng\n(kg)" };
            for (int i = 0; i < subCols.Length; i++)
            {
                ws.Cell(row, subCols[i]).Value = subLabels[i % 2];
                ws.Cell(row, subCols[i]).Style
                    .Font.SetBold(true)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                    .Alignment.SetWrapText(true)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#f0f0f0"))
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                    .Border.SetInsideBorder(XLBorderStyleValues.Thin);
            }
            ws.Row(row).Height = 22;
            row++;

            // ── DATA ROWS ─────────────────────────────────────────────────────
            int dataStart = row;
            int tongStLoai1 = 0, tongStPhoiNgan = 0, tongStLoai2 = 0, tongStLoai3 = 0, tongSoThanh = 0;
            decimal tongKlLoai1 = 0, tongKlPhoiNgan = 0, tongKlLoai2 = 0, tongKlLoai3 = 0, tongKhoiLuong = 0;

            foreach (var item in data)
            {
                ws.Cell(row, 1).Value = item.KipNgay ?? "";
                ws.Cell(row, 2).Value = item.MacThep ?? "";
                ws.Cell(row, 3).Value = item.KichThuoc ?? "";
                ws.Cell(row, 4).Value = item.StLoai1.HasValue ? (XLCellValue)item.StLoai1.Value : "";
                ws.Cell(row, 5).Value = item.KlLoai1.HasValue ? (XLCellValue)item.KlLoai1.Value : "";
                ws.Cell(row, 6).Value = item.StPhoiNgan.HasValue ? (XLCellValue)item.StPhoiNgan.Value : "";
                ws.Cell(row, 7).Value = item.KlPhoiNgan.HasValue ? (XLCellValue)item.KlPhoiNgan.Value : "";
                ws.Cell(row, 8).Value = item.StLoai2.HasValue ? (XLCellValue)item.StLoai2.Value : "";
                ws.Cell(row, 9).Value = item.KlLoai2.HasValue ? (XLCellValue)item.KlLoai2.Value : "";
                ws.Cell(row, 10).Value = item.StLoai3.HasValue ? (XLCellValue)item.StLoai3.Value : "";
                ws.Cell(row, 11).Value = item.KlLoai3.HasValue ? (XLCellValue)item.KlLoai3.Value : "";
                ws.Cell(row, 12).Value = item.TongSoThanh.HasValue ? (XLCellValue)item.TongSoThanh.Value : "";
                ws.Cell(row, 13).Value = item.TongKhoiLuong.HasValue ? (XLCellValue)item.TongKhoiLuong.Value : "";

                ws.Range(row, 1, row, COLS).Style
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                    .Border.SetInsideBorder(XLBorderStyleValues.Thin);

                ws.Cell(row, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);
                ws.Cell(row, 2).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);
                ws.Row(row).Height = 20;

                tongStLoai1 += item.StLoai1 ?? 0;
                tongKlLoai1 += item.KlLoai1 ?? 0;
                tongStPhoiNgan += item.StPhoiNgan ?? 0;
                tongKlPhoiNgan += item.KlPhoiNgan ?? 0;
                tongStLoai2 += item.StLoai2 ?? 0;
                tongKlLoai2 += item.KlLoai2 ?? 0;
                tongStLoai3 += item.StLoai3 ?? 0;
                tongKlLoai3 += item.KlLoai3 ?? 0;
                tongSoThanh += item.TongSoThanh ?? 0;
                tongKhoiLuong += item.TongKhoiLuong ?? 0;
                row++;
            }

            // Format số khối lượng cho data rows
            int dataEnd = row - 1;
            if (dataStart <= dataEnd)
            {
                foreach (int col in new[] { 5, 7, 9, 11, 13 })
                    ws.Range(dataStart, col, dataEnd, col).Style.NumberFormat.Format = "#,##0";
            }

            // ── TOTAL ROW ─────────────────────────────────────────────────────
            ws.Cell(row, 1).Value = "Tổng";
            ws.Range(row, 1, row, 3).Merge().Style
                .Font.SetBold(true)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#f5f5f5"))
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                .Border.SetInsideBorder(XLBorderStyleValues.Thin);

            ws.Cell(row, 4).Value = tongStLoai1;
            ws.Cell(row, 5).Value = tongKlLoai1;
            ws.Cell(row, 6).Value = tongStPhoiNgan;
            ws.Cell(row, 7).Value = tongKlPhoiNgan;
            ws.Cell(row, 8).Value = tongStLoai2;
            ws.Cell(row, 9).Value = tongKlLoai2;
            ws.Cell(row, 10).Value = tongStLoai3;
            ws.Cell(row, 11).Value = tongKlLoai3;
            ws.Cell(row, 12).Value = tongSoThanh;
            ws.Cell(row, 13).Value = tongKhoiLuong;

            ws.Range(row, 4, row, COLS).Style
                .Font.SetBold(true)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#f5f5f5"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                .Border.SetInsideBorder(XLBorderStyleValues.Thin);

            foreach (int col in new[] { 5, 7, 9, 11, 13 })
                ws.Cell(row, col).Style.NumberFormat.Format = "#,##0";

            ws.Row(row).Height = 20;
            row += 2;

            // ── NOTE ──────────────────────────────────────────────────────────
            ws.Cell(row, 1).Value = "Biên bản này được lập thành 03 (ba) bản có giá trị như nhau, mỗi bên giữ 01 (một) bản để làm căn cứ.";
            ws.Range(row, 1, row, COLS).Merge().Style.Font.SetFontSize(11);
            ws.Row(row).Height = 16;
            row += 2;

            // ── CHỮ KÝ: Kho phôi | P.QLCL | Xưởng Đúc ───────────────────────
            ws.Cell(row, 1).Value = "Kho phôi NM.HRC1";
            ws.Range(row, 1, row, 4).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(11)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(row, 5).Value = "P.QLCL";
            ws.Range(row, 5, row, 9).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(11)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(row, 10).Value = "Xưởng Đúc phôi vuông NM.HRC1";
            ws.Range(row, 10, row, COLS).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(11)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Row(row).Height = 16;
            row++;

            ws.Row(row).Height = 60; // vùng chữ ký
            row++;

            ws.Cell(row, 1).Value = khoPhoi?.HoVaTen ?? "";
            ws.Range(row, 1, row, 4).Merge().Style
                .Font.SetFontSize(11)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(row, 5).Value = qlcl?.HoVaTen ?? "";
            ws.Range(row, 5, row, 9).Merge().Style
                .Font.SetFontSize(11)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(row, 10).Value = xuongDuc?.HoVaTen ?? "";
            ws.Range(row, 10, row, COLS).Merge().Style
                .Font.SetFontSize(11)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Row(row).Height = 16;

            // ── PAGE SETUP: A4 Landscape (giống PDF) ──────────────────────────
            ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
            ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            ws.PageSetup.Margins.Left = 1.3;
            ws.PageSetup.Margins.Right = 0.8;
            ws.PageSetup.Margins.Top = 0.8;
            ws.PageSetup.Margins.Bottom = 0.6;
            ws.PageSetup.FitToPages(1, 0);

            using var ms = new MemoryStream();
            wb.SaveAs(ms);

            return new ExportFileResult
            {
                Content = ms.ToArray(),
                FileName = $"BM.11-QT.05.11_SanLuongPhoi_{ngaySX:yyyyMMdd}_Ca{ca}{kip}_{DateTime.Now:HHmmss}.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            };
        }
        public async Task<ExportFileResult> ExportExcelTongHopAsync(DateOnly? fromDate, DateOnly? toDate)
        {
            var phieuList = await _repo.GetDataSanLuongPhoiAsync(fromDate, toDate);
            var data      = BuildTongHopRows(phieuList);

            // ── BUILD EXCEL ──────────────────────────────────────────────────
            // 19 cột: STT | NgàySX | Kíp | Ca | MacThep | KichThuoc
            //       | [Loại I: St/Kl] | [Phôi ngắn: St/Kl] | [Loại II: St/Kl] | [Loại III: St/Kl]
            //       | TongSt | TongKl | (blank) | SoPhieu | TinhTrang
            const int COLS = 19;
            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("TongHopSanLuongPhoi");

            ws.Style.Font.FontName = "Times New Roman";
            ws.Style.Font.FontSize = 11;

            ws.Column(1).Width  = 5;   // STT
            ws.Column(2).Width  = 13;  // Ngày SX
            ws.Column(3).Width  = 6;   // Kíp
            ws.Column(4).Width  = 5;   // Ca
            ws.Column(5).Width  = 12;  // Mác thép
            ws.Column(6).Width  = 11;  // Kích thước
            ws.Column(7).Width  = 8;   // St Loại I
            ws.Column(8).Width  = 11;  // Kl Loại I
            ws.Column(9).Width  = 8;   // St Phôi ngắn
            ws.Column(10).Width = 11;  // Kl Phôi ngắn
            ws.Column(11).Width = 8;   // St Loại II
            ws.Column(12).Width = 11;  // Kl Loại II
            ws.Column(13).Width = 8;   // St Loại III
            ws.Column(14).Width = 11;  // Kl Loại III
            ws.Column(15).Width = 10;  // Tổng số thanh
            ws.Column(16).Width = 13;  // Tổng KL
            ws.Column(17).Width = 2;   // blank
            ws.Column(18).Width = 16;  // Số phiếu
            ws.Column(19).Width = 14;  // Trạng thái

            int row = 1;

            // ── TITLE ─────────────────────────────────────────────────────────
            ws.Cell(row, 1).Value = "TỔNG HỢP SẢN LƯỢNG PHÔI THÉP";
            ws.Range(row, 1, row, COLS).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            ws.Row(row).Height = 28;
            row++;

            // ── DATE RANGE ────────────────────────────────────────────────────
            ws.Cell(row, 1).Value = $"Từ ngày: {fromDate:dd/MM/yyyy}   Đến ngày: {toDate:dd/MM/yyyy}";
            ws.Range(row, 1, row, COLS).Merge().Style
                .Font.SetFontSize(11)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            ws.Row(row).Height = 18;
            row++;

            // ── TABLE HEADER (2 rows) ─────────────────────────────────────────
            int thRow = row;

            void MakeHdr(int r, int c1, int c2, string text, bool rowspan2 = false)
            {
                ws.Cell(r, c1).Value = text;
                var rng = rowspan2 ? ws.Range(r, c1, r + 1, c2) : ws.Range(r, c1, r, c2);
                if (c1 != c2 || rowspan2) rng.Merge();
                rng.Style
                    .Font.SetBold(true)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                    .Alignment.SetWrapText(true)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#d9e1f2"))
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                    .Border.SetInsideBorder(XLBorderStyleValues.Thin);
            }

            // Row 1: các cột rowspan=2
            MakeHdr(thRow, 1,  1,  "STT",         rowspan2: true);
            MakeHdr(thRow, 2,  2,  "Ngày SX",     rowspan2: true);
            MakeHdr(thRow, 3,  3,  "Kíp",         rowspan2: true);
            MakeHdr(thRow, 4,  4,  "Ca",          rowspan2: true);
            MakeHdr(thRow, 5,  5,  "Mác thép",    rowspan2: true);
            MakeHdr(thRow, 6,  6,  "Kích thước",  rowspan2: true);
            // colspan=2 groups
            MakeHdr(thRow, 7,  8,  "Loại I");
            MakeHdr(thRow, 9,  10, "Phôi ngắn - dài\n(11m - 9m - 6m)");
            MakeHdr(thRow, 11, 12, "Loại II");
            MakeHdr(thRow, 13, 14, "Loại III");
            // rowspan=2 cuối
            MakeHdr(thRow, 15, 15, "Tổng số\nthanh",      rowspan2: true);
            MakeHdr(thRow, 16, 16, "Tổng khối\nlượng (kg)", rowspan2: true);
            MakeHdr(thRow, 17, 17, "",                    rowspan2: true);
            MakeHdr(thRow, 18, 18, "Số phiếu",            rowspan2: true);
            MakeHdr(thRow, 19, 19, "Trạng thái",          rowspan2: true);

            ws.Row(thRow).Height = 22;
            row++;

            // Row 2: Số thanh / Khối lượng cho 4 nhóm
            int[] grpCols   = { 7, 8, 9, 10, 11, 12, 13, 14 };
            string[] grpLbl = { "Số\nthanh", "KL\n(kg)" };
            foreach (var (c, i) in grpCols.Select((c, i) => (c, i)))
            {
                ws.Cell(row, c).Value = grpLbl[i % 2];
                ws.Cell(row, c).Style
                    .Font.SetBold(true)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                    .Alignment.SetWrapText(true)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#d9e1f2"))
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                    .Border.SetInsideBorder(XLBorderStyleValues.Thin);
            }
            ws.Row(row).Height = 22;

            // freeze header tại đây
            ws.SheetView.FreezeRows(row);
            row++;

            // ── DATA ROWS ─────────────────────────────────────────────────────
            int dataStart = row;
            int stt = 1;
            int totSt1 = 0, totStN = 0, totSt2 = 0, totSt3 = 0, totSoThanh = 0;
            decimal totKl1 = 0, totKlN = 0, totKl2 = 0, totKl3 = 0, totKhoiLuong = 0;

            foreach (var item in data)
            {
                ws.Cell(row, 1).Value  = stt++;
                ws.Cell(row, 2).Value  = item.NgaySX?.ToDateTime(TimeOnly.MinValue);
                ws.Cell(row, 3).Value  = item.Kip ?? "";
                ws.Cell(row, 4).Value  = item.Ca.HasValue ? (XLCellValue)item.Ca.Value : "";
                ws.Cell(row, 5).Value  = item.MacThep ?? "";
                ws.Cell(row, 6).Value  = item.KichThuoc ?? "";

                ws.Cell(row, 7).Value  = item.StLoai1.HasValue    ? (XLCellValue)item.StLoai1.Value    : "";
                ws.Cell(row, 8).Value  = item.KlLoai1.HasValue    ? (XLCellValue)item.KlLoai1.Value    : "";
                ws.Cell(row, 9).Value  = item.StPhoiNgan.HasValue ? (XLCellValue)item.StPhoiNgan.Value : "";
                ws.Cell(row, 10).Value = item.KlPhoiNgan.HasValue ? (XLCellValue)item.KlPhoiNgan.Value : "";
                ws.Cell(row, 11).Value = item.StLoai2.HasValue    ? (XLCellValue)item.StLoai2.Value    : "";
                ws.Cell(row, 12).Value = item.KlLoai2.HasValue    ? (XLCellValue)item.KlLoai2.Value    : "";
                ws.Cell(row, 13).Value = item.StLoai3.HasValue    ? (XLCellValue)item.StLoai3.Value    : "";
                ws.Cell(row, 14).Value = item.KlLoai3.HasValue    ? (XLCellValue)item.KlLoai3.Value    : "";
                ws.Cell(row, 15).Value = item.TongSoThanh.HasValue   ? (XLCellValue)item.TongSoThanh.Value   : "";
                ws.Cell(row, 16).Value = item.TongKhoiLuong.HasValue ? (XLCellValue)item.TongKhoiLuong.Value : "";
                ws.Cell(row, 17).Value = "";
                ws.Cell(row, 18).Value = item.SoPhieu ?? "";

                // Trạng thái + màu
                var cellTT = ws.Cell(row, 19);
                switch (item.TinhTrang)
                {
                    case 0: cellTT.Value = "Đang lưu";       cellTT.Style.Fill.BackgroundColor = XLColor.LightGray;    break;
                    case 1: cellTT.Value = "Đã gửi";         cellTT.Style.Fill.BackgroundColor = XLColor.LightBlue;    break;
                    case 2: cellTT.Value = "Hoàn thành";     cellTT.Style.Fill.BackgroundColor = XLColor.LightGreen;   break;
                    case 3: cellTT.Value = "Đã thu hồi";     cellTT.Style.Fill.BackgroundColor = XLColor.Orange;       break;
                    case 4:
                        cellTT.Value = "Không xác nhận";
                        cellTT.Style.Fill.BackgroundColor = XLColor.Red;
                        cellTT.Style.Font.FontColor = XLColor.White;
                        break;
                    case 5:
                        cellTT.Value = "Đã chốt";
                        cellTT.Style.Fill.BackgroundColor = XLColor.DarkGreen;
                        cellTT.Style.Font.FontColor = XLColor.White;
                        break;
                    case 6: cellTT.Value = "Đang phê duyệt"; cellTT.Style.Fill.BackgroundColor = XLColor.Yellow;       break;
                    case 7:
                        cellTT.Value = "Hiệu chỉnh";
                        cellTT.Style.Fill.BackgroundColor = XLColor.MediumPurple;
                        cellTT.Style.Font.FontColor = XLColor.White;
                        break;
                    default: cellTT.Value = "Không xác định"; break;
                }

                ws.Range(row, 1, row, COLS).Style
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                    .Border.SetInsideBorder(XLBorderStyleValues.Thin);

                ws.Row(row).Height = 20;

                totSt1       += item.StLoai1    ?? 0;
                totKl1       += item.KlLoai1    ?? 0;
                totStN       += item.StPhoiNgan ?? 0;
                totKlN       += item.KlPhoiNgan ?? 0;
                totSt2       += item.StLoai2    ?? 0;
                totKl2       += item.KlLoai2    ?? 0;
                totSt3       += item.StLoai3    ?? 0;
                totKl3       += item.KlLoai3    ?? 0;
                totSoThanh   += item.TongSoThanh    ?? 0;
                totKhoiLuong += item.TongKhoiLuong  ?? 0;
                row++;
            }

            int dataEnd = row - 1;

            // ── FORMAT dữ liệu ────────────────────────────────────────────────
            if (dataStart <= dataEnd)
            {
                ws.Range(dataStart, 2, dataEnd, 2).Style.DateFormat.Format = "dd/MM/yyyy";

                foreach (int col in new[] { 8, 10, 12, 14, 16 })
                    ws.Range(dataStart, col, dataEnd, col).Style.NumberFormat.Format = "#,##0";

                // căn giữa: STT, Kíp, Ca
                ws.Range(dataStart, 1, dataEnd, 4).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                // căn phải: các cột số
                ws.Range(dataStart, 7, dataEnd, 16).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
            }

            // ── TOTAL ROW ─────────────────────────────────────────────────────
            ws.Cell(row, 1).Value = "TỔNG CỘNG";
            ws.Range(row, 1, row, 6).Merge().Style
                .Font.SetBold(true)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#f5f5f5"))
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                .Border.SetInsideBorder(XLBorderStyleValues.Thin);

            ws.Cell(row, 7).Value  = totSt1;
            ws.Cell(row, 8).Value  = totKl1;
            ws.Cell(row, 9).Value  = totStN;
            ws.Cell(row, 10).Value = totKlN;
            ws.Cell(row, 11).Value = totSt2;
            ws.Cell(row, 12).Value = totKl2;
            ws.Cell(row, 13).Value = totSt3;
            ws.Cell(row, 14).Value = totKl3;
            ws.Cell(row, 15).Value = totSoThanh;
            ws.Cell(row, 16).Value = totKhoiLuong;

            ws.Range(row, 7, row, 16).Style
                .Font.SetBold(true)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#f5f5f5"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                .Border.SetInsideBorder(XLBorderStyleValues.Thin);

            foreach (int col in new[] { 8, 10, 12, 14, 16 })
                ws.Cell(row, col).Style.NumberFormat.Format = "#,##0";

            ws.Row(row).Height = 22;

            // ── PAGE SETUP ────────────────────────────────────────────────────
            ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
            ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            ws.PageSetup.Margins.Left   = 1.3;
            ws.PageSetup.Margins.Right  = 0.8;
            ws.PageSetup.Margins.Top    = 0.8;
            ws.PageSetup.Margins.Bottom = 0.6;
            ws.PageSetup.FitToPages(1, 0);

            using var ms = new MemoryStream();
            wb.SaveAs(ms);

            return new ExportFileResult
            {
                Content     = ms.ToArray(),
                FileName    = $"TongHopSanLuongPhoi_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}_{DateTime.Now:HHmmss}.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            };
        }

        private List<BmSanLuongPhoiRow> BuildTongHopRows(List<dataproduct.api.Models.BmPhieu> phieuList)
        {
            var result  = new List<BmSanLuongPhoiRow>();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            foreach (var phieu in phieuList)
            {
                if (string.IsNullOrWhiteSpace(phieu.DataJson))
                    continue;

                BmPhieuSLPJson? json;
                try { json = JsonSerializer.Deserialize<BmPhieuSLPJson>(phieu.DataJson, options); }
                catch { continue; }

                if (json?.table1 == null) continue;

                foreach (var r in json.table1)
                {
                    result.Add(new BmSanLuongPhoiRow
                    {
                        SoPhieu       = phieu.SoPhieu,
                        NgaySX        = json.NgaySX,
                        Kip           = json.kip,
                        Ca            = json.ca,
                        MacThep       = r.macThep,
                        KichThuoc     = r.kichThuoc,
                        StLoai1       = r.stLoai1,
                        KlLoai1       = r.klLoai1,
                        StPhoiNgan    = r.stPhoiNgan,
                        KlPhoiNgan    = r.klPhoiNgan,
                        StLoai2       = r.stLoai2,
                        KlLoai2       = r.klLoai2,
                        StLoai3       = r.stLoai3,
                        KlLoai3       = r.klLoai3,
                        TongSoThanh   = r.tongSoThanh,
                        TongKhoiLuong = r.tongKhoiLuong,
                        TinhTrang     = phieu.TinhTrang
                    });
                }
            }

            return result;
        }
    }
}

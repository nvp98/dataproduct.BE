using ClosedXML.Excel;
using dataproduct.api.DTOs.Export;
using dataproduct.api.Models;
using dataproduct.api.ResponseModels;
using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Text;
using PaperKind = DinkToPdf.PaperKind;

namespace dataproduct.api.Services
{
    /// <summary>
    /// Xuất Excel/PDF cho 1 phiếu tiêu hao BOF cụ thể (mẫu HRC1_BB_NauLuyen_BOF.xlsx / HRC1_BB_NauLuyen.html) —
    /// mirror y hệt PhieuDetailExcelService (HRC2), chỉ giữ nhánh BOF (HRC1 chưa có LF/RH) và đổi field/khóa phụ liệu
    /// cho đúng model HRC1: PhuLieuID thay IDHeaderKey, KLGang thay KLGangLongCCT (KLGangLongCCT của HRC1 luôn NULL).
    /// Không cần bước "DataJson overrides" như HRC2 — HRC1_PhuLieu.IsManual/KLPhuGia_Manual đã là nguồn sự thật bền
    /// (ghi trực tiếp khi lưu phiếu ở DLNMHRC1Service.SaveHRC1ManualDataAsync), không cần đọc lại DataJson của phiếu.
    /// Không có bảng "Tồn silo" (STD_XUAT_NHAP_TON) cho HRC1 → phần footer luôn render rỗng (đúng hành vi mặc định
    /// của FooterConfig khi không có footerData/LuongTonLabels, giống hệt cách HRC2 xử lý khi thiếu dữ liệu XNT).
    /// </summary>
    public class Hrc1PhieuDetailExcelService
    {
        private readonly ProductFormContext _context;
        private readonly IConverter _pdfConverter;
        private readonly IWebHostEnvironment _env;
        private readonly PheDuyetService _pheDuyetService;

        private const int HeaderParentRow = 6;
        private const int HeaderChildRow = 7;
        private const int DataStartRow = 8;

        public Hrc1PhieuDetailExcelService(ProductFormContext context, PheDuyetService pheDuyetService, IConverter pdfConverter, IWebHostEnvironment env)
        {
            _context = context;
            _pdfConverter = pdfConverter;
            _env = env;
            _pheDuyetService = pheDuyetService;
        }

        public async Task<BmPhieu> GetBmPhieuByIdOrThrowAsync(Guid idPhieu)
        {
            var phieu = await _context.BmPhieus.FirstOrDefaultAsync(x => x.Idphieu == idPhieu);
            if (phieu == null) throw new Exception($"Không tìm thấy phiếu với IdPhieu='{idPhieu}'.");
            return phieu;
        }

        // -------------------------------------------------------
        // Export data query — mirror DLNMHRC1Repository.SearchThongKeApiAsync nhưng lọc đúng
        // 1 tổ hợp Ngày+Ca+Lò (không phân trang, không filter IsDelete/IsTrungMeThoi).
        // -------------------------------------------------------
        private async Task<(List<Hrc1PhuLieuHeaderTable> Headers, List<Hrc1ThongKeRow> Rows)> GetExportDataAsync(
            DateOnly ngay, int ca, int scope)
        {
            var items = await _context.Hrc1TieuHaoBofs
                .Where(x => !x.IsDeleted && x.NgaySanXuat == ngay && x.Ca == (byte)ca
                         && x.BieuMau == "BOF" && x.Scope == scope)
                .OrderBy(x => x.MeThoi)
                .AsNoTracking()
                .ToListAsync();

            var headers = await _context.Hrc1PhuLieuNms
                .Where(x => x.DangSuDung)
                .OrderBy(x => x.ThuTu ?? int.MaxValue).ThenBy(x => x.ID)
                .Select(x => new Hrc1PhuLieuHeaderTable { PhuLieuID = x.ID, TenPhuLieu = x.TenPhuLieu, ThuTu = x.ThuTu })
                .ToListAsync();

            var meIds = items.Select(x => x.ID).ToList();
            var plByMeId = meIds.Count > 0
                ? (await _context.Hrc1PhuLieus
                        .Where(x => meIds.Contains(x.MeID) && !x.IsDeleted && x.PhuLieuID.HasValue)
                        .ToListAsync())
                    .GroupBy(x => x.MeID)
                    .ToDictionary(g => g.Key, g => g.ToList())
                : new Dictionary<int, List<Hrc1PhuLieu>>();

            var rows = items.Select(b =>
            {
                var row = new Hrc1ThongKeRow { Data = MapData(b) };
                if (plByMeId.TryGetValue(b.ID, out var pls))
                {
                    row.Values = pls.Select(p => new Hrc1ThongKeValue
                    {
                        PhuLieuID = p.PhuLieuID!.Value,
                        KLPhuGia = (double?)p.KLPhuGia,
                        KLPhuGia_Manual = (double?)p.KLPhuGia_Manual,
                        IsManual = p.IsManual,
                        KLPhanBo = (double?)p.KLPhanBo,
                        TotalKLPhuGia = ComputeEffectiveTotal(p),
                    }).ToList();
                }
                return row;
            }).ToList();

            return (headers, rows);
        }

        private static double ComputeEffectiveTotal(Hrc1PhuLieu p)
        {
            var effective = p.IsManual ? (double)(p.KLPhuGia_Manual ?? 0) : (double)(p.KLPhuGia ?? 0);
            return effective + (double)(p.KLPhanBo ?? 0);
        }

        private static Hrc1TieuHaoBof_ResponseModel MapData(Hrc1TieuHaoBof b) => new Hrc1TieuHaoBof_ResponseModel
        {
            ID = b.ID,
            BieuMau = b.BieuMau,
            Scope = b.Scope,
            MeThoi = b.MeThoi,
            MacThep = b.MacThep,
            IsNM = b.IsNM,
            IsChuyenCa = b.IsChuyenCa,
            IsTrungMeThoi = b.IsTrungMeThoi,
            KLGang = b.KLGang,
            KLGangLongCCT = b.KLGangLongCCT,
            KLThepPhe = b.KLThepPhe,
            KLThepPheGang = b.KLThepPheGang,
            O2 = b.O2,
            N2 = b.N2,
            AR = b.AR,
            QueLayMau = b.QueLayMau,
            QueDoNhiet = b.QueDoNhiet,
            GhiChu = b.GhiChu,
            NgaySanXuat = b.NgaySanXuat,
            Ca = b.Ca,
            ThoiDiemBatDau = b.ThoiDiemBatDau,
            ThoiDiemKetThuc = b.ThoiDiemKetThuc,
        };

        // =========================================================
        // EXCEL
        // =========================================================

        public async Task<ExportFileResult> ExportExcelDetailAsync(DateOnly ngay, int ca, int scope, Guid idPhieu)
        {
            var phieu = await GetBmPhieuByIdOrThrowAsync(idPhieu);
            if (!phieu.NgaySX.HasValue || !phieu.Ca.HasValue)
                throw new InvalidOperationException("Phiếu thiếu NgaySX/Ca để export Excel.");

            var ngayPhieu = phieu.NgaySX.Value;
            var caPhieu = phieu.Ca.Value;
            var kipPhieu = phieu.Kip ?? "";
            var scopePhieu = phieu.Scope ?? scope;

            const string templateName = "HRC1_BB_NauLuyen_BOF";
            var templatePath = Path.Combine(_env.WebRootPath, "templates", $"{templateName}.xlsx");
            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"Không tìm thấy file mẫu Excel: {templatePath}");

            var (headers, rows) = await GetExportDataAsync(ngayPhieu, caPhieu, scopePhieu);
            var fileName = $"{templateName}_Ca{caPhieu}_{ngayPhieu:ddMMyyyy}.xlsx";

            // ClosedXML lỗi khi save workbook có drawing (logo) trực tiếp ra MemoryStream → save qua file tạm.
            var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.xlsx");
            try
            {
                using (var workbook = new XLWorkbook(templatePath))
                {
                    var ws = workbook.Worksheets.First();
                    await RenderBodyFromDbAsync(ws, headers, rows, scopePhieu, ngayPhieu: ngayPhieu, caPhieu: caPhieu, kip: kipPhieu, idPhieu: idPhieu);
                    workbook.SaveAs(tempPath);
                }

                var bytes = await File.ReadAllBytesAsync(tempPath);
                if (bytes.Length < 4 || bytes[0] != 'P' || bytes[1] != 'K')
                    throw new InvalidOperationException("File Excel xuất ra không hợp lệ.");
                using (var zip = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read))
                {
                    if (zip.GetEntry("[Content_Types].xml") == null)
                        throw new InvalidOperationException("File Excel xuất ra không hợp lệ (thiếu [Content_Types].xml).");
                }

                return new ExportFileResult
                {
                    Content = bytes,
                    FileName = fileName,
                    ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                };
            }
            finally
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
            }
        }

        private async Task RenderBodyFromDbAsync(IXLWorksheet ws,
            List<Hrc1PhuLieuHeaderTable> headers, List<Hrc1ThongKeRow> rows,
            int? scope, DateOnly? ngayPhieu, int? caPhieu, string kip, Guid? idPhieu)
        {
            int lastCol = ComputeLastCol(headers.Count);
            ws.Column(2).Width = 25;
            ws.Column(3).Width = 25;
            ws.Column(lastCol).Width = 25;

            ClearRowsFrom(ws, 4);
            UpdateHeaderRowMerges(ws, lastCol);
            RenderInfoRows(ws, rows, lastCol, scope, ngayPhieu, caPhieu, kip);

            int dataStartRow = DataStartRow;
            int dataEndRow = dataStartRow + rows.Count - 1;
            int totalRow = dataEndRow + 1;

            string? truongKipName = null, nguoiLapName = null;
            if (idPhieu.HasValue)
            {
                var pheDuyets = await _pheDuyetService.GetPheDuyetPhieuAsync(idPhieu.Value);
                truongKipName = pheDuyets.FirstOrDefault(x => x.CapDuyet == 1)?.HoVaTen;
                nguoiLapName = pheDuyets.FirstOrDefault(x => x.CapDuyet == 0)?.HoVaTen;
            }

            RenderColumnHeaders(ws, headers);
            RenderDataRows(ws, headers, rows, dataStartRow);
            RenderTotalRow(ws, totalRow, headers, rows);
            int tableLastRow = RenderFooter(ws, totalRow + 2, lastCol);
            RenderSignatureRow(ws, tableLastRow + 1, lastCol, truongKipName, nguoiLapName);

            ApplyUnifiedTableGridBorders(ws, HeaderParentRow, tableLastRow, lastCol);
        }

        // -------------------------------------------------------
        // Column position helpers — BOF: STT|MeThoi|MacThep|KLGangLong|KLThepPhe (5 cột) rồi phụ liệu từ col 6,
        // sau phụ liệu: Oxy|Nito|Ghi chú (3 cột).
        // -------------------------------------------------------
        private const int PhuLieuStartCol = 6;

        private static int ComputeLastCol(int dynamicCount) => (PhuLieuStartCol - 1) + dynamicCount + 3;

        private static void ClearRowsFrom(IXLWorksheet ws, int startRow)
        {
            var toUnmerge = ws.MergedRanges
                .Where(m => m.RangeAddress.LastAddress.RowNumber >= startRow)
                .ToList();
            foreach (var m in toUnmerge) m.Unmerge();

            var lastRow = ws.LastRowUsed()?.RowNumber() ?? (startRow - 1);
            for (int r = startRow; r <= lastRow; r++)
                ws.Row(r).Clear(XLClearOptions.Contents);
        }

        private static void UpdateHeaderRowMerges(IXLWorksheet ws, int lastCol)
        {
            var info = ws.Cell(1, 1).Value;

            var headerMerges = ws.MergedRanges
                .Where(m => m.RangeAddress.FirstAddress.RowNumber >= 1 && m.RangeAddress.LastAddress.RowNumber <= 3)
                .ToList();
            foreach (var m in headerMerges) m.Unmerge();

            ws.Range(1, 1, 3, lastCol).Merge();
            var cell = ws.Cell(1, 1);
            cell.Value = info;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
            cell.Style.Alignment.WrapText = true;
        }

        private static void RenderInfoRows(IXLWorksheet ws, List<Hrc1ThongKeRow> rows, int lastCol,
            int? scope, DateOnly? ngayPhieu, int? caPhieu, string kip)
        {
            var d = rows.FirstOrDefault()?.Data;
            var caValue = caPhieu ?? d?.Ca ?? 0;
            var rawDate = d?.NgaySanXuat;
            string ngayStr = ngayPhieu.HasValue ? ngayPhieu.Value.ToString("dd/MM/yyyy") : (rawDate?.ToString("dd/MM/yyyy") ?? "");

            string tenBm = $"BIÊN BẢN TIÊU HAO NẤU LUYỆN LÒ THỔI {scope}";

            ws.Range(4, 1, 4, lastCol).Merge();
            var c4 = ws.Cell(4, 1);
            c4.Value = tenBm;
            c4.Style.Font.Bold = true;
            c4.Style.Font.FontSize = 13;
            c4.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            c4.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            ws.Range(5, 1, 5, lastCol).Merge();
            var c5 = ws.Cell(5, 1);

            string gioBatDau, gioKetThuc, ngayKetThuc = ngayStr;
            if (caValue == 1)
            {
                gioBatDau = "08 giờ 00";
                gioKetThuc = "20 giờ 00";
            }
            else
            {
                gioBatDau = "20 giờ 00";
                gioKetThuc = "08 giờ 00";
                if (ngayPhieu.HasValue) ngayKetThuc = ngayPhieu.Value.AddDays(1).ToString("dd/MM/yyyy");
                else if (rawDate.HasValue) ngayKetThuc = rawDate.Value.AddDays(1).ToString("dd/MM/yyyy");
            }

            var kipSuffix = string.IsNullOrWhiteSpace(kip) ? "" : kip;
            c5.Value = $"Kíp {caValue}{kipSuffix}: Từ {gioBatDau} ngày {ngayStr} đến {gioKetThuc} ngày {ngayKetThuc}";
            c5.Style.Font.Italic = true;
            c5.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            c5.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }

        private static void RenderColumnHeaders(IXLWorksheet ws, List<Hrc1PhuLieuHeaderTable> headers)
        {
            const int s = PhuLieuStartCol;

            MergeVertCell(ws, 1, "STT");
            MergeVertCell(ws, 2, "Mẻ thổi");
            MergeVertCell(ws, 3, "Mác thép");
            MergeVertCell(ws, 4, "KL gang lỏng\n(tấn)");
            MergeVertCell(ws, 5, "KL thép phế\n(tấn)");

            if (headers.Count > 0)
            {
                MergeHorizCell(ws, HeaderParentRow, s, s + headers.Count - 1, "Phụ gia công nghệ (Kg)");
                for (int i = 0; i < headers.Count; i++)
                    HeaderCell(ws, HeaderChildRow, s + i, headers[i].TenPhuLieu ?? "");
            }

            int a = s + headers.Count;
            MergeHorizCell(ws, HeaderParentRow, a, a + 1, "Nhiên liệu");
            HeaderCell(ws, HeaderChildRow, a, "Oxy");
            HeaderCell(ws, HeaderChildRow, a + 1, "Nito");
            MergeVertCell(ws, a + 2, "Ghi chú");
        }

        private static void RenderDataRows(IXLWorksheet ws, List<Hrc1PhuLieuHeaderTable> headers, List<Hrc1ThongKeRow> rows, int dataStartRow)
        {
            const int s = PhuLieuStartCol;
            int r = dataStartRow;
            int n = 1;

            foreach (var row in rows)
            {
                var d = row.Data!;
                var vm = row.Values.ToDictionary(v => v.PhuLieuID, v => v.TotalKLPhuGia);

                ws.Cell(r, 1).Value = n++;
                ws.Cell(r, 2).Value = d.MeThoi ?? "";
                ws.Cell(r, 3).Value = d.MacThep ?? "";
                ws.Cell(r, 4).Value = Num((double?)d.KLGang);
                ws.Cell(r, 5).Value = Num((double?)((d.KLThepPhe ?? 0) + (d.KLThepPheGang ?? 0)));

                for (int i = 0; i < headers.Count; i++)
                    ws.Cell(r, s + i).Value = vm.TryGetValue(headers[i].PhuLieuID, out var kl) ? Num(kl) : Blank.Value;

                int a = s + headers.Count;
                ws.Cell(r, a++).Value = Num(d.O2);
                ws.Cell(r, a++).Value = Num(d.N2);
                ws.Cell(r, a).Value = d.GhiChu ?? "";

                r++;
            }
        }

        private static void RenderTotalRow(IXLWorksheet ws, int r, List<Hrc1PhuLieuHeaderTable> headers, List<Hrc1ThongKeRow> rows)
        {
            const int s = PhuLieuStartCol;

            ws.Range(r, 1, r, 3).Merge();
            ws.Cell(r, 1).Value = "Tổng cộng";
            ws.Cell(r, 1).Style.Font.Bold = true;
            ws.Cell(r, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(r, 4).Value = (XLCellValue)rows.Sum(x => (double?)x.Data?.KLGang ?? 0);
            ws.Cell(r, 5).Value = (XLCellValue)rows.Sum(x => (double?)((x.Data?.KLThepPhe ?? 0) + (x.Data?.KLThepPheGang ?? 0)) ?? 0);

            for (int i = 0; i < headers.Count; i++)
            {
                var plId = headers[i].PhuLieuID;
                ws.Cell(r, s + i).Value = (XLCellValue)rows.Sum(x => x.Values.FirstOrDefault(v => v.PhuLieuID == plId)?.TotalKLPhuGia ?? 0);
            }

            int a = s + headers.Count;
            ws.Cell(r, a++).Value = (XLCellValue)rows.Sum(x => x.Data?.O2 ?? 0);
            ws.Cell(r, a).Value = (XLCellValue)rows.Sum(x => x.Data?.N2 ?? 0);
            // Ghi chú — bỏ qua
        }

        /// <summary>
        /// Footer "Tồn trên silo | Tồn đầu kíp | Nhập trong kíp | Tồn cuối kíp" — HRC1 chưa có bảng
        /// Xuất-Nhập-Tồn tương ứng nên luôn render rỗng (chỉ header nhóm, không có dòng dữ liệu),
        /// đúng hành vi mặc định của HRC2 khi footerData/LuongTonLabels rỗng.
        /// </summary>
        private static int RenderFooter(IXLWorksheet ws, int startRow, int lastCol)
        {
            int N = lastCol;
            int g1s = Math.Max(2, N - 11);
            int g1e = N - 8;
            int g2s = N - 7;
            int g2e = N - 4;
            int g3s = N - 3;
            int g3e = N;

            int r = startRow;
            ws.Range(r, 1, r, g1s - 1).Merge();
            SetFooterLabelStyle(ws.Cell(r, 1), "Tồn trên silo");
            ws.Range(r, g1s, r, g1e).Merge();
            SetFooterLabelStyle(ws.Cell(r, g1s), "Tồn đầu kíp");
            ws.Range(r, g2s, r, g2e).Merge();
            SetFooterLabelStyle(ws.Cell(r, g2s), "Nhập trong kíp");
            ws.Range(r, g3s, r, g3e).Merge();
            SetFooterLabelStyle(ws.Cell(r, g3s), "Tồn cuối kíp");
            ApplyBorderOnly(ws.Cell(r, 1));
            ApplyBorderOnly(ws.Cell(r, g1s));
            ApplyBorderOnly(ws.Cell(r, g2s));
            ApplyBorderOnly(ws.Cell(r, g3s));

            return r;
        }

        private static void RenderSignatureRow(IXLWorksheet ws, int signRow, int lastCol, string? truongKipName, string? nguoiLapName)
        {
            int N = lastCol;
            int g3s = N - 3;
            int g3e = N;
            int leftEnd = g3s - 1;

            if (leftEnd >= 1)
            {
                ws.Range(signRow, 1, signRow, leftEnd).Merge();
                ws.Cell(signRow, 1).Value = "Trưởng kíp";
                ws.Cell(signRow, 1).Style.Font.Bold = true;
                ws.Cell(signRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(signRow, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }

            if (g3s <= g3e)
            {
                ws.Range(signRow, g3s, signRow, g3e).Merge();
                ws.Cell(signRow, g3s).Value = "Người lập";
                ws.Cell(signRow, g3s).Style.Font.Bold = true;
                ws.Cell(signRow, g3s).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(signRow, g3s).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }

            if (!string.IsNullOrWhiteSpace(truongKipName) || !string.IsNullOrWhiteSpace(nguoiLapName))
            {
                int nameRow = signRow + 1;
                if (leftEnd >= 1 && !string.IsNullOrWhiteSpace(truongKipName))
                {
                    ws.Range(nameRow, 1, nameRow, leftEnd).Merge();
                    ws.Cell(nameRow, 1).Value = truongKipName;
                    ws.Cell(nameRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(nameRow, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }
                if (g3s <= g3e && !string.IsNullOrWhiteSpace(nguoiLapName))
                {
                    ws.Range(nameRow, g3s, nameRow, g3e).Merge();
                    ws.Cell(nameRow, g3s).Value = nguoiLapName;
                    ws.Cell(nameRow, g3s).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(nameRow, g3s).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }
            }
        }

        private static void MergeVertCell(IXLWorksheet ws, int col, string text)
        {
            ws.Range(HeaderParentRow, col, HeaderChildRow, col).Merge();
            ApplyHeaderStyle(ws.Cell(HeaderParentRow, col), text);
        }

        private static void MergeHorizCell(IXLWorksheet ws, int row, int c1, int c2, string text)
        {
            ws.Range(row, c1, row, c2).Merge();
            ApplyHeaderStyle(ws.Cell(row, c1), text);
        }

        private static void HeaderCell(IXLWorksheet ws, int row, int col, string text)
            => ApplyHeaderStyle(ws.Cell(row, col), text);

        private static void ApplyHeaderStyle(IXLCell cell, string text)
        {
            cell.Value = text;
            cell.Style.Font.Bold = true;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Alignment.WrapText = true;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.OutsideBorderColor = XLColor.Black;
        }

        private static void SetFooterLabelStyle(IXLCell cell, string text)
        {
            cell.Value = text;
            cell.Style.Font.Bold = true;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }

        private static void ApplyBorderOnly(IXLCell cell)
        {
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.OutsideBorderColor = XLColor.Black;
        }

        private static void ApplyUnifiedTableGridBorders(IXLWorksheet ws, int firstRow, int lastRow, int lastCol)
        {
            if (lastRow < firstRow || lastCol < 1) return;
            var rng = ws.Range(firstRow, 1, lastRow, lastCol);
            rng.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            rng.Style.Border.InsideBorderColor = XLColor.Black;
            rng.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            rng.Style.Border.OutsideBorderColor = XLColor.Black;
        }

        private static XLCellValue Num(double? v) => v.HasValue ? (XLCellValue)v.Value : Blank.Value;

        // =========================================================
        // PDF
        // =========================================================

        public async Task<ExportFileResult> ExportPdfDetailAsync(DateOnly ngay, int ca, int scope, Guid idPhieu)
        {
            var (headers, rows) = await GetExportDataAsync(ngay, ca, scope);

            var pheDuyets = await _pheDuyetService.GetPheDuyetPhieuAsync(idPhieu);
            var truongKip = pheDuyets.FirstOrDefault(x => x.CapDuyet == 1);
            var nguoiLap = pheDuyets.FirstOrDefault(x => x.CapDuyet == 0);
            string chuKyTruongKipHtml = _pheDuyetService.FormatChuKy(truongKip?.ChuKy);
            string chuKyNguoiLapHtml = _pheDuyetService.FormatChuKy(nguoiLap?.ChuKy);
            string? truongKipName = truongKip?.HoVaTen;
            string? nguoiLapName = nguoiLap?.HoVaTen;

            var html = await BuildPdfHtmlAsync(ngay, ca, scope, headers, rows, chuKyTruongKipHtml, chuKyNguoiLapHtml, truongKipName, nguoiLapName);

            var doc = new HtmlToPdfDocument
            {
                GlobalSettings =
                {
                    PaperSize = PaperKind.A4,
                    Orientation = DinkToPdf.Orientation.Landscape,
                    Margins = new MarginSettings { Top = 10, Bottom = 10, Left = 10, Right = 10, Unit = Unit.Millimeters },
                },
                Objects =
                {
                    new ObjectSettings { HtmlContent = html, WebSettings = { DefaultEncoding = "utf-8" } },
                },
            };

            return new ExportFileResult
            {
                Content = _pdfConverter.Convert(doc),
                FileName = $"HRC1_BB_NauLuyen_BOF_Ca{ca}_{ngay:ddMMyyyy}.pdf",
                ContentType = "application/pdf",
            };
        }

        private async Task<string> BuildPdfHtmlAsync(
            DateOnly ngay, int ca, int scope,
            List<Hrc1PhuLieuHeaderTable> headers, List<Hrc1ThongKeRow> rows,
            string chuKyTruongKipHtml, string chuKyNguoiLapHtml,
            string? truongKipName, string? nguoiLapName)
        {
            var logoUrl = $"data:image/png;base64,{Convert.ToBase64String(await File.ReadAllBytesAsync(Path.Combine(_env.WebRootPath, "imgs", "LogoPDF.png")))}";

            string tenBm = $"BIÊN BẢN TIÊU HAO NẤU LUYỆN LÒ THỔI {scope}";

            string ngayStr = ngay.ToString("dd/MM/yyyy");
            string gioBatDau, gioKetThuc, ngayKetThuc = ngayStr;
            if (ca == 1)
            {
                gioBatDau = "08 giờ 00";
                gioKetThuc = "20 giờ 00";
            }
            else
            {
                gioBatDau = "20 giờ 00";
                gioKetThuc = "08 giờ 00";
                ngayKetThuc = ngay.AddDays(1).ToString("dd/MM/yyyy");
            }
            string infoKip = $"Kíp {ca}: Từ {gioBatDau} ngày {ngayStr} đến {gioKetThuc} ngày {ngayKetThuc}";

            // Trước tiên dùng nguyên mã ISO đang copy từ HRC2 (BM.08/QT.05.15) — đổi lại khi có mã HRC1 riêng.
            string bmCode = "BM.08/QT.05.15 <br /> Ngày hiệu lực: 10/01/2025 <br /> Lần sửa đổi: 00";

            string thead = PdfThead(headers);
            string tbody = PdfTbody(headers, rows);
            int lastCol = ComputeLastCol(headers.Count);
            string footer = PdfFooterHtml(lastCol, chuKyTruongKipHtml, chuKyNguoiLapHtml, truongKipName, nguoiLapName);

            var templatePath = Path.Combine(_env.WebRootPath, "template_html", "HRC1_BB_NauLuyen.html");
            var html = await File.ReadAllTextAsync(templatePath);

            return html
                .Replace("{{LogoUrl}}", logoUrl)
                .Replace("{{BmCode}}", bmCode)
                .Replace("{{TenBieuMau}}", tenBm)
                .Replace("{{InfoKip}}", infoKip)
                .Replace("{{TheadRows}}", thead)
                .Replace("{{TbodyRows}}", tbody)
                .Replace("{{FooterHtml}}", footer);
        }

        private static string PdfThead(List<Hrc1PhuLieuHeaderTable> h)
        {
            var r1 = new StringBuilder();
            var r2 = new StringBuilder();

            r1.Append("<th rowspan=\"2\">STT</th>");
            r1.Append("<th rowspan=\"2\">Mẻ thổi</th>");
            r1.Append("<th rowspan=\"2\">Mác thép</th>");
            r1.Append("<th rowspan=\"2\">KL gang lỏng<br/>(tấn)</th>");
            r1.Append("<th rowspan=\"2\">KL thép phế<br/>(tấn)</th>");

            if (h.Count > 0)
            {
                r1.Append($"<th colspan=\"{h.Count}\">Phụ gia công nghệ (Kg)</th>");
                foreach (var x in h) r2.Append($"<th>{x.TenPhuLieu}</th>");
            }

            r1.Append("<th colspan=\"2\">Nhiên liệu</th>");
            r2.Append("<th>Oxy</th><th>Nito</th>");
            r1.Append("<th rowspan=\"2\">Ghi chú</th>");

            return $"<thead><tr>{r1}</tr><tr>{r2}</tr></thead>";
        }

        private static string PdfTbody(List<Hrc1PhuLieuHeaderTable> headers, List<Hrc1ThongKeRow> rows)
        {
            var sb = new StringBuilder("<tbody>");
            int stt = 1;
            foreach (var row in rows)
            {
                var d = row.Data!;
                var vm = row.Values.ToDictionary(v => v.PhuLieuID, v => v.TotalKLPhuGia);
                sb.Append("<tr>");
                sb.Append($"<td>{stt++}</td><td>{d.MeThoi ?? ""}</td><td>{d.MacThep ?? ""}</td>");
                sb.Append($"<td>{PFmt((double?)d.KLGang)}</td><td>{PFmt((double?)((d.KLThepPhe ?? 0) + (d.KLThepPheGang ?? 0)))}</td>");
                foreach (var hx in headers)
                    sb.Append($"<td>{PFmt(vm.TryGetValue(hx.PhuLieuID, out var kl) ? kl : null)}</td>");
                sb.Append($"<td>{PFmt(d.O2)}</td><td>{PFmt(d.N2)}</td>");
                sb.Append($"<td class=\"td-left\">{d.GhiChu ?? ""}</td>");
                sb.Append("</tr>");
            }
            sb.Append("<tr class=\"total-row\"><td colspan=\"3\">Tổng cộng</td>");
            sb.Append($"<td>{PFmt(rows.Sum(x => (double?)x.Data?.KLGang ?? 0))}</td>");
            sb.Append($"<td>{PFmt(rows.Sum(x => (double?)((x.Data?.KLThepPhe ?? 0) + (x.Data?.KLThepPheGang ?? 0)) ?? 0))}</td>");
            foreach (var hx in headers)
            {
                var plId = hx.PhuLieuID;
                sb.Append($"<td>{PFmt(rows.Sum(x => x.Values.FirstOrDefault(v => v.PhuLieuID == plId)?.TotalKLPhuGia ?? 0))}</td>");
            }
            sb.Append($"<td>{PFmt(rows.Sum(x => x.Data?.O2 ?? 0))}</td>");
            sb.Append($"<td>{PFmt(rows.Sum(x => x.Data?.N2 ?? 0))}</td>");
            sb.Append("<td></td>");
            sb.Append("</tr></tbody>");
            return sb.ToString();
        }

        private static string PdfFooterHtml(int N, string chuKyTruongKipHtml, string chuKyNguoiLapHtml, string? truongKipName, string? nguoiLapName)
        {
            const int g3Span = 4, g2Span = 4, g1Span = 4;
            int siloSpan = Math.Max(1, N - 12);

            var sb = new StringBuilder("<table class=\"footer-tbl\">");
            sb.Append("<tr>");
            sb.Append($"<th colspan=\"{siloSpan}\">Tồn trên silo</th>");
            sb.Append($"<th colspan=\"{g1Span}\">Tồn đầu kíp</th>");
            sb.Append($"<th colspan=\"{g2Span}\">Nhập trong kíp</th>");
            sb.Append($"<th colspan=\"{g3Span}\">Tồn cuối kíp</th>");
            sb.Append("</tr>");
            sb.Append("</table>");

            int truongKipSpan = siloSpan + g1Span + g2Span;
            sb.Append("<table style=\"width:100%;margin-top:20px; border:none; border-collapse:collapse;\">");
            sb.Append("<tr>");
            sb.Append(
                $"<td colspan=\"{truongKipSpan}\" style=\"text-align:center;font-weight:bold;border:none;vertical-align:middle;\">"
                + "<div style=\"text-align:center;font-weight:bold;\">Trưởng kíp</div>"
                + $"{(string.IsNullOrWhiteSpace(chuKyTruongKipHtml) ? "" : chuKyTruongKipHtml)}"
                + $"{(string.IsNullOrWhiteSpace(truongKipName) ? "" : $"<div style=\"text-align:center;\">{truongKipName}</div>")}"
                + "</td>");
            sb.Append(
                $"<td colspan=\"{g3Span}\" style=\"text-align:center;font-weight:bold;border:none;vertical-align:middle;\">"
                + "<div style=\"text-align:center;font-weight:bold;\">Người lập</div>"
                + $"{(string.IsNullOrWhiteSpace(chuKyNguoiLapHtml) ? "" : chuKyNguoiLapHtml)}"
                + $"{(string.IsNullOrWhiteSpace(nguoiLapName) ? "" : $"<div style=\"text-align:center;\">{nguoiLapName}</div>")}"
                + "</td>");
            sb.Append("</tr>");
            sb.Append("</table>");
            return sb.ToString();
        }

        private static string PFmt(double? v) => v.HasValue ? v.Value.ToString("0.##") : "";
    }
}

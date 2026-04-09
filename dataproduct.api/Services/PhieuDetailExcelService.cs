using ClosedXML.Excel;
using dataproduct.api.DTOs.Export;
using dataproduct.api.Models;
using dataproduct.api.ResponseModels;
using dataproduct.api.Utils;
using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.Text;
using PaperKind = DinkToPdf.PaperKind;

namespace dataproduct.api.Services
{
    public class PhieuDetailExcelService
    {
        private readonly ProductFormContext _context;
        private readonly IConverter _pdfConverter;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        private readonly PheDuyetService _pheDuyetService;

        public PhieuDetailExcelService(ProductFormContext context, PheDuyetService pheDuyetService, IConverter pdfConverter, IConfiguration configuration, IWebHostEnvironment env)
        {
            _context       = context;
            _pdfConverter  = pdfConverter;
            _configuration = configuration;
            _env           = env;
            _pheDuyetService = pheDuyetService;
        }

        public async Task<BmPhieu> GetBmPhieuByIdOrThrowAsync(Guid idPhieu)
        {
            var phieu = await _context.BmPhieus.FirstOrDefaultAsync(x => x.Idphieu == idPhieu);
            if (phieu == null) throw new Exception($"Không tìm thấy phiếu với IdPhieu='{idPhieu}'.");
            return phieu;
        }

        // -------------------------------------------------------
        // Column position helpers
        // -------------------------------------------------------

        /// <summary>
        /// Cột bắt đầu của phụ liệu động (1-based):
        /// BOF = 6 (F), LF / RH = 5 (E).
        /// </summary>
        public static int GetPhuLieuStartCol(string bieuMau) =>
            bieuMau.Contains("BOF", StringComparison.OrdinalIgnoreCase) ? 6 : 5;

        /// <summary>ColIndex (1-based) của header thứ <paramref name="zeroBasedIndex"/> trong danh sách đã sort.</summary>
        public static int GetPhuLieuColIndex(string bieuMau, int zeroBasedIndex) =>
            GetPhuLieuStartCol(bieuMau) + zeroBasedIndex;

        // -------------------------------------------------------
        // Export data query (DB-based, không dùng DataJson)
        // -------------------------------------------------------

        /// <summary>
        /// Lấy dữ liệu từ DLNM_HRC2 + PhuLieu_HRC2 để export Excel thống kê.
        /// Params: ngay (DateOnly), ca, bieuMau, scope — tương tự SearchThongKeApiAsync nhưng không phân trang.
        /// </summary>
        public async Task<(List<PhuLieuHeaderTable> HeadersBOF, List<PhuLieuHeaderTable> HeadersLFRH, List<HRC2ThongKeRow> Rows)> GetExportDataAsync(
            DateOnly ngay, int ca, string bieuMau, int scope)
        {
            // 1. Lấy tất cả header Excel trong 1 query, split thành 2 list trong memory
            var allExcelHeaders = await _context.Header_Keys
                .Where(h => h.IsUsed_Excel == true)
                .Select(h => new { h.Id, h.TenHienThi, h.LoaiExcel, h.ThuTu_Excel_BOF, h.ThuTu_Excel_LFRH })
                .ToListAsync();

            var headersBOF = allExcelHeaders
                .Where(h => h.LoaiExcel == 1 || h.LoaiExcel == 3)
                .OrderBy(h => h.ThuTu_Excel_BOF ?? int.MaxValue)
                .ThenBy(h => h.Id)
                .Select(h => new PhuLieuHeaderTable
                {
                    IDHeaderKey = h.Id,
                    TenPhuLieu = h.TenHienThi,
                    LoaiThongKe = (byte)(h.LoaiExcel ?? 0)
                })
                .ToList();

            var headersLFRH = allExcelHeaders
                .Where(h => h.LoaiExcel == 2 || h.LoaiExcel == 3)
                .OrderBy(h => h.ThuTu_Excel_LFRH ?? int.MaxValue)
                .ThenBy(h => h.Id)
                .Select(h => new PhuLieuHeaderTable
                {
                    IDHeaderKey = h.Id,
                    TenPhuLieu = h.TenHienThi,
                    LoaiThongKe = (byte)(h.LoaiExcel ?? 0)
                })
                .ToList();

            // Chọn header list phù hợp với bieuMau hiện tại để build data rows
            var loaiBmKey = bieuMau.Trim().ToUpperInvariant();
            bool isBofExcel = loaiBmKey.Contains("BOF");
            var headers = isBofExcel ? headersBOF : headersLFRH;

            if (!headersBOF.Any() && !headersLFRH.Any())
                return (headersBOF, headersLFRH, new List<HRC2ThongKeRow>());

            var usedHeaderKeyIds = headers.Select(x => x.IDHeaderKey).ToHashSet();

            // 2. Lấy tất cả bản ghi DLNM_HRC2 theo filter (một bản ghi/REPORT_NO, lấy ID lớn nhất)
            var ngayDateTime = ngay.ToDateTime(TimeOnly.MinValue);

            var baseQuery = _context.DLNM_HRC2s
                .Where(x =>
                    x.Ngay.HasValue &&
                    x.Ngay.Value.Date == ngayDateTime.Date &&
                    x.Ca == ca &&
                    x.BieuMau == bieuMau &&
                    x.Scope == scope &&
                    x.REPORT_NO.HasValue);

            var groupedIds = baseQuery
                .GroupBy(x => x.REPORT_NO)
                .Select(g => g.Max(x => x.ID));

            var items = await _context.DLNM_HRC2s
                .Where(x => groupedIds.Contains(x.ID))
                .OrderBy(x => x.REPORT_NO)
                .AsNoTracking()
                .ToListAsync();

            if (!items.Any())
                return (headersBOF, headersLFRH, new List<HRC2ThongKeRow>());

            var reportNos = items
                .Where(x => x.REPORT_NO.HasValue)
                .Select(x => x.REPORT_NO!.Value)
                .ToList();

            // 3. Batch load mapped phụ liệu (có ID_PhuLieu → Header_Mappings → Header_Keys)
            var mappedRaw = await (
                from pl in _context.PhuLieu_HRC2s
                where pl.REPORT_NO.HasValue && reportNos.Contains(pl.REPORT_NO.Value)
                      && (pl.IsPhanBo != true) && pl.ID_PhuLieu.HasValue
                join hm in _context.Header_Mappings on pl.ID_PhuLieu.Value equals hm.ID_PhuLieu
                join hk in _context.Header_Keys on hm.ID_HeaderKey equals hk.Id
                where hk.IsActive && usedHeaderKeyIds.Contains(hk.Id)
                select new
                {
                    ReportNo = pl.REPORT_NO!.Value,
                    ID_HeaderKey = hk.Id,
                    pl.KLPhuGia,
                    pl.KLPhuGia_Manual,
                    pl.IsManual
                }
            ).ToListAsync();

            // 3b. Manual-only (ID_PhuLieu = NULL, chỉ có ID_HeaderKey)
            var manualOnlyRaw = await _context.PhuLieu_HRC2s
                .Where(pl =>
                    pl.REPORT_NO.HasValue &&
                    reportNos.Contains(pl.REPORT_NO.Value) &&
                    (pl.IsPhanBo != true) &&
                    !pl.ID_PhuLieu.HasValue &&
                    pl.ID_HeaderKey.HasValue &&
                    usedHeaderKeyIds.Contains(pl.ID_HeaderKey.Value))
                .Select(pl => new
                {
                    ReportNo = pl.REPORT_NO!.Value,
                    ID_HeaderKey = pl.ID_HeaderKey!.Value,
                    KLPhuGia = (double?)0,
                    pl.KLPhuGia_Manual,
                    pl.IsManual
                })
                .ToListAsync();

            mappedRaw.AddRange(manualOnlyRaw);

            // Group: reportNo → headerKeyId → (KLPhuGiaTotal, KLPhuGia_Manual, IsManual)
            var mappedByReportNo = mappedRaw
                .GroupBy(x => x.ReportNo)
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(x => x.ID_HeaderKey)
                          .ToDictionary(
                              hg => hg.Key,
                              hg => (
                                  KLPhuGiaTotal: (double?)hg.Sum(x => x.KLPhuGia ?? 0),
                                  KLPhuGia_Manual: hg.First().KLPhuGia_Manual,
                                  IsManual: hg.First().IsManual
                              )
                          )
                );

            // 4. Batch load phanBo
            var phanBoRaw = await _context.PhuLieu_HRC2s
                .Where(x =>
                    x.REPORT_NO.HasValue && reportNos.Contains(x.REPORT_NO.Value) &&
                    x.IsPhanBo == true &&
                    x.ID_HeaderKey.HasValue && usedHeaderKeyIds.Contains(x.ID_HeaderKey.Value))
                .Select(x => new
                {
                    ReportNo = x.REPORT_NO!.Value,
                    ID_HeaderKey = x.ID_HeaderKey!.Value,
                    x.KLPhuGia,
                    x.KLPhuGia_Manual,
                    x.IsManual
                })
                .ToListAsync();

            var phanBoByReportNo = phanBoRaw
                .GroupBy(x => x.ReportNo)
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(x => x.ID_HeaderKey)
                          .ToDictionary(
                              hg => hg.Key,
                              hg => (
                                  KLPhuGia: (double?)hg.Sum(x => x.KLPhuGia ?? 0),
                                  KLPhuGia_Manual: (double?)hg.Sum(x => x.KLPhuGia_Manual ?? 0),
                                  IsManual: hg.First().IsManual
                              )
                          )
                );

            // 5. Assemble rows
            var rows = items
                .Where(x => x.REPORT_NO.HasValue)
                .Select(x =>
                {
                    var reportNo = x.REPORT_NO!.Value;
                    mappedByReportNo.TryGetValue(reportNo, out var mappedDict);
                    phanBoByReportNo.TryGetValue(reportNo, out var phanBoDict);

                    var values = headers.Select(h =>
                    {
                        double? klPhuGia = null;
                        double? klPhuGia_Manual = null;
                        bool? isManual = null;

                        if (mappedDict != null && mappedDict.TryGetValue(h.IDHeaderKey, out var mapped))
                        {
                            klPhuGia = RoundNumber(mapped.KLPhuGiaTotal);
                            klPhuGia_Manual = RoundNumber(mapped.KLPhuGia_Manual);
                            isManual = mapped.IsManual;
                        }

                        var effectiveKL = klPhuGia_Manual ?? klPhuGia;
                        double? klPhanBo = null;
                        double? totalKLPhuGia;

                        if (phanBoDict != null && phanBoDict.TryGetValue(h.IDHeaderKey, out var phanBo))
                        {
                            klPhanBo = RoundNumber(phanBo.KLPhuGia);
                            totalKLPhuGia = RoundNumber((phanBo.KLPhuGia ?? 0) + (effectiveKL ?? 0));
                        }
                        else
                        {
                            totalKLPhuGia = effectiveKL;
                        }

                        return new HRC2ThongKeValue
                        {
                            IDHeaderKey = h.IDHeaderKey,
                            KLPhuGia = klPhuGia,
                            KLPhuGia_Manual = klPhuGia_Manual,
                            IsManual = isManual,
                            KLPhanBo = klPhanBo,
                            TotalKLPhuGia = totalKLPhuGia
                        };
                    }).ToList();

                    return new HRC2ThongKeRow
                    {
                        Data = new DLNM_HRC2_ResponseModels
                        {
                            ID = x.ID,
                            REPORT_NO = x.REPORT_NO,
                            NgaySx = x.NgaySx,
                            Ngay = x.Ngay,
                            Ca = x.Ca,
                            BieuMau = x.BieuMau,
                            Scope = x.Scope,
                            MeThoi = x.MeThoi,
                            MacThep = x.MacThep,
                            O2 = RoundNumber(x.O2),
                            AR_RH = RoundNumber(x.AR_RH),
                            N2 = RoundNumber(x.N2),
                            AR_BOF = RoundNumber(x.AR_BOF),
                            AR_LF = RoundNumber(x.AR_LF),
                            KLGangLong = RoundNumber(x.KLGangLong),
                            KLThepPhe = RoundNumber(x.KLThepPhe),
                            KLGangLongCCT = RoundNumber(x.KLGangLongCCT),
                            KLGangLongCR = RoundNumber(x.KLGangLongCR),
                            KLThepLong = RoundNumber(x.KLThepLong),
                            IsNM = x.IsNM,
                            IsChuyenCa = x.IsChuyenCa,
                            IsTrungMeThoi = x.IsTrungMeThoi,
                            QueLayMau = x.QueLayMau,
                            QueDoNhiet = x.QueDoNhiet,
                            GhiChu = x.GhiChu
                        },
                        Values = values
                    };
                })
                .ToList();

            return (headersBOF, headersLFRH, rows);
        }

        private static double? RoundNumber(double? value)
        {
            if (!value.HasValue) return null;
            var rounded = Math.Round(value.Value, 2, MidpointRounding.AwayFromZero);
            return Math.Abs(rounded % 1) < 0.0000001 ? Math.Truncate(rounded) : rounded;
        }

        public async Task<byte[]> ExportAsync(Guid phieuId)
        {
            var phieu = await _context.BmPhieus.FirstOrDefaultAsync(x => x.Idphieu == phieuId);
            if (phieu == null)
                throw new Exception("Không tìm thấy phiếu");

            using var workbook = new XLWorkbook();
            var ws = workbook.AddWorksheet("Phiếu");

            BuildHeader(ws, phieu);

            ws.Column(1).Width = 35;
            ws.Columns(2, 30).AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        // -------------------------------------------------------
        // Header phiếu (generic fallback)
        // -------------------------------------------------------
        private void BuildHeader(IXLWorksheet ws, BmPhieu phieu)
        {
            var titleCell = ws.Cell(1, 1);
            titleCell.Value = $"{phieu.MaBm} - {phieu.SoPhieu}";
            titleCell.Style.Font.Bold = true;
            titleCell.Style.Font.FontSize = 13;
            ws.Range(1, 1, 1, 6).Merge();

            int row = 3;
            WriteInfoRow(ws, row++, "Số phiếu", phieu.SoPhieu);
            WriteInfoRow(ws, row++, "Mã biểu mẫu", phieu.MaBm);
            WriteInfoRow(ws, row++, "Ngày sản xuất", phieu.NgaySX?.ToDateTime(TimeOnly.MinValue).ToString("dd/MM/yyyy"));
            WriteInfoRow(ws, row++, "Ca", phieu.Ca?.ToString());
            WriteInfoRow(ws, row++, "Kíp", phieu.Kip);
            WriteInfoRow(ws, row++, "Tình trạng", PhieuStatusDisplay.GetText(phieu.TinhTrang ?? 0));
        }

        private void WriteInfoRow(IXLWorksheet ws, int row, string label, string? value)
        {
            ws.Cell(row, 1).Value = label;
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 2).Value = value ?? "";
        }

        // ===============================================================
        // HRC2 NauLuyen — DB-based render
        // Layout: rows 1-3 từ template | rows 4-5 info | rows 6-7 headers | row 8+ data
        // ===============================================================

        private const int HeaderParentRow = 6;
        private const int HeaderChildRow  = 7;
        private const int DataStartRow    = 8;

        /// <summary>
        /// Replace {{Marker}} cells trong template (rows 1-3) với metadata phiếu.
        /// </summary>
        public void ReplaceMarkers(IXLWorksheet ws, BmPhieu phieu)
        {
            var replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "{{SoPhieu}}",   phieu.SoPhieu ?? "" },
                { "{{NgaySX}}",    phieu.NgaySX?.ToDateTime(TimeOnly.MinValue).ToString("dd/MM/yyyy") ?? "" },
                { "{{Ca}}",        phieu.Ca?.ToString() ?? "" },
                { "{{Kip}}",       phieu.Kip ?? "" },
                { "{{TinhTrang}}", PhieuStatusDisplay.GetText(phieu.TinhTrang ?? 0) },
            };

            foreach (var cell in ws.CellsUsed())
            {
                if (cell.DataType != XLDataType.Text) continue;
                var raw = cell.GetString();
                foreach (var (marker, value) in replacements)
                {
                    if (raw.Contains(marker, StringComparison.OrdinalIgnoreCase))
                        raw = raw.Replace(marker, value, StringComparison.OrdinalIgnoreCase);
                }
                cell.Value = raw;
            }
        }

        /// <summary>
        /// Orchestrator: cập nhật merge rows 1-3 → render rows 4-5 (info) → rows 6-7 (headers) → rows 8+ (data).
        /// </summary>
        public void RenderBodyFromDb(IXLWorksheet ws, string bieuMau,
            List<PhuLieuHeaderTable> headers, List<HRC2ThongKeRow> rows,
            int? scope = null, string gioBatDau = "", string gioKetThuc = "",
            DateOnly? ngayPhieu = null, int? caPhieu = null, string kip = "")
        {
            // Derive phân bổ headers từ data (headers có KLPhanBo != null/0)
            var phanBoKeyIds = rows
                .SelectMany(r => r.Values)
                .Where(v => v.KLPhanBo.HasValue && v.KLPhanBo != 0)
                .Select(v => v.IDHeaderKey)
                .Distinct()
                .ToHashSet();
            var phanBoHeaders = headers.Where(h => phanBoKeyIds.Contains(h.IDHeaderKey)).ToList();

            int lastCol = ComputeLastCol(bieuMau, headers.Count, phanBoHeaders.Count);
            ws.Column(2).Width = 25;
            ws.Column(3).Width = 25;
            ws.Column(lastCol).Width = 25;

            ClearRowsFrom(ws, 4);
            UpdateHeaderRowMerges(ws, lastCol);
            RenderInfoRows(ws, bieuMau, rows, lastCol, scope, gioBatDau, gioKetThuc, ngayPhieu, caPhieu, kip);

            // Data bắt đầu từ row 8 cho tất cả biểu mẫu.
            // Sau data: Tổng cộng (totalRow) → 1 dòng trắng → Footer.
            int dataStartRow = DataStartRow; // 8
            int dataEndRow   = dataStartRow + rows.Count - 1;
            int totalRow     = dataEndRow + 1;

            int tableLastRow;
            switch (bieuMau)
            {
                case "HRC2_BB_NauLuyen_BOF":
                    RenderColumnHeaders_BOF(ws, headers, phanBoHeaders);
                    RenderDataRows_BOF(ws, headers, phanBoHeaders, rows, dataStartRow, lastCol);
                    RenderTotalRow_BOF(ws, totalRow, lastCol, headers, phanBoHeaders, rows);
                    tableLastRow = RenderFooter(ws, totalRow + 2, lastCol, BofFooterConfig);
                    RenderSignatureRow(ws, tableLastRow + 1, lastCol, BofFooterConfig);
                    break;
                case "HRC2_BB_NauLuyen_LF":
                    RenderColumnHeaders_LF(ws, headers, phanBoHeaders);
                    RenderDataRows_LF(ws, headers, phanBoHeaders, rows, dataStartRow, lastCol);
                    RenderTotalRow_LF(ws, totalRow, lastCol, headers, phanBoHeaders, rows);
                   tableLastRow = RenderFooter(ws, totalRow + 2, lastCol, RhFooterConfig);
                    RenderSignatureRow(ws, tableLastRow + 1, lastCol, RhFooterConfig);
                    break;
                case "HRC2_BB_NauLuyen_RH":
                    RenderColumnHeaders_RH(ws, headers, phanBoHeaders);
                    RenderDataRows_RH(ws, headers, phanBoHeaders, rows, dataStartRow, lastCol);
                    RenderTotalRow_RH(ws, totalRow, lastCol, headers, phanBoHeaders, rows);
                    tableLastRow = RenderFooter(ws, totalRow + 2, lastCol, RhFooterConfig);
                    RenderSignatureRow(ws, tableLastRow + 1, lastCol, RhFooterConfig);
                    break;
                default:
                    tableLastRow = totalRow;
                    break;
            }

            // Sau khi merge header + body + footer xong: một lưới viền thống nhất (trong = mỏng, ngoài = đậm)
            ApplyUnifiedTableGridBorders(ws, HeaderParentRow, tableLastRow, lastCol);
        }

        // ===============================================================
        // BOF full render — theo spec mới (promt.md)
        // Layout: rows 1-3 template | row 4 title | row 5 kíp | rows 6-7 headers
        //         row 8 Tổng đầu kíp | row 9 labels | row 10+ data | Tổng cộng | LuongTon
        // ===============================================================

        /// <summary>
        /// Render toàn bộ biên bản BOF từ row 4 trở xuống vào worksheet đã mở sẵn từ template.
        /// Rows 1-3 giữ nguyên, chỉ cập nhật merge theo lastCol.
        /// </summary>
        public void RenderBOFExcel(
            IXLWorksheet ws,
            ExportBOFRequest request,
            List<HeaderKeyConfig> headerKeys,
            List<MeLuyenModel> data,
            List<LuongTonModel> luongTons)
        {
            int n       = headerKeys.Count;
            int lastCol = 8 + n; // 5 fixed-left + N dynamic + 3 fixed-right
            int s       = 6;     // cột bắt đầu phụ gia động (BOF = F)
            int a       = s + n; // cột Oxy

            // Không đụng rows 1-3 — giữ nguyên template (drawing, merge, nội dung)
            ClearRowsFrom(ws, 4);

            // Row 4: tiêu đề
            ws.Range(4, 1, 4, lastCol).Merge();
            var c4 = ws.Cell(4, 1);
            c4.Value                      = $"BIÊN BẢN TIÊU HAO NẤU LUYỆN LÒ THỔI {request.SoLo}";
            c4.Style.Font.Bold            = true;
            c4.Style.Font.FontSize        = 14;
            c4.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            c4.Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;

            // Row 5: thông tin kíp
            ws.Range(5, 1, 5, lastCol).Merge();
            var c5 = ws.Cell(5, 1);
            c5.Value = $"Kíp: Từ {request.GioBatDau} ngày {request.NgaySX:dd/MM/yyyy}" +
                       $" đến {request.GioKetThuc} ngày {request.NgaySX:dd/MM/yyyy}";
            c5.Style.Font.Italic          = true;
            c5.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            c5.Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;

            // Rows 6-7: column headers
            BofHeaderMergeVert(ws, 1, "STT");
            BofHeaderMergeVert(ws, 2, "Mẻ nấu số");
            BofHeaderMergeVert(ws, 3, "Mác thép");

            ws.Range(6, 4, 6, 5).Merge();
            BofHeaderCell(ws, 6, 4, "Nguyên liệu đầu vào\n(tấn)");
            BofHeaderCell(ws, 7, 4, "Gang lỏng");
            BofHeaderCell(ws, 7, 5, "Thép phế");

            if (n > 0)
            {
                ws.Range(6, s, 6, s + n - 1).Merge();
                BofHeaderCell(ws, 6, s, "Phụ gia công nghệ (Kg)");
                for (int i = 0; i < n; i++)
                    BofHeaderCell(ws, 7, s + i, headerKeys[i].TenHienThi);
            }

            ws.Range(6, a, 6, a + 1).Merge();
            BofHeaderCell(ws, 6, a,     "Nhiên liệu (m³)");
            BofHeaderCell(ws, 7, a,     "Oxy");
            BofHeaderCell(ws, 7, a + 1, "Nitơ");
            BofHeaderMergeVert(ws, a + 2, "Ghi chú");

            // Row 8: Tổng đầu kíp
            ws.Range(8, 1, 8, 3).Merge();
            ws.Cell(8, 1).Value           = "Tổng";
            ws.Cell(8, 1).Style.Font.Bold = true;
            for (int col = 1; col <= lastCol; col++)
                ApplyBorderOnly(ws.Cell(8, col));

            // Row 9: 3 nhãn bằng nhau
            int part = (lastCol + 2) / 3;
            int end1 = part;
            int end2 = Math.Min(part * 2, lastCol - 1);
            ws.Range(9, 1,        9, end1   ).Merge();
            ws.Range(9, end1 + 1, 9, end2   ).Merge();
            ws.Range(9, end2 + 1, 9, lastCol).Merge();
            BofHeaderCell(ws, 9, 1,        "Tên đầu kíp");
            BofHeaderCell(ws, 9, end1 + 1, "Nhập trong kíp");
            BofHeaderCell(ws, 9, end2 + 1, "Tên cuối kíp");

            // Row 10+: data
            const int DataRow = 10;
            int r   = DataRow;
            int stt = 1;
            foreach (var row in data)
            {
                ws.Cell(r, 1).Value = stt++;
                ws.Cell(r, 2).Value = row.MeNauSo  ?? "";
                ws.Cell(r, 3).Value = row.MacThep  ?? "";
                ws.Cell(r, 4).Value = Num(row.KLGangLong);
                ws.Cell(r, 5).Value = Num(row.KLThepPhe);
                for (int i = 0; i < n; i++)
                    ws.Cell(r, s + i).Value = row.PhuGia.TryGetValue(headerKeys[i].Id, out var kl)
                        ? Num(kl) : Blank.Value;
                ws.Cell(r, a    ).Value = Num(row.O2);
                ws.Cell(r, a + 1).Value = Num(row.N2);
                ws.Cell(r, a + 2).Value = row.GhiChu ?? "";
                ApplyDataRowBorder(ws, r, lastCol);
                r++;
            }

            // Tổng cộng
            ws.Range(r, 1, r, 3).Merge();
            ws.Cell(r, 1).Value           = "Tổng cộng";
            ws.Cell(r, 1).Style.Font.Bold = true;
            ws.Cell(r, 4).Value = data.Sum(d => d.KLGangLong ?? 0);
            ws.Cell(r, 5).Value = data.Sum(d => d.KLThepPhe  ?? 0);
            for (int i = 0; i < n; i++)
            {
                var hkId = headerKeys[i].Id;
                ws.Cell(r, s + i).Value = data.Sum(d =>
                    d.PhuGia.TryGetValue(hkId, out var kl) ? kl ?? 0 : 0);
            }
            ws.Cell(r, a    ).Value = data.Sum(d => d.O2 ?? 0);
            ws.Cell(r, a + 1).Value = data.Sum(d => d.N2 ?? 0);
            ApplyDataRowBorder(ws, r, lastCol);
            r++;

            // Lượng tồn
            foreach (var lt in luongTons)
            {
                ws.Range(r, 1, r, 2).Merge();
                ws.Cell(r, 1).Value = lt.TenLuongTon;
                ws.Cell(r, 3).Value = Num(lt.GiaTri);
                r++;
            }

            ws.SheetView.FreezeRows(DataRow - 1);
            ws.Columns().AdjustToContents();
        }

        private static void BofHeaderMergeVert(IXLWorksheet ws, int col, string text)
        {
            ws.Range(6, col, 7, col).Merge();
            ApplyBOFHeaderStyle(ws.Cell(6, col), text);
        }

        private static void BofHeaderCell(IXLWorksheet ws, int row, int col, string text)
            => ApplyBOFHeaderStyle(ws.Cell(row, col), text);

        private static void ApplyBOFHeaderStyle(IXLCell cell, string text)
        {
            cell.Value                           = text;
            cell.Style.Font.Bold                 = true;
            cell.Style.Alignment.Horizontal      = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical        = XLAlignmentVerticalValues.Center;
            cell.Style.Alignment.WrapText        = true;
            cell.Style.Fill.BackgroundColor      = XLColor.FromHtml("#D9D9D9");
            cell.Style.Border.OutsideBorder      = XLBorderStyleValues.Thin;
            cell.Style.Border.OutsideBorderColor = XLColor.Black;
        }

        private static void ApplyBorderOnly(IXLCell cell)
        {
            cell.Style.Border.OutsideBorder      = XLBorderStyleValues.Thin;
            cell.Style.Border.OutsideBorderColor = XLColor.Black;
        }

        private static void ApplyDataRowBorder(IXLWorksheet ws, int row, int lastCol)
        {
            for (int col = 1; col <= lastCol; col++)
                ApplyBorderOnly(ws.Cell(row, col));
        }

        /// <summary>
        /// Áp viền cho cả khối bảng (header 6-7, body, hàng tổng, dòng trắng trước footer nếu có, footer)
        /// sau khi đã merge xong. Trong: thin; bao ngoài cùng: medium.
        /// </summary>
        private static void ApplyUnifiedTableGridBorders(IXLWorksheet ws, int firstRow, int lastRow, int lastCol)
        {
            if (lastRow < firstRow || lastCol < 1)
                return;

            var rng = ws.Range(firstRow, 1, lastRow, lastCol);
            rng.Style.Border.InsideBorder       = XLBorderStyleValues.Thin;
            rng.Style.Border.InsideBorderColor  = XLColor.Black;
            rng.Style.Border.OutsideBorder      = XLBorderStyleValues.Medium;
            rng.Style.Border.OutsideBorderColor = XLColor.Black;
        }

        // -------------------------------------------------------
        // Xóa sạch nội dung + merge từ row startRow trở xuống
        // -------------------------------------------------------
        private static void ClearRowsFrom(IXLWorksheet ws, int startRow)
        {
            // Unmerge bất kỳ range nào chạm vào hoặc sau startRow
            // (kể cả merge bắt đầu từ row 1-3 nhưng kéo dài xuống row 4+)
            var toUnmerge = ws.MergedRanges
                .Where(m => m.RangeAddress.LastAddress.RowNumber >= startRow)
                .ToList();
            foreach (var m in toUnmerge)
                m.Unmerge();

            // Clear nội dung (không Delete để tránh shift rows gây corrupt file)
            var lastRow = ws.LastRowUsed()?.RowNumber() ?? (startRow - 1);
            for (int r = startRow; r <= lastRow; r++)
                ws.Row(r).Clear(XLClearOptions.Contents);
        }

        // -------------------------------------------------------
        // lastCol = (cols cố định trước) + (phụ liệu động) + (cols cố định sau)
        //   BOF: 5 + N + 3  → Nhiên liệu (Oxy | Nito) + Ghi chú
        //   LF : 4 + N + 4  → Khí (Argon) + Que lấy mẫu + Que đo nhiệt + Ghi chú
        //   RH : 4 + N + 6  → Khí (Argon | Nito | Oxi) + Que lấy mẫu + Que đo nhiệt + Ghi chú
        // -------------------------------------------------------
        private static int ComputeLastCol(string bieuMau, int dynamicCount, int phanBoCount = 0)
        {
            int startCol    = GetPhuLieuStartCol(bieuMau); // 6 (BOF) hoặc 5 (LF/RH)
            int fixedBefore = startCol - 1;
            string key = bieuMau.ToUpperInvariant();
            int fixedAfter = key.Contains("BOF") ? 3
                           : key.Contains("RH")  ? 6
                           :                       4; // LF
            return fixedBefore + dynamicCount + fixedAfter + phanBoCount;
        }

        // -------------------------------------------------------
        // Rows 1-3: re-merge theo lastCol — KHÔNG sửa nội dung cells, KHÔNG đụng drawing float
        //   Vùng A: A1:A3 (thông tin biểu mẫu — giữ nguyên content từ template)
        //   Vùng giữa: B1:(lastCol-1)3 (logo float, chỉ merge cells)
        //   Vùng phải: lastCol1:lastCol3 (thông tin phụ — lấy từ merge gốc cuối cùng)
        // -------------------------------------------------------
        // private static void UpdateHeaderRowMerges(IXLWorksheet ws, int lastCol)
        // {
        //     var allMerges = ws.MergedRanges
        //         .Where(m => m.RangeAddress.FirstAddress.RowNumber >= 1
        //                  && m.RangeAddress.LastAddress.RowNumber  <= 3)
        //         .OrderByDescending(m => m.RangeAddress.FirstAddress.ColumnNumber)
        //         .ToList();

        //     // Lưu nội dung vùng phải (merge xa nhất bên phải = thông tin phụ)
        //     XLCellValue rightValue = Blank.Value;
        //     if (allMerges.Count >= 2)
        //     {
        //         var rm = allMerges.First();
        //         rightValue = ws.Cell(rm.RangeAddress.FirstAddress.RowNumber,
        //                              rm.RangeAddress.FirstAddress.ColumnNumber).Value;
        //     }

        //     foreach (var m in allMerges)
        //         m.Unmerge();

        //     // 1. A1:A3 — giữ content col A, chỉ re-merge
        //     ws.Range(1, 1, 3, 1).Merge();
        //     ws.Cell(1, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;

        //     // 2. B1:(lastCol-1)3 — vùng giữa (logo float không bị ảnh hưởng)
        //     if (lastCol > 2)
        //         ws.Range(1, 2, 3, lastCol - 1).Merge();

        //     // 3. lastCol1:lastCol3 — thông tin phụ, căn trên-phải
        //     ws.Range(1, lastCol, 3, lastCol).Merge();
        //     ws.Cell(1, lastCol).Value = rightValue;
        //     ws.Cell(1, lastCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        //     ws.Cell(1, lastCol).Style.Alignment.Vertical   = XLAlignmentVerticalValues.Top;
        // }
        private static void UpdateHeaderRowMerges(IXLWorksheet ws, int lastCol)
        {
            if (ws == null) throw new ArgumentNullException(nameof(ws));
            if (lastCol < 1) throw new ArgumentException("lastCol phải >= 1");

            // 1. Lấy info ban đầu từ A1
            var info = ws.Cell(1, 1).Value;

            // 2. Lấy tất cả merge trong vùng header (row 1 -> 3)
            var headerMerges = ws.MergedRanges
                .Where(m => m.RangeAddress.FirstAddress.RowNumber >= 1
                        && m.RangeAddress.LastAddress.RowNumber  <= 3)
                .ToList();

            // 3. Unmerge toàn bộ header
            foreach (var m in headerMerges)
                m.Unmerge();

            // 4. Merge full từ A1 -> lastCol (3 dòng)
            var mergeRange = ws.Range(1, 1, 3, lastCol);
            mergeRange.Merge();

            // 5. Gán lại giá trị info vào A1 (cell gốc của merge)
            var cell = ws.Cell(1, 1);
            cell.Value = info;

            // 6. Style: hiển thị bên phải + căn trên
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            cell.Style.Alignment.Vertical   = XLAlignmentVerticalValues.Top;

            // (Optional) format thêm nếu cần
            // cell.Style.Font.Bold = true;
            // cell.Style.Font.FontSize = 12;

            // 7. Đảm bảo wrap text nếu info dài
            cell.Style.Alignment.WrapText = true;
        }

        // -------------------------------------------------------
        // Rows 4-5: tên biểu mẫu + thông tin kíp
        // -------------------------------------------------------
        private static void RenderInfoRows(IXLWorksheet ws, string bieuMau,
            List<HRC2ThongKeRow> rows, int lastCol,
            int? scope = null, string gioBatDau = "", string gioKetThuc = "",
            DateOnly? ngayPhieu = null, int? caPhieu = null, string kip = "")
        {
            var d = rows.FirstOrDefault()?.Data;
            var caValue = caPhieu ?? d?.Ca ?? 0;

            var rawDate = d?.Ngay ?? d?.NgaySx; // dùng fallback khi không có ngay từ BM_Phieu
            string ngayStr = ngayPhieu.HasValue ? ngayPhieu.Value.ToString("dd/MM/yyyy") : (rawDate?.ToString("dd/MM/yyyy") ?? "");

            string key   = bieuMau.ToUpperInvariant();
            string tenBm = key.Contains("BOF") ? $"BIÊN BẢN TIÊU HAO NẤU LUYỆN LÒ THỔI {scope}"
                         : key.Contains("LF")  ? "BẢNG TIÊU HAO NẤU LUYỆN LÒ TINH LUYỆN LF"
                         : key.Contains("RH")  ? $"BẢNG TIÊU HAO NẤU LUYỆN LÒ TINH LUYỆN RH {scope}"
                         : bieuMau;

            // Row 4: tên biểu mẫu — bold, font 13, căn giữa
            ws.Range(4, 1, 4, lastCol).Merge();
            var c4 = ws.Cell(4, 1);
            c4.Value                      = tenBm;
            c4.Style.Font.Bold            = true;
            c4.Style.Font.FontSize        = 13;
            c4.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            c4.Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;

            // Row 5: thông tin kíp — italic, căn giữa
            ws.Range(5, 1, 5, lastCol).Merge();
            var c5 = ws.Cell(5, 1);

            // Tính giờ bắt đầu/kết thúc. Nếu tham số rỗng thì tự suy ra theo ca.
            string gioBatDauLocal  = string.IsNullOrWhiteSpace(gioBatDau) ? "" : gioBatDau;
            string gioKetThucLocal = string.IsNullOrWhiteSpace(gioKetThuc) ? "" : gioKetThuc;
            string ngayKetThuc     = ngayStr;

            if (string.IsNullOrWhiteSpace(gioBatDauLocal) || string.IsNullOrWhiteSpace(gioKetThucLocal))
            {
                if (caValue == 1)
                {
                    gioBatDauLocal  = "08 giờ 00";
                    gioKetThucLocal = "20 giờ 00";
                }
                else if (caValue == 2)
                {
                    gioBatDauLocal  = "20 giờ 00";
                    gioKetThucLocal = "08 giờ 00";

                    if (ngayPhieu.HasValue) ngayKetThuc = ngayPhieu.Value.AddDays(1).ToString("dd/MM/yyyy");
                    else if (rawDate.HasValue) ngayKetThuc = rawDate.Value.AddDays(1).ToString("dd/MM/yyyy");
                }
            }

            var kipSuffix = string.IsNullOrWhiteSpace(kip) ? "" : kip;
            c5.Value = $"Kíp {caValue}{kipSuffix}: Từ {gioBatDauLocal} ngày {ngayStr} đến {gioKetThucLocal} ngày {ngayKetThuc}";
            c5.Style.Font.Italic          = true;
            c5.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            c5.Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;
        }

        // ===============================================================
        // Rows 6-7: column headers
        //   Cột cố định        → merge dọc rows 6-7
        //   Cột phụ liệu động  → row 6 = tên (header cha), row 7 = rỗng (header con)
        //   Nhóm cột ghép      → row 6 merge ngang, row 7 = tên từng cột con
        //   Cột đơn sau nhóm   → merge dọc rows 6-7
        // ===============================================================

        // BOF: startCol=6 | fixed before: 5 cols | fixed after: Nhiên liệu(2) + GhiChu(1) | phân bổ: P cols
        private static void RenderColumnHeaders_BOF(IXLWorksheet ws,
            List<PhuLieuHeaderTable> headers, List<PhuLieuHeaderTable> phanBoHeaders)
        {
            int s = GetPhuLieuStartCol("BOF"); // 6

            MergeVertCell(ws, 1, "STT");
            MergeVertCell(ws, 2, "Mẻ thổi");
            MergeVertCell(ws, 3, "Mác thép");
            MergeVertCell(ws, 4, "KL gang lỏng\n(tấn)");
            MergeVertCell(ws, 5, "KL thép phế\n(tấn)");

            if (headers.Count > 0)
            {
                MergeHorizCell(ws, HeaderParentRow, s, s + headers.Count - 1, "Phụ gia công nghệ (Kg)");
                for (int i = 0; i < headers.Count; i++)
                    HeaderCell(ws, HeaderChildRow, s + i, headers[i].TenPhuLieu);
            }

            int a = s + headers.Count;
            MergeHorizCell(ws, HeaderParentRow, a, a + 1, "Nhiên liệu");
            HeaderCell(ws, HeaderChildRow, a,     "Oxy");
            HeaderCell(ws, HeaderChildRow, a + 1, "Nito");
            MergeVertCell(ws, a + 2, "Ghi chú");

            RenderPhanBoHeaders(ws, a + 3, phanBoHeaders);
        }

        // LF: startCol=5 | fixed before: 4 cols | fixed after: Khí(1) + 3 cols đơn | phân bổ: P cols
        private static void RenderColumnHeaders_LF(IXLWorksheet ws,
            List<PhuLieuHeaderTable> headers, List<PhuLieuHeaderTable> phanBoHeaders)
        {
            int s = GetPhuLieuStartCol("LF"); // 5

            MergeVertCell(ws, 1, "STT");
            MergeVertCell(ws, 2, "Mẻ thổi");
            MergeVertCell(ws, 3, "Mác thép");
            MergeVertCell(ws, 4, "KL thép lỏng\n(tấn)");

            if (headers.Count > 0)
            {
                MergeHorizCell(ws, HeaderParentRow, s, s + headers.Count - 1, "Phụ gia công nghệ (Kg)");
                for (int i = 0; i < headers.Count; i++)
                    HeaderCell(ws, HeaderChildRow, s + i, headers[i].TenPhuLieu);
            }

            int a = s + headers.Count;
            HeaderCell(ws, HeaderParentRow, a,     "Khí");
            HeaderCell(ws, HeaderChildRow,  a,     "Argon");
            MergeVertCell(ws, a + 1, "Que lấy mẫu");
            MergeVertCell(ws, a + 2, "Que đo nhiệt");
            MergeVertCell(ws, a + 3, "Ghi chú");

            RenderPhanBoHeaders(ws, a + 4, phanBoHeaders);
        }

        // RH: startCol=5 | fixed before: 4 cols | fixed after: Khí(3) + 3 cols đơn | phân bổ: P cols
        private static void RenderColumnHeaders_RH(IXLWorksheet ws,
            List<PhuLieuHeaderTable> headers, List<PhuLieuHeaderTable> phanBoHeaders)
        {
            int s = GetPhuLieuStartCol("RH"); // 5

            MergeVertCell(ws, 1, "STT");
            MergeVertCell(ws, 2, "Mẻ thổi");
            MergeVertCell(ws, 3, "Mác thép");
            MergeVertCell(ws, 4, "KL thép lỏng\n(tấn)");

            if (headers.Count > 0)
            {
                MergeHorizCell(ws, HeaderParentRow, s, s + headers.Count - 1, "Phụ gia công nghệ (Kg)");
                for (int i = 0; i < headers.Count; i++)
                    HeaderCell(ws, HeaderChildRow, s + i, headers[i].TenPhuLieu);
            }

            int a = s + headers.Count;
            MergeHorizCell(ws, HeaderParentRow, a, a + 2, "Khí");
            HeaderCell(ws, HeaderChildRow, a,     "Argon");
            HeaderCell(ws, HeaderChildRow, a + 1, "Nito");
            HeaderCell(ws, HeaderChildRow, a + 2, "Oxi");
            MergeVertCell(ws, a + 3, "Que lấy mẫu");
            MergeVertCell(ws, a + 4, "Que đo nhiệt");
            MergeVertCell(ws, a + 5, "Ghi chú");

            RenderPhanBoHeaders(ws, a + 6, phanBoHeaders);
        }

        /// <summary>Render group header "Phân bổ" + tên từng cột phân bổ bắt đầu từ startCol.</summary>
        private static void RenderPhanBoHeaders(IXLWorksheet ws, int startCol, List<PhuLieuHeaderTable> phanBoHeaders)
        {
            if (phanBoHeaders.Count == 0) return;

            // Row 6: group header "Phân bổ" span toàn bộ các cột phân bổ
            MergeHorizCell(ws, HeaderParentRow, startCol, startCol + phanBoHeaders.Count - 1, "Phân bổ");

            // Row 7: tên từng cột
            for (int i = 0; i < phanBoHeaders.Count; i++)
                HeaderCell(ws, HeaderChildRow, startCol + i, phanBoHeaders[i].TenPhuLieu);
        }

        // ===============================================================
        // Rows 8+: data rows
        // ===============================================================

        private static void RenderDataRows_BOF(IXLWorksheet ws,
            List<PhuLieuHeaderTable> headers, List<PhuLieuHeaderTable> phanBoHeaders,
            List<HRC2ThongKeRow> rows, int dataStartRow, int lastCol)
        {
            int s = GetPhuLieuStartCol("BOF"); // 6
            int r = dataStartRow;
            int n = 1;

            foreach (var row in rows)
            {
                var d   = row.Data!;
                var vm  = row.Values.ToDictionary(v => v.IDHeaderKey, v => v.TotalKLPhuGia);
                var pbm = row.Values.ToDictionary(v => v.IDHeaderKey, v => v.KLPhanBo);

                ws.Cell(r, 1).Value = n++;
                ws.Cell(r, 2).Value = d.MeThoi  ?? "";
                ws.Cell(r, 3).Value = d.MacThep ?? "";
                ws.Cell(r, 4).Value = Num(d.KLGangLong);
                ws.Cell(r, 5).Value = Num(d.KLThepPhe);

                for (int i = 0; i < headers.Count; i++)
                    ws.Cell(r, s + i).Value = vm.TryGetValue(headers[i].IDHeaderKey, out var kl) ? Num(kl) : Blank.Value;

                int a = s + headers.Count;
                ws.Cell(r, a++).Value = Num(d.O2);
                ws.Cell(r, a++).Value = Num(d.N2);
                ws.Cell(r, a++).Value = d.GhiChu ?? "";

                for (int i = 0; i < phanBoHeaders.Count; i++)
                    ws.Cell(r, a + i).Value = pbm.TryGetValue(phanBoHeaders[i].IDHeaderKey, out var pb) ? Num(pb) : Blank.Value;

                r++;
            }
        }

        private static void RenderDataRows_LF(IXLWorksheet ws,
            List<PhuLieuHeaderTable> headers, List<PhuLieuHeaderTable> phanBoHeaders,
            List<HRC2ThongKeRow> rows, int dataStartRow, int lastCol)
        {
            int s = GetPhuLieuStartCol("LF"); // 5
            int r = dataStartRow;
            int n = 1;

            foreach (var row in rows)
            {
                var d   = row.Data!;
                var vm  = row.Values.ToDictionary(v => v.IDHeaderKey, v => v.TotalKLPhuGia);
                var pbm = row.Values.ToDictionary(v => v.IDHeaderKey, v => v.KLPhanBo);

                ws.Cell(r, 1).Value = n++;
                ws.Cell(r, 2).Value = d.MeThoi  ?? "";
                ws.Cell(r, 3).Value = d.MacThep ?? "";
                ws.Cell(r, 4).Value = Num(d.KLThepLong);

                for (int i = 0; i < headers.Count; i++)
                    ws.Cell(r, s + i).Value = vm.TryGetValue(headers[i].IDHeaderKey, out var kl) ? Num(kl) : Blank.Value;

                int a = s + headers.Count;
                ws.Cell(r, a++).Value = Num(d.AR_LF);
                ws.Cell(r, a++).Value = NumInt(d.QueLayMau);
                ws.Cell(r, a++).Value = NumInt(d.QueDoNhiet);
                ws.Cell(r, a++).Value = d.GhiChu ?? "";

                for (int i = 0; i < phanBoHeaders.Count; i++)
                    ws.Cell(r, a + i).Value = pbm.TryGetValue(phanBoHeaders[i].IDHeaderKey, out var pb) ? Num(pb) : Blank.Value;

                r++;
            }
        }

        private static void RenderDataRows_RH(IXLWorksheet ws,
            List<PhuLieuHeaderTable> headers, List<PhuLieuHeaderTable> phanBoHeaders,
            List<HRC2ThongKeRow> rows, int dataStartRow, int lastCol)
        {
            int s = GetPhuLieuStartCol("RH"); // 5
            int r = dataStartRow;
            int n = 1;

            foreach (var row in rows)
            {
                var d   = row.Data!;
                var vm  = row.Values.ToDictionary(v => v.IDHeaderKey, v => v.TotalKLPhuGia);
                var pbm = row.Values.ToDictionary(v => v.IDHeaderKey, v => v.KLPhanBo);

                ws.Cell(r, 1).Value = n++;
                ws.Cell(r, 2).Value = d.MeThoi  ?? "";
                ws.Cell(r, 3).Value = d.MacThep ?? "";
                ws.Cell(r, 4).Value = Num(d.KLThepLong);

                for (int i = 0; i < headers.Count; i++)
                    ws.Cell(r, s + i).Value = vm.TryGetValue(headers[i].IDHeaderKey, out var kl) ? Num(kl) : Blank.Value;

                int a = s + headers.Count;
                ws.Cell(r, a++).Value = Num(d.AR_RH);
                ws.Cell(r, a++).Value = Num(d.N2);
                ws.Cell(r, a++).Value = Num(d.O2);
                ws.Cell(r, a++).Value = NumInt(d.QueLayMau);
                ws.Cell(r, a++).Value = NumInt(d.QueDoNhiet);
                ws.Cell(r, a++).Value = d.GhiChu ?? "";

                for (int i = 0; i < phanBoHeaders.Count; i++)
                    ws.Cell(r, a + i).Value = pbm.TryGetValue(phanBoHeaders[i].IDHeaderKey, out var pb) ? Num(pb) : Blank.Value;

                r++;
            }
        }

        // -------------------------------------------------------
        // Total rows (sau data, trước footer)
        // -------------------------------------------------------

        private static void RenderTotalRow_BOF(IXLWorksheet ws, int r, int lastCol,
            List<PhuLieuHeaderTable> headers, List<PhuLieuHeaderTable> phanBoHeaders,
            List<HRC2ThongKeRow> rows)
        {
            int s = GetPhuLieuStartCol("BOF"); // 6

            ws.Range(r, 1, r, 3).Merge();
            ws.Cell(r, 1).Value                      = "Tổng cộng";
            ws.Cell(r, 1).Style.Font.Bold            = true;
            ws.Cell(r, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(r, 4).Value = (XLCellValue)rows.Sum(x => x.Data?.KLGangLong ?? 0);
            ws.Cell(r, 5).Value = (XLCellValue)rows.Sum(x => x.Data?.KLThepPhe  ?? 0);

            for (int i = 0; i < headers.Count; i++)
            {
                var hId = headers[i].IDHeaderKey;
                ws.Cell(r, s + i).Value = (XLCellValue)rows.Sum(x =>
                    x.Values.FirstOrDefault(v => v.IDHeaderKey == hId)?.TotalKLPhuGia ?? 0);
            }

            int a = s + headers.Count;
            ws.Cell(r, a++).Value = (XLCellValue)rows.Sum(x => x.Data?.O2 ?? 0);
            ws.Cell(r, a++).Value = (XLCellValue)rows.Sum(x => x.Data?.N2 ?? 0);
            a++; // Ghi chú — bỏ qua

            for (int i = 0; i < phanBoHeaders.Count; i++)
            {
                var hId = phanBoHeaders[i].IDHeaderKey;
                ws.Cell(r, a + i).Value = (XLCellValue)rows.Sum(x =>
                    x.Values.FirstOrDefault(v => v.IDHeaderKey == hId)?.KLPhanBo ?? 0);
            }

        }

        private static void RenderTotalRow_LF(IXLWorksheet ws, int r, int lastCol,
            List<PhuLieuHeaderTable> headers, List<PhuLieuHeaderTable> phanBoHeaders,
            List<HRC2ThongKeRow> rows)
        {
            int s = GetPhuLieuStartCol("LF"); // 5

            ws.Range(r, 1, r, 3).Merge();
            ws.Cell(r, 1).Value                      = "Tổng cộng";
            ws.Cell(r, 1).Style.Font.Bold            = true;
            ws.Cell(r, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(r, 4).Value = (XLCellValue)rows.Sum(x => x.Data?.KLThepLong ?? 0);

            for (int i = 0; i < headers.Count; i++)
            {
                var hId = headers[i].IDHeaderKey;
                ws.Cell(r, s + i).Value = (XLCellValue)rows.Sum(x =>
                    x.Values.FirstOrDefault(v => v.IDHeaderKey == hId)?.TotalKLPhuGia ?? 0);
            }

            int a = s + headers.Count;
            ws.Cell(r, a++).Value = (XLCellValue)rows.Sum(x => x.Data?.AR_LF ?? 0);
            ws.Cell(r, a++).Value = (XLCellValue)rows.Sum(x => x.Data?.QueLayMau ?? 0);
            ws.Cell(r, a++).Value = (XLCellValue)rows.Sum(x => x.Data?.QueDoNhiet ?? 0);
            a++; // Ghi chú — bỏ qua

            for (int i = 0; i < phanBoHeaders.Count; i++)
            {
                var hId = phanBoHeaders[i].IDHeaderKey;
                ws.Cell(r, a + i).Value = (XLCellValue)rows.Sum(x =>
                    x.Values.FirstOrDefault(v => v.IDHeaderKey == hId)?.KLPhanBo ?? 0);
            }
        }

        private static void RenderTotalRow_RH(IXLWorksheet ws, int r, int lastCol,
            List<PhuLieuHeaderTable> headers, List<PhuLieuHeaderTable> phanBoHeaders,
            List<HRC2ThongKeRow> rows)
        {
            int s = GetPhuLieuStartCol("RH"); // 5

            ws.Range(r, 1, r, 3).Merge();
            ws.Cell(r, 1).Value                      = "Tổng cộng";
            ws.Cell(r, 1).Style.Font.Bold            = true;
            ws.Cell(r, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(r, 4).Value = (XLCellValue)rows.Sum(x => x.Data?.KLThepLong ?? 0);

            for (int i = 0; i < headers.Count; i++)
            {
                var hId = headers[i].IDHeaderKey;
                ws.Cell(r, s + i).Value = (XLCellValue)rows.Sum(x =>
                    x.Values.FirstOrDefault(v => v.IDHeaderKey == hId)?.TotalKLPhuGia ?? 0);
            }

            int a = s + headers.Count;
            ws.Cell(r, a++).Value = (XLCellValue)rows.Sum(x => x.Data?.AR_RH ?? 0);
            ws.Cell(r, a++).Value = (XLCellValue)rows.Sum(x => x.Data?.N2    ?? 0);
            ws.Cell(r, a++).Value = (XLCellValue)rows.Sum(x => x.Data?.O2    ?? 0);
            ws.Cell(r, a++).Value = (XLCellValue)rows.Sum(x => x.Data?.QueLayMau  ?? 0);
            ws.Cell(r, a++).Value = (XLCellValue)rows.Sum(x => x.Data?.QueDoNhiet ?? 0);
            a++; // Ghi chú — bỏ qua

            for (int i = 0; i < phanBoHeaders.Count; i++)
            {
                var hId = phanBoHeaders[i].IDHeaderKey;
                ws.Cell(r, a + i).Value = (XLCellValue)rows.Sum(x =>
                    x.Values.FirstOrDefault(v => v.IDHeaderKey == hId)?.KLPhanBo ?? 0);
            }
        }

        // -------------------------------------------------------
        // Header cell helpers
        // -------------------------------------------------------

        /// <summary>Merge dọc rows 6-7 cho cột col, set giá trị + style.</summary>
        private static void MergeVertCell(IXLWorksheet ws, int col, string text)
        {
            ws.Range(HeaderParentRow, col, HeaderChildRow, col).Merge();
            ApplyHeaderStyle(ws.Cell(HeaderParentRow, col), text);
        }

        /// <summary>Merge ngang một row khoảng cột [c1, c2], set giá trị + style.</summary>
        private static void MergeHorizCell(IXLWorksheet ws, int row, int c1, int c2, string text)
        {
            ws.Range(row, c1, row, c2).Merge();
            ApplyHeaderStyle(ws.Cell(row, c1), text);
        }

        /// <summary>Set ô header đơn (không merge).</summary>
        private static void HeaderCell(IXLWorksheet ws, int row, int col, string text)
            => ApplyHeaderStyle(ws.Cell(row, col), text);

        private static void ApplyHeaderStyle(IXLCell cell, string text)
        {
            cell.Value                             = text;
            cell.Style.Font.Bold                   = true;
            cell.Style.Alignment.Horizontal        = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical          = XLAlignmentVerticalValues.Center;
            cell.Style.Alignment.WrapText          = true;
            cell.Style.Border.OutsideBorder        = XLBorderStyleValues.Thin;
            cell.Style.Border.OutsideBorderColor   = XLColor.Black;
        }

        // -------------------------------------------------------
        // Footer configs (BOF / RH)
        // -------------------------------------------------------

        private static readonly FooterConfig BofFooterConfig = new()
        {
            LuongTonLabels = new List<string>
            {
                "Lượng Vôi",
                "Lượng Dolomite",
                "Lượng Quặng",
                "Lượng FeSi",
                "Lượng SiMn",
                "Lượng FeMn",
                "Lượng LDSF",
                "Lượng Than",
                "Lượng AL",
                "Lượng Chất tăng cacbon",
                "Lượng HC-FeMn75",
                "Nguyên liệu khác:"
            }
        };

        private static readonly FooterConfig RhFooterConfig = new()
        {
            LuongTonLabels = new List<string>
            {
                "Lượng SiMn (kg)",
                "Lượng FeSi (kg)",
                "Lượng Vôi (kg)",
                "Lượng Than (kg)",
                "Lượng FeMn (kg)",
                "Lượng Huỳnh thạch (kg)",
                "Lượng Nhôm (kg)",
                "Khác"
            }
        };

        /// <summary>
        /// Render phần footer POST-BODY (sau data + 1 dòng trắng) gồm 3 phần:
        /// 1. Nhãn kíp — Tồn đầu kíp | Nhập trong kíp | Tồn cuối kíp
        /// 2. Khung lượng tồn (merge 4 vùng theo fixedTextCols)
        /// 3. Dòng Trưởng kíp / Người lập
        /// </summary>
        /// <returns>Hàng cuối cùng của khối footer (dòng ký tên).</returns>
        public static int RenderFooter(
            IXLWorksheet ws,
            int startRow,
            int lastCol,
            FooterConfig config)
        {
            int N = lastCol;

            // Boundaries cố định 4 cột/nhóm, tính từ cuối (1-based)
            // Đảm bảo g1s >= 2 để luôn có ít nhất 1 cột silo bên trái
            int g1s = Math.Max(2, N - 11); // Tồn đầu kíp: start
            int g1e = N - 8;               // Tồn đầu kíp: end
            int g2s = N - 7;               // Nhập trong kíp: start
            int g2e = N - 4;               // Nhập trong kíp: end
            int g3s = N - 3;               // Tồn cuối kíp: start
            int g3e = N;                   // Tồn cuối kíp: end

            int r = startRow;

            // ── Row header ──────────────────────────────────────────────
            // Tồn trên silo: col 1 → g1s-1
            ws.Range(r, 1,   r, g1s - 1).Merge();
            SetFooterLabelStyle(ws.Cell(r, 1), "Tồn trên silo");
            // 3 nhóm
            ws.Range(r, g1s, r, g1e).Merge();
            SetFooterLabelStyle(ws.Cell(r, g1s), config.LabelTonDauKip);
            ws.Range(r, g2s, r, g2e).Merge();
            SetFooterLabelStyle(ws.Cell(r, g2s), config.LabelNhapTrongKip);
            ws.Range(r, g3s, r, g3e).Merge();
            SetFooterLabelStyle(ws.Cell(r, g3s), config.LabelTonCuoiKip);
            ApplyBorderOnly(ws.Cell(r, 1));
            ApplyBorderOnly(ws.Cell(r, g1s));
            ApplyBorderOnly(ws.Cell(r, g2s));
            ApplyBorderOnly(ws.Cell(r, g3s));
            r++;

            // ── Label rows ────────────────────────────────────────────────
            // Mỗi dòng: đúng 4 vùng merge, không có ô đơn lẻ nào.
            // Nhóm 0 (silo): merge cột 1 → g1s-1, hiển thị label căn trái.
            // Nhóm 1,2,3: merge 4 cột, ô trống, có border.
            foreach (var label in config.LuongTonLabels)
            {
                ws.Range(r, 1,   r, g1s - 1).Merge();
                ws.Cell(r, 1).Value                      = label;
                ws.Cell(r, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                ws.Cell(r, 1).Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;
                ApplyBorderOnly(ws.Cell(r, 1));

                ws.Range(r, g1s, r, g1e).Merge();
                ApplyBorderOnly(ws.Cell(r, g1s));
                ws.Range(r, g2s, r, g2e).Merge();
                ApplyBorderOnly(ws.Cell(r, g2s));
                ws.Range(r, g3s, r, g3e).Merge();
                ApplyBorderOnly(ws.Cell(r, g3s));
                r++;
            }
            // Footer block kết thúc ở dòng ngay trước dòng chữ ký.
            return Math.Max(startRow, r - 1);
        }

        /// <summary>
        /// Render dòng chữ ký ở ngoài khối footer bảng (không nằm trong range apply border chung).
        /// </summary>
        private static void RenderSignatureRow(IXLWorksheet ws, int signRow, int lastCol, FooterConfig config)
        {
            int N = lastCol;
            int g3s = N - 3; // Tồn cuối kíp: start
            int g3e = N;     // Tồn cuối kíp: end

            // Trưởng kíp: col 1 → g3s-1
            int leftEnd = g3s - 1;
            if (leftEnd >= 1)
            {
                ws.Range(signRow, 1, signRow, leftEnd).Merge();
                ws.Cell(signRow, 1).Value = config.LabelTruongKip;
                ws.Cell(signRow, 1).Style.Font.Bold = true;
                ws.Cell(signRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(signRow, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                // ApplyBorderOnly(ws.Cell(signRow, 1));
            }

            // Người lập: g3s → N
            if (g3s <= g3e)
            {
                ws.Range(signRow, g3s, signRow, g3e).Merge();
                ws.Cell(signRow, g3s).Value = config.LabelNguoiLap;
                ws.Cell(signRow, g3s).Style.Font.Bold = true;
                ws.Cell(signRow, g3s).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(signRow, g3s).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                // ApplyBorderOnly(ws.Cell(signRow, g3s));
            }
        }

        private static void SetFooterLabelStyle(IXLCell cell, string text)
        {
            cell.Value                      = text;
            cell.Style.Font.Bold            = true;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;
        }

        // -------------------------------------------------------
        // Shared helpers
        // -------------------------------------------------------

        private static XLCellValue Num(double? v)   => v.HasValue ? (XLCellValue)v.Value : Blank.Value;
        private static XLCellValue NumInt(int? v)    => v.HasValue ? (XLCellValue)v.Value : Blank.Value;

        // ===============================================================
        // PDF Export — layout giống Excel (DinkToPdf)
        // ===============================================================

        public async Task<ExportFileResult> ExportPdfDetailAsync(
            DateOnly ngay, int ca, string bieuMau, int scope, Guid idPhieu,
            string gioBatDau = "", string gioKetThuc = "")
        {
            var (headersBOF, headersLFRH, rows) = await GetExportDataAsync(ngay, ca, bieuMau, scope);
            var imageSignsDto = await _pheDuyetService.GetPheDuyetPhieuAsync(idPhieu);

            // Footer HRC2_BB_NauLuyen có 2 vị trí ký:
            // - CapDuyet = 1: Trưởng/Phó kíp
            // - CapDuyet = 0: Người lập
            var truongKip = imageSignsDto?.FirstOrDefault(x => x.CapDuyet == 1);
            var nguoiLap = imageSignsDto?.FirstOrDefault(x => x.CapDuyet == 0);
            string chuKyTruongKipHtml = _pheDuyetService.FormatChuKy(truongKip?.ChuKy);
            string chuKyNguoiLapHtml = _pheDuyetService.FormatChuKy(nguoiLap?.ChuKy);

            bool isBof = bieuMau.Equals("BOF", StringComparison.OrdinalIgnoreCase);
            var headers = isBof ? headersBOF : headersLFRH;
            var phanBoKeyIds = rows
                .SelectMany(r => r.Values)
                .Where(v => v.KLPhanBo.HasValue && v.KLPhanBo != 0)
                .Select(v => v.IDHeaderKey)
                .Distinct()
                .ToHashSet();
            var phanBoHeaders = headers.Where(h => phanBoKeyIds.Contains(h.IDHeaderKey)).ToList();

            string templateName = bieuMau.ToUpperInvariant() switch
            {
                "BOF" => "HRC2_BB_NauLuyen_BOF",
                "LF"  => "HRC2_BB_NauLuyen_LF",
                "RH"  => "HRC2_BB_NauLuyen_RH",
                _     => bieuMau
            };

            var html = await BuildPdfHtmlAsync(
                bieuMau,
                ngay,
                ca,
                scope,
                gioBatDau,
                gioKetThuc,
                headers,
                phanBoHeaders,
                rows,
                chuKyTruongKipHtml,
                chuKyNguoiLapHtml);

            var doc = new HtmlToPdfDocument
            {
                GlobalSettings =
                {
                    PaperSize   = PaperKind.A4,
                    Orientation = DinkToPdf.Orientation.Landscape,
                    Margins     = new MarginSettings { Top = 10, Bottom = 10, Left = 10, Right = 10, Unit = Unit.Millimeters }
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

            return new ExportFileResult
            {
                Content     = _pdfConverter.Convert(doc),
                FileName    = $"{templateName}_Ca{ca}_{ngay:ddMMyyyy}.pdf",
                ContentType = "application/pdf"
            };
        }

        private async Task<string> BuildPdfHtmlAsync(
            string bieuMau, DateOnly ngay, int ca, int? scope,
            string gioBatDau, string gioKetThuc,
            List<PhuLieuHeaderTable> headers, List<PhuLieuHeaderTable> phanBoHeaders,
            List<HRC2ThongKeRow> rows,
            string chuKyTruongKipHtml,
            string chuKyNguoiLapHtml)
        {
            var logoUrl = _configuration.GetValue<string>("AppSettings:LogoUrl")
                          ?? "https://report.hoaphatdungquat.vn/img/logoHP.png";

            string key   = bieuMau.ToUpperInvariant();
            string tenBm = key.Contains("BOF") ? $"BIÊN BẢN TIÊU HAO NẤU LUYỆN LÒ THỔI {scope}"
                         : key.Contains("LF")  ? "BẢNG TIÊU HAO NẤU LUYỆN LÒ TINH LUYỆN LF"
                         : key.Contains("RH")  ? $"BẢNG TIÊU HAO NẤU LUYỆN LÒ TINH LUYỆN RH {scope}"
                         : bieuMau;

            // Ngày/ca cho InfoKip
            string ngayStr = ngay.ToString("dd/MM/yyyy");
            string caStr   = ca.ToString();

            // Nếu tham số gioBatDau/gioKetThuc rỗng thì tự tính như Excel
            string gioBatDauLocal  = string.IsNullOrWhiteSpace(gioBatDau) ? "" : gioBatDau;
            string gioKetThucLocal = string.IsNullOrWhiteSpace(gioKetThuc) ? "" : gioKetThuc;
            string ngayKetThuc     = ngayStr;

            if (string.IsNullOrWhiteSpace(gioBatDauLocal) || string.IsNullOrWhiteSpace(gioKetThucLocal))
            {
                if (ca == 1)
                {
                    gioBatDauLocal  = "08 giờ 00";
                    gioKetThucLocal = "20 giờ 00";
                }
                else if (ca == 2)
                {
                    gioBatDauLocal  = "20 giờ 00";
                    gioKetThucLocal = "08 giờ 00";
                    // tăng ngày +1 cho ca đêm
                    ngayKetThuc = ngay.AddDays(1).ToString("dd/MM/yyyy");
                }
            }

            string infoKip = $"Kíp {caStr}: Từ {gioBatDauLocal} ngày {ngayStr} đến {gioKetThucLocal} ngày {ngayKetThuc}";

            string bmCode = key.Contains("BOF") ? "BM.08/QT.05.15 <br /> Ngày hiệu lực: 10/01/2025 <br /> Lần sửa đổi: 00"
                          : key.Contains("LF")  ? "BM.14/QT.05.15 <br /> Ngày hiệu lực: 10/01/2025 <br /> Lần sửa đổi: 00"
                          :                       "BM.16/QT.05.15 <br /> Ngày hiệu lực: 10/01/2025 <br /> Lần sửa đổi: 00";

            string thead = key.Contains("BOF") ? PdfThead_BOF(headers, phanBoHeaders)
                         : key.Contains("LF")  ? PdfThead_LF(headers, phanBoHeaders)
                         :                       PdfThead_RH(headers, phanBoHeaders);

            string tbody = key.Contains("BOF") ? PdfTbody_BOF(headers, phanBoHeaders, rows)
                         : key.Contains("LF")  ? PdfTbody_LF(headers, phanBoHeaders, rows)
                         :                       PdfTbody_RH(headers, phanBoHeaders, rows);

            int N = ComputeLastCol(
                key.Contains("BOF") ? "HRC2_BB_NauLuyen_BOF"
              : key.Contains("LF")  ? "HRC2_BB_NauLuyen_LF"
              :                       "HRC2_BB_NauLuyen_RH",
                headers.Count, phanBoHeaders.Count);

            string footer = (key.Contains("BOF") || key.Contains("RH") || key.Contains("LF"))
                ? PdfFooterHtml(
                    N,
                    key.Contains("BOF") ? BofFooterConfig : RhFooterConfig,
                    chuKyTruongKipHtml,
                    chuKyNguoiLapHtml)
                : "";

            // Load template và replace placeholder
            var templatePath = Path.Combine(_env.WebRootPath, "template_html", "HRC2_BB_NauLuyen.html");
            var html = await File.ReadAllTextAsync(templatePath);

            return html
                .Replace("{{LogoUrl}}",    logoUrl)
                .Replace("{{BmCode}}",     bmCode)
                .Replace("{{TenBieuMau}}", tenBm)
                .Replace("{{InfoKip}}",    infoKip)
                .Replace("{{TheadRows}}",  thead)
                .Replace("{{TbodyRows}}",  tbody)
                .Replace("{{FooterHtml}}", footer);
        }

        // ── Thead helpers ───────────────────────────────────────────────

        private static string PdfThead_BOF(List<PhuLieuHeaderTable> h, List<PhuLieuHeaderTable> pb)
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

            if (pb.Count > 0)
            {
                r1.Append($"<th colspan=\"{pb.Count}\">Phân bổ</th>");
                foreach (var x in pb) r2.Append($"<th>{x.TenPhuLieu}</th>");
            }

            return $"<thead><tr>{r1}</tr><tr>{r2}</tr></thead>";
        }

        private static string PdfThead_LF(List<PhuLieuHeaderTable> h, List<PhuLieuHeaderTable> pb)
        {
            var r1 = new StringBuilder();
            var r2 = new StringBuilder();

            r1.Append("<th rowspan=\"2\">STT</th>");
            r1.Append("<th rowspan=\"2\">Mẻ thổi</th>");
            r1.Append("<th rowspan=\"2\">Mác thép</th>");
            r1.Append("<th rowspan=\"2\">KL thép lỏng<br/>(tấn)</th>");

            if (h.Count > 0)
            {
                r1.Append($"<th colspan=\"{h.Count}\">Phụ gia công nghệ (Kg)</th>");
                foreach (var x in h) r2.Append($"<th>{x.TenPhuLieu}</th>");
            }

            r1.Append("<th>Khí</th>");
            r2.Append("<th>Argon</th>");
            r1.Append("<th rowspan=\"2\">Que lấy mẫu</th>");
            r1.Append("<th rowspan=\"2\">Que đo nhiệt</th>");
            r1.Append("<th rowspan=\"2\">Ghi chú</th>");

            if (pb.Count > 0)
            {
                r1.Append($"<th colspan=\"{pb.Count}\">Phân bổ</th>");
                foreach (var x in pb) r2.Append($"<th>{x.TenPhuLieu}</th>");
            }

            return $"<thead><tr>{r1}</tr><tr>{r2}</tr></thead>";
        }

        private static string PdfThead_RH(List<PhuLieuHeaderTable> h, List<PhuLieuHeaderTable> pb)
        {
            var r1 = new StringBuilder();
            var r2 = new StringBuilder();

            r1.Append("<th rowspan=\"2\">STT</th>");
            r1.Append("<th rowspan=\"2\">Mẻ thổi</th>");
            r1.Append("<th rowspan=\"2\">Mác thép</th>");
            r1.Append("<th rowspan=\"2\">KL thép lỏng<br/>(tấn)</th>");

            if (h.Count > 0)
            {
                r1.Append($"<th colspan=\"{h.Count}\">Phụ gia công nghệ (Kg)</th>");
                foreach (var x in h) r2.Append($"<th>{x.TenPhuLieu}</th>");
            }

            r1.Append("<th colspan=\"3\">Khí</th>");
            r2.Append("<th>Argon</th><th>Nito</th><th>Oxi</th>");
            r1.Append("<th rowspan=\"2\">Que lấy mẫu</th>");
            r1.Append("<th rowspan=\"2\">Que đo nhiệt</th>");
            r1.Append("<th rowspan=\"2\">Ghi chú</th>");

            if (pb.Count > 0)
            {
                r1.Append($"<th colspan=\"{pb.Count}\">Phân bổ</th>");
                foreach (var x in pb) r2.Append($"<th>{x.TenPhuLieu}</th>");
            }

            return $"<thead><tr>{r1}</tr><tr>{r2}</tr></thead>";
        }

        // ── Tbody helpers ───────────────────────────────────────────────

        private static string PdfTbody_BOF(
            List<PhuLieuHeaderTable> headers, List<PhuLieuHeaderTable> phanBoHeaders,
            List<HRC2ThongKeRow> rows)
        {
            var sb = new StringBuilder("<tbody>");
            int stt = 1;
            foreach (var row in rows)
            {
                var d   = row.Data!;
                var vm  = row.Values.ToDictionary(v => v.IDHeaderKey, v => v.TotalKLPhuGia);
                var pbm = row.Values.ToDictionary(v => v.IDHeaderKey, v => v.KLPhanBo);
                sb.Append("<tr>");
                sb.Append($"<td>{stt++}</td><td>{d.MeThoi ?? ""}</td><td>{d.MacThep ?? ""}</td>");
                sb.Append($"<td>{PFmt(d.KLGangLong)}</td><td>{PFmt(d.KLThepPhe)}</td>");
                foreach (var hx in headers)
                    sb.Append($"<td>{PFmt(vm.TryGetValue(hx.IDHeaderKey, out var kl) ? kl : null)}</td>");
                sb.Append($"<td>{PFmt(d.O2)}</td><td>{PFmt(d.N2)}</td>");
                sb.Append($"<td class=\"td-left\">{d.GhiChu ?? ""}</td>");
                foreach (var pb in phanBoHeaders)
                    sb.Append($"<td>{PFmt(pbm.TryGetValue(pb.IDHeaderKey, out var pv) ? pv : null)}</td>");
                sb.Append("</tr>");
            }
            // Tổng cộng
            sb.Append("<tr class=\"total-row\"><td colspan=\"3\">Tổng cộng</td>");
            sb.Append($"<td>{PFmt(rows.Sum(x => x.Data?.KLGangLong ?? 0))}</td>");
            sb.Append($"<td>{PFmt(rows.Sum(x => x.Data?.KLThepPhe  ?? 0))}</td>");
            foreach (var hx in headers) { var id = hx.IDHeaderKey; sb.Append($"<td>{PFmt(rows.Sum(x => x.Values.FirstOrDefault(v => v.IDHeaderKey == id)?.TotalKLPhuGia ?? 0))}</td>"); }
            sb.Append($"<td>{PFmt(rows.Sum(x => x.Data?.O2 ?? 0))}</td>");
            sb.Append($"<td>{PFmt(rows.Sum(x => x.Data?.N2 ?? 0))}</td>");
            sb.Append("<td></td>");
            foreach (var pb in phanBoHeaders) { var id = pb.IDHeaderKey; sb.Append($"<td>{PFmt(rows.Sum(x => x.Values.FirstOrDefault(v => v.IDHeaderKey == id)?.KLPhanBo ?? 0))}</td>"); }
            sb.Append("</tr></tbody>");
            return sb.ToString();
        }

        private static string PdfTbody_LF(
            List<PhuLieuHeaderTable> headers, List<PhuLieuHeaderTable> phanBoHeaders,
            List<HRC2ThongKeRow> rows)
        {
            var sb = new StringBuilder("<tbody>");
            int stt = 1;
            foreach (var row in rows)
            {
                var d   = row.Data!;
                var vm  = row.Values.ToDictionary(v => v.IDHeaderKey, v => v.TotalKLPhuGia);
                var pbm = row.Values.ToDictionary(v => v.IDHeaderKey, v => v.KLPhanBo);
                sb.Append("<tr>");
                sb.Append($"<td>{stt++}</td><td>{d.MeThoi ?? ""}</td><td>{d.MacThep ?? ""}</td>");
                sb.Append($"<td>{PFmt(d.KLThepLong)}</td>");
                foreach (var hx in headers)
                    sb.Append($"<td>{PFmt(vm.TryGetValue(hx.IDHeaderKey, out var kl) ? kl : null)}</td>");
                sb.Append($"<td>{PFmt(d.AR_LF)}</td>");
                sb.Append($"<td>{PFmt(d.QueLayMau)}</td><td>{PFmt(d.QueDoNhiet)}</td>");
                sb.Append($"<td class=\"td-left\">{d.GhiChu ?? ""}</td>");
                foreach (var pb in phanBoHeaders)
                    sb.Append($"<td>{PFmt(pbm.TryGetValue(pb.IDHeaderKey, out var pv) ? pv : null)}</td>");
                sb.Append("</tr>");
            }
            sb.Append("<tr class=\"total-row\"><td colspan=\"3\">Tổng cộng</td>");
            sb.Append($"<td>{PFmt(rows.Sum(x => x.Data?.KLThepLong ?? 0))}</td>");
            foreach (var hx in headers) { var id = hx.IDHeaderKey; sb.Append($"<td>{PFmt(rows.Sum(x => x.Values.FirstOrDefault(v => v.IDHeaderKey == id)?.TotalKLPhuGia ?? 0))}</td>"); }
            sb.Append($"<td>{PFmt(rows.Sum(x => x.Data?.AR_LF     ?? 0))}</td>");
            sb.Append($"<td>{PFmt(rows.Sum(x => x.Data?.QueLayMau  ?? 0))}</td>");
            sb.Append($"<td>{PFmt(rows.Sum(x => x.Data?.QueDoNhiet ?? 0))}</td>");
            sb.Append("<td></td>");
            foreach (var pb in phanBoHeaders) { var id = pb.IDHeaderKey; sb.Append($"<td>{PFmt(rows.Sum(x => x.Values.FirstOrDefault(v => v.IDHeaderKey == id)?.KLPhanBo ?? 0))}</td>"); }
            sb.Append("</tr></tbody>");
            return sb.ToString();
        }

        private static string PdfTbody_RH(
            List<PhuLieuHeaderTable> headers, List<PhuLieuHeaderTable> phanBoHeaders,
            List<HRC2ThongKeRow> rows)
        {
            var sb = new StringBuilder("<tbody>");
            int stt = 1;
            foreach (var row in rows)
            {
                var d   = row.Data!;
                var vm  = row.Values.ToDictionary(v => v.IDHeaderKey, v => v.TotalKLPhuGia);
                var pbm = row.Values.ToDictionary(v => v.IDHeaderKey, v => v.KLPhanBo);
                sb.Append("<tr>");
                sb.Append($"<td>{stt++}</td><td>{d.MeThoi ?? ""}</td><td>{d.MacThep ?? ""}</td>");
                sb.Append($"<td>{PFmt(d.KLThepLong)}</td>");
                foreach (var hx in headers)
                    sb.Append($"<td>{PFmt(vm.TryGetValue(hx.IDHeaderKey, out var kl) ? kl : null)}</td>");
                sb.Append($"<td>{PFmt(d.AR_RH)}</td><td>{PFmt(d.N2)}</td><td>{PFmt(d.O2)}</td>");
                sb.Append($"<td>{PFmt(d.QueLayMau)}</td><td>{PFmt(d.QueDoNhiet)}</td>");
                sb.Append($"<td class=\"td-left\">{d.GhiChu ?? ""}</td>");
                foreach (var pb in phanBoHeaders)
                    sb.Append($"<td>{PFmt(pbm.TryGetValue(pb.IDHeaderKey, out var pv) ? pv : null)}</td>");
                sb.Append("</tr>");
            }
            sb.Append("<tr class=\"total-row\"><td colspan=\"3\">Tổng cộng</td>");
            sb.Append($"<td>{PFmt(rows.Sum(x => x.Data?.KLThepLong ?? 0))}</td>");
            foreach (var hx in headers) { var id = hx.IDHeaderKey; sb.Append($"<td>{PFmt(rows.Sum(x => x.Values.FirstOrDefault(v => v.IDHeaderKey == id)?.TotalKLPhuGia ?? 0))}</td>"); }
            sb.Append($"<td>{PFmt(rows.Sum(x => x.Data?.AR_RH      ?? 0))}</td>");
            sb.Append($"<td>{PFmt(rows.Sum(x => x.Data?.N2         ?? 0))}</td>");
            sb.Append($"<td>{PFmt(rows.Sum(x => x.Data?.O2         ?? 0))}</td>");
            sb.Append($"<td>{PFmt(rows.Sum(x => x.Data?.QueLayMau  ?? 0))}</td>");
            sb.Append($"<td>{PFmt(rows.Sum(x => x.Data?.QueDoNhiet ?? 0))}</td>");
            sb.Append("<td></td>");
            foreach (var pb in phanBoHeaders) { var id = pb.IDHeaderKey; sb.Append($"<td>{PFmt(rows.Sum(x => x.Values.FirstOrDefault(v => v.IDHeaderKey == id)?.KLPhanBo ?? 0))}</td>"); }
            sb.Append("</tr></tbody>");
            return sb.ToString();
        }

        // ── Footer helper ───────────────────────────────────────────────

        private static string PdfFooterHtml(
            int N,
            FooterConfig config,
            string chuKyTruongKipHtml,
            string chuKyNguoiLapHtml)
        {
            // Theo spec: 3 nhóm cuối mỗi nhóm 4 cột, silo chiếm phần còn lại
            int g3Span   = 4;
            int g2Span   = 4;
            int g1Span   = 4;
            int siloSpan = Math.Max(1, N - 12);

            var sb = new StringBuilder("<table class=\"footer-tbl\">");

            // Header row
            sb.Append("<tr>");
            sb.Append($"<th colspan=\"{siloSpan}\">Tồn trên silo</th>");
            sb.Append($"<th colspan=\"{g1Span}\">{config.LabelTonDauKip}</th>");
            sb.Append($"<th colspan=\"{g2Span}\">{config.LabelNhapTrongKip}</th>");
            sb.Append($"<th colspan=\"{g3Span}\">{config.LabelTonCuoiKip}</th>");
            sb.Append("</tr>");

            // Label rows — 4 vùng merge mỗi dòng
            foreach (var label in config.LuongTonLabels)
            {
                sb.Append("<tr>");
                sb.Append($"<td colspan=\"{siloSpan}\" class=\"td-left\">{label}</td>");
                sb.Append($"<td colspan=\"{g1Span}\"></td>");
                sb.Append($"<td colspan=\"{g2Span}\"></td>");
                sb.Append($"<td colspan=\"{g3Span}\"></td>");
                sb.Append("</tr>");
            }

            // Close footer table: sign row render tách riêng để không chịu border ngoài (outer medium) của footer.
            sb.Append("</table>");

            // Sign row (outside footer table)
            int truongKipSpan = siloSpan + g1Span + g2Span; // col 1 → N-4
            sb.Append($"<table style=\"width:100%;margin-top:20px; border:none; border-collapse:collapse;\">");
            sb.Append("<tr>");
            sb.Append(
                $"<td colspan=\"{truongKipSpan}\" style=\"text-align:center;font-weight:bold;border:none;vertical-align:middle;\">"
                + $"<div style=\"text-align:center;font-weight:bold;\">{config.LabelTruongKip}</div>"
                + $"{(string.IsNullOrWhiteSpace(chuKyTruongKipHtml) ? "" : chuKyTruongKipHtml)}"
                + $"</td>");
            sb.Append(
                $"<td colspan=\"{g3Span}\" style=\"text-align:center;font-weight:bold;border:none;vertical-align:middle;\">"
                + $"<div style=\"text-align:center;font-weight:bold;\">{config.LabelNguoiLap}</div>"
                + $"{(string.IsNullOrWhiteSpace(chuKyNguoiLapHtml) ? "" : chuKyNguoiLapHtml)}"
                + $"</td>");
            sb.Append("</tr>");
            sb.Append("</table>");
            return sb.ToString();
        }

        private static string PFmt(double? v) => v.HasValue ? v.Value.ToString("0.##") : "";
        private static string PFmt(int? v)    => v.HasValue ? v.Value.ToString() : "";
    }

    public class FooterConfig
    {
        public List<string> LuongTonLabels { get; set; } = new();
        public string LabelTonDauKip    { get; set; } = "Tồn đầu kíp";
        public string LabelNhapTrongKip { get; set; } = "Nhập trong kíp";
        public string LabelTonCuoiKip   { get; set; } = "Tồn cuối kíp";
        public string LabelTruongKip    { get; set; } = "Trưởng kíp";
        public string LabelNguoiLap     { get; set; } = "Người lập";
    }
}

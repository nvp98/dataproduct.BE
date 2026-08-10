using ClosedXML.Excel;
using dataproduct.api.DTOs;
using dataproduct.api.DTOs.Export;
using dataproduct.api.Models;
using dataproduct.api.Models.MasterData;
using dataproduct.api.Repositories;
using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace dataproduct.api.Services
{
    public class Hrc2SlabService
    {
        private readonly IHrc2SlabRepository _repo;
        private readonly ProductFormContext _context;
        private readonly ProductDataMasterDbContext _masterCtx;
        private readonly IWebHostEnvironment _env;
        private readonly IConverter _pdfConverter;
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;

        public Hrc2SlabService(
            IHrc2SlabRepository repo,
            ProductFormContext context,
            ProductDataMasterDbContext masterCtx,
            IWebHostEnvironment env,
            IConverter pdfConverter,
            IConfiguration config,
            IHttpClientFactory httpClientFactory)
        {
            _repo = repo;
            _context = context;
            _masterCtx = masterCtx;
            _env = env;
            _pdfConverter = pdfConverter;
            _config = config;
            _httpClientFactory = httpClientFactory;
        }

        public Task<(IEnumerable<Hrc2SlabItem> Data, int TotalCount)> SearchAsync(Hrc2SlabSearchRequest req)
            => _repo.SearchAsync(req);

        public Task<IEnumerable<Hrc2SlabTongHopItem>> GetTongHopAsync(
            string? tuNgay, string? denNgay, string? ca, string? kip)
            => _repo.GetTongHopAsync(tuNgay, denNgay, ca, kip);

        public Task<IEnumerable<Hrc2PhieuBBSLItem>> GetPhieuBBSLAsync(string? kip, int? ca)
            => _repo.GetPhieuBBSLAsync(kip, ca);

        public Task<IEnumerable<Hrc2SlabTongHopItem>> GetRuotPhieuAsync(Guid idPhieu)
            => _repo.GetRuotPhieuAsync(idPhieu);

        public Task<IEnumerable<Hrc2SlabItem>> GetSlabsByPhieuAsync(Guid idPhieu)
            => _repo.GetSlabsByPhieuAsync(idPhieu);

        public Task XacNhanAsync(Hrc2XacNhanRequest req)
            => _repo.XacNhanAsync(req.IdSlabs, req.LoaiXacNhan, req.NguoiThucHien);

        public Task HuyXacNhanAsync(Hrc2XacNhanRequest req)
            => _repo.HuyXacNhanAsync(req.IdSlabs, req.LoaiXacNhan, req.NguoiThucHien);

        public Task ChotPhieuAsync(Hrc2ChotPhieuRequest req)
            => _repo.ChotPhieuAsync(req.IdPhieu, req.NguoiThucHien);

        public Task HuyChotPhieuAsync(Hrc2ChotPhieuRequest req)
            => _repo.HuyChotPhieuAsync(req.IdPhieu, req.NguoiThucHien);

        public async Task<int> ChuyenBbslAsync(Hrc2ChuyenBbslRequest req)
            => await _repo.ChuyenBbslAsync(req.IdSlabs, req.IdPhieu, req.NguoiThucHien);

        public async Task<int> ThuHoiAsync(Hrc2ChuyenBbslRequest req)
            => await _repo.ThuHoiAsync(req.IdSlabs, req.NguoiThucHien);

        // Sync status từ BK_SyncHRC2SlabControl
        public async Task<object?> GetSyncStatusAsync()
        {
            var latest = await _context.BkSyncHrc2SlabControls
                .AsNoTracking()
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync();

            if (latest == null) return null;

            return new
            {
                id          = latest.Id,
                trangThai   = latest.TrangThai,
                batDauLuc   = latest.BatDauLuc,
                ketThucLuc  = latest.KetThucLuc,
                ghiChu      = latest.GhiChu,
            };
        }
        public Task<SyncStatusItem> SyncAsync(DateOnly? ngayBatDau, DateOnly? ngayKetThuc)
            => _repo.SyncAsync(ngayBatDau, ngayKetThuc);

        // ── Chi tiết Excel (BBGN phôi tấm HRC2) ─────────────────────────────

        public async Task<ExportFileResult> ExportChiTietExcelAsync(Guid idPhieu)
        {
            var phieu = await GetPhieuAsync(idPhieu);
            var (soPhieu, ngaySX, _, _) = ExtractPhieuInfo(phieu);

            var trangThais = await GetTrangThaisAsync(idPhieu);

            var templatePath = Path.Combine(_env.WebRootPath, "templates", "HRC2_BBSL_PhoiTam.xlsx");
            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"Không tìm thấy file mẫu: {templatePath}");

            using var workbook = new XLWorkbook(templatePath);
            var ws = workbook.Worksheet(1);

            const int startRow = 6;
            var rowIndex = startRow;
            var stt = 1;

            void SetTrangThaiCell(int r, int c, int trangThai)
            {
                var cell = ws.Cell(r, c);
                cell.Value = trangThai == 1 ? "Đã xác nhận" : "Chưa xác nhận";
                cell.Style.Fill.BackgroundColor = trangThai == 1
                    ? XLColor.FromHtml("#C6EFCE")
                    : XLColor.FromHtml("#D9D9D9");
            }

            foreach (var tt in trangThais)
            {
                var slab = tt.Slab;
                var kt = (slab.ChieuDay != null && slab.ChieuRong != null && slab.ChieuDai != null)
                    ? $"{slab.ChieuDay}x{slab.ChieuRong}x{slab.ChieuDai}"
                    : "";

                if (rowIndex > startRow)
                    ws.Row(startRow).CopyTo(ws.Row(rowIndex));

                ws.Cell(rowIndex, 1).Value = stt;
                ws.Cell(rowIndex, 2).Value = slab.ShiftName ?? "";
                ws.Cell(rowIndex, 3).Value = slab.IdSlab ?? "";
                ws.Cell(rowIndex, 4).Value = slab.OrderId ?? "";
                ws.Cell(rowIndex, 5).Value = slab.MeThep ?? "";
                ws.Cell(rowIndex, 6).Value = slab.MayDuc.HasValue ? $"Máy {slab.MayDuc}" : "";
                ws.Cell(rowIndex, 7).Value = kt;
                ws.Cell(rowIndex, 8).Value = slab.MacThep ?? "";
                ws.Cell(rowIndex, 9).Value = slab.KhoiLuong.HasValue ? (double)slab.KhoiLuong.Value : 0;
                ws.Cell(rowIndex, 10).Value = slab.ChatLuong ?? "";
                SetTrangThaiCell(rowIndex, 11, tt.TrangThaiDuc);
                SetTrangThaiCell(rowIndex, 12, tt.TrangThaiKho);
                SetTrangThaiCell(rowIndex, 13, tt.TrangThaiPKH);

                rowIndex++;
                stt++;
            }

            if (rowIndex > startRow)
                SetThinBorders(ws, startRow, rowIndex - 1, 13);

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            ms.Position = 0;

            return new ExportFileResult
            {
                Content = ms.ToArray(),
                FileName = $"HRC2_BBSL_PhoiTam_{soPhieu}_{ngaySX.Replace("/", "")}_{DateTime.Now:HHmmss}.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            };
        }

        // ── Tổng hợp Excel (BBXNSL phôi tấm HRC2) ───────────────────────────
        // Cột khớp JSON config HRC2_BBGN_PhoiTam.layout[0].columns (27 cột leaf):
        // 1=STT, 2=shiftName, 3=macThep, 4=meThep, 5=kichThuoc,
        // 6-17=phôi nguội (loại I/II/IItp/III/IIItp/ngắndài × so/kl),
        // 18-25=phôi nóng (loại I/II/IItp/IIItp × so/kl),
        // 26=tongSoPhoi, 27=tongKhoiLuong

        public async Task<ExportFileResult> ExportTongHopExcelAsync(Guid idPhieu)
        {
            var phieu = await GetPhieuAsync(idPhieu);
            var (soPhieu, ngaySX, _, _) = ExtractPhieuInfo(phieu);

            var trangThais = await GetTrangThaisAsync(idPhieu);
            var pivotRows = BuildPivotRows(trangThais);

            var templatePath = Path.Combine(_env.WebRootPath, "templates", "HRC2_BBXNSL_PhoiTam.xlsx");
            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"Không tìm thấy file mẫu: {templatePath}");

            using var workbook = new XLWorkbook(templatePath);
            var ws = workbook.Worksheet(1);

            const int startRow = 6;
            const int lastCol = 27;
            var rowIndex = startRow;
            var stt = 1;

            void SetSo(int r, int c, int v)
            {
                if (v > 0) ws.Cell(r, c).Value = v;
                else ws.Cell(r, c).Clear(XLClearOptions.Contents);
            }

            void SetKl(int r, int c, decimal v)
            {
                if (v > 0) ws.Cell(r, c).Value = (double)v;
                else ws.Cell(r, c).Clear(XLClearOptions.Contents);
            }

            foreach (var row in pivotRows)
            {
                if (rowIndex > startRow)
                    ws.Row(startRow).CopyTo(ws.Row(rowIndex));

                ws.Cell(rowIndex, 1).Value = stt;
                ws.Cell(rowIndex, 2).Value = row.ShiftName;
                ws.Cell(rowIndex, 3).Value = row.MacThep;
                ws.Cell(rowIndex, 4).Value = row.MeThep;
                ws.Cell(rowIndex, 5).Value = row.KichThuoc;
                SetSo(rowIndex, 6,  row.NguoiLoai1So);   SetKl(rowIndex, 7,  row.NguoiLoai1Kl);
                SetSo(rowIndex, 8,  row.NguoiLoai2So);   SetKl(rowIndex, 9,  row.NguoiLoai2Kl);
                SetSo(rowIndex, 10, row.NguoiLoai2TpSo); SetKl(rowIndex, 11, row.NguoiLoai2TpKl);
                SetSo(rowIndex, 12, row.NguoiLoai3So);   SetKl(rowIndex, 13, row.NguoiLoai3Kl);
                SetSo(rowIndex, 14, row.NguoiLoai3TpSo); SetKl(rowIndex, 15, row.NguoiLoai3TpKl);
                SetSo(rowIndex, 16, row.NguoiNganDaiSo); SetKl(rowIndex, 17, row.NguoiNganDaiKl);
                SetSo(rowIndex, 18, row.NongLoai1So);    SetKl(rowIndex, 19, row.NongLoai1Kl);
                SetSo(rowIndex, 20, row.NongLoai2So);    SetKl(rowIndex, 21, row.NongLoai2Kl);
                SetSo(rowIndex, 22, row.NongLoai2TpSo);  SetKl(rowIndex, 23, row.NongLoai2TpKl);
                SetSo(rowIndex, 24, row.NongLoai3TpSo);  SetKl(rowIndex, 25, row.NongLoai3TpKl);
                SetSo(rowIndex, 26, row.TongSoPhoi);     SetKl(rowIndex, 27, row.TongKhoiLuong);

                rowIndex++;
                stt++;
            }

            // Total row
            if (pivotRows.Count > 0)
            {
                if (rowIndex > startRow)
                    ws.Row(startRow).CopyTo(ws.Row(rowIndex));

                ws.Cell(rowIndex, 1).Value = "Tổng";
                for (int c = 2; c <= 5; c++) ws.Cell(rowIndex, c).Value = "";
                SetSo(rowIndex, 6,  pivotRows.Sum(r => r.NguoiLoai1So));   SetKl(rowIndex, 7,  pivotRows.Sum(r => r.NguoiLoai1Kl));
                SetSo(rowIndex, 8,  pivotRows.Sum(r => r.NguoiLoai2So));   SetKl(rowIndex, 9,  pivotRows.Sum(r => r.NguoiLoai2Kl));
                SetSo(rowIndex, 10, pivotRows.Sum(r => r.NguoiLoai2TpSo)); SetKl(rowIndex, 11, pivotRows.Sum(r => r.NguoiLoai2TpKl));
                SetSo(rowIndex, 12, pivotRows.Sum(r => r.NguoiLoai3So));   SetKl(rowIndex, 13, pivotRows.Sum(r => r.NguoiLoai3Kl));
                SetSo(rowIndex, 14, pivotRows.Sum(r => r.NguoiLoai3TpSo)); SetKl(rowIndex, 15, pivotRows.Sum(r => r.NguoiLoai3TpKl));
                SetSo(rowIndex, 16, pivotRows.Sum(r => r.NguoiNganDaiSo)); SetKl(rowIndex, 17, pivotRows.Sum(r => r.NguoiNganDaiKl));
                SetSo(rowIndex, 18, pivotRows.Sum(r => r.NongLoai1So));    SetKl(rowIndex, 19, pivotRows.Sum(r => r.NongLoai1Kl));
                SetSo(rowIndex, 20, pivotRows.Sum(r => r.NongLoai2So));    SetKl(rowIndex, 21, pivotRows.Sum(r => r.NongLoai2Kl));
                SetSo(rowIndex, 22, pivotRows.Sum(r => r.NongLoai2TpSo));  SetKl(rowIndex, 23, pivotRows.Sum(r => r.NongLoai2TpKl));
                SetSo(rowIndex, 24, pivotRows.Sum(r => r.NongLoai3TpSo));  SetKl(rowIndex, 25, pivotRows.Sum(r => r.NongLoai3TpKl));
                SetSo(rowIndex, 26, pivotRows.Sum(r => r.TongSoPhoi));     SetKl(rowIndex, 27, pivotRows.Sum(r => r.TongKhoiLuong));
            }

            var lastRow = pivotRows.Count > 0 ? rowIndex : rowIndex - 1;
            if (lastRow >= startRow)
                SetThinBorders(ws, startRow, lastRow, lastCol);

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            ms.Position = 0;

            return new ExportFileResult
            {
                Content = ms.ToArray(),
                FileName = $"HRC2_BBXNSL_PhoiTam_{soPhieu}_{ngaySX.Replace("/", "")}_{DateTime.Now:HHmmss}.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            };
        }

        // ── Tổng hợp PDF (BBXNSL phôi tấm HRC2) ─────────────────────────────

        public async Task<ExportFileResult> ExportTongHopPdfAsync(Guid idPhieu)
        {
            var phieu = await GetPhieuAsync(idPhieu);
            var (soPhieu, ngaySX, ca, kip) = ExtractPhieuInfo(phieu);

            var trangThais = await GetTrangThaisAsync(idPhieu);
            var pivotRows = BuildPivotRows(trangThais);

            var vi = System.Globalization.CultureInfo.GetCultureInfo("vi-VN");
            var rowsHtml = new StringBuilder();

            static string N0(int v, System.Globalization.CultureInfo c) => v > 0 ? v.ToString("N0", c) : "";
            static string NKl(decimal v, System.Globalization.CultureInfo c) => v > 0 ? v.ToString("N0", c) : "";

            void Td2(int so, decimal kl)
            {
                rowsHtml.Append($"<td class=\"num\">{N0(so, vi)}</td><td class=\"num\">{NKl(kl, vi)}</td>");
            }

            var stt = 1;
            foreach (var r in pivotRows)
            {
                rowsHtml.Append("<tr>");
                rowsHtml.Append($"<td>{stt++}</td>");
                rowsHtml.Append($"<td>{r.ShiftName}</td>");
                rowsHtml.Append($"<td class=\"txt\">{r.MacThep}</td>");
                rowsHtml.Append($"<td class=\"txt\">{r.MeThep}</td>");
                rowsHtml.Append($"<td>{r.KichThuoc}</td>");
                Td2(r.NguoiLoai1So,   r.NguoiLoai1Kl);
                Td2(r.NguoiLoai2So,   r.NguoiLoai2Kl);
                Td2(r.NguoiLoai2TpSo, r.NguoiLoai2TpKl);
                Td2(r.NguoiLoai3So,   r.NguoiLoai3Kl);
                Td2(r.NguoiLoai3TpSo, r.NguoiLoai3TpKl);
                Td2(r.NguoiNganDaiSo, r.NguoiNganDaiKl);
                Td2(r.NongLoai1So,    r.NongLoai1Kl);
                Td2(r.NongLoai2So,    r.NongLoai2Kl);
                Td2(r.NongLoai2TpSo,  r.NongLoai2TpKl);
                Td2(r.NongLoai3TpSo,  r.NongLoai3TpKl);
                rowsHtml.Append($"<td class=\"num\"><strong>{N0(r.TongSoPhoi, vi)}</strong></td>");
                rowsHtml.Append($"<td class=\"num\"><strong>{NKl(r.TongKhoiLuong, vi)}</strong></td>");
                rowsHtml.Append("</tr>");
            }

            // Total row
            void TdTotal2(Func<TongHopPivotRow, int> soFn, Func<TongHopPivotRow, decimal> klFn)
            {
                rowsHtml.Append($"<td class=\"num\">{N0(pivotRows.Sum(soFn), vi)}</td><td class=\"num\">{NKl(pivotRows.Sum(klFn), vi)}</td>");
            }

            rowsHtml.Append("<tr class=\"total-row\">");
            rowsHtml.Append("<td colspan=\"5\" style=\"text-align:center\"><strong>Tổng cộng: </strong></td>");
            TdTotal2(r => r.NguoiLoai1So,   r => r.NguoiLoai1Kl);
            TdTotal2(r => r.NguoiLoai2So,   r => r.NguoiLoai2Kl);
            TdTotal2(r => r.NguoiLoai2TpSo, r => r.NguoiLoai2TpKl);
            TdTotal2(r => r.NguoiLoai3So,   r => r.NguoiLoai3Kl);
            TdTotal2(r => r.NguoiLoai3TpSo, r => r.NguoiLoai3TpKl);
            TdTotal2(r => r.NguoiNganDaiSo, r => r.NguoiNganDaiKl);
            TdTotal2(r => r.NongLoai1So,    r => r.NongLoai1Kl);
            TdTotal2(r => r.NongLoai2So,    r => r.NongLoai2Kl);
            TdTotal2(r => r.NongLoai2TpSo,  r => r.NongLoai2TpKl);
            TdTotal2(r => r.NongLoai3TpSo,  r => r.NongLoai3TpKl);
            rowsHtml.Append($"<td class=\"num\"><strong>{N0(pivotRows.Sum(r => r.TongSoPhoi), vi)}</strong></td>");
            rowsHtml.Append($"<td class=\"num\"><strong>{NKl(pivotRows.Sum(r => r.TongKhoiLuong), vi)}</strong></td>");
            rowsHtml.Append("</tr>");

            var (ducUserId, khoUserId, qlclUserId) = await GetPhieuSignersAsync(idPhieu);
            var signerIds = new[] { ducUserId, khoUserId, qlclUserId }
                .Where(id => id != null).Select(id => id!.Value).Distinct().ToList();
            var userMap = signerIds.Count > 0
                ? await _masterCtx.Tbl_TaiKhoan
                    .Include(t => t.ViTri)
                    .Include(t => t.PhongBan)
                    .Where(t => signerIds.Contains(t.ID_TaiKhoan))
                    .ToDictionaryAsync(t => t.ID_TaiKhoan)
                : new Dictionary<int, TaiKhoan>();

            var (ducSigImg, ducSigTen) = await BuildSigPartsAsync(ducUserId, userMap);
            var (khoSigImg, khoSigTen) = await BuildSigPartsAsync(khoUserId, userMap);
            var (qlclSigImg, qlclSigTen) = await BuildSigPartsAsync(qlclUserId, userMap);

            var thanhPhanHtml = string.Concat(
                BuildThanhPhanLine(1, qlclUserId, userMap),
                BuildThanhPhanLine(2, ducUserId, userMap),
                BuildThanhPhanLine(3, khoUserId, userMap));

            var templatePath = Path.Combine(_env.WebRootPath, "template_html", "HRC2_BBXNSL_PhoiTam.html");
            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"Không tìm thấy template HTML: {templatePath}");

            var html = await File.ReadAllTextAsync(templatePath);
            var logoUrl = $"data:image/png;base64,{Convert.ToBase64String(await File.ReadAllBytesAsync(Path.Combine(_env.WebRootPath, "imgs", "LogoPDF.png")))}";

            html = html
                .Replace("{{LogoUrl}}",       logoUrl)
                .Replace("{{soPhieu}}", soPhieu)
                .Replace("{{ngaySX}}", ngaySX)
                .Replace("{{mayduc}}", phieu?.MayDuc.HasValue == true ? $"Máy {phieu.MayDuc}" : "")
                .Replace("{{ca}}", ca)
                .Replace("{{kip}}", kip)
                .Replace("{{TABLE_BODY}}", rowsHtml.ToString())
                .Replace("{{ThanhPhanHtml}}", thanhPhanHtml)
                .Replace("{{DucKyImgHtml}}", ducSigImg)
                .Replace("{{DucKyTenHtml}}", ducSigTen)
                .Replace("{{KhoKyImgHtml}}", khoSigImg)
                .Replace("{{KhoKyTenHtml}}", khoSigTen)
                .Replace("{{QLCLKyImgHtml}}", qlclSigImg)
                .Replace("{{QLCLKyTenHtml}}", qlclSigTen);

            var doc = new HtmlToPdfDocument
            {
                GlobalSettings =
                {
                    PaperSize = PaperKind.A3,
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
                            PrintMediaType = true
                        },
                        LoadSettings =
                        {
                            BlockLocalFileAccess = false,
                            LoadErrorHandling = ContentErrorHandling.Ignore
                        }
                    }
                }
            };

            var pdfBytes = _pdfConverter.Convert(doc);
            return new ExportFileResult
            {
                Content = pdfBytes,
                FileName = $"HRC2_BBXNSL_PhoiTam_{soPhieu}_{ngaySX.Replace("/", "")}_{DateTime.Now:HHmmss}.pdf",
                ContentType = "application/pdf"
            };
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetThinBorders(IXLWorksheet ws, int fromRow, int toRow, int lastCol)
        {
            var range = ws.Range(fromRow, 1, toRow, lastCol);
            range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        }

        private async Task<BmPhieu?> GetPhieuAsync(Guid idPhieu)
        {
            return await _context.BmPhieus
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Idphieu == idPhieu && p.MaBm == "HRC2_BBSL_PhoiTam");
        }

        private static (string SoPhieu, string NgaySX, string Ca, string Kip) ExtractPhieuInfo(BmPhieu? phieu)
        {
            var soPhieu = phieu?.SoPhieu ?? "";
            string ngaySX = "", ca = "", kip = "";

            if (phieu?.DataJson != null)
            {
                try
                {
                    using var doc = JsonDocument.Parse(phieu.DataJson);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("NgaySX", out var ngProp) && ngProp.ValueKind != JsonValueKind.Null)
                    {
                        var ngStr = ngProp.GetString();
                        if (!string.IsNullOrEmpty(ngStr) && DateTime.TryParse(ngStr, out var dt))
                            ngaySX = dt.ToString("dd/MM/yyyy");
                        else
                            ngaySX = ngStr ?? "";
                    }

                    if (root.TryGetProperty("ca", out var caProp))
                    {
                        int caVal = 0;
                        if (caProp.ValueKind == JsonValueKind.Number)
                            caVal = caProp.GetInt32();
                        else if (caProp.ValueKind == JsonValueKind.String && int.TryParse(caProp.GetString(), out var cv))
                            caVal = cv;
                        // Ca ngày = 1, Ca đêm = 2 — giữ nguyên số để ghép "Kíp: {ca}{kip}" (vd "1A")
                        ca = caVal > 0 ? caVal.ToString() : "";
                    }

                    if (root.TryGetProperty("kip", out var kipProp) && kipProp.ValueKind != JsonValueKind.Null)
                        kip = kipProp.GetString() ?? "";
                }
                catch { /* ignore parse errors */ }
            }

            return (soPhieu, ngaySX, ca, kip);
        }

        private async Task<List<BkHrc2SlabTrangThai>> GetTrangThaisAsync(Guid idPhieu)
        {
            return await _context.BkHrc2SlabTrangThais
                .AsNoTracking()
                .Include(t => t.Slab)
                .Where(t => t.IdPhieuBBSL == idPhieu && t.TrangThaiKCS == 1)
                .OrderBy(t => t.Slab.BkmisId)
                .ToListAsync();
        }

        // Người ký cho biên bản: X.ĐPT → Đúc, Kho phôi → Kho, P.QLCL → KCS
        private async Task<(int? Duc, int? Kho, int? Qlcl)> GetPhieuSignersAsync(Guid idPhieu)
        {
            var trangThais = await _context.BkHrc2SlabTrangThais
                .AsNoTracking()
                .Where(t => t.IdPhieuBBSL == idPhieu)
                .ToListAsync();

            int? ducUserId = trangThais
                .Where(t => t.NguoiXacNhanDuc != null)
                .OrderByDescending(t => t.NgayXacNhanDuc)
                .Select(t => t.NguoiXacNhanDuc)
                .FirstOrDefault();

            int? khoUserId = trangThais
                .Where(t => t.NguoiXacNhanKho != null)
                .OrderByDescending(t => t.NgayXacNhanKho)
                .Select(t => t.NguoiXacNhanKho)
                .FirstOrDefault();

            int? qlclUserId = trangThais
                .Where(t => t.NguoiChuyenKCS != null)
                .OrderByDescending(t => t.NgayChuyenKCS)
                .Select(t => t.NguoiChuyenKCS)
                .FirstOrDefault();

            return (ducUserId, khoUserId, qlclUserId);
        }

        // Dòng "Thành phần" trong biên bản PDF — cùng nguồn dữ liệu với chữ ký (Duc/Kho/Qlcl),
        // format text theo kiểu BBGN_ThepLong.html: "N. Nhãn: Ông/Bà: Tên   Chức vụ: ..."
        // Dòng "Thành phần" trong biên bản PDF — cùng nguồn dữ liệu với chữ ký (Duc/Kho/Qlcl),
        // format: "N. Ông/Bà: Tên   Chức vụ: ...   BP: ..."
        private static string BuildThanhPhanLine(int idx, int? userId, Dictionary<int, TaiKhoan> userMap)
        {
            var u = userId != null && userMap.TryGetValue(userId.Value, out var uu) ? uu : null;
            if (u == null)
                return $"<div class=\"info-row\"><span class=\"info-idx\">{idx}.</span><span class=\"info-field info-name\">Ông/Bà: -</span></div>";

            var ten = System.Net.WebUtility.HtmlEncode(u.HoVaTen ?? "");
            var chucVu = System.Net.WebUtility.HtmlEncode(u.ViTri?.TenViTri ?? "");
            var bp = System.Net.WebUtility.HtmlEncode(u.PhongBan?.TenNgan ?? "");

            return $"<div class=\"info-row\">" +
                   $"<span class=\"info-idx\">{idx}.</span>" +
                   $"<span class=\"info-field info-name\">Ông/Bà: <strong>{ten}</strong></span>" +
                   $"<span class=\"info-field info-chucvu\">Chức vụ: {chucVu}</span>" +
                   $"<span class=\"info-field info-bp\">BP: {bp}</span>" +
                   $"</div>";
        }

        private async Task<(string ImgHtml, string TenHtml)> BuildSigPartsAsync(int? userId, Dictionary<int, TaiKhoan> userMap)
        {
            if (userId == null || !userMap.TryGetValue(userId.Value, out var u))
                return ("", "");

            var imgTag = await FormatChuKyImgAsync(u.ChuKy);
            var name = System.Net.WebUtility.HtmlEncode(u.HoVaTen ?? "");
            return (imgTag, name);
        }

        private async Task<string> FormatChuKyImgAsync(string? chuKy)
        {
            if (string.IsNullOrWhiteSpace(chuKy)) return "";

            if (chuKy.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
                return $"<img src=\"{chuKy}\" style=\"max-width:120px;max-height:50px;\" />";

            string url = chuKy.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? chuKy
                : (_config.GetValue<string>("AppSettings:Domain") ?? "https://report.hoaphatdungquat.vn").TrimEnd('/') + chuKy;

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                var bytes = await client.GetByteArrayAsync(url);
                var mime = url.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? "image/png" : "image/jpeg";
                return $"<img src=\"data:{mime};base64,{Convert.ToBase64String(bytes)}\" style=\"max-width:120px;max-height:50px;\" />";
            }
            catch { return ""; }
        }

        // Dịch getColKeys() của FE sang C# — xác định cột pivot từ loaiPhoi + chatLuongTPHH
        private static (string? SoKey, string? KlKey) GetPivotKeys(string? loaiPhoi, string? chatLuong)
        {
            var lp = loaiPhoi ?? "";
            var cl = chatLuong ?? "";
            var isNguoi = Regex.IsMatch(lp, "nguội|nguoi", RegexOptions.IgnoreCase);
            var isNong  = Regex.IsMatch(lp, "nóng|nong",   RegexOptions.IgnoreCase);
            if (!isNguoi && !isNong) return (null, null);
            var p = isNguoi ? "nguoi" : "nong";
            string? mid = null;
            if      (Regex.IsMatch(cl, @"iii.*th[àa]nh|3.*th[àa]nh", RegexOptions.IgnoreCase)) mid = "loai3tp";
            else if (Regex.IsMatch(cl, @"\biii\b|\b3\b",              RegexOptions.IgnoreCase)) mid = isNguoi ? "loai3" : null;
            else if (Regex.IsMatch(cl, @"ii.*th[àa]nh|2.*th[àa]nh",  RegexOptions.IgnoreCase)) mid = "loai2tp";
            else if (Regex.IsMatch(cl, @"\bii\b|\b2\b",               RegexOptions.IgnoreCase)) mid = "loai2";
            else if (Regex.IsMatch(cl, @"\bi\b|\b1\b",                RegexOptions.IgnoreCase)) mid = "loai1";
            else if (isNguoi && Regex.IsMatch(cl, @"ng[aắ]n",         RegexOptions.IgnoreCase)) mid = "nganDai";
            if (mid == null) return (null, null);
            return ($"{p}_{mid}_so", $"{p}_{mid}_kl");
        }

        // Pivot row — nhóm theo (meThep, macThep, kichThuoc), phân loại theo loaiPhoi + chatLuongTPHH
        private class TongHopPivotRow
        {
            public string ShiftName = "";
            public string MacThep   = "";
            public string MeThep    = "";
            public string KichThuoc = "";
            public int NguoiLoai1So;    public decimal NguoiLoai1Kl;
            public int NguoiLoai2So;    public decimal NguoiLoai2Kl;
            public int NguoiLoai2TpSo;  public decimal NguoiLoai2TpKl;
            public int NguoiLoai3So;    public decimal NguoiLoai3Kl;
            public int NguoiLoai3TpSo;  public decimal NguoiLoai3TpKl;
            public int NguoiNganDaiSo;  public decimal NguoiNganDaiKl;
            public int NongLoai1So;     public decimal NongLoai1Kl;
            public int NongLoai2So;     public decimal NongLoai2Kl;
            public int NongLoai2TpSo;   public decimal NongLoai2TpKl;
            public int NongLoai3TpSo;   public decimal NongLoai3TpKl;
            public int TongSoPhoi;      public decimal TongKhoiLuong;
            public HashSet<string> _shiftNames = new();
        }

        private static string FmtKichThuoc(decimal v) => Math.Round(v, 0, MidpointRounding.AwayFromZero).ToString("0");

        private static List<TongHopPivotRow> BuildPivotRows(List<BkHrc2SlabTrangThai> trangThais)
        {
            var map = new Dictionary<string, TongHopPivotRow>();

            foreach (var tt in trangThais)
            {
                var slab = tt.Slab;
                var kt = (slab.ChieuDay != null && slab.ChieuRong != null && slab.ChieuDai != null)
                    ? $"{FmtKichThuoc(slab.ChieuDay.Value)}x{FmtKichThuoc(slab.ChieuRong.Value)}x{FmtKichThuoc(slab.ChieuDai.Value)}"
                    : "";
                var key = $"{slab.MeThep ?? ""}|{slab.MacThep ?? ""}|{kt}";

                if (!map.TryGetValue(key, out var row))
                {
                    row = new TongHopPivotRow
                    {
                        MacThep   = slab.MacThep ?? "",
                        MeThep    = slab.MeThep  ?? "",
                        KichThuoc = kt
                    };
                    map[key] = row;
                }

                if (!string.IsNullOrEmpty(slab.ShiftName))
                    row._shiftNames.Add(slab.ShiftName);

                var kl = slab.KhoiLuong ?? 0;
                var (soKey, _) = GetPivotKeys(slab.LoaiPhoi, slab.ChatLuongTPHH);

                switch (soKey)
                {
                    case "nguoi_loai1_so":   row.NguoiLoai1So++;   row.NguoiLoai1Kl   += kl; break;
                    case "nguoi_loai2_so":   row.NguoiLoai2So++;   row.NguoiLoai2Kl   += kl; break;
                    case "nguoi_loai2tp_so": row.NguoiLoai2TpSo++; row.NguoiLoai2TpKl += kl; break;
                    case "nguoi_loai3_so":   row.NguoiLoai3So++;   row.NguoiLoai3Kl   += kl; break;
                    case "nguoi_loai3tp_so": row.NguoiLoai3TpSo++; row.NguoiLoai3TpKl += kl; break;
                    case "nguoi_nganDai_so": row.NguoiNganDaiSo++; row.NguoiNganDaiKl += kl; break;
                    case "nong_loai1_so":    row.NongLoai1So++;    row.NongLoai1Kl    += kl; break;
                    case "nong_loai2_so":    row.NongLoai2So++;    row.NongLoai2Kl    += kl; break;
                    case "nong_loai2tp_so":  row.NongLoai2TpSo++;  row.NongLoai2TpKl  += kl; break;
                    case "nong_loai3tp_so":  row.NongLoai3TpSo++;  row.NongLoai3TpKl  += kl; break;
                }

                row.TongSoPhoi++;
                row.TongKhoiLuong += kl;
            }

            return map.Values.Select(r =>
            {
                r.ShiftName = string.Join(", ", r._shiftNames);
                return r;
            }).ToList();
        }
    }
}

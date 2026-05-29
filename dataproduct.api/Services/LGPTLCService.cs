using ClosedXML.Excel;
using dataproduct.api.DTOs.CTD_Dto;
using dataproduct.api.DTOs.Export;
using dataproduct.api.DTOs.NMLG_Dto;
using dataproduct.api.Models;
using dataproduct.api.Repositories;
using DinkToPdf;
using DinkToPdf.Contracts;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace dataproduct.api.Services
{
    public class LGPTLCService
    {
        private readonly ILGPTLCRepository _repo;
        private readonly IConverter _pdfConverter;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;

        public LGPTLCService(
            ILGPTLCRepository repo,
            IConverter pdfConverter,
            IWebHostEnvironment env,
            IConfiguration configuration)
        {
            _repo = repo;
            _pdfConverter = pdfConverter;
            _env = env;
            _configuration = configuration;
        }

        // ─── Auto data (from LG_NKVHPT_DuLieu) ───────────────────────────────────

        public async Task<List<LG_NKVHPT_DuLieuAuto>> GetAutoDataAsync(GetAutoDataRequest request)
        {
            if (request.IDLoCao <= 0)
                throw new Exception("ID lò cao không hợp lệ");

            if (request.NgaySanXuat == DateTime.MinValue)
                throw new Exception("Ngày vận hành không hợp lệ");

            return await _repo.GetAutoDataAsync(request.IDLoCao, request.NgaySanXuat);
        }

        // ─── Chi tiết (LG_NKVHPT_ChiTiet) ────────────────────────────────────────

        public Task<List<LGPTLCChiTietDto>> GetChiTietByPhieuAsync(Guid idPhieu)
            => _repo.GetChiTietByPhieuAsync(idPhieu);

        public Task UpdateManualValuesAsync(UpdateLGPTLCManualDto dto)
            => _repo.UpdateManualValuesAsync(dto);

        public Task UpdatePhieuSummaryAsync(Guid idPhieu, UpdateLGPTLCSummaryDto dto)
            => _repo.UpdatePhieuSummaryAsync(idPhieu, dto);

        public Task<BmPhieu?> GetPhieuByIdAsync(Guid idPhieu)
            => _repo.GetPhieuByIdAsync(idPhieu);

        // ─── Initializer: parse DataJson and insert chi tiết ─────────────────────
        public async Task<int> InsertFromPhieuJsonAsync(BmPhieu phieu)
        {
            if (phieu == null || string.IsNullOrWhiteSpace(phieu.DataJson))
                return 0;

            try
            {
                using var jsonDoc = JsonDocument.Parse(phieu.DataJson);
                var root = jsonDoc.RootElement;

                if (!TryGetArray(root, "nhatKyVanHanh", out var tableRows))
                    return 0;

                var rows = tableRows.EnumerateArray().ToList();
                if (rows.Count == 0)
                    return 0;

                var ngay = TryGetDateTime(phieu.NgaySX?.ToDateTime(TimeOnly.MinValue), root, "NgaySX", "ngaySX", "ngaySanXuat", "ngay");
                if (ngay == null)
                    return 0;

                var idLoCao = TryGetInt(phieu.Scope, root, "scope", "Scope", "idLoCao", "IDLoCao", "loCao");
                var ca = TryGetInt(phieu.Ca, root, "ca", "Ca");
                if (idLoCao == null || ca == null)
                    return 0;

                var kip = TryGetString(phieu.Kip, root, "kip", "Kip");

                await _repo.DeleteChiTietByPhieuAsync(phieu.Idphieu);

                var entities = new List<LG_NKVHPT_ChiTiet>();
                for (int i = 0; i < rows.Count; i++)
                {
                    var row = rows[i];
                    if (row.ValueKind != JsonValueKind.Object)
                        continue;

                    entities.Add(new LG_NKVHPT_ChiTiet
                    {
                        IDPhieu = phieu.Idphieu,
                        SoPhieu = phieu.SoPhieu,
                        ID_LoCao = idLoCao.Value,
                        NgayVanHanh = ngay.Value,
                        IDCa = ca.Value,
                        Kip = kip,
                        ThoiGian = ParseRowTime(row, ngay.Value, ca.Value, i),

                        NhietDoSiloBotThan1_Auto = GetDecimal(row, "nhietDoSiloBotThan1_Auto"),
                        NhietDoSiloBotThan1_Manual = GetDecimal(row, "nhietDoSiloBotThan1_Manual", "nhietDoSiloBotThan1"),

                        NhietDoSiloBotThan2_Auto = GetDecimal(row, "nhietDoSiloBotThan2_Auto"),
                        NhietDoSiloBotThan2_Manual = GetDecimal(row, "nhietDoSiloBotThan2_Manual", "nhietDoSiloBotThan2"),

                        NhietDoBonPhunThoi1_Auto = GetDecimal(row, "nhietDoBonPhunThoi1_Auto"),
                        NhietDoBonPhunThoi1_Manual = GetDecimal(row, "nhietDoBonPhunThoi1_Manual", "nhietDoBonPhunThoi1"),

                        NhietDoBonPhunThoi2_Auto = GetDecimal(row, "nhietDoBonPhunThoi2_Auto"),
                        NhietDoBonPhunThoi2_Manual = GetDecimal(row, "nhietDoBonPhunThoi2_Manual", "nhietDoBonPhunThoi2"),

                        NhietDoBonPhunThoi3_Auto = GetDecimal(row, "nhietDoBonPhunThoi3_Auto"),
                        NhietDoBonPhunThoi3_Manual = GetDecimal(row, "nhietDoBonPhunThoi3_Manual", "nhietDoBonPhunThoi3"),

                        DongDienMayNghien_Auto = GetDecimal(row, "dongDienMayNghien_Auto"),
                        DongDienMayNghien_Manual = GetDecimal(row, "dongDienMayNghien_Manual", "dongDienMayNghien"),

                        DongDienQuatGioNguoc_Auto = GetDecimal(row, "dongDienQuatGioNguoc_Auto"),
                        DongDienQuatGioNguoc_Manual = GetDecimal(row, "dongDienQuatGioNguoc_Manual", "dongDienQuatGioNguoc"),

                        NhietDoDauVaoMayNghien_Auto = GetDecimal(row, "nhietDoDauVaoMayNghien_Auto"),
                        NhietDoDauVaoMayNghien_Manual = GetDecimal(row, "nhietDoDauVaoMayNghien_Manual", "nhietDoDauVaoMayNghien"),

                        NhietDoDauRaMayNghien_Auto = GetDecimal(row, "nhietDoDauRaMayNghien_Auto"),
                        NhietDoDauRaMayNghien_Manual = GetDecimal(row, "nhietDoDauRaMayNghien_Manual", "nhietDoDauRaMayNghien"),

                        NhietDoKhoangLo_Auto = GetDecimal(row, "nhietDoKhoangLo_Auto"),
                        NhietDoKhoangLo_Manual = GetDecimal(row, "nhietDoKhoangLo_Manual", "nhietDoKhoangLo"),

                        MucLieuSiloBotThan1_Auto = GetDecimal(row, "mucLieuSiloBotThan1_Auto"),
                        MucLieuSiloBotThan1_Manual = GetDecimal(row, "mucLieuSiloBotThan1_Manual", "mucLieuSiloBotThan1"),

                        MucLieuSiloBotThan2_Auto = GetDecimal(row, "mucLieuSiloBotThan2_Auto"),
                        MucLieuSiloBotThan2_Manual = GetDecimal(row, "mucLieuSiloBotThan2_Manual", "mucLieuSiloBotThan2"),

                        MucLieuSiloThanTho_Auto = GetDecimal(row, "mucLieuSiloThanTho_Auto"),
                        MucLieuSiloThanTho_Manual = GetDecimal(row, "mucLieuSiloThanTho_Manual", "mucLieuSiloThanTho"),

                        TrongLuongBonPhunThoi1_Auto = GetDecimal(row, "trongLuongBonPhunThoi1_Auto"),
                        TrongLuongBonPhunThoi1_Manual = GetDecimal(row, "trongLuongBonPhunThoi1_Manual", "trongLuongBonPhunThoi1"),

                        TrongLuongBonPhunThoi2_Auto = GetDecimal(row, "trongLuongBonPhunThoi2_Auto"),
                        TrongLuongBonPhunThoi2_Manual = GetDecimal(row, "trongLuongBonPhunThoi2_Manual", "trongLuongBonPhunThoi2"),

                        TrongLuongBonPhunThoi3_Auto = GetDecimal(row, "trongLuongBonPhunThoi3_Auto"),
                        TrongLuongBonPhunThoi3_Manual = GetDecimal(row, "trongLuongBonPhunThoi3_Manual", "trongLuongBonPhunThoi3"),

                        ApLucKhiThan_Auto = GetDecimal(row, "apLucKhiThan_Auto"),
                        ApLucKhiThan_Manual = GetDecimal(row, "apLucKhiThan_Manual", "apLucKhiThan"),

                        ApLucBonKhiN2_Auto = GetDecimal(row, "apLucBonKhiN2_Auto"),
                        ApLucBonKhiN2_Manual = GetDecimal(row, "apLucBonKhiN2_Manual", "apLucBonKhiN2"),

                        NhietDoTramDauBoiTron_Auto = GetDecimal(row, "nhietDoTramDauBoiTron_Auto"),
                        NhietDoTramDauBoiTron_Manual = GetDecimal(row, "nhietDoTramDauBoiTron_Manual", "nhietDoTramDauBoiTron"),

                        NhietDoStatoDongCoMayNghien_Auto = GetDecimal(row, "nhietDoStatoDongCoMayNghien_Auto"),
                        NhietDoStatoDongCoMayNghien_Manual = GetDecimal(row, "nhietDoStatoDongCoMayNghien_Manual", "nhietDoStatoDongCoMayNghien"),

                        NhietDoTrucDongCoMayNghien_Auto = GetDecimal(row, "nhietDoTrucDongCoMayNghien_Auto"),
                        NhietDoTrucDongCoMayNghien_Manual = GetDecimal(row, "nhietDoTrucDongCoMayNghien_Manual", "nhietDoTrucDongCoMayNghien"),

                        ApLucTrucNghien_Auto = GetDecimal(row, "apLucTrucNghien_Auto"),
                        ApLucTrucNghien_Manual = GetDecimal(row, "apLucTrucNghien_Manual", "apLucTrucNghien"),

                        YeuCauTuLoCao_Auto = GetDecimal(row, "yeuCauTuLoCao_Auto"),
                        YeuCauTuLoCao_Manual = GetDecimal(row, "yeuCauTuLoCao_Manual", "yeuCauTuLoCao"),

                        LuongThanPhunThucTe_Auto = GetDecimal(row, "luongThanPhunThucTe_Auto"),
                        LuongThanPhunThucTe_Manual = GetDecimal(row, "luongThanPhunThucTe_Manual", "luongThanPhunThucTe"),

                        LuyKeLuongPhunThanTrongCa_Auto = GetDecimal(row, "luyKeLuongPhunThanTrongCa_Auto"),
                        LuyKeLuongPhunThanTrongCa_Manual = GetDecimal(row, "luyKeLuongPhunThanTrongCa_Manual", "luyKeLuongPhunThanTrongCa"),

                        SoLuongSungPhun_Auto = GetInt(row, "soLuongSungPhun_Auto"),
                        SoLuongSungPhun_Manual = GetInt(row, "soLuongSungPhun_Manual", "soLuongSungPhun"),

                        ApLucGioLanhLoCao_Auto = GetDecimal(row, "apLucGioLanhLoCao_Auto"),
                        ApLucGioLanhLoCao_Manual = GetDecimal(row, "apLucGioLanhLoCao_Manual", "apLucGioLanhLoCao"),

                        SanLuongNghien = GetDecimalFromSummary(root, "tinhHinhSanLuong", "sanLuongNghien"),
                        SanLuongPhun = GetDecimalFromSummary(root, "tinhHinhSanLuong", "sanLuongPhun"),

                        GhiChu = GetString(row, "ghiChu", "GhiChu"),
                        TinhTrangSanXuat = GetString(root, "tinhTrangSanXuat", "tinhTrangSanXuatNgay", "tinhTrangSanXuatDem"),
                        SapXepSanXuat = GetString(root, "sapXepSanXuat", "SapXepSanXuat"),

                        CreatedAt = DateTime.Now
                    });
                }

                if (entities.Count == 0)
                    return 0;

                await _repo.AddChiTietRangeAsync(entities);
                return entities.Count;
            }
            catch
            {
                return 0;
            }
        }

        private static bool TryGetArray(JsonElement obj, string key, out JsonElement array)
        {
            array = default;
            if (obj.TryGetProperty(key, out var el) && el.ValueKind == JsonValueKind.Array)
            {
                array = el;
                return true;
            }

            return false;
        }

        private static DateTime? TryGetDateTime(DateTime? fallback, JsonElement obj, params string[] keys)
        {
            if (fallback.HasValue)
                return fallback;

            var str = TryGetString(obj, keys);
            if (string.IsNullOrWhiteSpace(str))
                return null;

            return DateTime.TryParse(str, out var dt) ? dt : null;
        }

        private static int? TryGetInt(int? fallback, JsonElement obj, params string[] keys)
        {
            if (fallback.HasValue)
                return fallback;

            foreach (var key in keys)
            {
                if (!obj.TryGetProperty(key, out var val) || val.ValueKind == JsonValueKind.Null)
                    continue;

                if (val.ValueKind == JsonValueKind.Number && val.TryGetInt32(out var n))
                    return n;

                if (val.ValueKind == JsonValueKind.String && int.TryParse(val.GetString(), out var s))
                    return s;
            }

            return null;
        }

        private static string? TryGetString(JsonElement obj, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (!obj.TryGetProperty(key, out var val) || val.ValueKind == JsonValueKind.Null)
                    continue;

                return val.ValueKind == JsonValueKind.String ? val.GetString() : val.ToString();
            }

            return null;
        }

        private static string? TryGetString(string? fallback, JsonElement obj, params string[] keys)
        {
            if (!string.IsNullOrWhiteSpace(fallback))
                return fallback;

            return TryGetString(obj, keys);
        }

        private static decimal? GetDecimal(JsonElement obj, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (!obj.TryGetProperty(key, out var val) || val.ValueKind == JsonValueKind.Null)
                    continue;

                if (val.ValueKind == JsonValueKind.Number && val.TryGetDecimal(out var d))
                    return d;

                if (val.ValueKind == JsonValueKind.String && decimal.TryParse(val.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var s))
                    return s;

                if (val.ValueKind == JsonValueKind.String && decimal.TryParse(val.GetString(), out var local))
                    return local;
            }

            return null;
        }

        private static int? GetInt(JsonElement obj, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (!obj.TryGetProperty(key, out var val) || val.ValueKind == JsonValueKind.Null)
                    continue;

                if (val.ValueKind == JsonValueKind.Number && val.TryGetInt32(out var n))
                    return n;

                if (val.ValueKind == JsonValueKind.String && int.TryParse(val.GetString(), out var s))
                    return s;
            }

            return null;
        }

        private static decimal? GetDecimalFromSummary(JsonElement root, string summaryKey, string fieldKey)
        {
            if (root.TryGetProperty(summaryKey, out var summary) && summary.ValueKind == JsonValueKind.Object)
            {
                var value = GetDecimal(summary, fieldKey);
                if (value.HasValue)
                    return value;
            }

            return GetDecimal(root, fieldKey);
        }

        private static DateTime ParseRowTime(JsonElement row, DateTime ngay, int ca, int index)
        {
            var baseHour = ca == 1 ? 8 : 20;

            var timeText = GetString(row, "thoiGian", "ThoiGian");
            if (!string.IsNullOrWhiteSpace(timeText))
            {
                var digits = new string(timeText.Where(char.IsDigit).ToArray());
                if (digits.Length >= 1 && int.TryParse(digits[..Math.Min(digits.Length, 2)], out var hour))
                        return ngay.Date.AddHours(Math.Clamp(hour, 0, 23));
            }

                    return ngay.Date.AddHours(Math.Clamp(baseHour + index, 0, 23));
        }

        private static string? GetString(JsonElement obj, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (!obj.TryGetProperty(key, out var val) || val.ValueKind == JsonValueKind.Null)
                    continue;

                return val.ValueKind == JsonValueKind.String ? val.GetString() : val.ToString();
            }

            return null;
        }

        // ─── Export PDF ──────────────────────────────────────────────────────────────

        public async Task<ExportFileResult> ExportPhunThanPdfAsync(Guid idPhieu, List<PheDuyetDto> pheDuyets)
        {
            var phieu = await _repo.GetPhieuByIdAsync(idPhieu)
                ?? throw new Exception("Không tìm thấy phiếu.");

            var chiTiet = await _repo.GetChiTietByPhieuAsync(idPhieu);
            var firstRow = chiTiet.FirstOrDefault();

            var ngay = firstRow?.NgayVanHanh ?? DateTime.MinValue;
            var loCao = firstRow?.IDLoCao ?? 0;
            var ngayDisplay = ngay != DateTime.MinValue ? ngay.ToString("dd/MM/yyyy") : "";
            var loCaoDisplay = loCao > 0 ? $"Lò cao {loCao}" : "";

            // Lấy dữ liệu tổng hợp từ DataJson
            string kip1 = "", kip2 = "", kip3 = "", tong = "";
            string sanLuongNghien = "", sanLuongPhun = "";
            string tinhTrangNgay = "", tinhTrangDem = "", sapXep = "";
            if (!string.IsNullOrWhiteSpace(phieu.DataJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(phieu.DataJson);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("tinhHinhSanLuong", out var sl))
                    {
                        kip1 = GetString(sl, "kip1") ?? "";
                        kip2 = GetString(sl, "kip2") ?? "";
                        kip3 = GetString(sl, "kip3") ?? "";
                        tong = GetString(sl, "tong") ?? "";
                        sanLuongNghien = GetString(sl, "sanLuongNghien") ?? "";
                        sanLuongPhun = GetString(sl, "sanLuongPhun") ?? "";
                    }
                    tinhTrangNgay = GetString(root, "tinhTrangSanXuatNgay", "tinhTrangSanXuat") ?? "";
                    tinhTrangDem = GetString(root, "tinhTrangSanXuatDem") ?? "";
                    sapXep = GetString(root, "sapXepSanXuat") ?? "";
                }
                catch { /* ignore parse errors */ }
            }

            // Xây dựng HTML rows (Manual ?? Auto)
            static string Nd(decimal? m, decimal? a) => (m ?? a) is { } v ? v.ToString("N1") : "";
            static string Ni2(int? m, int? a) => (m ?? a) is { } v ? v.ToString() : "";

            var rows = new StringBuilder();
            foreach (var c in chiTiet)
            {
                var hour = c.ThoiGian.Hour;
                var tg = $"{hour:D2}h";
                rows.Append($@"<tr>
<td>{tg}</td>
<td>{Nd(c.NhietDoSiloBotThan1_Manual, c.NhietDoSiloBotThan1_Auto)}</td>
<td>{Nd(c.NhietDoSiloBotThan2_Manual, c.NhietDoSiloBotThan2_Auto)}</td>
<td>{Nd(c.NhietDoBonPhunThoi1_Manual, c.NhietDoBonPhunThoi1_Auto)}</td>
<td>{Nd(c.NhietDoBonPhunThoi2_Manual, c.NhietDoBonPhunThoi2_Auto)}</td>
<td>{Nd(c.NhietDoBonPhunThoi3_Manual, c.NhietDoBonPhunThoi3_Auto)}</td>
<td>{Nd(c.DongDienMayNghien_Manual, c.DongDienMayNghien_Auto)}</td>
<td>{Nd(c.DongDienQuatGioNguoc_Manual, c.DongDienQuatGioNguoc_Auto)}</td>
<td>{Nd(c.NhietDoDauVaoMayNghien_Manual, c.NhietDoDauVaoMayNghien_Auto)}</td>
<td>{Nd(c.NhietDoDauRaMayNghien_Manual, c.NhietDoDauRaMayNghien_Auto)}</td>
<td>{Nd(c.NhietDoKhoangLo_Manual, c.NhietDoKhoangLo_Auto)}</td>
<td>{Nd(c.MucLieuSiloBotThan1_Manual, c.MucLieuSiloBotThan1_Auto)}</td>
<td>{Nd(c.MucLieuSiloBotThan2_Manual, c.MucLieuSiloBotThan2_Auto)}</td>
<td>{Nd(c.MucLieuSiloThanTho_Manual, c.MucLieuSiloThanTho_Auto)}</td>
<td>{Nd(c.TrongLuongBonPhunThoi1_Manual, c.TrongLuongBonPhunThoi1_Auto)}</td>
<td>{Nd(c.TrongLuongBonPhunThoi2_Manual, c.TrongLuongBonPhunThoi2_Auto)}</td>
<td>{Nd(c.TrongLuongBonPhunThoi3_Manual, c.TrongLuongBonPhunThoi3_Auto)}</td>
<td>{Nd(c.ApLucKhiThan_Manual, c.ApLucKhiThan_Auto)}</td>
<td>{Nd(c.ApLucBonKhiN2_Manual, c.ApLucBonKhiN2_Auto)}</td>
<td>{Nd(c.NhietDoTramDauBoiTron_Manual, c.NhietDoTramDauBoiTron_Auto)}</td>
<td>{Nd(c.NhietDoStatoDongCoMayNghien_Manual, c.NhietDoStatoDongCoMayNghien_Auto)}</td>
<td>{Nd(c.NhietDoTrucDongCoMayNghien_Manual, c.NhietDoTrucDongCoMayNghien_Auto)}</td>
<td>{Nd(c.ApLucTrucNghien_Manual, c.ApLucTrucNghien_Auto)}</td>
<td>{Nd(c.YeuCauTuLoCao_Manual, c.YeuCauTuLoCao_Auto)}</td>
<td>{Nd(c.LuongThanPhunThucTe_Manual, c.LuongThanPhunThucTe_Auto)}</td>
<td>{Nd(c.LuyKeLuongPhunThanTrongCa_Manual, c.LuyKeLuongPhunThanTrongCa_Auto)}</td>
<td>{Ni2(c.SoLuongSungPhun_Manual, c.SoLuongSungPhun_Auto)}</td>
<td>{Nd(c.ApLucGioLanhLoCao_Manual, c.ApLucGioLanhLoCao_Auto)}</td>
<td class=""td-left"">{System.Web.HttpUtility.HtmlEncode(c.GhiChu ?? "")}</td>
</tr>");
            }

            var nvVanHanhCaNgay = pheDuyets.FirstOrDefault(x => x.CapDuyet == 0);
            var nvVanHanhCaDem  = pheDuyets.FirstOrDefault(x => x.CapDuyet == 1);

            var logoUrl = _configuration.GetValue<string>("AppSettings:LogoUrl")
                          ?? "https://report.hoaphatdungquat.vn/img/logoHP.png";
            var logoBase64 = await ConvertImageUrlToBase64Async(logoUrl);
            var signNgay   = await FormatChuKyBase64Async(nvVanHanhCaNgay?.ChuKy, nvVanHanhCaNgay?.TinhTrang == 1);
            var signDem    = await FormatChuKyBase64Async(nvVanHanhCaDem?.ChuKy,  nvVanHanhCaDem?.TinhTrang  == 1);

            var templatePath = Path.Combine(_env.WebRootPath, "template_html",
                "BM.10-QT.05.09_Nhat_ky_van_hanh_phun_than_lo_cao.html");

            var html = await File.ReadAllTextAsync(templatePath);
            html = html
                .Replace("{{LogoUrl}}", logoBase64)
                .Replace("{{LoCao}}", loCaoDisplay)
                .Replace("{{NgaySX}}", ngayDisplay)
                .Replace("{{DataRows}}", rows.ToString())
                .Replace("{{Kip1}}", kip1)
                .Replace("{{Kip2}}", kip2)
                .Replace("{{Kip3}}", kip3)
                .Replace("{{Tong}}", tong)
                .Replace("{{SanLuongNghien}}", sanLuongNghien)
                .Replace("{{SanLuongPhun}}", sanLuongPhun)
                .Replace("{{TinhTrangSanXuatNgay}}", System.Web.HttpUtility.HtmlEncode(tinhTrangNgay))
                .Replace("{{TinhTrangSanXuatDem}}", System.Web.HttpUtility.HtmlEncode(tinhTrangDem))
                .Replace("{{SapXepSanXuat}}", System.Web.HttpUtility.HtmlEncode(sapXep))
                .Replace("{{Sign_NVVanHanhCaNgay}}", signNgay)
                .Replace("{{Ten_NVVanHanhCaNgay}}", nvVanHanhCaNgay?.HoVaTen ?? "")
                .Replace("{{Sign_NVVanHanhCaDem}}", signDem)
                .Replace("{{Ten_NVVanHanhCaDem}}", nvVanHanhCaDem?.HoVaTen ?? "");

            var pdfDoc = new HtmlToPdfDocument
            {
                GlobalSettings = { PaperSize = PaperKind.A4, Orientation = Orientation.Landscape },
                Objects =
                {
                    new ObjectSettings
                    {
                        HtmlContent = html,
                        WebSettings  = { DefaultEncoding = "utf-8", LoadImages = true, EnableJavascript = false, PrintMediaType = true },
                        LoadSettings = { BlockLocalFileAccess = false, LoadErrorHandling = ContentErrorHandling.Ignore }
                    }
                }
            };

            var pdfBytes = _pdfConverter.Convert(pdfDoc);
            var fileName = $"NK_PhunThanLoCao_{phieu.SoPhieu ?? idPhieu.ToString("N")}_{DateTime.Now:yyyyMMdd_HHmm}.pdf";
            return new ExportFileResult { Content = pdfBytes, FileName = fileName, ContentType = "application/pdf" };
        }

        // ─── Export Excel ────────────────────────────────────────────────────────────

        public async Task<ExportFileResult> ExportPhunThanExcelAsync(Guid idPhieu)
        {
            var phieu = await _repo.GetPhieuByIdAsync(idPhieu)
                ?? throw new Exception("Không tìm thấy phiếu.");

            var chiTiet = await _repo.GetChiTietByPhieuAsync(idPhieu);
            var firstRow = chiTiet.FirstOrDefault();
            var ngay = firstRow?.NgayVanHanh ?? DateTime.MinValue;
            var loCao = firstRow?.IDLoCao ?? 0;

            string kip1 = "", kip2 = "", kip3 = "", tong = "";
            string sanLuongNghien = "", sanLuongPhun = "";
            string tinhTrangNgay = "", tinhTrangDem = "", sapXep = "";
            if (!string.IsNullOrWhiteSpace(phieu.DataJson))
            {
                try
                {
                    using var jdoc = JsonDocument.Parse(phieu.DataJson);
                    var root = jdoc.RootElement;
                    if (root.TryGetProperty("tinhHinhSanLuong", out var sl))
                    {
                        kip1 = GetString(sl, "kip1") ?? "";
                        kip2 = GetString(sl, "kip2") ?? "";
                        kip3 = GetString(sl, "kip3") ?? "";
                        tong = GetString(sl, "tong") ?? "";
                        sanLuongNghien = GetString(sl, "sanLuongNghien") ?? "";
                        sanLuongPhun   = GetString(sl, "sanLuongPhun") ?? "";
                    }
                    tinhTrangNgay = GetString(root, "tinhTrangSanXuatNgay", "tinhTrangSanXuat") ?? "";
                    tinhTrangDem  = GetString(root, "tinhTrangSanXuatDem") ?? "";
                    sapXep        = GetString(root, "sapXepSanXuat") ?? "";
                }
                catch { }
            }

            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("Nhật ký phun than");

            // Styles
            var hdrFill  = XLColor.FromHtml("#DCE6F1");
            var boldFont = true;

            void SetBorder(IXLRange rng)
            {
                rng.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                rng.Style.Border.InsideBorder  = XLBorderStyleValues.Thin;
            }

            // ── Header công ty ──────────────────────────────────────────────────
            ws.Cell(1, 1).Value = "CÔNG TY CỔ PHẦN THÉP HOÀ PHÁT DUNG QUẤT";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Range(1, 1, 1, 10).Merge();

            ws.Cell(2, 1).Value = $"BM.10/QT.05.09 | Ngày hiệu lực: 10/04/2025 | Lần sửa đổi: 01";
            ws.Range(2, 1, 2, 10).Merge();

            // ── Tiêu đề ─────────────────────────────────────────────────────────
            int titleRow = 3;
            var loCaoStr = loCao > 0 ? $"LÒ CAO {loCao}" : "";
            ws.Cell(titleRow, 1).Value = $"NHẬT KÝ VẬN HÀNH PHUN THAN {loCaoStr}";
            ws.Cell(titleRow, 1).Style.Font.Bold = true;
            ws.Cell(titleRow, 1).Style.Font.FontSize = 13;
            ws.Cell(titleRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range(titleRow, 1, titleRow, 29).Merge();

            ws.Cell(4, 1).Value = ngay != DateTime.MinValue ? $"Ngày {ngay:dd/MM/yyyy}" : "";
            ws.Cell(4, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range(4, 1, 4, 29).Merge();

            // ── Header row 1 (cấp 1) ──────────────────────────────────────────
            int hdr1 = 5, hdr2 = 6, dataStart = 7;

            // Mapping: (col index 1-based, row1 text, colSpan, rowSpan, row2 text)
            var hdrDefs = new (int Col, string R1, int CS, int RS, string R2)[]
            {
                (1,  "Thời\ngian",                            1, 2, ""),
                (2,  "Nhiệt độ\nSilo bột than\n(°C)",         2, 1, ""),
                (4,  "Nhiệt độ bồn\nphun thổi (°C)",          3, 1, ""),
                (7,  "Dòng điện\nmáy nghiền",                 1, 1, "A"),
                (8,  "Dòng điện\nquạt gió\nthổi ngược",       1, 1, "A"),
                (9,  "Nhiệt độ\nđầu vào\nmáy nghiền",         1, 1, "°C"),
                (10, "Nhiệt độ\nđầu ra\nmáy nghiền",          1, 1, "°C"),
                (11, "Nhiệt độ\nkhoang lò",                   1, 1, "°C"),
                (12, "Mức liệu\nsilo bột than",               2, 1, ""),
                (14, "Mức liệu\nsilo than thô",               1, 1, "m"),
                (15, "Trọng lượng\nbồn phun thổi (t)",        3, 1, ""),
                (18, "Áp lực\nkhí than",                      1, 1, "Kpa"),
                (19, "Áp lực\nbồn khí N₂",                   1, 1, "Kpa"),
                (20, "Nhiệt độ\ntrạm dầu bôi trơn",          1, 1, "°C"),
                (21, "Nhiệt độ\nstato động cơ\nmáy nghiền",   1, 1, "°C"),
                (22, "Nhiệt độ ổ\ntrục động cơ\nmáy nghiền",  1, 1, "°C"),
                (23, "Áp lực\ntrục nghiền",                   1, 1, "Mpa"),
                (24, "Yêu cầu\ntừ lò cao",                    1, 1, "t/h"),
                (25, "Lượng than\nphun thực tế",              1, 1, "t/h"),
                (26, "Lũy kế lượng\nphun than\ntrong ca",     1, 1, "t"),
                (27, "Số lượng\nsúng phun",                   1, 1, "cái"),
                (28, "Áp lực gió\nlạnh lò cao",               1, 1, "Kpa"),
                (29, "Ghi chú",                               1, 2, ""),
            };

            // Sub-headers row 2 for grouped columns
            var subHeaders = new (int Col, string Text)[]
            {
                (2,  "1#"), (3,  "2#"),
                (4,  "1#"), (5,  "2#"), (6,  "3#"),
                (12, "1#"), (13, "2#"),
                (15, "1#"), (16, "2#"), (17, "3#"),
            };

            foreach (var (col, r1, cs, rs, r2) in hdrDefs)
            {
                var cell = ws.Cell(hdr1, col);
                cell.Value = r1;
                cell.Style.Font.Bold = boldFont;
                cell.Style.Fill.BackgroundColor = hdrFill;
                cell.Style.Alignment.WrapText = true;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                if (cs > 1) ws.Range(hdr1, col, hdr1, col + cs - 1).Merge();
                if (rs == 2) ws.Range(hdr1, col, hdr2, col).Merge();
                else if (!string.IsNullOrEmpty(r2))
                {
                    var cell2 = ws.Cell(hdr2, col);
                    cell2.Value = r2;
                    cell2.Style.Font.Bold = boldFont;
                    cell2.Style.Fill.BackgroundColor = hdrFill;
                    cell2.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell2.Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;
                }
            }

            foreach (var (col, text) in subHeaders)
            {
                var cell = ws.Cell(hdr2, col);
                cell.Value = text;
                cell.Style.Font.Bold = boldFont;
                cell.Style.Fill.BackgroundColor = hdrFill;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // ── Dữ liệu ─────────────────────────────────────────────────────────
            static decimal? Eff(decimal? m, decimal? a) => m ?? a;
            static int? EffI(int? m, int? a) => m ?? a;

            int r = dataStart;
            foreach (var c in chiTiet)
            {
                ws.Cell(r, 1).Value  = $"{c.ThoiGian.Hour:D2}h";
                ws.Cell(r, 2).Value  = (double?)Eff(c.NhietDoSiloBotThan1_Manual, c.NhietDoSiloBotThan1_Auto);
                ws.Cell(r, 3).Value  = (double?)Eff(c.NhietDoSiloBotThan2_Manual, c.NhietDoSiloBotThan2_Auto);
                ws.Cell(r, 4).Value  = (double?)Eff(c.NhietDoBonPhunThoi1_Manual, c.NhietDoBonPhunThoi1_Auto);
                ws.Cell(r, 5).Value  = (double?)Eff(c.NhietDoBonPhunThoi2_Manual, c.NhietDoBonPhunThoi2_Auto);
                ws.Cell(r, 6).Value  = (double?)Eff(c.NhietDoBonPhunThoi3_Manual, c.NhietDoBonPhunThoi3_Auto);
                ws.Cell(r, 7).Value  = (double?)Eff(c.DongDienMayNghien_Manual, c.DongDienMayNghien_Auto);
                ws.Cell(r, 8).Value  = (double?)Eff(c.DongDienQuatGioNguoc_Manual, c.DongDienQuatGioNguoc_Auto);
                ws.Cell(r, 9).Value  = (double?)Eff(c.NhietDoDauVaoMayNghien_Manual, c.NhietDoDauVaoMayNghien_Auto);
                ws.Cell(r, 10).Value = (double?)Eff(c.NhietDoDauRaMayNghien_Manual, c.NhietDoDauRaMayNghien_Auto);
                ws.Cell(r, 11).Value = (double?)Eff(c.NhietDoKhoangLo_Manual, c.NhietDoKhoangLo_Auto);
                ws.Cell(r, 12).Value = (double?)Eff(c.MucLieuSiloBotThan1_Manual, c.MucLieuSiloBotThan1_Auto);
                ws.Cell(r, 13).Value = (double?)Eff(c.MucLieuSiloBotThan2_Manual, c.MucLieuSiloBotThan2_Auto);
                ws.Cell(r, 14).Value = (double?)Eff(c.MucLieuSiloThanTho_Manual, c.MucLieuSiloThanTho_Auto);
                ws.Cell(r, 15).Value = (double?)Eff(c.TrongLuongBonPhunThoi1_Manual, c.TrongLuongBonPhunThoi1_Auto);
                ws.Cell(r, 16).Value = (double?)Eff(c.TrongLuongBonPhunThoi2_Manual, c.TrongLuongBonPhunThoi2_Auto);
                ws.Cell(r, 17).Value = (double?)Eff(c.TrongLuongBonPhunThoi3_Manual, c.TrongLuongBonPhunThoi3_Auto);
                ws.Cell(r, 18).Value = (double?)Eff(c.ApLucKhiThan_Manual, c.ApLucKhiThan_Auto);
                ws.Cell(r, 19).Value = (double?)Eff(c.ApLucBonKhiN2_Manual, c.ApLucBonKhiN2_Auto);
                ws.Cell(r, 20).Value = (double?)Eff(c.NhietDoTramDauBoiTron_Manual, c.NhietDoTramDauBoiTron_Auto);
                ws.Cell(r, 21).Value = (double?)Eff(c.NhietDoStatoDongCoMayNghien_Manual, c.NhietDoStatoDongCoMayNghien_Auto);
                ws.Cell(r, 22).Value = (double?)Eff(c.NhietDoTrucDongCoMayNghien_Manual, c.NhietDoTrucDongCoMayNghien_Auto);
                ws.Cell(r, 23).Value = (double?)Eff(c.ApLucTrucNghien_Manual, c.ApLucTrucNghien_Auto);
                ws.Cell(r, 24).Value = (double?)Eff(c.YeuCauTuLoCao_Manual, c.YeuCauTuLoCao_Auto);
                ws.Cell(r, 25).Value = (double?)Eff(c.LuongThanPhunThucTe_Manual, c.LuongThanPhunThucTe_Auto);
                ws.Cell(r, 26).Value = (double?)Eff(c.LuyKeLuongPhunThanTrongCa_Manual, c.LuyKeLuongPhunThanTrongCa_Auto);
                ws.Cell(r, 27).Value = EffI(c.SoLuongSungPhun_Manual, c.SoLuongSungPhun_Auto);
                ws.Cell(r, 28).Value = (double?)Eff(c.ApLucGioLanhLoCao_Manual, c.ApLucGioLanhLoCao_Auto);
                ws.Cell(r, 29).Value = c.GhiChu ?? "";

                ws.Row(r).Cells(1, 29).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(r, 29).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                r++;
            }

            // Border toàn bộ bảng
            SetBorder(ws.Range(hdr1, 1, r - 1, 29));

            // ── Tình hình sản lượng ──────────────────────────────────────────────
            r++;
            ws.Cell(r, 1).Value = "Tình hình sản lượng";
            ws.Cell(r, 1).Style.Font.Bold = true;
            ws.Range(r, 1, r, 3).Merge();
            ws.Cell(r, 4).Value = "1- Tình trạng sản xuất từ 8h00-20h00";
            ws.Cell(r, 4).Style.Font.Bold = true;
            ws.Range(r, 4, r, 16).Merge();
            ws.Cell(r, 17).Value = "2- Tình trạng sản xuất từ 20h00-8h00";
            ws.Cell(r, 17).Style.Font.Bold = true;
            ws.Range(r, 17, r, 29).Merge();
            r++;
            ws.Cell(r, 1).Value = "Kíp"; ws.Cell(r, 2).Value = kip1;
            ws.Cell(r, 3).Value = kip2;  ws.Cell(r, 4).Value = kip3;
            ws.Cell(r, 5).Value = "Tổng"; ws.Cell(r, 6).Value = tong;
            ws.Cell(r, 7).Value = tinhTrangNgay;
            ws.Range(r, 7, r + 1, 16).Merge();
            ws.Cell(r, 17).Value = tinhTrangDem;
            ws.Range(r, 17, r + 1, 29).Merge();
            r++;
            ws.Cell(r, 1).Value = "SL nghiền (t)"; ws.Cell(r, 2).Value = sanLuongNghien;
            ws.Range(r, 2, r, 6).Merge();
            r++;
            ws.Cell(r, 1).Value = "SL phun (t)";   ws.Cell(r, 2).Value = sanLuongPhun;
            ws.Range(r, 2, r, 6).Merge();
            r++;
            ws.Cell(r, 1).Value = "Sắp xếp sản xuất:";
            ws.Cell(r, 1).Style.Font.Bold = true;
            ws.Cell(r, 2).Value = sapXep;
            ws.Range(r, 2, r, 29).Merge();

            // Cột width
            ws.Column(1).Width  = 8;
            ws.Columns(2, 28).Width = 10;
            ws.Column(29).Width = 20;
            ws.Row(hdr1).Height = 40;
            ws.Row(hdr2).Height = 15;

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            var fileName = $"NK_PhunThanLoCao_{phieu.SoPhieu ?? idPhieu.ToString("N")}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            return new ExportFileResult
            {
                Content     = ms.ToArray(),
                FileName    = fileName,
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            };
        }

        // ─── Shared helpers ──────────────────────────────────────────────────────────

        private async Task<string> ConvertImageUrlToBase64Async(string imageUrl)
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                var bytes = await client.GetByteArrayAsync(imageUrl);
                var ext   = Path.GetExtension(imageUrl).TrimStart('.').ToLower();
                var mime  = ext == "png" ? "image/png" : "image/jpeg";
                return $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
            }
            catch { return imageUrl; }
        }

        private async Task<string> FormatChuKyBase64Async(string? chuKy, bool daKy = false)
        {
            if (string.IsNullOrWhiteSpace(chuKy))
            {
                return daKy
                    ? "<div style='text-align:center;font-style:italic;color:red'>Đã ký<br/>(Chưa cập nhật chữ ký)</div>"
                    : "";
            }
            if (chuKy.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
                return $"<img src=\"{chuKy}\" style=\"max-width:150px;max-height:80px;\" />";

            var domain = _configuration.GetValue<string>("AppSettings:Domain") ?? "https://report.hoaphatdungquat.vn";
            var url = chuKy.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? chuKy
                : domain.TrimEnd('/') + chuKy;

            var b64 = await ConvertImageUrlToBase64Async(url);
            return !string.IsNullOrEmpty(b64)
                ? $"<img src=\"{b64}\" style=\"max-width:150px;max-height:80px;\" />"
                : "";
        }
    }
}

using dataproduct.api.DTOs;
using dataproduct.api.DTOs.Export;
using dataproduct.api.Models;
using dataproduct.api.Models.MasterData;
using dataproduct.api.Repositories;
using dataproduct.api.ResponseModels;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.AspNetCore.Hosting;
using System.IO.Compression;
using System.Text;
using PaperKind = DinkToPdf.PaperKind;

namespace dataproduct.api.Services
{
    public class DLNMHRC1Service
    {
        private readonly IDLNMHRC1Repository _repo;
        private readonly ISTD_NXT_HRC1Repository _stdNxtRepo;
        private readonly HRC1_NMSyncService _syncService;
        private readonly SyncPhanLoaiService _syncPhanLoaiService;
        private readonly ProductFormContext _context;
        private readonly ProductDataMasterDbContext _masterContext;
        // Dùng cho export Excel/PDF chi tiết phiếu (gộp từ Hrc1PhieuDetailExcelService cũ — tránh sinh
        // thêm file, xem vùng "EXPORT EXCEL/PDF CHI TIẾT PHIẾU" ở cuối class).
        private readonly IConverter _pdfConverter;
        private readonly IWebHostEnvironment _env;
        private readonly PheDuyetService _pheDuyetService;
        private readonly HRC2_NMSyncService _hrc2NMSyncService;

        private const int HeaderParentRow = 6;
        private const int HeaderChildRow = 7;
        private const int DataStartRow = 8;

        // Debounce sync: mỗi key (ngay_ca_scope) chỉ sync tối đa 1 lần / 2 phút — cùng cơ chế DLNMHRC2Service.
        private static readonly ConcurrentDictionary<string, DateTime> _lastSyncTimes = new();
        private static readonly TimeSpan SyncCooldown = TimeSpan.FromMinutes(2);

        private static bool ShouldSync(SyncFromNM_HRC1_Request req)
        {
            var key = $"{req.NgaySX:yyyy-MM-dd}_{req.Ca}_{req.Scope}";
            var now = DateTime.UtcNow;
            if (_lastSyncTimes.TryGetValue(key, out var last) && now - last < SyncCooldown)
                return false;
            _lastSyncTimes[key] = now;
            return true;
        }

        // Debounce riêng cho sync Mác thép của LF — key có tiền tố "LFMAC_" để không đụng key sync
        // BOF ở trên (key đó không phân biệt BieuMau, TL số và Lò số có thể trùng 1-5).
        private static bool ShouldSyncLfMacThep(DateOnly ngay, int ca, int scope)
        {
            var key = $"LFMAC_{ngay:yyyy-MM-dd}_{ca}_{scope}";
            var now = DateTime.UtcNow;
            if (_lastSyncTimes.TryGetValue(key, out var last) && now - last < SyncCooldown)
                return false;
            _lastSyncTimes[key] = now;
            return true;
        }

        public DLNMHRC1Service(
            IDLNMHRC1Repository repo,
            ISTD_NXT_HRC1Repository stdNxtRepo,
            HRC1_NMSyncService syncService,
            SyncPhanLoaiService syncPhanLoaiService,
            ProductFormContext context,
            ProductDataMasterDbContext masterContext,
            PheDuyetService pheDuyetService,
            IConverter pdfConverter,
            IWebHostEnvironment env,
            HRC2_NMSyncService hrc2NMSyncService)
        {
            _repo = repo;
            _stdNxtRepo = stdNxtRepo;
            _syncService = syncService;
            _syncPhanLoaiService = syncPhanLoaiService;
            _context = context;
            _masterContext = masterContext;
            _pheDuyetService = pheDuyetService;
            _pdfConverter = pdfConverter;
            _env = env;
            _hrc2NMSyncService = hrc2NMSyncService;
        }

        /// <summary>
        /// Autocomplete meThoi khi thêm dòng tay ở phiếu Tiêu hao BOF/LF — mirror
        /// BBGN_ThepLongService.SearchMeThoi, đọc thẳng Tbl_MeThoi bên DB Master. Có IdLoThoi (đúng
        /// Lò thổi/Scope đang chọn trên phiếu) → chỉ tìm đúng lò đó. Không có → fallback toàn bộ lò
        /// thổi nhà máy 1 ({1,2,3,4,5}, giống nhánh nhaMay==1 mặc định của BBGN).
        /// </summary>
        public async Task<List<string>> SearchMeThoiAsync(HRC1_SearchMeThoiRequest request)
        {
            var query = request?.IdLoThoi is int idLoThoi
                ? _masterContext.Tbl_MeThoi.Where(x => x.Is_Delete != true && x.ID_LoThoi == idLoThoi)
                : _masterContext.Tbl_MeThoi.Where(x => x.Is_Delete != true && new[] { 1, 2, 3, 4, 5 }.Contains(x.ID_LoThoi));

            if (!string.IsNullOrWhiteSpace(request?.SearchStr))
                query = query.Where(x => x.MaMeThoi.Contains(request.SearchStr.Trim()));

            return await query
                .OrderByDescending(x => x.ID)
                .Select(x => x.MaMeThoi)
                .Take(50)
                .ToListAsync();
        }

        public async Task<List<Hrc1GroupedByMeThoiModel>> FilterGroupedAsync(SyncFromNM_HRC1_Request request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // Chỉ BOF có nguồn NM (linked-server view) để đồng bộ. Các BieuMau khác (vd "LF", nhập tay
            // hoàn toàn) không có gì để sync — gọi sync cho chúng sẽ khiến SP_HRC1_BOF_Sync_Full throw
            // (LoThoi=5 không được hỗ trợ) hoặc vô tình đồng bộ đè dữ liệu BOF nếu Scope trùng 1-4.
            if (ShouldSync(request) && string.Equals(request.BieuMau, "BOF", StringComparison.OrdinalIgnoreCase))
                await _syncService.SyncHRC1FromNMAsync(request);

            var ngay = DateOnly.FromDateTime(request.NgaySX);

            // LF không có nguồn NM riêng cho Mác thép — Mác thép của nó "mượn" từ HRC1_MeThep.MacThepBKMIS
            // (liên kết 1 chiều từ module BBGN_ThepLong, xem HRC1_BBGNRepository.UpsertLfTieuHaoFromMeAsync).
            // Field đó tự cập nhật khi có thao tác bên BBGN (nhận mẻ/nhập liệu), nhưng nếu Mác thép được
            // đồng bộ mới từ Linked Server (nút "Làm mới" bên BBGN) SAU khi mẻ đã liên kết, LF không tự biết —
            // nên khi "Làm mới dữ liệu" bên LF cũng phải tự đồng bộ lại đúng như BBGN đang làm.
            if (string.Equals(request.BieuMau, "LF", StringComparison.OrdinalIgnoreCase)
                && ShouldSyncLfMacThep(ngay, request.Ca, request.Scope))
                await SyncLfMacThepFromMeThepAsync(ngay, request.Ca, request.Scope);

            var allData = await _repo.GetAllAsync(ngay, request.Ca, request.Scope, request.BieuMau ?? "BOF");
            return await _repo.GetAllGroupedBatchAsync(allData);
        }

        /// <summary>
        /// Đồng bộ Mác thép cho các dòng Hrc1TieuHao(LF) trong đúng Ngày/Ca/Scope: gọi lại
        /// SyncPhanLoaiService (Linked Server) để làm mới HRC1_MeThep.MacThepBKMIS theo MaMe, rồi copy
        /// giá trị đó vào Hrc1TieuHao.MacThep — mirror đúng field mapping của UpsertLfTieuHaoFromMeAsync.
        /// Không đụng dòng đã IsEdited=true (đã sửa tay bên Tiêu hao).
        /// </summary>
        private async Task SyncLfMacThepFromMeThepAsync(DateOnly ngay, int ca, int scope)
        {
            var lfRows = await _context.Hrc1TieuHaos
                .Where(x => x.BieuMau == "LF" && !x.IsDeleted && !x.IsEdited
                         && x.NgaySanXuat == ngay && x.Ca == (byte)ca && x.Scope == scope
                         && x.MeThoi != null)
                .ToListAsync();
            if (lfRows.Count == 0) return;

            var meThois = lfRows.Select(x => x.MeThoi!).Distinct().ToList();

            await _syncPhanLoaiService.SyncHRC1MeThepAsync(meThois);

            var meThepMap = await _context.HRC1_MeTheps
                .Where(x => x.MaMe != null && meThois.Contains(x.MaMe))
                .ToDictionaryAsync(x => x.MaMe!, x => x.MacThepBKMIS);

            bool changed = false;
            foreach (var row in lfRows)
            {
                if (row.MeThoi != null && meThepMap.TryGetValue(row.MeThoi, out var macThep)
                    && !string.IsNullOrWhiteSpace(macThep) && row.MacThep != macThep)
                {
                    row.MacThep = macThep;
                    row.NgayCapNhat = DateTime.Now;
                    changed = true;
                }
            }
            if (changed) await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Ép đồng bộ ngay lập tức, bỏ qua cooldown — dùng cho nút "Đồng bộ lại" nếu FE cần.
        /// </summary>
        public async Task ForceSyncAsync(SyncFromNM_HRC1_Request request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var key = $"{request.NgaySX:yyyy-MM-dd}_{request.Ca}_{request.Scope}";
            _lastSyncTimes[key] = DateTime.MinValue;
            await _syncService.SyncHRC1FromNMAsync(request);
        }

        /// <summary>
        /// "Làm mới" trên trang Sổ Xuất-Nhập-Tồn HRC1. Chỉ BOF có sync tự động từ NM (lò 1-5, view nguồn
        /// vw_BOF5 đã có đủ mẻ/MeThoi, có thể chưa đủ hết các cột phụ liệu — SP_HRC1_BOF_Sync_Full xử lý
        /// bình thường, không throw).
        /// LF không có SP sync (hoàn toàn nhập tay qua phiếu HRC1_BB_TieuHao_LF) nên không cần trigger gì
        /// thêm — chỉ đọc thẳng HRC1_TieuHao/HRC1_PhuLieu đã có sẵn. Mirror
        /// DLNMHRC2Service.FilterSTD_NXTAsync.
        /// </summary>
        public async Task<List<FilterSTD_NXTResponse_HRC1>> FilterSTD_NXTAsync(FilterSTD_NXTRequest_HRC1 request)
        {
            for (var loThoi = 1; loThoi <= 5; loThoi++)
            {
                var syncReq = new SyncFromNM_HRC1_Request
                {
                    NgaySX = request.NgaySX,
                    Ca = request.Ca,
                    Scope = loThoi,
                    BieuMau = "BOF",
                };
                if (ShouldSync(syncReq))
                    await _syncService.SyncHRC1FromNMAsync(syncReq);
            }

            var result = await _repo.GetHRC1GroupedByMaterialAsync(request.NgaySX, request.Ca);

            if (request.IdPhieu.HasValue && request.IdPhieu.Value != Guid.Empty)
            {
                var phuLieuIds = (request.PhuLieuIds != null && request.PhuLieuIds.Count > 0)
                    ? request.PhuLieuIds.Where(id => id > 0).Distinct().ToList()
                    : result.Select(x => x.PhuLieuID).Distinct().ToList();

                if (phuLieuIds.Count > 0)
                {
                    await _stdNxtRepo.GetHRC1FilterInitAsync(new InitXuatNhapTonHRC1Request
                    {
                        NgaySX = request.NgaySX,
                        Ca = request.Ca,
                        IdPhieu = request.IdPhieu.Value,
                        PhuLieus = phuLieuIds.Select(id => new IdPhuLieuModel { Id_PhuLieu = id }).ToList()
                    });
                }
            }

            return result;
        }

        // =========================================================
        // Lưu phụ liệu manual + manual_col từ formData phiếu (hook IPhieuJsonInitializer)
        // =========================================================
        public async Task SaveHRC1ManualFromPhieuFormAsync(JsonElement formData)
        {
            var (models, manualColHeaderKeyIds) = await BuildModelToInsert(formData);
            await SaveHRC1ManualDataAsync(models, manualColHeaderKeyIds);
        }

        private double? TryGetDouble(JsonElement row, string key)
        {
            if (row.TryGetProperty(key, out var p))
                return TryConvertNumeric(p);
            return null;
        }

        private decimal? TryGetDecimal(JsonElement row, string key)
        {
            var d = TryGetDouble(row, key);
            return d.HasValue ? (decimal?)d.Value : null;
        }

        private int? TryGetInt(JsonElement row, string key)
        {
            var d = TryGetDouble(row, key);
            return d.HasValue ? (int?)d.Value : null;
        }

        private double? TryConvertNumeric(JsonElement val)
        {
            if (val.ValueKind == JsonValueKind.Number)
                return val.GetDouble();

            if (val.ValueKind == JsonValueKind.String &&
                double.TryParse(val.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                return d;

            return null;
        }

        public async Task<(List<Hrc1InsertModel> Models, HashSet<int> ManualColHeaderKeyIds)> BuildModelToInsert(JsonElement formData)
        {
            var result = new List<Hrc1InsertModel>();

            string? bm = formData.TryGetProperty("maBm", out var bmProp) ? bmProp.GetString() : null;
            if (!string.Equals(bm, "HRC1_BB_TieuHao_BOF", StringComparison.OrdinalIgnoreCase))
                return (result, new HashSet<int>());

            int scope = formData.GetProperty("scope").GetInt32();
            int ca = formData.GetProperty("ca").GetInt32();

            string? ngaySXstr = formData.TryGetProperty("NgaySX", out var nsxProp) ? nsxProp.GetString() : null;
            DateOnly ngaySX = !string.IsNullOrEmpty(ngaySXstr)
                ? DateOnly.Parse(ngaySXstr)
                : DateOnly.FromDateTime(DateTime.Now);

            // "BOF_PhuGia": 13 phụ liệu cố định; "adjust": cột điều chỉnh tự do (manual_col_*)
            var colGroups = new List<string> { "BOF_PhuGia", "adjust" };

            if (!formData.TryGetProperty("table1DynamicColumns", out var dynamicRoot))
                return (result, new HashSet<int>());

            var dynamicCols = colGroups
                .Where(g => dynamicRoot.TryGetProperty(g, out _))
                .SelectMany(g => dynamicRoot.GetProperty(g).EnumerateArray())
                .ToList();

            if (!formData.TryGetProperty("table1", out var table1Prop))
                return (result, new HashSet<int>());
            var table1 = table1Prop.EnumerateArray().ToList();

            var manualColIds = new HashSet<int>();
            foreach (var col in dynamicCols)
            {
                var di = col.GetProperty("dataIndex").GetString();
                if (di != null && di.StartsWith("manual_col_") && int.TryParse(di.Substring("manual_col_".Length), out var id))
                    manualColIds.Add(id);
            }
            var manualColHeaderKeyIds = new HashSet<int>(manualColIds);

            var headerKeyLabelMap = manualColIds.Count > 0
                ? await _context.Header_Keys
                    .Where(k => manualColIds.Contains(k.Id))
                    .ToDictionaryAsync(k => k.Id, k => k.TenHienThi)
                : new Dictionary<int, string>();

            foreach (var row in table1)
            {
                bool isNMRow = !row.TryGetProperty("IsNM", out var flag) || flag.ValueKind != JsonValueKind.False;

                if (!row.TryGetProperty("id", out var idProp) && isNMRow) continue;
                int? rowId = idProp.ValueKind == JsonValueKind.Number ? idProp.GetInt32() : null;
                if (isNMRow && (rowId == null || rowId <= 0)) continue;

                string? meThoi = row.TryGetProperty("meThoi", out var mtp) ? mtp.GetString() : null;
                if (isNMRow && string.IsNullOrEmpty(meThoi)) continue;

                var phuLieus = BuildPhuLieus(row, dynamicCols, meThoi, isNMRow, headerKeyLabelMap);

                var o2 = TryGetDouble(row, "o2");
                var n2 = TryGetDouble(row, "n2");
                var ar = TryGetDouble(row, "ar");
                var queLayMau = TryGetInt(row, "queLayMau");
                var queDoNhiet = TryGetInt(row, "queDoNhiet");
                var ghiChu = row.TryGetProperty("ghiChu", out var gcProp) && gcProp.ValueKind == JsonValueKind.String
                    ? gcProp.GetString() : null;

                if (isNMRow)
                {
                    // Mẻ từ NM: chỉ các trường nhập tay (khí + que + ghi chú) được phép sửa qua form.
                    if (!phuLieus.Any() && o2 == null && n2 == null && ar == null
                        && queLayMau == null && queDoNhiet == null && ghiChu == null)
                        continue;

                    result.Add(new Hrc1InsertModel
                    {
                        Id = rowId,
                        NgaySanXuat = ngaySX,
                        Ca = (byte)ca,
                        Scope = scope,
                        MeThoi = meThoi!,
                        O2 = o2,
                        N2 = n2,
                        AR = ar,
                        QueLayMau = queLayMau,
                        QueDoNhiet = queDoNhiet,
                        GhiChu = ghiChu,
                        IsNM = true,
                        RowKey = Guid.NewGuid(),
                        PhuLieus = phuLieus
                    });
                }
                else
                {
                    result.Add(new Hrc1InsertModel
                    {
                        Id = rowId,
                        NgaySanXuat = ngaySX,
                        Ca = (byte)ca,
                        Scope = scope,
                        MeThoi = meThoi ?? "",
                        MacThep = row.TryGetProperty("macThep", out var mt) ? mt.GetString() : null,
                        KLGang = TryGetDecimal(row, "klGang"),
                        KLThepPhe = TryGetDecimal(row, "klThepPhe"),
                        O2 = o2,
                        N2 = n2,
                        AR = ar,
                        QueLayMau = queLayMau,
                        QueDoNhiet = queDoNhiet,
                        GhiChu = ghiChu,
                        IsNM = false,
                        RowKey = Guid.NewGuid(),
                        PhuLieus = phuLieus
                    });
                }
            }

            return (result, manualColHeaderKeyIds);
        }

        private List<Hrc1_PhuLieuInsertModel> BuildPhuLieus(
            JsonElement row,
            List<JsonElement> dynamicCols,
            string? meThoi,
            bool isNMRow,
            Dictionary<int, string> headerKeyLabelMap)
        {
            var result = new List<Hrc1_PhuLieuInsertModel>();
            int? rowId = row.TryGetProperty("id", out var rowIdProp) && rowIdProp.ValueKind == JsonValueKind.Number
                ? rowIdProp.GetInt32()
                : null;

            foreach (var col in dynamicCols)
            {
                string dataIndex = col.GetProperty("dataIndex").GetString() ?? "";
                bool isManualAddedAdjust = dataIndex.StartsWith("manual_col_");
                bool isPhuLieuColumn = dataIndex.StartsWith("phuLieu_");
                if (!isManualAddedAdjust && !isPhuLieuColumn) continue;

                double? currentNumeric;
                if (isManualAddedAdjust)
                {
                    currentNumeric = TryResolveManualAdjustValue(row, rowId, meThoi, col, dataIndex);
                }
                else
                {
                    if (!row.TryGetProperty(dataIndex, out var valProp)) continue;
                    currentNumeric = TryConvertNumeric(valProp);
                }

                string? label = col.TryGetProperty("label", out var lblProp) ? lblProp.GetString() : null;

                if (isManualAddedAdjust)
                {
                    if (!currentNumeric.HasValue) continue;
                    if (!isNMRow && currentNumeric.Value == 0) continue;

                    var suffix = dataIndex.Substring("manual_col_".Length);
                    int? headerKeyId = int.TryParse(suffix, out var parsedHk) ? parsedHk : (int?)null;
                    if (headerKeyId.HasValue && string.IsNullOrEmpty(label))
                        headerKeyLabelMap.TryGetValue(headerKeyId.Value, out label);

                    result.Add(new Hrc1_PhuLieuInsertModel
                    {
                        MeThoi = meThoi,
                        PhuLieuID = null,
                        ID_HeaderKey = headerKeyId,
                        TenPhuLieu = label,
                        IsManual = true,
                        IsAddManual = true,
                        KLPhuGia_Manual = currentNumeric
                    });
                    continue;
                }

                // Cột phụ liệu cố định: dataIndex = phuLieu_{PhuLieuID}
                var plSuffix = dataIndex.Substring("phuLieu_".Length);
                if (!int.TryParse(plSuffix, out var phuLieuId)) continue;

                var origKey = $"{dataIndex}__orig";
                bool hasOrig = row.TryGetProperty(origKey, out var origProp);
                var origNumeric = hasOrig ? TryConvertNumeric(origProp) : null;

                var manualFlagKey = $"{dataIndex}__IsManual";
                bool isManualFromFlag = row.TryGetProperty(manualFlagKey, out var manualFlagProp) &&
                                        manualFlagProp.ValueKind == JsonValueKind.True;

                bool isManualFromOrig =
                    hasOrig
                    && origNumeric.HasValue
                    && currentNumeric.HasValue
                    && Math.Abs(currentNumeric.Value - origNumeric.Value) > 0.000001;

                bool isManual = isManualFromFlag || isManualFromOrig;

                // Cột này có đang được quản lý qua nút "Thêm cột điều chỉnh" không — do FE đánh dấu tường minh
                // (TaoTieuHaoLoThoi.tsx: manuallyAddedPhuLieuDataIndexes). KHÔNG suy luận từ việc có baseline/__orig
                // hay không, vì 13 phụ liệu mặc định luôn có baseline thật (KLPhuGia = 0, không NULL) dù đang ẩn khỏi
                // cột tự động — nếu suy luận theo baseline sẽ nhầm thành "sửa tay 1 giá trị NM có sẵn" (IsAddManual sai).
                var addManualFlagKey = $"{dataIndex}__IsAddManual";
                bool isAddManualColumn = row.TryGetProperty(addManualFlagKey, out var addManualProp) &&
                                         addManualProp.ValueKind == JsonValueKind.True;

                double? klPhuGia;
                double? klPhuGiaManual;
                if (!isManual)
                {
                    klPhuGia = currentNumeric;
                    klPhuGiaManual = null;
                }
                else if (isManualFromOrig)
                {
                    klPhuGia = origNumeric;
                    klPhuGiaManual = currentNumeric;
                }
                else if (origNumeric.HasValue)
                {
                    klPhuGia = origNumeric;
                    klPhuGiaManual = null;
                }
                else
                {
                    klPhuGia = null;
                    klPhuGiaManual = currentNumeric;
                }

                // Chỉ skip khi không phải manual — record manual có thể cần dọn dẹp record cũ trong DB
                if (!isManual && (klPhuGia == null || klPhuGia == 0) && (klPhuGiaManual == null || klPhuGiaManual == 0))
                    continue;

                result.Add(new Hrc1_PhuLieuInsertModel
                {
                    MeThoi = meThoi,
                    PhuLieuID = phuLieuId,
                    TenPhuLieu = label,
                    KLPhuGia = klPhuGia,
                    IsManual = isManual,
                    KLPhuGia_Manual = klPhuGiaManual,
                    IsAddManual = isAddManualColumn,
                    IsNM = isAddManualColumn ? false : (bool?)null
                });
            }

            return result;
        }

        private double? TryResolveManualAdjustValue(
            JsonElement row,
            int? rowId,
            string? meThoi,
            JsonElement dynamicCol,
            string dataIndex)
        {
            if (row.TryGetProperty(dataIndex, out var rowVal))
            {
                var rowNumeric = TryConvertNumeric(rowVal);
                if (rowNumeric.HasValue)
                    return rowNumeric;
            }

            if (!dynamicCol.TryGetProperty("values", out var valuesProp) ||
                valuesProp.ValueKind != JsonValueKind.Array)
                return null;

            foreach (var item in valuesProp.EnumerateArray())
            {
                var itemRowId = item.TryGetProperty("rowId", out var ridProp) &&
                                ridProp.ValueKind == JsonValueKind.Number
                    ? ridProp.GetInt32()
                    : (int?)null;

                var itemMeThoi = item.TryGetProperty("meThoi", out var mtProp) &&
                                 mtProp.ValueKind == JsonValueKind.String
                    ? mtProp.GetString()
                    : null;

                var matchByRowId = rowId.HasValue && itemRowId.HasValue && rowId.Value == itemRowId.Value;
                var matchByMeThoi = !string.IsNullOrWhiteSpace(meThoi) &&
                                    !string.IsNullOrWhiteSpace(itemMeThoi) &&
                                    string.Equals(meThoi, itemMeThoi, StringComparison.OrdinalIgnoreCase);

                if (!matchByRowId && !matchByMeThoi)
                    continue;

                if (!item.TryGetProperty("value", out var valueProp))
                    return null;

                var val = TryConvertNumeric(valueProp);
                if (val.HasValue)
                    return val;
            }

            return null;
        }

        public async Task SaveHRC1ManualDataAsync(List<Hrc1InsertModel> models, HashSet<int> manualColHeaderKeyIds)
        {
            if (models == null || !models.Any()) return;

            var meMap = new Dictionary<Guid, Hrc1TieuHao>();

            var allMeThois = models.Select(m => m.MeThoi).Distinct().ToList();
            var allIds = models.Where(m => m.Id.HasValue && m.Id.Value > 0).Select(m => m.Id!.Value).ToList();

            var existingRows = await _context.Hrc1TieuHaos
                .Where(x => !x.IsDeleted &&
                    (allIds.Contains(x.ID) || (x.MeThoi != null && allMeThois.Contains(x.MeThoi) && x.BieuMau == "BOF")))
                .ToListAsync();

            // Thu thập MeThoi cũ bị thay đổi trước khi ghi đè (để re-check trùng cho MeThoi cũ)
            var oldMeThoiChanges = new HashSet<string>();
            foreach (var model in models)
            {
                if (!model.Id.HasValue || model.Id <= 0) continue;
                var ex = existingRows.FirstOrDefault(x => x.ID == model.Id.Value);
                if (ex == null || ex.IsNM) continue;
                if (!string.IsNullOrEmpty(ex.MeThoi) && !string.Equals(ex.MeThoi, model.MeThoi, StringComparison.OrdinalIgnoreCase))
                    oldMeThoiChanges.Add(ex.MeThoi);
            }

            foreach (var model in models)
            {
                var existing = model.Id.HasValue && model.Id > 0
                    ? existingRows.FirstOrDefault(x => x.ID == model.Id.Value)
                    : null;

                // IsNM=true: chỉ cho phép sửa các field nhập tay, không đụng field NM khác
                if (existing != null && existing.IsNM)
                {
                    if (model.O2.HasValue) existing.O2 = model.O2;
                    if (model.N2.HasValue) existing.N2 = model.N2;
                    if (model.AR.HasValue) existing.AR = model.AR;
                    if (model.QueLayMau.HasValue) existing.QueLayMau = model.QueLayMau;
                    if (model.QueDoNhiet.HasValue) existing.QueDoNhiet = model.QueDoNhiet;
                    if (model.GhiChu != null) existing.GhiChu = model.GhiChu;
                    existing.NgayCapNhat = DateTime.Now;
                    _context.Hrc1TieuHaos.Update(existing);
                    meMap[model.RowKey] = existing;
                    continue;
                }

                if (existing == null && model.Id.HasValue && model.Id > 0) continue; // tham chiếu cũ không còn tồn tại

                Hrc1TieuHao entity;
                if (existing == null)
                {
                    entity = new Hrc1TieuHao
                    {
                        BieuMau = "BOF",
                        Scope = model.Scope,
                        MeThoi = model.MeThoi,
                        MacThep = model.MacThep,
                        KLGang = model.KLGang,
                        KLThepPhe = model.KLThepPhe,
                        O2 = model.O2,
                        N2 = model.N2,
                        AR = model.AR,
                        QueLayMau = model.QueLayMau,
                        QueDoNhiet = model.QueDoNhiet,
                        GhiChu = model.GhiChu,
                        NgaySanXuat = model.NgaySanXuat,
                        Ca = model.Ca,
                        IsNM = false,
                        IsEdited = false,
                        NgayTao = DateTime.Now
                    };
                    await _context.Hrc1TieuHaos.AddAsync(entity);
                }
                else
                {
                    existing.MeThoi = model.MeThoi;
                    existing.MacThep = model.MacThep;
                    existing.KLGang = model.KLGang;
                    existing.KLThepPhe = model.KLThepPhe;
                    existing.O2 = model.O2;
                    existing.N2 = model.N2;
                    existing.AR = model.AR;
                    existing.QueLayMau = model.QueLayMau;
                    existing.QueDoNhiet = model.QueDoNhiet;
                    existing.GhiChu = model.GhiChu;
                    existing.NgaySanXuat = model.NgaySanXuat;
                    existing.Ca = model.Ca;
                    existing.NgayCapNhat = DateTime.Now;
                    _context.Hrc1TieuHaos.Update(existing);
                    entity = existing;
                }

                meMap[model.RowKey] = entity;
            }

            await _context.SaveChangesAsync();

            // Check trùng (SP_HRC1_BOF_CapNhatTrangThaiTrung) cho mọi MeThoi bị ảnh hưởng — mới lẫn cũ bị đổi
            var affectedMeThois = meMap.Values
                .Select(x => x.MeThoi)
                .Where(x => !string.IsNullOrEmpty(x))
                .Cast<string>()
                .Concat(oldMeThoiChanges)
                .Distinct();
            foreach (var mt in affectedMeThois)
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC dbo.SP_HRC1_BOF_CapNhatTrangThaiTrung @BieuMau={0}, @MeThoi={1}", "BOF", mt);
            }

            // -------------------------------------------------------
            // UPSERT Hrc1PhuLieu
            // -------------------------------------------------------
            var meIds = meMap.Values.Select(x => x.ID).Distinct().ToList();
            var existingPL = await _context.Hrc1PhuLieus
                .Where(x => meIds.Contains(x.MeID) && !x.IsDeleted)
                .ToListAsync();

            foreach (var model in models)
            {
                if (!meMap.TryGetValue(model.RowKey, out var me)) continue;

                foreach (var pl in model.PhuLieus)
                {
                    bool isManualCol = pl.PhuLieuID == null && pl.IsAddManual == true && pl.ID_HeaderKey.HasValue;

                    Hrc1PhuLieu? existingPl = isManualCol
                        ? existingPL.FirstOrDefault(x =>
                            x.MeID == me.ID && x.PhuLieuID == null && x.IsAddManual && x.ID_HeaderKey == pl.ID_HeaderKey)
                        : existingPL.FirstOrDefault(x => x.MeID == me.ID && x.PhuLieuID == pl.PhuLieuID);

                    // "Xóa manual về ban đầu": isManual=true nhưng không còn giá trị nào để lưu
                    bool isClearManual = pl.IsManual == true
                        && pl.KLPhuGia_Manual == null
                        && (pl.KLPhuGia == null || pl.KLPhuGia == 0);

                    if (isClearManual)
                    {
                        if (existingPl != null && isManualCol)
                        {
                            _context.Hrc1PhuLieus.Remove(existingPl);
                            existingPL.Remove(existingPl);
                        }
                        else if (existingPl != null)
                        {
                            existingPl.IsManual = true;
                            existingPl.KLPhuGia_Manual = null;
                        }
                        continue;
                    }

                    if (existingPl == null)
                    {
                        // Phụ liệu cố định (PhuLieuID.HasValue) chưa sửa tay: baseline phải đến từ SP_Sync_HRC1_PhuLieu,
                        // không tự sinh mới ở đây để tránh trùng/ghi đè sai — chỉ insert khi có đánh dấu sửa tay/thêm tay.
                        if (pl.PhuLieuID.HasValue && pl.IsManual != true)
                            continue;

                        var newEntity = new Hrc1PhuLieu
                        {
                            MeID = me.ID,
                            PhuLieuID = pl.PhuLieuID,
                            TenPhuLieu = pl.TenPhuLieu,
                            KLPhuGia = (decimal?)pl.KLPhuGia,
                            KLPhuGia_Manual = (decimal?)pl.KLPhuGia_Manual,
                            IsManual = pl.IsManual ?? false,
                            IsAddManual = pl.IsAddManual ?? false,
                            ID_HeaderKey = pl.ID_HeaderKey,
                            IsNM = pl.IsNM ?? me.IsNM,
                            NgayTao = DateTime.Now
                        };
                        await _context.Hrc1PhuLieus.AddAsync(newEntity);
                        existingPL.Add(newEntity);
                    }
                    else
                    {
                        // KLPhuGia (baseline từ NM) KHÔNG BAO GIỜ bị ghi đè trên record đã tồn tại (spec invariant)
                        if (!string.IsNullOrWhiteSpace(pl.TenPhuLieu))
                            existingPl.TenPhuLieu = pl.TenPhuLieu;
                        if (pl.ID_HeaderKey.HasValue)
                            existingPl.ID_HeaderKey = pl.ID_HeaderKey;

                        if (pl.IsManual == true)
                        {
                            existingPl.IsManual = true;
                            existingPl.KLPhuGia_Manual = (decimal?)pl.KLPhuGia_Manual;
                            existingPl.IsAddManual = pl.IsAddManual ?? false;
                        }
                        else
                        {
                            existingPl.IsManual = false;
                            existingPl.KLPhuGia_Manual = null;
                            existingPl.IsAddManual = false;
                        }
                        if (pl.IsNM.HasValue)
                            existingPl.IsNM = pl.IsNM.Value;
                        existingPl.NgayCapNhat = DateTime.Now;
                        _context.Hrc1PhuLieus.Update(existingPl);
                    }
                }
            }

            await _context.SaveChangesAsync();

            // -------------------------------------------------------
            // DELETE manual_col_* bị gỡ khỏi form ở FE
            // -------------------------------------------------------
            var allowed = manualColHeaderKeyIds ?? new HashSet<int>();
            var toDelete = await _context.Hrc1PhuLieus
                .Where(x =>
                    meIds.Contains(x.MeID) &&
                    x.IsAddManual &&
                    x.PhuLieuID == null &&
                    x.ID_HeaderKey.HasValue &&
                    !allowed.Contains(x.ID_HeaderKey.Value))
                .ToListAsync();
            if (toDelete.Count > 0)
            {
                _context.Hrc1PhuLieus.RemoveRange(toDelete);
                await _context.SaveChangesAsync();
            }
        }

        // =========================================================
        // Xóa dòng mẻ (khác nhau theo nguồn IsNM — xem TaoTieuHaoLoThoi.tsx handleDeleteRow)
        // =========================================================

        /// <summary>
        /// Xóa mềm 1 mẻ từ NM (IsNM = true): chỉ đánh dấu IsDeleted, không xóa khỏi DB — dữ liệu vẫn còn để đối chiếu,
        /// chỉ ẩn khỏi kết quả filter (DLNMHRC1Repository.GetAllAsync luôn where IsDeleted == false).
        /// </summary>
        public async Task<bool> DeleteRowNMAsync(int id)
        {
            var existing = await _context.Hrc1TieuHaos.FirstOrDefaultAsync(x => x.ID == id && !x.IsDeleted);
            if (existing == null) return false;

            existing.IsDeleted = true;
            existing.NgayXoa = DateTime.Now;
            _context.Hrc1TieuHaos.Update(existing);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(existing.MeThoi))
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC dbo.SP_HRC1_BOF_CapNhatTrangThaiTrung @BieuMau={0}, @MeThoi={1}",
                    existing.BieuMau ?? "BOF", existing.MeThoi);
            }

            return true;
        }

        /// <summary>
        /// Xóa vĩnh viễn 1 mẻ thêm tay (IsNM = false) — dòng do người dùng tự thêm qua nút "+ Thêm dòng", không có
        /// nguồn NM để đối chiếu nên không cần giữ lại. Chỉ chấp nhận xóa cứng cho dòng IsNM = false; nếu ai đó
        /// gọi nhầm lên 1 dòng IsNM = true thì từ chối để tránh mất dữ liệu NM vĩnh viễn (phải dùng DeleteRowNMAsync).
        /// Xóa kèm toàn bộ phụ liệu liên quan (HRC1_PhuLieu.MeID).
        /// </summary>
        public async Task<bool> DeleteManualRowAsync(int id)
        {
            var existing = await _context.Hrc1TieuHaos.FirstOrDefaultAsync(x => x.ID == id);
            if (existing == null || existing.IsNM) return false;

            var relatedPhuLieus = await _context.Hrc1PhuLieus.Where(x => x.MeID == id).ToListAsync();
            if (relatedPhuLieus.Count > 0)
                _context.Hrc1PhuLieus.RemoveRange(relatedPhuLieus);

            _context.Hrc1TieuHaos.Remove(existing);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(existing.MeThoi))
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC dbo.SP_HRC1_BOF_CapNhatTrangThaiTrung @BieuMau={0}, @MeThoi={1}",
                    existing.BieuMau ?? "BOF", existing.MeThoi);
            }

            return true;
        }

        // =========================================================
        // Thống kê tiêu hao BOF/LF — ThongKeTieuHaoHRC1.tsx
        // =========================================================

        public Task<SearchThongKeHrc1ApiResponse> SearchThongKeAsync(SearchThongKeHrc1 dto)
            => _repo.SearchThongKeApiAsync(dto);

        public Task<List<ThongKeSumItemHrc1>> GetThongKeSumAsync(SearchThongKeHrc1 dto)
            => _repo.GetThongKeSumAsync(dto);

        // =========================================================
        // Chuyển mẻ sang ca khác — dùng bởi nút mũi tên trong CustomTableHRC (xem TaoTieuHaoLoThoi.tsx
        // / TaoTieuHaoTinhLuyenLF.tsx, prop chuyenMeThoiApi). Mirror DLNMHRC2Service.ChuyenMeThoiAsync.
        // =========================================================
        public Task<bool> ChuyenMeThoiAsync(ChuyenMeThoiRequest request)
            => _repo.ChuyenMeThoiAsync(request);

        /// <summary>
        /// Xuất Excel thống kê tiêu hao BOF/LF — dispatch theo dto.BieuMau. BOF dùng mẫu
        /// HRC1_PKH_BOF.xlsx, mirror y hệt layout DLNMHRC2Service.ExportThongKeExcelAsync (nhánh BOF),
        /// chỉ đổi field/khóa phụ liệu cho đúng model HRC1 (PhuLieuID thay IDHeaderKey, KLGang thay
        /// KLGangLongCCT vì HRC1 KLGangLongCCT hiện luôn NULL). LF dùng mẫu HRC1_PKH_LF.xlsx (xem
        /// BuildLfExportFile) — mẫu này CHƯA có file thật trong wwwroot/templates, cần bổ sung trước khi
        /// nút Excel ở tab LF của ThongKeTieuHaoHRC1.tsx dùng được (sẽ báo lỗi "không tìm thấy file mẫu"
        /// cho tới khi đó).
        /// </summary>
        public async Task<ExportFileResult> ExportThongKeExcelAsync(SearchThongKeHrc1 dto)
        {
            dto.Page = 1;
            dto.PageSize = int.MaxValue;
            var bieuMau = string.IsNullOrWhiteSpace(dto.BieuMau) ? "BOF" : dto.BieuMau;

            var result = await SearchThongKeAsync(dto);
            if (result.Data.Count == 0)
                throw new InvalidOperationException("Không có dữ liệu phù hợp với điều kiện lọc để xuất Excel.");

            // Thứ tự cột phụ liệu khi xuất Excel (ThuTu_Excel_BOF/ThuTu_Excel_LF) có thể khác thứ tự
            // hiển thị trên bảng Thống kê (ThuTu_TK_BOF/ThuTu_TK_LF) — sắp lại đúng theo mẫu Excel
            // trước khi build file, xem Models/Hrc1PhuLieuNm.cs.
            if (result.PhuLieuHeaderTables.Count > 0)
            {
                var phuLieuIds = result.PhuLieuHeaderTables.Select(h => h.PhuLieuID).ToList();
                var excelOrderMap = bieuMau == "LF"
                    ? await _context.Hrc1PhuLieuNms
                        .Where(x => phuLieuIds.Contains(x.ID))
                        .ToDictionaryAsync(x => x.ID, x => x.ThuTu_Excel_LF)
                    : await _context.Hrc1PhuLieuNms
                        .Where(x => phuLieuIds.Contains(x.ID))
                        .ToDictionaryAsync(x => x.ID, x => x.ThuTu_Excel_BOF);

                result.PhuLieuHeaderTables = result.PhuLieuHeaderTables
                    .OrderBy(h => excelOrderMap.TryGetValue(h.PhuLieuID, out var order) ? (order ?? int.MaxValue) : int.MaxValue)
                    .ThenBy(h => h.PhuLieuID)
                    .ToList();
            }

            return bieuMau == "LF"
                ? BuildLfExportFile(dto.Scope, result)
                : BuildBofExportFile(dto.Scope, result);
        }

        private static ExportFileResult BuildBofExportFile(int? scope, SearchThongKeHrc1ApiResponse result)
        {
            var headers = result.PhuLieuHeaderTables;

            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "templates", "HRC1_PKH_BOF.xlsx");
            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"Không tìm thấy file mẫu Excel: {templatePath}");

            using var fs = new FileStream(templatePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var workbook = new XLWorkbook(fs);
            var ws = workbook.Worksheet(1);

            var title = $"BIÊN BẢN TIÊU HAO NẤU LUYỆN LÒ THỔI {scope}".Trim();

            ws.Range(4, 1, 4, 27).Merge();
            ws.Cell(4, 1).Value = title;
            ws.Cell(4, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(4, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Cell(4, 1).Style.Font.Bold = true;
            ws.Cell(4, 1).Style.Font.FontSize = 16;

            // ===== HEADER PHỤ LIỆU ĐỘNG + CÁC CỘT CỐ ĐỊNH (mirror layout HRC2 BOF) =====
            const int headerRow = 7;
            const int headerStartCol = 9;

            if (headers.Count > 0)
            {
                int headerEndCol = headerStartCol + headers.Count - 1;
                var phuGiaRange = ws.Range(6, headerStartCol, 6, headerEndCol);
                phuGiaRange.Merge();
                phuGiaRange.Value = "Phụ gia công nghệ (Kg)";
                phuGiaRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                phuGiaRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                phuGiaRange.Style.Alignment.WrapText = true;
                phuGiaRange.Style.Font.Bold = true;
            }

            int headerCol = headerStartCol;
            foreach (var h in headers)
            {
                ws.Cell(headerRow, headerCol).Value = h.TenPhuLieu;
                headerCol++;
            }

            int extraStartCol = headerStartCol + headers.Count;
            int fuelStartCol = extraStartCol;

            var fuelHeader = ws.Range(6, fuelStartCol, 6, fuelStartCol + 1);
            fuelHeader.Merge();
            fuelHeader.Value = "Nhiên liệu (m³)";
            fuelHeader.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            fuelHeader.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            fuelHeader.Style.Alignment.WrapText = true;
            fuelHeader.Style.Font.Bold = true;

            ws.Cell(7, fuelStartCol).Value = "Oxy";
            ws.Cell(7, fuelStartCol + 1).Value = "Nitơ";

            int noteCol = fuelStartCol + 2;
            var noteHeader = ws.Range(6, noteCol, 7, noteCol);
            noteHeader.Merge();
            noteHeader.Value = "Ghi chú";
            noteHeader.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            noteHeader.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            noteHeader.Style.Alignment.WrapText = true;
            noteHeader.Style.Font.Bold = true;

            int scrapCol = fuelStartCol + 3;
            var scrapHeader = ws.Range(6, scrapCol, 7, scrapCol);
            scrapHeader.Merge();
            scrapHeader.Value = "KL thép phế trong thùng gang (tấn)";
            scrapHeader.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            scrapHeader.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            scrapHeader.Style.Alignment.WrapText = true;
            scrapHeader.Style.Font.Bold = true;

            const int startRow = 8;
            int currentRow = startRow;

            foreach (var row in result.Data)
            {
                var d = row.Data;
                if (d == null) continue;

                ws.Cell(currentRow, 1).Value = currentRow - startRow + 1;
                ws.Cell(currentRow, 2).Value = d.NgaySanXuat.HasValue ? d.NgaySanXuat.Value.ToString("dd/MM/yyyy") : "";
                ws.Cell(currentRow, 3).Value = d.Ca == 1 ? "Ca ngày" : d.Ca == 2 ? "Ca đêm" : "";
                ws.Cell(currentRow, 4).Value = "";
                ws.Cell(currentRow, 5).Value = d.MeThoi;
                ws.Cell(currentRow, 6).Value = d.MacThep;
                ws.Cell(currentRow, 7).Value = (double?)d.KLGang;
                // KL thép phế = thép phế NM + thép phế từ gang (KLThepPheGang hiện luôn NULL, chưa xác nhận nguồn)
                ws.Cell(currentRow, 8).Value = (double?)((d.KLThepPhe ?? 0) + (d.KLThepPheGang ?? 0));

                var valueByPhuLieuId = row.Values
                    .Where(v => v.TotalKLPhuGia.HasValue)
                    .ToDictionary(v => v.PhuLieuID, v => v.TotalKLPhuGia!.Value);

                int colIndex = headerStartCol;
                foreach (var h in headers)
                {
                    if (valueByPhuLieuId.TryGetValue(h.PhuLieuID, out var value) && value != 0)
                        ws.Cell(currentRow, colIndex).Value = value;
                    colIndex++;
                }

                if (d.O2.HasValue) ws.Cell(currentRow, fuelStartCol).Value = d.O2.Value;
                if (d.N2.HasValue) ws.Cell(currentRow, fuelStartCol + 1).Value = d.N2.Value;

                var sumThepPhe = (double)((d.KLThepPhe ?? 0) + (d.KLThepPheGang ?? 0));
                if (sumThepPhe != 0)
                    ws.Cell(currentRow, scrapCol).Value = sumThepPhe;

                ws.Row(currentRow).Height = 18;
                currentRow++;
            }

            // ===== DÒNG TỔNG =====
            currentRow += 1;
            int totalRow = currentRow;

            ws.Range(totalRow, 1, totalRow, 6).Merge();
            ws.Cell(totalRow, 1).Value = "Tổng";
            ws.Cell(totalRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(totalRow, 1).Style.Font.Bold = true;

            int lastDataRow = currentRow - 1;
            ws.Range(startRow, 1, lastDataRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(totalRow, 7).FormulaA1 = $"SUM(G{startRow}:G{lastDataRow})";
            ws.Cell(totalRow, 8).FormulaA1 = $"SUM(H{startRow}:H{lastDataRow})";

            int col = headerStartCol;
            foreach (var h in headers)
            {
                var colLetter = ws.Cell(1, col).Address.ColumnLetter;
                ws.Cell(totalRow, col).FormulaA1 = $"SUM({colLetter}{startRow}:{colLetter}{lastDataRow})";
                col++;
            }

            currentRow += 2;
            int lastFooterRow = currentRow;

            var lastUsedColumn = ws.LastColumnUsed();
            int lastColumn = lastUsedColumn != null ? lastUsedColumn.ColumnNumber() : 34;

            var headerBorderRange = ws.Range(6, 1, 7, lastColumn);
            headerBorderRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
            headerBorderRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            headerBorderRange.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            headerBorderRange.Style.Border.RightBorder = XLBorderStyleValues.Thin;
            headerBorderRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            headerBorderRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerBorderRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            headerBorderRange.Style.Alignment.WrapText = true;

            var dataRange = ws.Range(startRow, 1, totalRow, lastColumn);
            dataRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.RightBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            var footerRange = ws.Range(currentRow, 1, lastFooterRow, lastColumn);
            footerRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
            footerRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            footerRange.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            footerRange.Style.Border.RightBorder = XLBorderStyleValues.Thin;
            footerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return new ExportFileResult
            {
                Content = stream.ToArray(),
                FileName = $"ThongKe_HRC1_BOF_{DateTime.Now:yyyyMMdd_HHmm}.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            };
        }

        /// <summary>
        /// Xuất Excel thống kê tiêu hao LF theo mẫu HRC1_PKH_LF.xlsx — mirror layout của
        /// BuildBofExportFile nhưng đổi đúng cột theo dữ liệu thật của LF (xem HRC1_BB_TieuHao_LF.json):
        /// không có Gang lỏng/Thép phế (BOF) mà chỉ có 1 cột "Khối lượng thép lỏng", không có Oxy/Nitơ mà
        /// chỉ có Argon, và không có khái niệm "KL thép phế trong thùng gang". File mẫu HRC1_PKH_LF.xlsx
        /// hiện CHƯA tồn tại trong wwwroot/templates — cần được bổ sung thủ công trước khi dùng được.
        /// </summary>
        private static ExportFileResult BuildLfExportFile(int? scope, SearchThongKeHrc1ApiResponse result)
        {
            var headers = result.PhuLieuHeaderTables;

            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "templates", "HRC1_PKH_LF.xlsx");
            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"Không tìm thấy file mẫu Excel: {templatePath}");

            using var fs = new FileStream(templatePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var workbook = new XLWorkbook(fs);
            var ws = workbook.Worksheet(1);

            var title = $"BIÊN BẢN TIÊU HAO NẤU LUYỆN TINH LUYỆN LF {scope}".Trim();

            ws.Range(4, 1, 4, 27).Merge();
            ws.Cell(4, 1).Value = title;
            ws.Cell(4, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(4, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Cell(4, 1).Style.Font.Bold = true;
            ws.Cell(4, 1).Style.Font.FontSize = 16;

            // ===== HEADER PHỤ LIỆU ĐỘNG + CÁC CỘT CỐ ĐỊNH =====
            // Cột cố định LF chỉ có 7 (không có cột Thép phế riêng như BOF): STT, Ngày SX, Ca, Kíp,
            // Mẻ nấu, Mác thép, Khối lượng thép lỏng — nên headerStartCol lùi 1 so với BOF (8 thay vì 9).
            const int headerRow = 7;
            const int headerStartCol = 8;

            if (headers.Count > 0)
            {
                int headerEndCol = headerStartCol + headers.Count - 1;
                var phuGiaRange = ws.Range(6, headerStartCol, 6, headerEndCol);
                phuGiaRange.Merge();
                phuGiaRange.Value = "Chất hợp kim hóa / Phụ gia khử oxy (Kg)";
                phuGiaRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                phuGiaRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                phuGiaRange.Style.Alignment.WrapText = true;
                phuGiaRange.Style.Font.Bold = true;
            }

            int headerCol = headerStartCol;
            foreach (var h in headers)
            {
                ws.Cell(headerRow, headerCol).Value = h.TenPhuLieu;
                headerCol++;
            }

            int extraStartCol = headerStartCol + headers.Count;
            int fuelCol = extraStartCol;

            var fuelHeader = ws.Range(6, fuelCol, 7, fuelCol);
            fuelHeader.Merge();
            fuelHeader.Value = "Argon (m³)";
            fuelHeader.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            fuelHeader.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            fuelHeader.Style.Alignment.WrapText = true;
            fuelHeader.Style.Font.Bold = true;

            int noteCol = fuelCol + 1;
            var noteHeader = ws.Range(6, noteCol, 7, noteCol);
            noteHeader.Merge();
            noteHeader.Value = "Ghi chú";
            noteHeader.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            noteHeader.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            noteHeader.Style.Alignment.WrapText = true;
            noteHeader.Style.Font.Bold = true;

            const int startRow = 8;
            int currentRow = startRow;

            foreach (var row in result.Data)
            {
                var d = row.Data;
                if (d == null) continue;

                ws.Cell(currentRow, 1).Value = currentRow - startRow + 1;
                ws.Cell(currentRow, 2).Value = d.NgaySanXuat.HasValue ? d.NgaySanXuat.Value.ToString("dd/MM/yyyy") : "";
                ws.Cell(currentRow, 3).Value = d.Ca == 1 ? "Ca ngày" : d.Ca == 2 ? "Ca đêm" : "";
                ws.Cell(currentRow, 4).Value = "";
                ws.Cell(currentRow, 5).Value = d.MeThoi;
                ws.Cell(currentRow, 6).Value = d.MacThep;
                ws.Cell(currentRow, 7).Value = (double?)d.KLThepLong;

                var valueByPhuLieuId = row.Values
                    .Where(v => v.TotalKLPhuGia.HasValue)
                    .ToDictionary(v => v.PhuLieuID, v => v.TotalKLPhuGia!.Value);

                int colIndex = headerStartCol;
                foreach (var h in headers)
                {
                    if (valueByPhuLieuId.TryGetValue(h.PhuLieuID, out var value) && value != 0)
                        ws.Cell(currentRow, colIndex).Value = value;
                    colIndex++;
                }

                if (d.AR.HasValue) ws.Cell(currentRow, fuelCol).Value = d.AR.Value;

                ws.Row(currentRow).Height = 18;
                currentRow++;
            }

            // ===== DÒNG TỔNG =====
            currentRow += 1;
            int totalRow = currentRow;

            ws.Range(totalRow, 1, totalRow, 6).Merge();
            ws.Cell(totalRow, 1).Value = "Tổng";
            ws.Cell(totalRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(totalRow, 1).Style.Font.Bold = true;

            int lastDataRow = currentRow - 1;
            ws.Range(startRow, 1, lastDataRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(totalRow, 7).FormulaA1 = $"SUM(G{startRow}:G{lastDataRow})";

            int col = headerStartCol;
            foreach (var h in headers)
            {
                var colLetter = ws.Cell(1, col).Address.ColumnLetter;
                ws.Cell(totalRow, col).FormulaA1 = $"SUM({colLetter}{startRow}:{colLetter}{lastDataRow})";
                col++;
            }

            currentRow += 2;
            int lastFooterRow = currentRow;

            var lastUsedColumn = ws.LastColumnUsed();
            int lastColumn = lastUsedColumn != null ? lastUsedColumn.ColumnNumber() : 30;

            var headerBorderRange = ws.Range(6, 1, 7, lastColumn);
            headerBorderRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
            headerBorderRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            headerBorderRange.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            headerBorderRange.Style.Border.RightBorder = XLBorderStyleValues.Thin;
            headerBorderRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            headerBorderRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerBorderRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            headerBorderRange.Style.Alignment.WrapText = true;

            var dataRange = ws.Range(startRow, 1, totalRow, lastColumn);
            dataRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.RightBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            var footerRange = ws.Range(currentRow, 1, lastFooterRow, lastColumn);
            footerRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
            footerRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            footerRange.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            footerRange.Style.Border.RightBorder = XLBorderStyleValues.Thin;
            footerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return new ExportFileResult
            {
                Content = stream.ToArray(),
                FileName = $"ThongKe_HRC1_LF_{DateTime.Now:yyyyMMdd_HHmm}.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            };
        }

        // =========================================================
        // ===== LF manual save pipeline (Tiêu hao tinh luyện LF) =====
        // LF không có nguồn NM nào để đồng bộ — mọi mẻ + phụ liệu đều do người dùng nhập tay
        // (IsNM=false vĩnh viễn). Vì vậy KHÔNG tái dùng BuildModelToInsert/SaveHRC1ManualDataAsync
        // của BOF: các method đó có nhiều chỗ giả định luôn có baseline từ SP_Sync_HRC1_PhuLieu
        // (vd guard "chỉ insert phụ liệu cố định khi IsManual=true" — với LF thì mọi phụ liệu đều
        // "IsManual=false" theo đúng nghĩa "đây là giá trị gốc", nên guard đó sẽ chặn insert vĩnh viễn
        // nếu tái dùng nguyên văn). Pipeline dưới đây đơn giản hơn nhiều, không có khái niệm
        // auto-vs-manual, không đọc __orig/__IsManual từ FE.
        // =========================================================

        public async Task SaveHRC1LFManualFromPhieuFormAsync(JsonElement formData)
        {
            var models = await BuildLFModelToInsert(formData);
            await SaveHRC1LFManualDataAsync(models);
        }

        public Task<List<Hrc1InsertModel>> BuildLFModelToInsert(JsonElement formData)
        {
            var result = new List<Hrc1InsertModel>();

            string? bm = formData.TryGetProperty("maBm", out var bmProp) ? bmProp.GetString() : null;
            if (!string.Equals(bm, "HRC1_BB_TieuHao_LF", StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(result);

            int scope = formData.GetProperty("scope").GetInt32();
            int ca = formData.GetProperty("ca").GetInt32();

            string? ngaySXstr = formData.TryGetProperty("NgaySX", out var nsxProp) ? nsxProp.GetString() : null;
            DateOnly ngaySX = !string.IsNullOrEmpty(ngaySXstr)
                ? DateOnly.Parse(ngaySXstr)
                : DateOnly.FromDateTime(DateTime.Now);

            if (!formData.TryGetProperty("table1DynamicColumns", out var dynamicRoot))
                return Task.FromResult(result);

            // "LF_PhuGia": phụ liệu từ danh mục HRC1_PhuLieuNM (hiển thị mặc định); "adjust": cột
            // "Thêm cột điều chỉnh" — sau khi user chọn/tạo phụ liệu, dataIndex đổi thành phuLieu_{id}
            // (Loại A, giống hệt cơ chế BOF ở BuildModelToInsert) nên BuildLFPhuLieus xử lý đồng nhất,
            // không cần phân biệt nguồn gốc cột.
            var lfColGroups = new List<string> { "LF_PhuGia", "adjust" };
            var phuGiaCols = lfColGroups
                .Where(g => dynamicRoot.TryGetProperty(g, out _))
                .SelectMany(g => dynamicRoot.GetProperty(g).EnumerateArray())
                .ToList();

            if (!formData.TryGetProperty("table1", out var table1Prop))
                return Task.FromResult(result);
            var table1 = table1Prop.EnumerateArray().ToList();

            foreach (var row in table1)
            {
                string? meThoi = row.TryGetProperty("meThoi", out var mtp) && mtp.ValueKind == JsonValueKind.String
                    ? mtp.GetString()
                    : null;
                if (string.IsNullOrWhiteSpace(meThoi)) continue;

                int? rowId = row.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.Number
                    ? idProp.GetInt32()
                    : (int?)null;

                var phuLieus = BuildLFPhuLieus(row, phuGiaCols, meThoi);

                var macThep = row.TryGetProperty("macThep", out var mt) && mt.ValueKind == JsonValueKind.String
                    ? mt.GetString()
                    : null;
                var klThepLong = TryGetDecimal(row, "klThepLong");
                var o2 = TryGetDouble(row, "o2");
                var n2 = TryGetDouble(row, "n2");
                var ar = TryGetDouble(row, "ar");
                var queLayMau = TryGetInt(row, "queLayMau");
                var queDoNhiet = TryGetInt(row, "queDoNhiet");
                var ghiChu = row.TryGetProperty("ghiChu", out var gcProp) && gcProp.ValueKind == JsonValueKind.String
                    ? gcProp.GetString()
                    : null;

                result.Add(new Hrc1InsertModel
                {
                    Id = rowId,
                    NgaySanXuat = ngaySX,
                    Ca = (byte)ca,
                    Scope = scope,
                    MeThoi = meThoi!,
                    MacThep = macThep,
                    KLThepLong = klThepLong,
                    O2 = o2,
                    N2 = n2,
                    AR = ar,
                    QueLayMau = queLayMau,
                    QueDoNhiet = queDoNhiet,
                    GhiChu = ghiChu,
                    IsNM = false,
                    RowKey = Guid.NewGuid(),
                    PhuLieus = phuLieus
                });
            }

            return Task.FromResult(result);
        }

        private List<Hrc1_PhuLieuInsertModel> BuildLFPhuLieus(JsonElement row, List<JsonElement> phuGiaCols, string? meThoi)
        {
            var result = new List<Hrc1_PhuLieuInsertModel>();

            foreach (var col in phuGiaCols)
            {
                string dataIndex = col.GetProperty("dataIndex").GetString() ?? "";
                if (!dataIndex.StartsWith("phuLieu_")) continue;
                if (!int.TryParse(dataIndex.Substring("phuLieu_".Length), out var phuLieuId)) continue;
                if (!row.TryGetProperty(dataIndex, out var valProp)) continue;

                // Chỉ loại NULL — giữ nguyên giá trị 0 (hợp lệ, khác nghĩa với "không nhập"), theo đúng
                // quy ước đã áp dụng cho sync BOF (mục 3.6 tài liệu hrc1_tieuhaoBOF.md).
                var value = TryConvertNumeric(valProp);
                if (!value.HasValue) continue;

                string? label = col.TryGetProperty("label", out var lblProp) ? lblProp.GetString() : null;

                result.Add(new Hrc1_PhuLieuInsertModel
                {
                    MeThoi = meThoi,
                    PhuLieuID = phuLieuId,
                    TenPhuLieu = label,
                    KLPhuGia = value,
                    IsManual = false,
                    KLPhuGia_Manual = null,
                });
            }

            return result;
        }

        public async Task SaveHRC1LFManualDataAsync(List<Hrc1InsertModel> models)
        {
            if (models == null || !models.Any()) return;

            var meMap = new Dictionary<Guid, Hrc1TieuHao>();

            var allMeThois = models.Select(m => m.MeThoi).Distinct().ToList();
            var allIds = models.Where(m => m.Id.HasValue && m.Id.Value > 0).Select(m => m.Id!.Value).ToList();

            var existingRows = await _context.Hrc1TieuHaos
                .Where(x => !x.IsDeleted &&
                    (allIds.Contains(x.ID) || (x.MeThoi != null && allMeThois.Contains(x.MeThoi) && x.BieuMau == "LF")))
                .ToListAsync();

            var oldMeThoiChanges = new HashSet<string>();
            foreach (var model in models)
            {
                if (!model.Id.HasValue || model.Id <= 0) continue;
                var ex = existingRows.FirstOrDefault(x => x.ID == model.Id.Value);
                if (ex == null) continue;
                if (!string.IsNullOrEmpty(ex.MeThoi) && !string.Equals(ex.MeThoi, model.MeThoi, StringComparison.OrdinalIgnoreCase))
                    oldMeThoiChanges.Add(ex.MeThoi);
            }

            foreach (var model in models)
            {
                var existing = model.Id.HasValue && model.Id > 0
                    ? existingRows.FirstOrDefault(x => x.ID == model.Id.Value)
                    : null;

                if (existing == null && model.Id.HasValue && model.Id > 0) continue; // tham chiếu cũ không còn tồn tại

                Hrc1TieuHao entity;
                if (existing == null)
                {
                    entity = new Hrc1TieuHao
                    {
                        BieuMau = "LF",
                        Scope = model.Scope,
                        MeThoi = model.MeThoi,
                        MacThep = model.MacThep,
                        KLThepLong = model.KLThepLong,
                        O2 = model.O2,
                        N2 = model.N2,
                        AR = model.AR,
                        QueLayMau = model.QueLayMau,
                        QueDoNhiet = model.QueDoNhiet,
                        GhiChu = model.GhiChu,
                        NgaySanXuat = model.NgaySanXuat,
                        Ca = model.Ca,
                        IsNM = false,
                        IsEdited = false,
                        NgayTao = DateTime.Now
                    };
                    await _context.Hrc1TieuHaos.AddAsync(entity);
                }
                else
                {
                    existing.MeThoi = model.MeThoi;
                    existing.MacThep = model.MacThep;
                    existing.KLThepLong = model.KLThepLong;
                    existing.O2 = model.O2;
                    existing.N2 = model.N2;
                    existing.AR = model.AR;
                    existing.QueLayMau = model.QueLayMau;
                    existing.QueDoNhiet = model.QueDoNhiet;
                    existing.GhiChu = model.GhiChu;
                    existing.NgaySanXuat = model.NgaySanXuat;
                    existing.Ca = model.Ca;
                    existing.NgayCapNhat = DateTime.Now;
                    _context.Hrc1TieuHaos.Update(existing);
                    entity = existing;
                }

                meMap[model.RowKey] = entity;
            }

            await _context.SaveChangesAsync();

            var affectedMeThois = meMap.Values
                .Select(x => x.MeThoi)
                .Where(x => !string.IsNullOrEmpty(x))
                .Cast<string>()
                .Concat(oldMeThoiChanges)
                .Distinct();
            foreach (var mt in affectedMeThois)
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC dbo.SP_HRC1_BOF_CapNhatTrangThaiTrung @BieuMau={0}, @MeThoi={1}", "LF", mt);
            }

            // -------------------------------------------------------
            // UPSERT Hrc1PhuLieu — KHÔNG có guard "baseline phải đến từ sync" như BOF (xem comment
            // đầu vùng này): LF không có baseline, KLPhuGia chính là giá trị người dùng nhập.
            // -------------------------------------------------------
            var meIds = meMap.Values.Select(x => x.ID).Distinct().ToList();
            var existingPL = await _context.Hrc1PhuLieus
                .Where(x => meIds.Contains(x.MeID) && !x.IsDeleted)
                .ToListAsync();

            foreach (var model in models)
            {
                if (!meMap.TryGetValue(model.RowKey, out var me)) continue;

                foreach (var pl in model.PhuLieus)
                {
                    var existingPl = existingPL.FirstOrDefault(x => x.MeID == me.ID && x.PhuLieuID == pl.PhuLieuID);

                    // Ô bị xóa trắng ở FE (không còn số) — dọn luôn record, không giữ dòng rỗng.
                    if (pl.KLPhuGia == null)
                    {
                        if (existingPl != null)
                        {
                            _context.Hrc1PhuLieus.Remove(existingPl);
                            existingPL.Remove(existingPl);
                        }
                        continue;
                    }

                    if (existingPl == null)
                    {
                        var newEntity = new Hrc1PhuLieu
                        {
                            MeID = me.ID,
                            PhuLieuID = pl.PhuLieuID,
                            TenPhuLieu = pl.TenPhuLieu,
                            KLPhuGia = (decimal?)pl.KLPhuGia,
                            IsManual = false,
                            IsAddManual = false,
                            IsNM = false,
                            NgayTao = DateTime.Now
                        };
                        await _context.Hrc1PhuLieus.AddAsync(newEntity);
                        existingPL.Add(newEntity);
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(pl.TenPhuLieu))
                            existingPl.TenPhuLieu = pl.TenPhuLieu;
                        existingPl.KLPhuGia = (decimal?)pl.KLPhuGia;
                        existingPl.IsManual = false;
                        existingPl.KLPhuGia_Manual = null;
                        existingPl.NgayCapNhat = DateTime.Now;
                        _context.Hrc1PhuLieus.Update(existingPl);
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        // =========================================================
        // EXPORT EXCEL/PDF CHI TIẾT PHIẾU (gộp từ Hrc1PhieuDetailExcelService cũ)
        // Xuất Excel/PDF cho 1 phiếu tiêu hao BOF cụ thể (mẫu HRC1_BB_NauLuyen_BOF.xlsx / HRC1_BB_NauLuyen.html) —
        // mirror y hệt PhieuDetailExcelService (HRC2), chỉ giữ nhánh BOF (HRC1 chưa có LF/RH) và đổi field/khóa phụ liệu
        // cho đúng model HRC1: PhuLieuID thay IDHeaderKey, KLGang thay KLGangLongCCT (KLGangLongCCT của HRC1 luôn NULL).
        // Không cần bước "DataJson overrides" như HRC2 — HRC1_PhuLieu.IsManual/KLPhuGia_Manual đã là nguồn sự thật bền
        // (ghi trực tiếp khi lưu phiếu ở SaveHRC1ManualDataAsync), không cần đọc lại DataJson của phiếu.
        // Không có bảng "Tồn silo" (STD_XUAT_NHAP_TON) cho HRC1 → phần footer luôn render rỗng (đúng hành vi mặc định
        // của FooterConfig khi không có footerData/LuongTonLabels, giống hệt cách HRC2 xử lý khi thiếu dữ liệu XNT).
        // =========================================================

        public async Task<BmPhieu> GetBmPhieuByIdOrThrowAsync(Guid idPhieu)
        {
            var phieu = await _context.BmPhieus.FirstOrDefaultAsync(x => x.Idphieu == idPhieu);
            if (phieu == null) throw new Exception($"Không tìm thấy phiếu với IdPhieu='{idPhieu}'.");
            return phieu;
        }

        // -------------------------------------------------------
        // Entry point cho plugin exporter (IPhieuExcelExporter/IPhieuPdfExporter, xem
        // Services/Exporters/HRC1TieuHaoExcelExporter.cs, HRC1TieuHaoPdfExporter.cs) — chỉ cần idPhieu,
        // tự suy ra BOF/LF từ MaBm của phiếu (HRC1_BB_TieuHao_BOF / HRC1_BB_TieuHao_LF) và Ngày/Ca/Lò từ
        // chính phiếu, không cần query string như api/DLNMHRC1/export-*-detail cũ.
        // -------------------------------------------------------
        private static string ResolveBieuMauFromMaBm(string? maBm) =>
            !string.IsNullOrWhiteSpace(maBm) && maBm.EndsWith("_LF", StringComparison.OrdinalIgnoreCase)
                ? "LF"
                : "BOF";

        public async Task<ExportFileResult> ExportTieuHaoExcelAsync(Guid phieuId)
        {
            var phieu = await GetBmPhieuByIdOrThrowAsync(phieuId);
            if (!phieu.NgaySX.HasValue || !phieu.Ca.HasValue)
                throw new InvalidOperationException("Phiếu thiếu Ngày SX/Ca để xuất Excel.");
            var bieuMau = ResolveBieuMauFromMaBm(phieu.MaBm);
            return await ExportExcelDetailAsync(phieu.NgaySX.Value, phieu.Ca.Value, phieu.Scope ?? 0, phieuId, bieuMau);
        }

        public async Task<ExportFileResult> ExportTieuHaoPdfAsync(Guid phieuId)
        {
            var phieu = await GetBmPhieuByIdOrThrowAsync(phieuId);
            if (!phieu.NgaySX.HasValue || !phieu.Ca.HasValue)
                throw new InvalidOperationException("Phiếu thiếu Ngày SX/Ca để xuất PDF.");
            var bieuMau = ResolveBieuMauFromMaBm(phieu.MaBm);
            return await ExportPdfDetailAsync(phieu.NgaySX.Value, phieu.Ca.Value, phieu.Scope ?? 0, phieuId, bieuMau);
        }

        // -------------------------------------------------------
        // Export data query — mirror DLNMHRC1Repository.SearchThongKeApiAsync nhưng lọc đúng
        // 1 tổ hợp Ngày+Ca+Lò (không phân trang, không filter IsDelete/IsTrungMeThoi).
        // -------------------------------------------------------
        private async Task<(List<Hrc1PhuLieuHeaderTable> Headers, List<Hrc1ThongKeRow> Rows)> GetExportDataAsync(
            DateOnly ngay, int ca, int scope, string bieuMau = "BOF")
        {
            var items = await _context.Hrc1TieuHaos
                .Where(x => !x.IsDeleted && x.NgaySanXuat == ngay && x.Ca == (byte)ca
                         && x.BieuMau == bieuMau && x.Scope == scope)
                .OrderBy(x => x.MeThoi)
                .AsNoTracking()
                .ToListAsync();

            // Thứ tự cột phụ liệu: BOF sắp theo ThuTu_Excel_BOF, LF sắp theo ThuTu_Excel_LF — cột ThuTu
            // đơn cũ đã bỏ (xem Hrc1PhuLieuNm.cs).
            bool isLF = bieuMau == "LF";
            var headersQuery = _context.Hrc1PhuLieuNms.Where(x => x.DangSuDung);
            var headers = isLF
                ? await headersQuery.OrderBy(x => x.ThuTu_Excel_LF ?? int.MaxValue).ThenBy(x => x.ID)
                    .Select(x => new Hrc1PhuLieuHeaderTable { PhuLieuID = x.ID, TenPhuLieu = x.TenPhuLieu })
                    .ToListAsync()
                : await headersQuery.OrderBy(x => x.ThuTu_Excel_BOF ?? int.MaxValue).ThenBy(x => x.ID)
                    .Select(x => new Hrc1PhuLieuHeaderTable { PhuLieuID = x.ID, TenPhuLieu = x.TenPhuLieu })
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

        // Trả null nếu phụ liệu này thực sự không có số liệu nào (kể cả trường hợp "xóa manual về ban
        // đầu" — IsManual=true nhưng KLPhuGia_Manual=null) — để Export hiển thị ô trống thay vì "0"
        // gây hiểu nhầm là đã đo được giá trị 0 (xem DLNMHRC1Repository.ComputeEffectiveTotal — bản gốc).
        private static double? ComputeEffectiveTotal(Hrc1PhuLieu p)
        {
            double? effective = p.IsManual ? (double?)p.KLPhuGia_Manual : (double?)p.KLPhuGia;
            if (!effective.HasValue && !p.KLPhanBo.HasValue) return null;
            return (effective ?? 0) + (double)(p.KLPhanBo ?? 0);
        }

        private static Hrc1TieuHao_ResponseModel MapData(Hrc1TieuHao b) => new Hrc1TieuHao_ResponseModel
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

        // ---- EXCEL ----

        public async Task<ExportFileResult> ExportExcelDetailAsync(DateOnly ngay, int ca, int scope, Guid idPhieu, string bieuMau = "BOF")
        {
            var phieu = await GetBmPhieuByIdOrThrowAsync(idPhieu);
            if (!phieu.NgaySX.HasValue || !phieu.Ca.HasValue)
                throw new InvalidOperationException("Phiếu thiếu NgaySX/Ca để export Excel.");

            var ngayPhieu = phieu.NgaySX.Value;
            var caPhieu = phieu.Ca.Value;
            var kipPhieu = phieu.Kip ?? "";
            var scopePhieu = phieu.Scope ?? scope;

            // Dùng chung 1 file letterhead (logo + khung company) cho cả BOF/LF — nội dung ISO code (dòng
            // 1-3) được ghi đè theo bieuMau trong UpdateHeaderRowMerges, phần lưới dữ liệu (từ dòng 4) hoàn
            // toàn generate bằng code nên không cần file mẫu riêng cho LF.
            const string templateFileName = "HRC1_BB_NauLuyen_BOF";
            var templatePath = Path.Combine(_env.WebRootPath, "templates", $"{templateFileName}.xlsx");
            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"Không tìm thấy file mẫu Excel: {templatePath}");

            var (headers, rows) = await GetExportDataAsync(ngayPhieu, caPhieu, scopePhieu, bieuMau);
            var fileName = $"HRC1_BB_NauLuyen_{bieuMau}_Ca{caPhieu}_{ngayPhieu:ddMMyyyy}.xlsx";

            // ClosedXML lỗi khi save workbook có drawing (logo) trực tiếp ra MemoryStream → save qua file tạm.
            var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.xlsx");
            try
            {
                using (var workbook = new XLWorkbook(templatePath))
                {
                    var ws = workbook.Worksheets.First();
                    await RenderBodyFromDbAsync(ws, headers, rows, scopePhieu, ngayPhieu: ngayPhieu, caPhieu: caPhieu, kip: kipPhieu, idPhieu: idPhieu, bieuMau: bieuMau);
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
            int? scope, DateOnly? ngayPhieu, int? caPhieu, string kip, Guid? idPhieu, string bieuMau = "BOF")
        {
            int lastCol = ComputeLastCol(headers.Count, bieuMau);
            ws.Column(2).Width = 25;
            ws.Column(3).Width = 25;
            ws.Column(lastCol).Width = 25;

            ClearRowsFrom(ws, 4);
            string isoText = bieuMau == "LF"
                ? "BM.14/QT.05.15\nNgày hiệu lực: 10/01/2025\nLần sửa đổi: 00"
                : "BM.08/QT.05.15\nNgày hiệu lực: 10/01/2025\nLần sửa đổi: 00";
            UpdateHeaderRowMerges(ws, lastCol, isoText);
            RenderInfoRows(ws, rows, lastCol, scope, ngayPhieu, caPhieu, kip, bieuMau);

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

            RenderColumnHeaders(ws, headers, bieuMau);
            RenderDataRows(ws, headers, rows, dataStartRow, bieuMau);
            RenderTotalRow(ws, totalRow, headers, rows, bieuMau);
            int tableLastRow = RenderFooter(ws, totalRow + 2, lastCol);
            RenderSignatureRow(ws, tableLastRow + 1, lastCol, truongKipName, nguoiLapName);

            ApplyUnifiedTableGridBorders(ws, HeaderParentRow, tableLastRow, lastCol);
        }

        // -------------------------------------------------------
        // Column position helpers.
        // BOF: STT|MeThoi|MacThep|KLGangLong|KLThepPhe (5 cột cố định) rồi phụ liệu, sau phụ liệu:
        // Oxy|Nito|Argon|QueLayMau|QueDoNhiet|Ghi chú (6 cột).
        // LF: STT|MeThoi|MacThep|KLThepLong (4 cột cố định) rồi phụ liệu, sau phụ liệu:
        // Argon|QueLayMau|QueDoNhiet|Ghi chú (4 cột, LF không có Oxy/Nito).
        // -------------------------------------------------------
        private static int FixedColsCount(string bieuMau) => bieuMau == "LF" ? 4 : 5;
        private static int TailColsCount(string bieuMau) => bieuMau == "LF" ? 4 : 6;
        private static int PhuLieuStartCol(string bieuMau) => FixedColsCount(bieuMau) + 1;

        private static int ComputeLastCol(int dynamicCount, string bieuMau) =>
            FixedColsCount(bieuMau) + dynamicCount + TailColsCount(bieuMau);

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

        private static void UpdateHeaderRowMerges(IXLWorksheet ws, int lastCol, string isoText)
        {
            var headerMerges = ws.MergedRanges
                .Where(m => m.RangeAddress.FirstAddress.RowNumber >= 1 && m.RangeAddress.LastAddress.RowNumber <= 3)
                .ToList();
            foreach (var m in headerMerges) m.Unmerge();

            ws.Range(1, 1, 3, lastCol).Merge();
            var cell = ws.Cell(1, 1);
            cell.Value = isoText;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
            cell.Style.Alignment.WrapText = true;
        }

        private static void RenderInfoRows(IXLWorksheet ws, List<Hrc1ThongKeRow> rows, int lastCol,
            int? scope, DateOnly? ngayPhieu, int? caPhieu, string kip, string bieuMau)
        {
            var d = rows.FirstOrDefault()?.Data;
            var caValue = caPhieu ?? d?.Ca ?? 0;
            var rawDate = d?.NgaySanXuat;
            string ngayStr = ngayPhieu.HasValue ? ngayPhieu.Value.ToString("dd/MM/yyyy") : (rawDate?.ToString("dd/MM/yyyy") ?? "");

            string tenBm = bieuMau == "LF"
                ? $"BIÊN BẢN TIÊU HAO NẤU LUYỆN TINH LUYỆN LF {scope}"
                : $"BIÊN BẢN TIÊU HAO NẤU LUYỆN LÒ THỔI {scope}";

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

        private static void RenderColumnHeaders(IXLWorksheet ws, List<Hrc1PhuLieuHeaderTable> headers, string bieuMau)
        {
            bool isLF = bieuMau == "LF";
            int s = PhuLieuStartCol(bieuMau);

            MergeVertCell(ws, 1, "STT");
            MergeVertCell(ws, 2, isLF ? "Mẻ nấu" : "Mẻ thổi");
            MergeVertCell(ws, 3, "Mác thép");
            if (isLF)
            {
                MergeVertCell(ws, 4, "KL thép lỏng\n(tấn)");
            }
            else
            {
                MergeVertCell(ws, 4, "KL gang lỏng\n(tấn)");
                MergeVertCell(ws, 5, "KL thép phế\n(tấn)");
            }

            if (headers.Count > 0)
            {
                MergeHorizCell(ws, HeaderParentRow, s, s + headers.Count - 1,
                    isLF ? "Chất hợp kim hóa / Phụ gia khử oxy (Kg)" : "Phụ gia công nghệ (Kg)");
                for (int i = 0; i < headers.Count; i++)
                    HeaderCell(ws, HeaderChildRow, s + i, headers[i].TenPhuLieu ?? "");
            }

            int a = s + headers.Count;
            if (isLF)
            {
                MergeVertCell(ws, a, "Argon\n(m3)");
                a += 1;
            }
            else
            {
                MergeHorizCell(ws, HeaderParentRow, a, a + 2, "Khí (nhập tay)");
                HeaderCell(ws, HeaderChildRow, a, "Oxy");
                HeaderCell(ws, HeaderChildRow, a + 1, "Nito");
                HeaderCell(ws, HeaderChildRow, a + 2, "Argon");
                a += 3;
            }

            MergeHorizCell(ws, HeaderParentRow, a, a + 1, "Que");
            HeaderCell(ws, HeaderChildRow, a, "Que lấy mẫu");
            HeaderCell(ws, HeaderChildRow, a + 1, "Que đo nhiệt");
            a += 2;

            MergeVertCell(ws, a, "Ghi chú");
        }

        private static void RenderDataRows(IXLWorksheet ws, List<Hrc1PhuLieuHeaderTable> headers, List<Hrc1ThongKeRow> rows, int dataStartRow, string bieuMau)
        {
            bool isLF = bieuMau == "LF";
            int s = PhuLieuStartCol(bieuMau);
            int r = dataStartRow;
            int n = 1;

            foreach (var row in rows)
            {
                var d = row.Data!;
                var vm = row.Values.ToDictionary(v => v.PhuLieuID, v => v.TotalKLPhuGia);

                ws.Cell(r, 1).Value = n++;
                ws.Cell(r, 2).Value = d.MeThoi ?? "";
                ws.Cell(r, 3).Value = d.MacThep ?? "";
                if (isLF)
                {
                    ws.Cell(r, 4).Value = Num((double?)d.KLThepLong);
                }
                else
                {
                    ws.Cell(r, 4).Value = Num((double?)d.KLGang);
                    ws.Cell(r, 5).Value = Num((double?)((d.KLThepPhe ?? 0) + (d.KLThepPheGang ?? 0)));
                }

                for (int i = 0; i < headers.Count; i++)
                    ws.Cell(r, s + i).Value = vm.TryGetValue(headers[i].PhuLieuID, out var kl) ? Num(kl) : Blank.Value;

                int a = s + headers.Count;
                if (isLF)
                {
                    ws.Cell(r, a++).Value = Num(d.AR);
                }
                else
                {
                    ws.Cell(r, a++).Value = Num(d.O2);
                    ws.Cell(r, a++).Value = Num(d.N2);
                    ws.Cell(r, a++).Value = Num(d.AR);
                }
                ws.Cell(r, a++).Value = d.QueLayMau.HasValue ? (XLCellValue)d.QueLayMau.Value : Blank.Value;
                ws.Cell(r, a++).Value = d.QueDoNhiet.HasValue ? (XLCellValue)d.QueDoNhiet.Value : Blank.Value;
                ws.Cell(r, a).Value = d.GhiChu ?? "";

                r++;
            }
        }

        private static void RenderTotalRow(IXLWorksheet ws, int r, List<Hrc1PhuLieuHeaderTable> headers, List<Hrc1ThongKeRow> rows, string bieuMau)
        {
            bool isLF = bieuMau == "LF";
            int s = PhuLieuStartCol(bieuMau);

            ws.Range(r, 1, r, 3).Merge();
            ws.Cell(r, 1).Value = "Tổng cộng";
            ws.Cell(r, 1).Style.Font.Bold = true;
            ws.Cell(r, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            if (isLF)
            {
                ws.Cell(r, 4).Value = (XLCellValue)rows.Sum(x => (double?)x.Data?.KLThepLong ?? 0);
            }
            else
            {
                ws.Cell(r, 4).Value = (XLCellValue)rows.Sum(x => (double?)x.Data?.KLGang ?? 0);
                ws.Cell(r, 5).Value = (XLCellValue)rows.Sum(x => (double?)((x.Data?.KLThepPhe ?? 0) + (x.Data?.KLThepPheGang ?? 0)) ?? 0);
            }

            for (int i = 0; i < headers.Count; i++)
            {
                var plId = headers[i].PhuLieuID;
                ws.Cell(r, s + i).Value = (XLCellValue)rows.Sum(x => x.Values.FirstOrDefault(v => v.PhuLieuID == plId)?.TotalKLPhuGia ?? 0);
            }

            int a = s + headers.Count;
            if (isLF)
            {
                ws.Cell(r, a++).Value = (XLCellValue)rows.Sum(x => x.Data?.AR ?? 0);
            }
            else
            {
                ws.Cell(r, a++).Value = (XLCellValue)rows.Sum(x => x.Data?.O2 ?? 0);
                ws.Cell(r, a++).Value = (XLCellValue)rows.Sum(x => x.Data?.N2 ?? 0);
                ws.Cell(r, a++).Value = (XLCellValue)rows.Sum(x => x.Data?.AR ?? 0);
            }
            ws.Cell(r, a++).Value = (XLCellValue)rows.Sum(x => x.Data?.QueLayMau ?? 0);
            ws.Cell(r, a).Value = (XLCellValue)rows.Sum(x => x.Data?.QueDoNhiet ?? 0);
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

        // ---- PDF ----

        public async Task<ExportFileResult> ExportPdfDetailAsync(DateOnly ngay, int ca, int scope, Guid idPhieu, string bieuMau = "BOF")
        {
            var (headers, rows) = await GetExportDataAsync(ngay, ca, scope, bieuMau);

            var pheDuyets = await _pheDuyetService.GetPheDuyetPhieuAsync(idPhieu);
            var truongKip = pheDuyets.FirstOrDefault(x => x.CapDuyet == 1);
            var nguoiLap = pheDuyets.FirstOrDefault(x => x.CapDuyet == 0);
            string chuKyTruongKipHtml = _pheDuyetService.FormatChuKy(truongKip?.ChuKy);
            string chuKyNguoiLapHtml = _pheDuyetService.FormatChuKy(nguoiLap?.ChuKy);
            string? truongKipName = truongKip?.HoVaTen;
            string? nguoiLapName = nguoiLap?.HoVaTen;

            var html = await BuildPdfHtmlAsync(ngay, ca, scope, headers, rows, chuKyTruongKipHtml, chuKyNguoiLapHtml, truongKipName, nguoiLapName, bieuMau);

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
                FileName = $"HRC1_BB_NauLuyen_{bieuMau}_Ca{ca}_{ngay:ddMMyyyy}.pdf",
                ContentType = "application/pdf",
            };
        }

        private async Task<string> BuildPdfHtmlAsync(
            DateOnly ngay, int ca, int scope,
            List<Hrc1PhuLieuHeaderTable> headers, List<Hrc1ThongKeRow> rows,
            string chuKyTruongKipHtml, string chuKyNguoiLapHtml,
            string? truongKipName, string? nguoiLapName, string bieuMau)
        {
            bool isLF = bieuMau == "LF";
            var logoUrl = $"data:image/png;base64,{Convert.ToBase64String(await File.ReadAllBytesAsync(Path.Combine(_env.WebRootPath, "imgs", "LogoPDF.png")))}";

            string tenBm = isLF
                ? $"BIÊN BẢN TIÊU HAO NẤU LUYỆN TINH LUYỆN LF {scope}"
                : $"BIÊN BẢN TIÊU HAO NẤU LUYỆN LÒ THỔI {scope}";

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

            // Mã ISO riêng theo biểu mẫu — mirror isoInfo.code trong BM_config/HRC1_BB_TieuHao_BOF.json
            // (BM.08/QT.05.15) và HRC1_BB_TieuHao_LF.json (BM.14/QT.05.15).
            string bmCode = isLF
                ? "BM.14/QT.05.15 <br /> Ngày hiệu lực: 10/01/2025 <br /> Lần sửa đổi: 00"
                : "BM.08/QT.05.15 <br /> Ngày hiệu lực: 10/01/2025 <br /> Lần sửa đổi: 00";

            string thead = PdfThead(headers, bieuMau);
            string tbody = PdfTbody(headers, rows, bieuMau);
            int lastCol = ComputeLastCol(headers.Count, bieuMau);
            // string footer = PdfFooterHtml(lastCol, chuKyTruongKipHtml, chuKyNguoiLapHtml, truongKipName, nguoiLapName);

            var templatePath = Path.Combine(_env.WebRootPath, "template_html", "HRC1_BB_NauLuyen.html");
            var html = await File.ReadAllTextAsync(templatePath);

            return html
                .Replace("{{LogoUrl}}", logoUrl)
                .Replace("{{BmCode}}", bmCode)
                .Replace("{{TenBieuMau}}", tenBm)
                .Replace("{{InfoKip}}", infoKip)
                .Replace("{{TheadRows}}", thead)
                .Replace("{{TbodyRows}}", tbody);
                // .Replace("{{FooterHtml}}", footer);
        }

        private static string PdfThead(List<Hrc1PhuLieuHeaderTable> h, string bieuMau)
        {
            bool isLF = bieuMau == "LF";
            var r1 = new StringBuilder();
            var r2 = new StringBuilder();

            r1.Append("<th rowspan=\"2\">STT</th>");
            r1.Append($"<th rowspan=\"2\">{(isLF ? "Mẻ nấu" : "Mẻ thổi")}</th>");
            r1.Append("<th rowspan=\"2\">Mác thép</th>");
            if (isLF)
            {
                r1.Append("<th rowspan=\"2\">KL thép lỏng<br/>(tấn)</th>");
            }
            else
            {
                r1.Append("<th rowspan=\"2\">KL gang lỏng<br/>(tấn)</th>");
                r1.Append("<th rowspan=\"2\">KL thép phế<br/>(tấn)</th>");
            }

            if (h.Count > 0)
            {
                r1.Append($"<th colspan=\"{h.Count}\">{(isLF ? "Chất hợp kim hóa / Phụ gia khử oxy (Kg)" : "Phụ gia công nghệ (Kg)")}</th>");
                foreach (var x in h) r2.Append($"<th>{x.TenPhuLieu}</th>");
            }

            if (isLF)
            {
                r1.Append("<th rowspan=\"2\">Argon<br/>(m3)</th>");
            }
            else
            {
                r1.Append("<th colspan=\"3\">Khí (nhập tay)</th>");
                r2.Append("<th>Oxy</th><th>Nito</th><th>Argon</th>");
            }

            r1.Append("<th colspan=\"2\">Que</th>");
            r2.Append("<th>Que lấy mẫu</th><th>Que đo nhiệt</th>");

            r1.Append("<th rowspan=\"2\">Ghi chú</th>");

            return $"<thead><tr>{r1}</tr><tr>{r2}</tr></thead>";
        }

        private static string PdfTbody(List<Hrc1PhuLieuHeaderTable> headers, List<Hrc1ThongKeRow> rows, string bieuMau)
        {
            bool isLF = bieuMau == "LF";
            var sb = new StringBuilder("<tbody>");
            int stt = 1;
            foreach (var row in rows)
            {
                var d = row.Data!;
                var vm = row.Values.ToDictionary(v => v.PhuLieuID, v => v.TotalKLPhuGia);
                sb.Append("<tr>");
                sb.Append($"<td>{stt++}</td><td>{d.MeThoi ?? ""}</td><td>{d.MacThep ?? ""}</td>");
                if (isLF)
                {
                    sb.Append($"<td>{PFmt((double?)d.KLThepLong)}</td>");
                }
                else
                {
                    sb.Append($"<td>{PFmt((double?)d.KLGang)}</td><td>{PFmt((double?)((d.KLThepPhe ?? 0) + (d.KLThepPheGang ?? 0)))}</td>");
                }
                foreach (var hx in headers)
                    sb.Append($"<td>{PFmt(vm.TryGetValue(hx.PhuLieuID, out var kl) ? kl : null)}</td>");
                if (isLF)
                {
                    sb.Append($"<td>{PFmt(d.AR)}</td>");
                }
                else
                {
                    sb.Append($"<td>{PFmt(d.O2)}</td><td>{PFmt(d.N2)}</td><td>{PFmt(d.AR)}</td>");
                }
                sb.Append($"<td>{(d.QueLayMau.HasValue ? d.QueLayMau.Value.ToString() : "")}</td>");
                sb.Append($"<td>{(d.QueDoNhiet.HasValue ? d.QueDoNhiet.Value.ToString() : "")}</td>");
                sb.Append($"<td class=\"td-left\">{d.GhiChu ?? ""}</td>");
                sb.Append("</tr>");
            }
            sb.Append("<tr class=\"total-row\"><td colspan=\"3\">Tổng cộng</td>");
            if (isLF)
            {
                sb.Append($"<td>{PFmt(rows.Sum(x => (double?)x.Data?.KLThepLong ?? 0))}</td>");
            }
            else
            {
                sb.Append($"<td>{PFmt(rows.Sum(x => (double?)x.Data?.KLGang ?? 0))}</td>");
                sb.Append($"<td>{PFmt(rows.Sum(x => (double?)((x.Data?.KLThepPhe ?? 0) + (x.Data?.KLThepPheGang ?? 0)) ?? 0))}</td>");
            }
            foreach (var hx in headers)
            {
                var plId = hx.PhuLieuID;
                sb.Append($"<td>{PFmt(rows.Sum(x => x.Values.FirstOrDefault(v => v.PhuLieuID == plId)?.TotalKLPhuGia ?? 0))}</td>");
            }
            if (isLF)
            {
                sb.Append($"<td>{PFmt(rows.Sum(x => x.Data?.AR ?? 0))}</td>");
            }
            else
            {
                sb.Append($"<td>{PFmt(rows.Sum(x => x.Data?.O2 ?? 0))}</td>");
                sb.Append($"<td>{PFmt(rows.Sum(x => x.Data?.N2 ?? 0))}</td>");
                sb.Append($"<td>{PFmt(rows.Sum(x => x.Data?.AR ?? 0))}</td>");
            }
            sb.Append($"<td>{rows.Sum(x => x.Data?.QueLayMau ?? 0)}</td>");
            sb.Append($"<td>{rows.Sum(x => x.Data?.QueDoNhiet ?? 0)}</td>");
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

        public async Task<RefreshGangMetricsResult> RefreshGangMetricsByPhieuIdsAsync(List<Guid> phieuIds)
        {
            // Chỉ lấy phiếu BOF/LF, lấy distinct slot theo từng loại biểu mẫu — không quan tâm
            // TinhTrang của phiếu (làm mới bất kể trạng thái).
            var slots = await _context.BmPhieus
                .Where(p => phieuIds.Contains(p.Idphieu) &&
                            (p.MaBm == "HRC1_BB_TieuHao_BOF" || p.MaBm == "HRC1_BB_TieuHao_LF"))
                .Select(p => new { p.MaBm, p.NgaySX, p.Ca, p.Scope })
                .Distinct()
                .ToListAsync();

            int skippedPhieu = phieuIds.Count - slots.Count;

            if (slots.Count == 0)
                return new RefreshGangMetricsResult
                {
                    UpdatedRows = 0,
                    SkippedPhieu = skippedPhieu,
                    Message = "Không có phiếu HRC1_BB_TieuHao_BOF/LF nào trong danh sách."
                };

            // BOF: KLGangLongCCT + KLThepPheGang từ DB GangLong (theo MeThoi).
            var bofRowsById = new Dictionary<long, Hrc1TieuHao>();
            foreach (var slot in slots.Where(s => s.MaBm == "HRC1_BB_TieuHao_BOF"))
            {
                if (slot.NgaySX == null || slot.Ca == null || slot.Scope == null) continue;
                var slotRows = await _context.Hrc1TieuHaos
                    .Where(x =>
                        x.IsNM == true &&
                        x.IsDeleted != true &&
                        x.NgaySanXuat == slot.NgaySX &&
                        x.Ca == slot.Ca.Value &&
                        x.Scope == slot.Scope.Value &&
                        x.BieuMau == "BOF" &&
                        x.MeThoi != null)
                    .ToListAsync();
                foreach (var r in slotRows)
                    bofRowsById[r.ID] = r;
            }

            // LF: KLThepLong từ HRC1_MeThep (giao nhận thép lỏng).
            var lfRowsById = new Dictionary<long, Hrc1TieuHao>();
            foreach (var slot in slots.Where(s => s.MaBm == "HRC1_BB_TieuHao_LF"))
            {
                if (slot.NgaySX == null || slot.Ca == null || slot.Scope == null) continue;
                var slotRows = await _context.Hrc1TieuHaos
                    .Where(x =>
                        x.IsDeleted != true &&
                        x.NgaySanXuat == slot.NgaySX &&
                        x.Ca == slot.Ca.Value &&
                        x.Scope == slot.Scope.Value &&
                        x.BieuMau == "LF" &&
                        x.MeThoi != null)
                    .ToListAsync();
                foreach (var r in slotRows)
                    lfRowsById[r.ID] = r;
            }

            if (bofRowsById.Count == 0 && lfRowsById.Count == 0)
                return new RefreshGangMetricsResult
                {
                    UpdatedRows = 0,
                    SkippedPhieu = skippedPhieu,
                    Message = "Không tìm thấy dữ liệu mẻ nào."
                };

            int updated = 0;
            if (bofRowsById.Count > 0)
                updated += await _syncService.RefreshGangMetricsForRowsAsync(bofRowsById.Values.ToList());
            if (lfRowsById.Count > 0)
                updated += await _syncService.RefreshThepLongForRowsAsync(lfRowsById.Values.ToList());

            return new RefreshGangMetricsResult
            {
                UpdatedRows = updated,
                SkippedPhieu = skippedPhieu,
                Message = $"Đã làm mới {updated} mẻ từ {slots.Count} slot ({phieuIds.Count} phiếu)."
            };
        }
    }
}

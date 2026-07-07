using dataproduct.api.DTOs;
using dataproduct.api.DTOs.Export;
using dataproduct.api.Models;
using dataproduct.api.Repositories;
using dataproduct.api.ResponseModels;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;

namespace dataproduct.api.Services
{
    public class DLNMHRC1Service
    {
        private readonly IDLNMHRC1Repository _repo;
        private readonly HRC1_NMSyncService _syncService;
        private readonly ProductFormContext _context;

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

        public DLNMHRC1Service(IDLNMHRC1Repository repo, HRC1_NMSyncService syncService, ProductFormContext context)
        {
            _repo = repo;
            _syncService = syncService;
            _context = context;
        }

        public async Task<List<Hrc1GroupedByMeThoiModel>> FilterGroupedAsync(SyncFromNM_HRC1_Request request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (ShouldSync(request))
                await _syncService.SyncHRC1FromNMAsync(request);

            var ngay = DateOnly.FromDateTime(request.NgaySX);
            var allData = await _repo.GetAllAsync(ngay, request.Ca, request.Scope, request.BieuMau ?? "BOF");
            return await _repo.GetAllGroupedBatchAsync(allData);
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

            var meMap = new Dictionary<Guid, Hrc1TieuHaoBof>();

            var allMeThois = models.Select(m => m.MeThoi).Distinct().ToList();
            var allIds = models.Where(m => m.Id.HasValue && m.Id.Value > 0).Select(m => m.Id!.Value).ToList();

            var existingRows = await _context.Hrc1TieuHaoBofs
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
                    _context.Hrc1TieuHaoBofs.Update(existing);
                    meMap[model.RowKey] = existing;
                    continue;
                }

                if (existing == null && model.Id.HasValue && model.Id > 0) continue; // tham chiếu cũ không còn tồn tại

                Hrc1TieuHaoBof entity;
                if (existing == null)
                {
                    entity = new Hrc1TieuHaoBof
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
                    await _context.Hrc1TieuHaoBofs.AddAsync(entity);
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
                    _context.Hrc1TieuHaoBofs.Update(existing);
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
            var existing = await _context.Hrc1TieuHaoBofs.FirstOrDefaultAsync(x => x.ID == id && !x.IsDeleted);
            if (existing == null) return false;

            existing.IsDeleted = true;
            existing.NgayXoa = DateTime.Now;
            _context.Hrc1TieuHaoBofs.Update(existing);
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
            var existing = await _context.Hrc1TieuHaoBofs.FirstOrDefaultAsync(x => x.ID == id);
            if (existing == null || existing.IsNM) return false;

            var relatedPhuLieus = await _context.Hrc1PhuLieus.Where(x => x.MeID == id).ToListAsync();
            if (relatedPhuLieus.Count > 0)
                _context.Hrc1PhuLieus.RemoveRange(relatedPhuLieus);

            _context.Hrc1TieuHaoBofs.Remove(existing);
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
        // Thống kê tiêu hao BOF — ThongKeTieuHaoBOF.tsx
        // =========================================================

        public Task<SearchThongKeHrc1ApiResponse> SearchThongKeAsync(SearchThongKeHrc1 dto)
            => _repo.SearchThongKeApiAsync(dto);

        public Task<List<ThongKeSumItemHrc1>> GetThongKeSumAsync(SearchThongKeHrc1 dto)
            => _repo.GetThongKeSumAsync(dto);

        /// <summary>
        /// Xuất Excel thống kê tiêu hao BOF theo mẫu HRC1_PKH_BOF.xlsx — mirror y hệt layout
        /// DLNMHRC2Service.ExportThongKeExcelAsync (nhánh BOF), chỉ đổi field/khóa phụ liệu cho đúng model HRC1
        /// (PhuLieuID thay IDHeaderKey, KLGang thay KLGangLongCCT vì HRC1 KLGangLongCCT hiện luôn NULL).
        /// </summary>
        public async Task<ExportFileResult> ExportThongKeExcelAsync(SearchThongKeHrc1 dto)
        {
            dto.Page = 1;
            dto.PageSize = int.MaxValue;

            var result = await SearchThongKeAsync(dto);
            if (result.Data.Count == 0)
                throw new InvalidOperationException("Không có dữ liệu phù hợp với điều kiện lọc để xuất Excel.");

            var headers = result.PhuLieuHeaderTables;

            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "templates", "HRC1_PKH_BOF.xlsx");
            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"Không tìm thấy file mẫu Excel: {templatePath}");

            using var fs = new FileStream(templatePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var workbook = new XLWorkbook(fs);
            var ws = workbook.Worksheet(1);

            var scope = dto.Scope;
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
    }
}

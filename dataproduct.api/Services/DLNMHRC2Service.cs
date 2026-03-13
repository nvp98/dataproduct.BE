using dataproduct.api.DTOs;
using dataproduct.api.DTOs.Export;
using dataproduct.api.Models;
using dataproduct.api.Repositories;
using dataproduct.api.ResponseModels;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace dataproduct.api.Services
{
    public class DLNMHRC2Service
    {
        private readonly IDLNMHRC2Repository _repo;
        private readonly HRC2_NMSyncService _hrc2NMSyncService;
        private readonly ISTD_NXT_HRC2Repository _stdNxtRepo;
        private readonly ProductFormContext _context;

        public DLNMHRC2Service(
            IDLNMHRC2Repository repo,
            HRC2_NMSyncService hrc2NMSyncService,
            ISTD_NXT_HRC2Repository stdNxtRepo,
            ProductFormContext context)
        {
            _repo = repo;
            _hrc2NMSyncService = hrc2NMSyncService;
            _stdNxtRepo = stdNxtRepo;
            _context = context;
        }

        // =========================================================
        // HRC2: Lưu phụ liệu manual + manual_col từ formData phiếu
        // =========================================================
        public async Task SaveHRC2ManualFromPhieuFormAsync(JsonElement formData)
        {
            var (models, manualColHeaderKeyIds) = await BuildModelToInsert(formData);
            await SaveHRC2ManualDataAsync(models, manualColHeaderKeyIds);
        }

        private double? TryGetDouble(JsonElement row, string key)
        {
            if (row.TryGetProperty(key, out var p))
                return TryConvertNumeric(p);
            return null;
        }

        private double? TryConvertNumeric(JsonElement val)
        {
            if (val.ValueKind == JsonValueKind.Number)
                return val.GetDouble();

            if (val.ValueKind == JsonValueKind.String &&
                double.TryParse(val.GetString(), out var d))
                return d;

            return null;
        }

        public async Task<(List<HRC2InsertModel> Models, HashSet<int> ManualColHeaderKeyIds)> BuildModelToInsert(JsonElement formData)
        {
            var result = new List<HRC2InsertModel>();

            string bm = formData.GetProperty("maBm").GetString();
            int scope = formData.GetProperty("scope").GetInt32();
            int ca = formData.GetProperty("ca").GetInt32();

            string ngaySXstr = formData.TryGetProperty("NgaySX", out var nsxProp)
                ? nsxProp.GetString() : null;
            DateTime ngaySX = !string.IsNullOrEmpty(ngaySXstr)
                ? DateTime.Parse(ngaySXstr) : DateTime.Now;

            var bmColumnMap = new Dictionary<string, List<string>>
            {
                { "HRC2_BB_NauLuyen_BOF", new List<string> { "BOF_PhuGia", "others", "adjust" } },
                { "HRC2_BB_NauLuyen_LF",  new List<string> { "PG", "KL", "others", "adjust" } },
                { "HRC2_BB_NauLuyen_RH",  new List<string> { "PG", "KL", "others", "adjust" } }
            };

            if (!bmColumnMap.TryGetValue(bm, out var colGroups))
                return (result, new HashSet<int>());

            string loaiBM = bm switch
            {
                "HRC2_BB_NauLuyen_BOF" => "BOF",
                "HRC2_BB_NauLuyen_LF" => "LF",
                _ => "RH"
            };

            var dynamicRoot = formData.GetProperty("table1DynamicColumns");
            var dynamicCols = colGroups
                .Where(g => dynamicRoot.TryGetProperty(g, out _))
                .SelectMany(g => dynamicRoot.GetProperty(g).EnumerateArray())
                .ToList();

            // Preload tất cả label cần thiết 1 lần — tránh query trong vòng lặp
            var manualColIds = dynamicCols
                .Select(col => col.GetProperty("dataIndex").GetString())
                .Where(di => di.StartsWith("manual_col_"))
                .Select(di => int.TryParse(di.Substring("manual_col_".Length), out var id) ? id : (int?)null)
                .Where(id => id.HasValue)
                .Select(id => id.Value)
                .Distinct()
                .ToList();
            var manualColHeaderKeyIds = new HashSet<int>(manualColIds);

            var headerKeyLabelMap = manualColIds.Any()
                ? await _context.Header_Keys
                    .Where(k => manualColIds.Contains(k.Id))
                    .ToDictionaryAsync(k => k.Id, k => k.TenHienThi)
                : new Dictionary<int, string>();

            var table1 = formData.GetProperty("table1").EnumerateArray().ToList();

            foreach (var row in table1)
            {
                bool isNMRow = !row.TryGetProperty("IsNM", out var flag)
                               || flag.ValueKind != JsonValueKind.False;

                if (!row.TryGetProperty("id", out var idProp) && isNMRow) continue;
                int? rowId = idProp.ValueKind == JsonValueKind.Number ? idProp.GetInt32() : null;
                if (isNMRow && (rowId == null || rowId <= 0)) continue;

                string meThoi = row.TryGetProperty("meThoi", out var mtp) ? mtp.GetString() : null;
                if (isNMRow && string.IsNullOrEmpty(meThoi)) continue;

                // Xử lý dynamic columns — dùng chung cho cả 2 loại row
                var phuLieus = BuildPhuLieus(
                    row, dynamicCols, meThoi, loaiBM,
                    isNMRow, headerKeyLabelMap
                );

                if (isNMRow)
                {
                    if (!phuLieus.Any()) continue;

                    result.Add(new HRC2InsertModel
                    {
                        Id = rowId,
                        Ngay = ngaySX,
                        Ca = ca,
                        BieuMau = loaiBM,
                        Scope = scope,
                        MeThoi = meThoi,
                        MacThep = row.TryGetProperty("macThep", out var mct) ? mct.GetString() : null,
                        IsNM = true,
                        IsChuyenCa = false,
                        RowKey = Guid.NewGuid(),
                        hRC2_PhuLieus = phuLieus
                    });
                }
                else
                {
                    var model = new HRC2InsertModel
                    {
                        Id = row.TryGetProperty("id", out var idRow) ? idRow.GetInt32() : null,
                        Ngay = ngaySX,
                        Ca = ca,
                        BieuMau = loaiBM,
                        Scope = scope,
                        MeThoi = meThoi,
                        MacThep = row.TryGetProperty("macThep", out var mt) ? mt.GetString() : null,
                        O2 = TryGetDouble(row, "o2"),
                        N2 = TryGetDouble(row, "n2"),
                        AR_RH = TryGetDouble(row, "ar_RH"),
                        AR_LF = TryGetDouble(row, "ar_LF"),
                        AR_BOF = TryGetDouble(row, "ar_BOF"),
                        KLGangLong = TryGetDouble(row, "klGangLong"),
                        KLThepPhe = TryGetDouble(row, "klThepPhe"),
                        KlThepLong = TryGetDouble(row, "klThepLong"),
                        IsNM = false,
                        IsChuyenCa = false,
                        hRC2_PhuLieus = phuLieus
                    };
                    result.Add(model);
                }
            }

            return (result, manualColHeaderKeyIds);
        }

        private List<HRC2_PhuLieuInSertModel> BuildPhuLieus(
            JsonElement row,
            List<JsonElement> dynamicCols,
            string meThoi,
            string loaiBM,
            bool isNMRow,
            Dictionary<int, string> headerKeyLabelMap)
        {
            var result = new List<HRC2_PhuLieuInSertModel>();

            foreach (var col in dynamicCols)
            {
                string dataIndex = col.GetProperty("dataIndex").GetString();
                if (!row.TryGetProperty(dataIndex, out var valProp)) continue;

                bool isAdjustColumn = dataIndex.StartsWith("adjust_") && dataIndex.EndsWith("_adjust");
                bool isManualAddedAdjust = dataIndex.StartsWith("manual_col_");

                if (isAdjustColumn && !isManualAddedAdjust) continue;

                var currentNumeric = TryConvertNumeric(valProp);

                string label = col.TryGetProperty("label", out var lblProp) ? lblProp.GetString() : null;

                int? headerKeyId = col.TryGetProperty("headerKeyId", out var hkProp)
                                   && hkProp.ValueKind == JsonValueKind.Number
                    ? hkProp.GetInt32() : null;

                if (!headerKeyId.HasValue && isManualAddedAdjust)
                {
                    var suffix = dataIndex.Substring("manual_col_".Length);
                    if (int.TryParse(suffix, out var parsedId))
                        headerKeyId = parsedId;
                }

                // manual_col_*
                if (isManualAddedAdjust)
                {
                    if (!currentNumeric.HasValue) continue;
                    // isNMRow: cho phép 0; isNMRow=false: bỏ qua nếu = 0
                    if (!isNMRow && currentNumeric.Value == 0) continue;

                    if (headerKeyId.HasValue && string.IsNullOrEmpty(label))
                        headerKeyLabelMap.TryGetValue(headerKeyId.Value, out label);

                    result.Add(new HRC2_PhuLieuInSertModel
                    {
                        MeThoi = meThoi,
                        BieuMau = loaiBM,
                        ID_HeaderKey = headerKeyId,
                        TenHienThi = label,
                        IsPhanBo = true,
                        IsManual = true,
                        KLPhuGia_Manual = currentNumeric
                    });
                    continue;
                }

                // Cột thường: so sánh __orig
                var origKey = $"{dataIndex}__orig";
                bool hasOrig = row.TryGetProperty(origKey, out var origProp);
                var origNumeric = hasOrig ? TryConvertNumeric(origProp) : null;

                bool isManual = hasOrig
                                && origNumeric.HasValue
                                && currentNumeric.HasValue
                                && Math.Abs(currentNumeric.Value - origNumeric.Value) > 0.000001;

                // isNMRow: chỉ lấy dòng đã sửa tay
                if (isNMRow && !isManual) continue;

                double? klPhuGia = isManual ? origNumeric : currentNumeric;
                double? klPhuGia_Manual = isManual ? currentNumeric : null;

                if ((klPhuGia == null || klPhuGia == 0)
                    && (klPhuGia_Manual == null || klPhuGia_Manual == 0)) continue;

                int? idPhuLieu = null;
                string tenPhuLieu = null;
                if (col.TryGetProperty("mappingPayload", out var mp) && mp.ValueKind != JsonValueKind.Null)
                {
                    if (mp.TryGetProperty("idPhuLieu", out var idp) && idp.ValueKind == JsonValueKind.Number)
                        idPhuLieu = idp.GetInt32();
                    tenPhuLieu = mp.TryGetProperty("tenPhuLieu", out var tnp) ? tnp.GetString() : null;
                }

                result.Add(new HRC2_PhuLieuInSertModel
                {
                    MeThoi = meThoi,
                    BieuMau = loaiBM,
                    ID_PhuLieu = idPhuLieu,
                    TenPhuLieu = tenPhuLieu,
                    KLPhuGia = klPhuGia,
                    ID_HeaderKey = headerKeyId,
                    TenHienThi = label,
                    IsPhanBo = false,
                    IsManual = isManual,
                    KLPhuGia_Manual = klPhuGia_Manual
                });
            }

            return result;
        }

        public async Task SaveHRC2ManualDataAsync(List<HRC2InsertModel> models, HashSet<int> manualColHeaderKeyIds)
        {
            if (models == null || !models.Any()) return;

            var dlnmMap = new Dictionary<Guid, DLNM_HRC2>();

            // Preload tất cả mẻ thoi liên quan 1 lần
            var allMeThois = models.Select(m => m.MeThoi).Distinct().ToList();
            var allBieuMaus = models.Select(m => m.BieuMau).Distinct().ToList();
            var allIds = models.Where(m => m.Id > 0).Select(m => m.Id.Value).ToList();

            var existingDLNMs = await _context.DLNM_HRC2s
                .Where(x => allIds.Contains(x.ID)
                         || (allMeThois.Contains(x.MeThoi) && allBieuMaus.Contains(x.BieuMau)))
                .ToListAsync();

            // -------------------------------------------------------
            // UPSERT DLNM_HRC2
            // -------------------------------------------------------
            foreach (var model in models)
            {
                var existing = model.Id > 0
                    ? existingDLNMs.FirstOrDefault(x => x.ID == model.Id)
                    : null;

                // IsNM=true: chỉ map, không sửa
                if (existing?.IsNM == true)
                {
                    dlnmMap[model.RowKey] = existing;
                    continue;
                }

                if (existing == null && model.Id > 0) continue;

                // Check trùng mẻ thoi — dùng preloaded data
                var sameMeThoi = existingDLNMs
                    .Where(x => x.MeThoi == model.MeThoi && x.BieuMau == model.BieuMau)
                    .ToList();

                bool isTrung = existing == null
                    ? sameMeThoi.Any()
                    : sameMeThoi.Any(x => x.ID != existing.ID);

                // Chỉ update flag nếu thay đổi — tránh update thừa
                foreach (var item in sameMeThoi.Where(x => x.IsTrungMeThoi != isTrung))
                {
                    item.IsTrungMeThoi = isTrung;
                    _context.DLNM_HRC2s.Update(item);
                }

                DLNM_HRC2 dlnm;
                if (existing == null)
                {
                    dlnm = new DLNM_HRC2
                    {
                        NgaySx = model.Ngay,
                        Ngay = model.Ngay,
                        Ca = model.Ca,
                        BieuMau = model.BieuMau,
                        Scope = model.Scope,
                        MeThoi = model.MeThoi,
                        MacThep = model.MacThep,
                        O2 = model.O2,
                        N2 = model.N2,
                        AR_RH = model.AR_RH,
                        AR_LF = model.AR_LF,
                        AR_BOF = model.AR_BOF,
                        KLGangLong = model.KLGangLong,
                        KLThepPhe = model.KLThepPhe,
                        KLThepLong = model.KlThepLong,
                        IsNM = false,
                        IsChuyenCa = model.IsChuyenCa,
                        IsTrungMeThoi = isTrung,
                        TempKey = Guid.NewGuid()
                    };
                    await _context.DLNM_HRC2s.AddAsync(dlnm);
                }
                else
                {
                    existing.MeThoi = model.MeThoi;
                    existing.MacThep = model.MacThep;
                    existing.O2 = model.O2;
                    existing.N2 = model.N2;
                    existing.AR_RH = model.AR_RH;
                    existing.AR_LF = model.AR_LF;
                    existing.AR_BOF = model.AR_BOF;
                    existing.KLGangLong = model.KLGangLong;
                    existing.KLThepPhe = model.KLThepPhe;
                    existing.KLThepLong = model.KlThepLong;
                    existing.IsChuyenCa = model.IsChuyenCa;
                    existing.NgaySx = model.Ngay;
                    existing.IsTrungMeThoi = isTrung;
                    _context.DLNM_HRC2s.Update(existing);
                    dlnm = existing;
                }

                dlnmMap[model.RowKey] = dlnm;
            }

            await _context.SaveChangesAsync();

            // -------------------------------------------------------
            // UPSERT PhuLieu_HRC2
            // -------------------------------------------------------
            var dlnmIds = dlnmMap.Values.Select(x => x.ID).Distinct().ToList();
            var existingPL = await _context.PhuLieu_HRC2s
                .Where(x => dlnmIds.Contains(x.ID_MeThoi))
                .ToListAsync();

            foreach (var model in models)
            {
                if (!dlnmMap.TryGetValue(model.RowKey, out var dlnm)) continue;

                foreach (var pl in model.hRC2_PhuLieus)
                {
                    var existing = existingPL.FirstOrDefault(x =>
                        x.ID_MeThoi == dlnm.ID &&
                        x.ID_HeaderKey == pl.ID_HeaderKey &&
                        x.ID_PhuLieu == pl.ID_PhuLieu &&
                        x.IsPhanBo == (pl.IsPhanBo ?? false));

                    // IsNM=true + không phải phanbo → skip
                    if (existing == null && dlnm.IsNM == true && !(pl.IsPhanBo ?? false))
                        continue;

                    if (existing == null)
                    {
                        await _context.PhuLieu_HRC2s.AddAsync(new PhuLieu_HRC2
                        {
                            REPORT_NO = dlnm.REPORT_NO,
                            BieuMau = model.BieuMau,
                            MeThoi = model.MeThoi,
                            ID_PhuLieu = pl.ID_PhuLieu,
                            TenPhuLieu = pl.TenPhuLieu,
                            KLPhuGia = pl.KLPhuGia,
                            KLPhuGia_Manual = pl.KLPhuGia_Manual,
                            IsManual = pl.IsManual ?? false,
                            ID_HeaderKey = pl.ID_HeaderKey,
                            TenHienThi = pl.TenHienThi,
                            ID_MeThoi = dlnm.ID,
                            IsPhanBo = pl.IsPhanBo ?? false
                        });
                    }
                    else
                    {
                        existing.KLPhuGia = pl.KLPhuGia;

                        if (pl.IsManual == true)
                        {
                            existing.IsManual = true;
                            existing.KLPhuGia_Manual = pl.KLPhuGia_Manual;
                        }
                        else if (!(existing.IsManual ?? false))
                        {
                            existing.IsManual = false;
                            existing.KLPhuGia_Manual = null;
                        }

                        _context.PhuLieu_HRC2s.Update(existing);
                    }
                }
            }

            await _context.SaveChangesAsync();

            // -------------------------------------------------------
            // DELETE manual_col_* removed on FE
            // -------------------------------------------------------
            var allowed = manualColHeaderKeyIds ?? new HashSet<int>();
            var toDelete = await _context.PhuLieu_HRC2s
                .Where(x =>
                    dlnmIds.Contains(x.ID_MeThoi) &&
                    x.IsPhanBo == true &&
                    x.IsManual == true &&
                    x.ID_HeaderKey.HasValue &&
                    !allowed.Contains(x.ID_HeaderKey.Value))
                .ToListAsync();
            if (toDelete.Count > 0)
            {
                _context.PhuLieu_HRC2s.RemoveRange(toDelete);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<DLNM_HRC2>> GetAllAsync(DateTime? Ngay, int? Ca, string? BieuMau, int? Scope)
        {
            return  await _repo.GetAllAsync(Ngay,Ca,BieuMau,Scope);
        }

        public async Task<DLNM_HRC2?> GetByIdAsync(int id)
        {
            var x = await _repo.GetByIdAsync(id);
            if (x == null) return null;
            return x;
            
        }

        public async Task<HRC2DetailByReportNoModel?> GetByReportNoAsync(int reportNo)
        {
            return await _repo.GetByReportNoAsync(reportNo);
        }

        // public async Task<IEnumerable<HRC2DetailByReportNoModel>> FilterAsync(SyncFromNM_HRC2_Request request)
        // {
        //     await _hrc2NMSyncService.SyncHRC2FromNMAsync(request);

        //     var allData = await _repo.GetAllAsync(request.NgaySX , request.Ca, request.LoaiBM, request.Scope);
        //     var reportNos = allData
        //         .Select(x => (int?)x.REPORT_NO)
        //         .Where(x => x.HasValue && x.Value != 0)
        //         .Select(x => x!.Value)
        //         .Distinct()
        //         .ToList();

        //     var result = new List<HRC2DetailByReportNoModel>();
        //     foreach (var reportNo in reportNos)
        //     {
        //         var detail = await _repo.GetByReportNoAsync(reportNo);
        //         if (detail != null)
        //         {
        //             result.Add(detail);
        //         }
        //     }

        //     return result;
        // }

        public async Task<IEnumerable<HRC2GroupedByReportNoModel>> FilterGroupedAsync(SyncFromNM_HRC2_Request request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            await _hrc2NMSyncService.SyncHRC2FromNMAsync(request);
            var allData = await _repo.GetAllAsync(request.NgaySX, request.Ca, request.LoaiBM, request.Scope);
            var ids = allData
                .Select(x => (int?)x.ID)
                .Where(x => x.HasValue && x.Value != 0)
                .Select(x => x!.Value)
                .ToList();

            var result = new List<HRC2GroupedByReportNoModel>();
            foreach (var id in ids)
            {
                var detail = await _repo.GetByIdGroupedAsync(id);
                if (detail != null)
                {
                    result.Add(detail);
                }
            }

            return result;
        }

        public async Task<DLNM_HRC2> CreateAsync(DLNM_HRC2 entity)
        {
            await _repo.AddAsync(entity);
            return entity;
        }

        public async Task<bool> UpdateAsync(int id, DLNM_HRC2 entity)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return false;
            entity.ID = id;
            await _repo.UpdateAsync(entity);
            return true;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return false;
            await _repo.DeleteAsync(id);
            return true;
        }

        public async Task<PagedResult<DLNM_HRC2>> SearchWithPagingAsync(DateTime? NgaySX, int? Ca, string? LoaiBM, int? Scope, string? searchText, int page, int pageSize)
        {
            var (data, totalCount) = await _repo.SearchWithPagingAsync(NgaySX, Ca, LoaiBM, Scope, searchText, page, pageSize);
            
            return new PagedResult<DLNM_HRC2>
            {
                Data = data.ToList(),
                TotalRecords = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        // Search grouped theo REPORT_NO (dùng cho Thống kê HRC2 ở UI)
        public async Task<PagedResult<HRC2GroupedByReportNoModel>> SearchGroupedWithPagingAsync(
            DateTime? NgaySX,
            int? Ca,
            string? LoaiBM,
            int? Scope,
            string? searchText,
            int page,
            int pageSize
        )
        {
            // Sync dữ liệu từ NM nếu đủ điều kiện bộ lọc giống API filter
            if (NgaySX.HasValue && Ca.HasValue && Scope.HasValue && !string.IsNullOrWhiteSpace(LoaiBM))
            {
                await _hrc2NMSyncService.SyncHRC2FromNMAsync(new SyncFromNM_HRC2_Request
                {
                    NgaySX = NgaySX.Value,
                    Ca = Ca.Value,
                    LoaiBM = LoaiBM!,
                    Scope = Scope.Value
                });
            }

            var (baseList, totalCount) = await _repo.SearchWithPagingAsync(NgaySX, Ca, LoaiBM, Scope, searchText, page, pageSize);
            var result = new List<HRC2GroupedByReportNoModel>();

            foreach (var item in baseList)
            {
                if (item == null) continue;
                if (item.ID == 0) continue;
                var detail = await _repo.GetByIdGroupedAsync((int)item.ID);
                if (detail != null)
                {
                    result.Add(detail);
                }
            }

            return new PagedResult<HRC2GroupedByReportNoModel>
            {
                Data = result,
                TotalRecords = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<PagedResult<HRC2FilterThongKe>> SearchGroupedWithPagingAsync(
            SearchThongKe dto
        )
        {
            var (data, totalCount) = await _repo.SearchThongKeAsync(dto);

            return new PagedResult<HRC2FilterThongKe>
            {
                Data = data.ToList(),
                TotalRecords = totalCount,
                Page = dto.Page ?? 0,
                PageSize = dto.PageSize ?? 0
            };
        }

        public async Task<bool> ChuyenMeThoiAsync(ChuyenMeThoiRequest request)
        {
            return await _repo.ChuyenMeThoiAsync(request);
        }

        public async Task<IEnumerable<FilterSTD_NXTResponse>> FilterSTD_NXTAsync(FilterSTD_NXTRequest request)
        {
            var result = (await _repo.GetHRC2GroupedByMaterialAsync(request.NgaySX, request.Ca)).ToList();
            if (request.IdPhieu.HasValue && request.IdPhieu.Value != Guid.Empty)
            {
                // Ưu tiên dùng danh sách HeaderKeyIds từ FE (phản ánh đúng bảng đang hiển thị, kể cả dòng mới chưa lưu)
                var headerKeys = (request.HeaderKeyIds != null && request.HeaderKeyIds.Count > 0)
                    ? request.HeaderKeyIds
                        .Where(id => id > 0)
                        .Distinct()
                        .Select(id => new IdHeaderKeyModel { Id_HeaderKey = id })
                        .ToList()
                    : result
                        .Where(x => x.HeaderKeyId.HasValue && x.HeaderKeyId.Value > 0)
                        .Select(x => new IdHeaderKeyModel { Id_HeaderKey = x.HeaderKeyId!.Value })
                        .DistinctBy(x => x.Id_HeaderKey)
                        .ToList();
                if (headerKeys.Count > 0)
                {
                    await _stdNxtRepo.GetHRC2FilterInitAsync(new InitXuatNhapTonHRC2Request
                    {
                        NgaySX = request.NgaySX,
                        Ca = request.Ca,
                        IdPhieu = request.IdPhieu.Value,
                        HeaderKeys = headerKeys
                    });
                }
            }
            return result;
        }

        public async Task<ExportFileResult> ExportThongKeExcelAsync(SearchThongKe dto)
        {
            dto.Page = null;
            dto.PageSize = null;

            var (rows, _) = await _repo.SearchThongKeAsync(dto);
            var list = rows.ToList();

            if (!list.Any())
                throw new InvalidOperationException("Không có dữ liệu phù hợp với điều kiện lọc để xuất Excel.");

            var headers = list.First().phuLieuHeaderTables ?? new List<PhuLieuHeaderTable>();

            var loaiBmKey = (dto.LoaiBM ?? "").Trim().ToUpperInvariant();
            var templateFileName = loaiBmKey == "BOF" ? "PKH_BOF.xlsx" : "PKH_LFRH.xlsx";

            var templatePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "templates",
                templateFileName);

            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"Không tìm thấy file mẫu Excel: {templatePath}");

            using var fs = new FileStream(
                templatePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite);

            using var workbook = new XLWorkbook(fs);
            var ws = workbook.Worksheet(1);

            var scope = dto.Scope;

            string title = "";
            if(loaiBmKey == "BOF")
            {
                title = $"BIÊN BẢN TIÊU HAO NẤU LUYỆN LÒ THỔI {scope}".Trim();
            }
            else
            {
                if(loaiBmKey == "LF")
                    title = $"BẢNG TIÊU HAO NẤU LUYỆN LÒ TINH LUYỆN LF {scope}".Trim();
                else
                    title = $"BẢNG TIÊU HAO NẤU LUYỆN LÒ TINH LUYỆN RH {scope}".Trim();
            }
            // merge dòng 4 (ví dụ 27 cột theo layout hiện tại)
            ws.Range(4, 1, 4, 27).Merge();

            ws.Cell(4, 1).Value = title;
            ws.Cell(4, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(4, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            ws.Cell(4, 1).Style.Font.Bold = true;
            ws.Cell(4, 1).Style.Font.FontSize = 16;

            // ===== HEADER PHỤ LIỆU ĐỘNG + CÁC CỘT CỐ ĐỊNH THEO MẪU =====
            // BOF: phụ liệu từ cột 6, LF/RH: phụ liệu từ cột 5 (dòng 7)
            int headerRow = 7;
            int headerStartCol = loaiBmKey == "BOF" ? 6 : 5;

            // Dòng 6: tiêu đề chung cho vùng phụ liệu
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

            // Dòng 7: tên từng phụ liệu
            int headerCol = headerStartCol;
            foreach (var h in headers)
            {
                ws.Cell(headerRow, headerCol).Value = h.TenPhuLieu;
                headerCol++;
            }

            // Sau các phụ liệu, bổ sung các cột cố định tùy theo loại BM
            int extraStartCol = headerStartCol + headers.Count;

            if (loaiBmKey == "BOF")
            {
                // Hình 2: Nhiên liệu (m3) / Oxy / Nitơ / Ghi chú / KL thép phế trong thùng gang (tấn)
                int fuelStartCol = extraStartCol;

                // Dòng 6: merge 2 cột cho "Nhiên liệu (m3)"
                ws.Range(6, fuelStartCol, 6, fuelStartCol + 1).Merge();
                var fuelHeader = ws.Range(6, fuelStartCol, 6, fuelStartCol + 1);
                fuelHeader.Merge();
                fuelHeader.Value = "Nhiên liệu (m³)";
                fuelHeader.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                fuelHeader.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                fuelHeader.Style.Alignment.WrapText = true;
                fuelHeader.Style.Font.Bold = true;

                // Dòng 7: "Oxy" và "Nitơ"
                ws.Cell(7, fuelStartCol).Value = "Oxy";
                ws.Cell(7, fuelStartCol + 1).Value = "Nitơ";

                // "Ghi chú" – merge 2 dòng 6-7
                int noteCol = fuelStartCol + 2;
                ws.Range(6, noteCol, 7, noteCol).Merge();
                var noteHeader = ws.Range(6, noteCol, 7, noteCol);
                noteHeader.Merge();
                noteHeader.Value = "Ghi chú";
                noteHeader.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                noteHeader.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                noteHeader.Style.Alignment.WrapText = true;
                noteHeader.Style.Font.Bold = true;

                // "KL thép phế trong thùng gang (tấn)" – merge 2 dòng 6-7
                int scrapCol = fuelStartCol + 3;
                var scrapHeader = ws.Range(6, scrapCol, 7, scrapCol);
                scrapHeader.Merge();
                scrapHeader.Value = "KL thép phế trong thùng gang (tấn)";
                scrapHeader.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                scrapHeader.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                scrapHeader.Style.Alignment.WrapText = true;
                scrapHeader.Style.Font.Bold = true;
            }
            else
            {
                // LF/RH – tương tự hình 1
                int gasCol = extraStartCol;

                // Cột "Khí" (dòng 6) + "Argon (m3)" (dòng 7)
                ws.Cell(6, gasCol).Value = "Khí";
                ws.Cell(7, gasCol).Value = "Argon (m³)";
                ws.Range(6, gasCol, 7, gasCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Range(6, gasCol, 7, gasCol).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                ws.Range(6, gasCol, 7, gasCol).Style.Alignment.WrapText = true;
                ws.Range(6, gasCol, 7, gasCol).Style.Font.Bold = true;

                // "Que lấy mẫu (Cái)" – merge 2 dòng 6-7
                int queLayMauCol = gasCol + 1;
                var queLayMauHeader = ws.Range(6, queLayMauCol, 7, queLayMauCol);
                queLayMauHeader.Merge();
                queLayMauHeader.Value = "Que lấy mẫu (Cái)";
                queLayMauHeader.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                queLayMauHeader.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                queLayMauHeader.Style.Alignment.WrapText = true;
                queLayMauHeader.Style.Font.Bold = true;

                // "Que đo nhiệt (Cái)" – merge 2 dòng 6-7
                int queDoNhietCol = gasCol + 2;
                var queDoNhietHeader = ws.Range(6, queDoNhietCol, 7, queDoNhietCol);
                queDoNhietHeader.Merge();
                queDoNhietHeader.Value = "Que đo nhiệt (Cái)";
                queDoNhietHeader.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                queDoNhietHeader.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                queDoNhietHeader.Style.Alignment.WrapText = true;
                queDoNhietHeader.Style.Font.Bold = true;

                // "Ghi chú" – merge 2 dòng 6-7
                int noteColLfRh = gasCol + 3;
                var noteLfRhHeader = ws.Range(6, noteColLfRh, 7, noteColLfRh);
                noteLfRhHeader.Merge();
                noteLfRhHeader.Value = "Ghi chú";
                noteLfRhHeader.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                noteLfRhHeader.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                noteLfRhHeader.Style.Alignment.WrapText = true;
                noteLfRhHeader.Style.Font.Bold = true;
            }

            int startRow = 8;
            int currentRow = startRow;

            foreach (var item in list)
            {
                var d = item.dulieu?.data;
                if (d == null) continue;

                ws.Cell(currentRow, 1).Value = currentRow - startRow + 1;

                ws.Cell(currentRow, 2).Value = d.MeThoi;
                ws.Cell(currentRow, 3).Value = d.MacThep;
                ws.Cell(currentRow, 4).Value = d.KLGangLongCCT;
                ws.Cell(currentRow, 5).Value = d.KLThepPhe;

                var valueByHeaderKeyId = (item.dulieu?.mappedPhulieus ?? Enumerable.Empty<HeaderKeyGroupedByReportNoModel>())
                    .Where(x => x.ID_HeaderKey.HasValue)
                    .ToDictionary(
                        x => x.ID_HeaderKey!.Value,
                        x => x.KLPhuGiaTotal ?? x.KLPhuGia ?? 0
                    );

                int colIndex = 6;

                foreach (var h in headers)
                {
                    if (valueByHeaderKeyId.TryGetValue(h.IDHeaderKey, out var value) && value != 0)
                        ws.Cell(currentRow, colIndex).Value = value;

                    colIndex++;
                }

                // tăng chiều cao dòng dữ liệu cho dễ đọc
                ws.Row(currentRow).Height = 18;

                currentRow++;
            }


            // ===== DÒNG TỔNG =====
            currentRow += 1;
            int totalRow = currentRow;

            ws.Range(totalRow, 1, totalRow, 3).Merge();
            ws.Cell(totalRow, 1).Value = "Tổng";
            ws.Cell(totalRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(totalRow, 1).Style.Font.Bold = true;

            int lastDataRow = currentRow - 1;
            ws.Range(startRow, 1, lastDataRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(totalRow, 4).FormulaA1 = $"SUM(D{startRow}:D{lastDataRow})";
            ws.Cell(totalRow, 5).FormulaA1 = $"SUM(E{startRow}:E{lastDataRow})";

            int col = 6;

            foreach (var h in headers)
            {
                var colLetter = ws.Cell(1, col).Address.ColumnLetter;
                ws.Cell(totalRow, col).FormulaA1 = $"SUM({colLetter}{startRow}:{colLetter}{lastDataRow})";
                col++;
            }

            currentRow += 2;

            // ===== HEADER FOOTER =====
            ws.Range(currentRow, 16, currentRow, 19).Merge();
            ws.Range(currentRow, 20, currentRow, 23).Merge();
            ws.Range(currentRow, 24, currentRow, 27).Merge();

            ws.Cell(currentRow, 16).Value = "Tồn đầu kíp";
            ws.Cell(currentRow, 20).Value = "Nhập trong kíp";
            ws.Cell(currentRow, 24).Value = "Tồn cuối kíp";

            ws.Row(currentRow).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Row(currentRow).Style.Font.Bold = true;

            List<string> materials;

            if (loaiBmKey.Contains("BOF"))
            {
                materials = new List<string>
                {
                    "Lượng Vôi",
                    "Lượng Dolomite",
                    "Lượng Quặng",
                    "Lượng FeSi",
                    "Lượng SiMn",
                    "Lượng FeMn",
                    "Lượng Than",
                    "Lượng AL",
                    "FeCrHC",
                    "Lượng Chất tăng cacbon",
                    "Lượng HC-FeMn75",
                    "Nguyên liệu khác: Coke",
                    "FeP",
                    "Lượng FeCr LC",
                    "Bauxite",
                    "LDSF",
                    "Chất cải tính xỉ"
                };
            }
            else
            {
                materials = new List<string>
                {
                    "Tồn trên silo",
                    "Lượng SiMn (kg)",
                    "Lượng FeSi (kg)",
                    "Lượng vôi (kg)",
                    "Lượng than (kg)",
                    "Lượng FeMn (kg)",
                    "Lượng Huỳnh thạch (kg)",
                    "Lượng Nhôm (kg)",
                    "Khác"
                };
            }

            int r = currentRow + 1;

            foreach (var m in materials)
            {
                ws.Range(r, 1, r, 15).Merge();
                ws.Cell(r, 1).Value = m;

                ws.Range(r, 16, r, 19).Merge();
                ws.Range(r, 20, r, 23).Merge();
                ws.Range(r, 24, r, 27).Merge();

                r++;
            }

            int lastFooterRow = r - 1;

            // ===== BORDER TOÀN BỘ =====

            // Xác định số cột cuối cùng dựa trên nội dung thực tế của sheet (template + cột động)
            var lastUsedColumn = ws.LastColumnUsed();
            int lastColumn = lastUsedColumn != null ? lastUsedColumn.ColumnNumber() : 34;

            // border header (dòng 6-7) + căn giữa + wrap text toàn vùng header
            var headerBorderRange = ws.Range(6, 1, 7, lastColumn);
            headerBorderRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
            headerBorderRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            headerBorderRange.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            headerBorderRange.Style.Border.RightBorder = XLBorderStyleValues.Thin;
            headerBorderRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            headerBorderRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerBorderRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            headerBorderRange.Style.Alignment.WrapText = true;

            // border bảng dữ liệu + dòng tổng
            var dataRange = ws.Range(startRow, 1, totalRow, lastColumn);
            dataRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.RightBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // border footer
            var footerRange = ws.Range(currentRow, 1, lastFooterRow, lastColumn);
            footerRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
            footerRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            footerRange.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            footerRange.Style.Border.RightBorder = XLBorderStyleValues.Thin;
            footerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            var fileName = $"ThongKe_HRC2_{loaiBmKey}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";

            return new ExportFileResult
            {
                Content = stream.ToArray(),
                FileName = fileName,
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            };

        }
    }
}

using dataproduct.api.DTOs;
using dataproduct.api.Models;
using dataproduct.api.Models.MasterData;
using dataproduct.api.Repositories;
using dataproduct.api.ResponseModels;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace dataproduct.api.Services
{
    public class HRC2_NMSyncService
    {
        private readonly ProductFormContext _context;
        private readonly ProductDataMasterDbContext _masterDataContext;
        private readonly string _productDataConnStr;
        public HRC2_NMSyncService (ProductFormContext context, ProductDataMasterDbContext masterDataContext, IConfiguration config)
        {
            _context = context;
            _masterDataContext = masterDataContext;
            _productDataConnStr = config.GetConnectionString("MasterDbConnection") 
                ?? throw new InvalidOperationException("MasterDbConnection string is not configured");
        }

        public async Task<List<HRC2_NM>> GetFromNmAsync(string plant, int plantNo, DateTime workDate, int shift)
        {
            var parameters = new[]
            {
                new SqlParameter("@Plant", plant),
                new SqlParameter("@PlantNo", plantNo),
                new SqlParameter("@WorkDate", workDate),
                new SqlParameter("@Shift", shift)
            };
            return await _context.Set<HRC2_NM>()
                .FromSqlRaw("EXEC sp_GetHRC2FromNM_Test @Plant, @PlantNo, @WorkDate, @Shift", parameters)
                .ToListAsync();
        }

        public async Task<List<HRC2_NM>> GetByMeThoiFromNmAsync(string plant, int plantNo, string meThoi)
        {
            var parameters = new[]
            {
                new SqlParameter("@Plant", plant),
                new SqlParameter("@PlantNo", plantNo),
                new SqlParameter("@MeThoi", meThoi),
            };
            return await _context.Set<HRC2_NM>()
                .FromSqlRaw("EXEC sp_GetHRC2ByMeThoiFromNM_Test @Plant, @PlantNo, @MeThoi", parameters)
                .ToListAsync();
        }

        public async Task SyncByReportNoAsync(List<HRC2_NM> nmData, SyncFromNM_HRC2_Request request)
        {
            nmData ??= new List<HRC2_NM>();
            using var tran = await _context.Database.BeginTransactionAsync();
            try
            {
                // Group theo cả REPORT_NO và PRODUCT_ID (MeThoi) - vì cùng MeThoi nhưng khác REPORT_NO vẫn lấy
                var nmLookup = nmData
                    .GroupBy(x => new { ReportNo = x.REPORT_NO, MeThoi = x.PRODUCT_ID })
                    .Where(g => !string.IsNullOrWhiteSpace(g.Key.MeThoi))
                    .ToDictionary(
                        g => new { ReportNo = g.Key.ReportNo, MeThoi = g.Key.MeThoi! },
                        g => g.ToList());

                // Lấy danh sách các mẻ đã chuyển đi khỏi ca/ngày hiện tại (để loại bỏ khỏi danh sách xử lý)
                // Lấy theo cả REPORT_NO và MeThoi
                var transferredAwayRecords = await _context.DLNM_HRC2s
                    .Where(x =>
                        x.IsNM == true &&
                        x.IsChuyenCa == true &&
                        x.MeThoi != null &&
                        x.REPORT_NO != null &&
                        (x.Ngay != request.NgaySX || x.Ca != request.Ca || x.Scope != request.Scope || x.BieuMau != request.LoaiBM))
                    .Select(x => new { ReportNo = x.REPORT_NO!.Value, MeThoi = x.MeThoi! })
                    .ToListAsync();

                var productKeys = new HashSet<(decimal ReportNo, string MeThoi)>(nmLookup.Keys.Select(k => (k.ReportNo, k.MeThoi)));

                // Loại bỏ các mẻ đã chuyển đi khỏi danh sách xử lý (theo cả REPORT_NO và MeThoi)
                foreach (var transferred in transferredAwayRecords)
                {
                    productKeys.Remove((transferred.ReportNo, transferred.MeThoi));
                }

                // Lấy các mẻ đã chuyển đến ca/ngày hiện tại (để cập nhật riêng)
                // Lấy theo cả REPORT_NO và MeThoi
                var transferredToRecords = await _context.DLNM_HRC2s
                    .Where(x =>
                        x.IsNM == true &&
                        x.IsChuyenCa == true &&
                        x.Ngay == request.NgaySX &&
                        x.Ca == request.Ca &&
                        x.Scope == request.Scope &&
                        x.BieuMau == request.LoaiBM &&
                        x.MeThoi != null &&
                        x.REPORT_NO != null)
                    .Select(x => new { ReportNo = x.REPORT_NO!.Value, MeThoi = x.MeThoi! })
                    .ToListAsync();

                // Thêm các mẻ đã chuyển đến vào danh sách xử lý (theo cả REPORT_NO và MeThoi)
                foreach (var transferred in transferredToRecords)
                {
                    productKeys.Add((transferred.ReportNo, transferred.MeThoi));
                }

                // Cache kết quả stored procedure
                var gangLongMetricsCache =
                    new Dictionary<string, (double? CCT, double? CR)>(StringComparer.OrdinalIgnoreCase);

                // Chỉ gọi stored procedure khi LoaiBM = "BOF"
                if (request.LoaiBM == "BOF")
                {
                    // Tập hợp tất cả meThoi cần gọi stored procedure (tránh gọi trùng lặp)
                    var meThoiSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    // Thu thập tất cả meThoi từ productKeys
                    foreach (var key in productKeys)
                    {
                        if (!string.IsNullOrWhiteSpace(key.MeThoi))
                        {
                            meThoiSet.Add(key.MeThoi);
                        }
                    }

                    // Gọi stored procedure tuần tự per MeThoi
                    foreach (var meThoi in meThoiSet)
                    {
                        var cct = await ExecuteGangLongStoredProcAsync("usp_Sum_KL_GangLong_By_MeThoi_CCT", meThoi);
                        // var cr = await ExecuteGangLongStoredProcAsync("usp_Sum_KL_GangLong_By_MeThoi_CR", meThoi);
                        var cr = 0;
                        gangLongMetricsCache[meThoi] = (cct, cr);
                    }
                }

                // Xử lý từng mẻ (theo cả REPORT_NO và MeThoi)
                foreach (var key in productKeys)
                {
                    if (string.IsNullOrWhiteSpace(key.MeThoi))
                    {
                        continue;
                    }

                    // Lấy các DLNM_HRC2 records cũ (filter theo cả REPORT_NO và MeThoi)
                    // Load vào memory trước rồi filter bằng AreEqual để tránh lỗi EF Core translation
                    var allDLNMRecords = await _context.DLNM_HRC2s
                        .Where(x =>
                            x.IsNM == true &&
                            x.MeThoi == key.MeThoi &&
                            x.REPORT_NO == (int)key.ReportNo &&
                            x.Ngay == request.NgaySX &&
                            x.Ca == request.Ca &&
                            x.Scope == request.Scope)
                        .ToListAsync();

                    var existingDLNMRecords = allDLNMRecords
                        .Where(x => AreEqual(x.BieuMau, request.LoaiBM))
                        .ToList();

                    // Lấy các PhuLieu_HRC2 records cũ (filter theo cả REPORT_NO và MeThoi)
                    var existingPhuLieuRecords = await _context.PhuLieu_HRC2s
                        .Where(x =>
                            x.MeThoi == key.MeThoi &&
                            x.REPORT_NO == (int)key.ReportNo &&
                            x.BieuMau == request.LoaiBM)
                        .ToListAsync();

                    // Kiểm tra xem mẻ này có đã chuyển đến ca/ngày hiện tại không
                    var isMovedToSlot = existingDLNMRecords.Any(x => x.IsChuyenCa == true);

                    List<HRC2_NM> nmGroup;
                    if (isMovedToSlot)
                    {
                        // Mẻ đã chuyển đến ca/ngày hiện tại: query riêng từ NM
                        nmGroup = await GetByMeThoiFromNmAsync(request.LoaiBM, request.Scope, key.MeThoi);
                    }
                    else
                    {
                        // Mẻ chưa chuyển đến: lấy từ nmLookup (theo cả REPORT_NO và MeThoi)
                        var lookupKey = new { ReportNo = key.ReportNo, MeThoi = key.MeThoi };
                        if (!nmLookup.TryGetValue(lookupKey, out var nmGroupFromLookup))
                        {
                            continue; // Không có dữ liệu trong NM
                        }
                        nmGroup = nmGroupFromLookup;
                    }

                    if (nmGroup == null || !nmGroup.Any())
                    {
                        continue;
                    }

                    // ========== UPSERT DLNM_HRC2 ==========
                    // Lấy record đầu tiên trong group (thông tin chính giống nhau)
                    var firstNm = nmGroup.First();
                    
                    // Tìm record hiện có trong DB (cùng REPORT_NO, MeThoi, Ngay, Ca, Scope, BieuMau)
                    var existingDLNM = existingDLNMRecords.FirstOrDefault();
                    
                    DLNM_HRC2 dlnmEntity;
                    long dlnmId;
                    
                    if (existingDLNM != null)
                    {
                        // UPDATE: Cập nhật record hiện có
                        dlnmEntity = existingDLNM;
                        dlnmId = existingDLNM.ID;
                        UpdateDLNMEntityFromNm(dlnmEntity, firstNm, gangLongMetricsCache, request, isMovedToSlot);
                    }
                    else
                    {
                        // INSERT: Tạo record mới
                        dlnmEntity = CreateDLNMEntityFromNm(firstNm, gangLongMetricsCache, overwriteSlot: true);
                        dlnmEntity.Ngay = request.NgaySX;
                        dlnmEntity.Ca = request.Ca;
                        dlnmEntity.BieuMau = request.LoaiBM;
                        dlnmEntity.Scope = request.Scope;
                        dlnmEntity.IsChuyenCa = isMovedToSlot;
                        _context.DLNM_HRC2s.Add(dlnmEntity);
                        await _context.SaveChangesAsync(); // Save để lấy ID
                        dlnmId = dlnmEntity.ID;
                    }

                    // Kiểm tra và cập nhật IsTrungMeThoi
                    var listTrungMe = await _context.DLNM_HRC2s
                        .Where(x => x.BieuMau == request.LoaiBM && x.MeThoi == dlnmEntity.MeThoi)
                        .ToListAsync();
                    if (listTrungMe.Count > 1)
                    {
                        foreach (var item in listTrungMe)
                        {
                            item.IsTrungMeThoi = true;
                        }
                        dlnmEntity.IsTrungMeThoi = true;
                    }

                    // ========== UPSERT PhuLieu_HRC2 ==========
                    // Tạo lookup từ nmData: key = (REPORT_NO, MeThoi, MATERIAL_NO) - cast về int để match với DB
                    var nmPhuLieuLookup = nmGroup
                        .Where(nm => nm.MATERIAL_NO.HasValue)
                        .GroupBy(nm => new { 
                            ReportNo = (int)nm.REPORT_NO, 
                            MeThoi = nm.PRODUCT_ID, 
                            MaterialNo = (int)nm.MATERIAL_NO!.Value 
                        })
                        .ToDictionary(
                            g => new { ReportNo = g.Key.ReportNo, MeThoi = g.Key.MeThoi, MaterialNo = g.Key.MaterialNo },
                            g => g.First());

                    // Tạo lookup từ DB: key = (REPORT_NO, MeThoi, ID_PhuLieu)
                    var existingPhuLieuLookup = existingPhuLieuRecords
                        .Where(x => x.ID_PhuLieu.HasValue && x.REPORT_NO.HasValue)
                        .GroupBy(x => new { 
                            ReportNo = x.REPORT_NO!.Value, 
                            MeThoi = x.MeThoi, 
                            MaterialNo = x.ID_PhuLieu!.Value 
                        })
                        .ToDictionary(
                            g => new { ReportNo = g.Key.ReportNo, MeThoi = g.Key.MeThoi, MaterialNo = g.Key.MaterialNo },
                            g => g.First());

                    // Update hoặc Insert PhuLieu_HRC2 từ nmData
                    foreach (var nmPhuLieu in nmPhuLieuLookup.Values)
                    {
                        var lookupKey = new { 
                            ReportNo = (int)nmPhuLieu.REPORT_NO, 
                            MeThoi = nmPhuLieu.PRODUCT_ID, 
                            MaterialNo = (int)nmPhuLieu.MATERIAL_NO!.Value 
                        };
                        
                        if (existingPhuLieuLookup.TryGetValue(lookupKey, out var existingPhuLieu))
                        {
                            // UPDATE: Cập nhật record hiện có
                            UpdatePhuLieuEntityFromNm(existingPhuLieu, nmPhuLieu, dlnmId);
                        }
                        else
                        {
                            // INSERT: Tạo record mới
                            var phuLieuEntity = CreatePhuLieuEntityFromNm(
                                nmPhuLieu,
                                request.NgaySX,
                                request.Ca,
                                request.LoaiBM,
                                request.Scope,
                                dlnmId,
                                isChuyenCa: isMovedToSlot);
                            _context.PhuLieu_HRC2s.Add(phuLieuEntity);
                        }
                    }

                    // Xóa các PhuLieu_HRC2 dư thừa (có trong DB nhưng không có trong nmData)
                    var phuLieuToDelete = existingPhuLieuRecords
                        .Where(existing => 
                            existing.ID_PhuLieu.HasValue &&
                            existing.REPORT_NO.HasValue &&
                            !nmPhuLieuLookup.ContainsKey(new { 
                                ReportNo = existing.REPORT_NO!.Value, 
                                MeThoi = existing.MeThoi, 
                                MaterialNo = existing.ID_PhuLieu!.Value 
                            }))
                        .ToList();
                    
                    if (phuLieuToDelete.Any())
                    {
                        _context.PhuLieu_HRC2s.RemoveRange(phuLieuToDelete);
                    }
                }

                // ========== XÓA DLNM_HRC2 DƯ THỪA ==========
                // Xóa các DLNM_HRC2 có trong DB (trong slot hiện tại) nhưng không có trong productKeys
                var allDLNMInSlot = await _context.DLNM_HRC2s
                    .Where(x =>
                        x.IsNM == true &&
                        x.Ngay == request.NgaySX &&
                        x.Ca == request.Ca &&
                        x.Scope == request.Scope &&
                        x.MeThoi != null &&
                        x.REPORT_NO != null)
                    .ToListAsync();

                var dlnmInSlot = allDLNMInSlot
                    .Where(x => AreEqual(x.BieuMau, request.LoaiBM))
                    .ToList();

                var dlnmToDelete = dlnmInSlot
                    .Where(x => !productKeys.Contains((x.REPORT_NO!.Value, x.MeThoi!)))
                    .ToList();

                if (dlnmToDelete.Any())
                {
                    // Xóa các PhuLieu_HRC2 liên quan trước
                    var dlnmIdsToDelete = dlnmToDelete.Select(x => x.ID).ToList();
                    var phuLieuToDeleteByDlnm = await _context.PhuLieu_HRC2s
                        .Where(x => dlnmIdsToDelete.Contains(x.ID_MeThoi))
                        .ToListAsync();
                    if (phuLieuToDeleteByDlnm.Any())
                    {
                        _context.PhuLieu_HRC2s.RemoveRange(phuLieuToDeleteByDlnm);
                    }

                    // Xóa các DLNM_HRC2
                    _context.DLNM_HRC2s.RemoveRange(dlnmToDelete);
                }

                
                await _context.SaveChangesAsync();

                await tran.CommitAsync();
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();
                throw;  // ném lên để biết lỗi
            }
        }


        /// <summary>
        /// Tạo 1 PhuLieu_HRC2 entity từ HRC2_NM (mỗi record NM = 1 phụ liệu)
        /// </summary>
        private PhuLieu_HRC2 CreatePhuLieuEntityFromNm(HRC2_NM nm, DateTime? ngay, int? ca, string? bieuMau, int? scope, long idMeThoi, bool isChuyenCa = false)
        {
            return new PhuLieu_HRC2
            {
                REPORT_NO = (int)nm.REPORT_NO,
                BieuMau = bieuMau ?? nm.PLANT,
                MeThoi = nm.PRODUCT_ID,
                ID_PhuLieu = nm.MATERIAL_NO.HasValue ? (int?)nm.MATERIAL_NO.Value : null,
                TenPhuLieu = nm.DESCRIPTION_EN,
                KLPhuGia = nm.KLPhuGia,
                ID_HeaderKey = null, // Sẽ được map sau nếu có
                TenHienThi = null,
                ID_MeThoi = idMeThoi
            };
        }

        /// <summary>
        /// Tạo 1 DLNM_HRC2 entity từ HRC2_NM (chỉ lưu thông tin chính, không có phụ liệu)
        /// </summary>
        private DLNM_HRC2 CreateDLNMEntityFromNm(HRC2_NM nm, Dictionary<string, (double? CCT, double? CR)> gangLongMetricsCache, bool overwriteSlot = true)
        {
            var entity = new DLNM_HRC2
            {
                IsChuyenCa = false,
                REPORT_NO = (int)nm.REPORT_NO,
                NgaySx = nm.PRODUCTION_DATE,
                MacThep = nm.GRADE_ID_PLAN,
                O2 = nm.O2,
                AR_RH = nm.AR_RH,
                N2 = nm.N2,
                AR_BOF = nm.AR_BOF,
                AR_LF = nm.AR_LF,
                KLGangLong = nm.KLGangLong,
                KLThepPhe = nm.KLThepPhe,
                MeThoi = nm.PRODUCT_ID,
                IsNM = true
            };

            if (overwriteSlot)
            {
                entity.Ngay = nm.ShiftDate;
                entity.Ca = nm.Shift.HasValue ? (int?)nm.Shift.Value : null;
                entity.BieuMau = nm.PLANT;
                entity.Scope = nm.PLANT_NO.HasValue ? (int?)nm.PLANT_NO.Value : null;
            }

            // Lấy từ cache thay vì gọi stored procedure mỗi lần
            if (!string.IsNullOrWhiteSpace(nm.PRODUCT_ID) && gangLongMetricsCache.TryGetValue(nm.PRODUCT_ID, out var metrics))
            {
                entity.KLGangLongCCT = metrics.CCT;
                entity.KLGangLongCR = metrics.CR;
            }
            else
            {
                entity.KLGangLongCCT = null;
                entity.KLGangLongCR = null;
            }

            return entity;
        }

        /// <summary>
        /// Cập nhật DLNM_HRC2 entity từ HRC2_NM (giữ nguyên ID và các trường không thay đổi)
        /// </summary>
        private void UpdateDLNMEntityFromNm(DLNM_HRC2 entity, HRC2_NM nm, Dictionary<string, (double? CCT, double? CR)> gangLongMetricsCache, SyncFromNM_HRC2_Request request, bool isMovedToSlot)
        {
            // Cập nhật các trường từ NM data
            entity.REPORT_NO = (int)nm.REPORT_NO;
            entity.NgaySx = nm.PRODUCTION_DATE;
            entity.MacThep = nm.GRADE_ID_PLAN;
            entity.O2 = nm.O2;
            entity.AR_RH = nm.AR_RH;
            entity.N2 = nm.N2;
            entity.AR_BOF = nm.AR_BOF;
            entity.AR_LF = nm.AR_LF;
            entity.KLGangLong = nm.KLGangLong;
            entity.KLThepPhe = nm.KLThepPhe;
            entity.MeThoi = nm.PRODUCT_ID;
            entity.IsNM = true;
            
            // Cập nhật slot info
            entity.Ngay = request.NgaySX;
            entity.Ca = request.Ca;
            entity.BieuMau = request.LoaiBM;
            entity.Scope = request.Scope;
            entity.IsChuyenCa = isMovedToSlot;

            // Lấy từ cache thay vì gọi stored procedure mỗi lần
            if (!string.IsNullOrWhiteSpace(nm.PRODUCT_ID) && gangLongMetricsCache.TryGetValue(nm.PRODUCT_ID, out var metrics))
            {
                entity.KLGangLongCCT = metrics.CCT;
                entity.KLGangLongCR = metrics.CR;
            }
            else
            {
                entity.KLGangLongCCT = null;
                entity.KLGangLongCR = null;
            }
        }

        /// <summary>
        /// Cập nhật PhuLieu_HRC2 entity từ HRC2_NM (giữ nguyên ID và ID_HeaderKey, TenHienThi nếu đã có)
        /// </summary>
        private void UpdatePhuLieuEntityFromNm(PhuLieu_HRC2 entity, HRC2_NM nm, long idMeThoi)
        {
            // Cập nhật các trường từ NM data
            entity.REPORT_NO = (int)nm.REPORT_NO;
            entity.MeThoi = nm.PRODUCT_ID;
            entity.ID_PhuLieu = nm.MATERIAL_NO.HasValue ? (int?)nm.MATERIAL_NO.Value : null;
            entity.TenPhuLieu = nm.DESCRIPTION_EN;
            entity.KLPhuGia = nm.KLPhuGia;
            entity.ID_MeThoi = idMeThoi;
            
            // Giữ nguyên ID_HeaderKey và TenHienThi nếu đã có (không ghi đè mapping)
            // entity.ID_HeaderKey và entity.TenHienThi giữ nguyên giá trị hiện có
        }

        
        private async Task<double?> ExecuteGangLongStoredProcAsync(string procedureName, string meThoi)
        {
            if (string.IsNullOrWhiteSpace(meThoi))
                return null;

            await using var connection = new SqlConnection(_productDataConnStr);
            await connection.OpenAsync();

            try
            {
                using var command = new SqlCommand(procedureName, connection);
                command.CommandType = CommandType.StoredProcedure;
                command.CommandTimeout = 30;

                command.Parameters.Add(new SqlParameter("@MeThoi", SqlDbType.NVarChar, 20)
                {
                    Value = meThoi
                });

                var result = await command.ExecuteScalarAsync();

                if (result == null || result == DBNull.Value)
                    return null;

                return Convert.ToDouble(result);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Error executing {procedureName} for MeThoi '{meThoi}': {ex.Message}", ex);
            }
        }

        private static bool AreEqual(string? value1, string? value2)
        {
            return string.Equals(value1 ?? string.Empty, value2 ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        public async Task SyncHRC2FromNMAsync(SyncFromNM_HRC2_Request request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            // 1. Lấy từ NM
            var nmData = await GetFromNmAsync(request.LoaiBM, request.Scope, request.NgaySX, request.Ca);

            await SyncByReportNoAsync(nmData, request);
        }

        public async Task DeleteRowByKeyAsync(int rowKey)
        {
            var row = await _context.DLNM_HRC2s.FindAsync(rowKey);
            if (row == null)
            {
                throw new InvalidOperationException($"Row with key {rowKey} not found");
            }
            _context.DLNM_HRC2s.Remove(row);
            await _context.SaveChangesAsync();
        }
    }
}
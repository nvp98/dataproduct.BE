using dataproduct.api.DTOs;
using dataproduct.api.Models;
using dataproduct.api.Models.MasterData;
using dataproduct.api.Repositories;
using dataproduct.api.ResponseModels;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;

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
                .FromSqlRaw("EXEC sp_GetHRC2FromNM @Plant, @PlantNo, @WorkDate, @Shift", parameters)
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
                .FromSqlRaw("EXEC sp_GetHRC2ByMeThoiFromNM @Plant, @PlantNo, @MeThoi", parameters)
                .ToListAsync();
        }
        public async Task SyncByReportNoAsync(List<HRC2_NM> nmData, SyncFromNM_HRC2_Request request)
        {
            nmData ??= new List<HRC2_NM>();

            var nmLookup = nmData
                .GroupBy(x => x.PRODUCT_ID)
                .Where(g => !string.IsNullOrWhiteSpace(g.Key))
                .ToDictionary(g => g.Key!, g => g.ToList());

            // Lấy danh sách các mẻ đã chuyển đi khỏi ca/ngày hiện tại (để loại bỏ khỏi danh sách xử lý)
            var transferredAwayProductIds = await _context.DLNM_HRC2s
                .Where(x =>
                    x.IsNM == true &&
                    x.IsChuyenCa == true &&
                    x.MeThoi != null &&
                    (x.Ngay != request.NgaySX || x.Ca != request.Ca || x.Scope != request.Scope || x.BieuMau != request.LoaiBM))
                .Select(x => x.MeThoi!)
                .Distinct()
                .ToListAsync();

            var productIds = new HashSet<string>(nmLookup.Keys, StringComparer.OrdinalIgnoreCase);

            // Loại bỏ các mẻ đã chuyển đi khỏi danh sách xử lý
            foreach (var transferredId in transferredAwayProductIds)
            {
                productIds.Remove(transferredId);
            }

            // Lấy các mẻ đã chuyển đến ca/ngày hiện tại (để cập nhật riêng)
            var transferredToProductIds = await _context.DLNM_HRC2s
                .Where(x =>
                    x.IsNM == true &&
                    x.IsChuyenCa == true &&
                    x.Ngay == request.NgaySX &&
                    x.Ca == request.Ca &&
                    x.Scope == request.Scope &&
                    x.BieuMau == request.LoaiBM &&
                    x.MeThoi != null)
                .Select(x => x.MeThoi!)
                .Distinct()
                .ToListAsync();

            // Thêm các mẻ đã chuyển đến vào danh sách xử lý
            foreach (var productId in transferredToProductIds)
            {
                productIds.Add(productId);
            }

            // Cache kết quả stored procedure
            var gangLongMetricsCache =
                new Dictionary<string, (double? CCT, double? CR)>(StringComparer.OrdinalIgnoreCase);
            
            // Chỉ gọi stored procedure khi LoaiBM = "BOF"
            if (request.LoaiBM == "BOF")
            {
                // Tập hợp tất cả meThoi cần gọi stored procedure (tránh gọi trùng lặp)
                var meThoiSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                
                // Thu thập tất cả meThoi từ nmData và transferred records
                foreach (var productId in productIds)
                {
                    if (!string.IsNullOrWhiteSpace(productId))
                    {
                        meThoiSet.Add(productId);
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
           
            // Xử lý từng mẻ
            foreach (var productId in productIds)
            {
                if (string.IsNullOrWhiteSpace(productId))
                {
                    continue;
                }

                // Lấy các DLNM_HRC2 records cũ (chỉ 1 record per MeThoi trong slot)
                // Load vào memory trước rồi filter bằng AreEqual để tránh lỗi EF Core translation
                var allDLNMRecords = await _context.DLNM_HRC2s
                    .Where(x => 
                        x.IsNM == true && 
                        x.MeThoi == productId &&
                        x.Ngay == request.NgaySX &&
                        x.Ca == request.Ca &&
                        x.Scope == request.Scope)
                    .ToListAsync();
                
                var existingDLNMRecords = allDLNMRecords
                    .Where(x => AreEqual(x.BieuMau, request.LoaiBM))
                    .ToList();

                // Lấy các PhuLieu_HRC2 records cũ (nhiều records per MeThoi)
                var existingPhuLieuRecords = await _context.PhuLieu_HRC2s
                    .Where(x => 
                        x.MeThoi == productId &&
                        x.BieuMau == request.LoaiBM &&
                        x.REPORT_NO.HasValue)
                    .ToListAsync();

                // Kiểm tra xem mẻ này có đã chuyển đến ca/ngày hiện tại không
                var isMovedToSlot = existingDLNMRecords.Any(x => x.IsChuyenCa == true);

                List<HRC2_NM> nmGroup;
                if (isMovedToSlot)
                {
                    // Mẻ đã chuyển đến ca/ngày hiện tại: query riêng từ NM
                    nmGroup = await GetByMeThoiFromNmAsync(request.LoaiBM, request.Scope, productId);
                }
                else
                {
                    // Mẻ chưa chuyển đến: lấy từ nmLookup
                    if (!nmLookup.TryGetValue(productId, out var nmGroupFromLookup))
                    {
                        continue; // Không có dữ liệu trong NM
                    }
                    nmGroup = nmGroupFromLookup;
                }

                if (nmGroup == null || !nmGroup.Any())
                {
                    continue;
                }

                // Xóa các records cũ
                if (existingDLNMRecords.Any())
                {
                    _context.DLNM_HRC2s.RemoveRange(existingDLNMRecords);
                }
                if (existingPhuLieuRecords.Any())
                {
                    _context.PhuLieu_HRC2s.RemoveRange(existingPhuLieuRecords);
                }

                // Tạo 1 DLNM_HRC2 record (lấy từ record đầu tiên trong group, vì thông tin chính giống nhau)
                var firstNm = nmGroup.First();
                var dlnmEntity = CreateDLNMEntityFromNm(firstNm, gangLongMetricsCache, overwriteSlot: true);
                dlnmEntity.Ngay = request.NgaySX;
                dlnmEntity.Ca = request.Ca;
                dlnmEntity.BieuMau = request.LoaiBM;
                dlnmEntity.Scope = request.Scope;
                dlnmEntity.IsChuyenCa = isMovedToSlot;
                _context.DLNM_HRC2s.Add(dlnmEntity);

                // Tạo nhiều PhuLieu_HRC2 records (mỗi record trong group = 1 phụ liệu)
                foreach (var nm in nmGroup)
                {
                    // Chỉ tạo PhuLieu_HRC2 nếu có MATERIAL_NO (có phụ liệu)
                    if (nm.MATERIAL_NO.HasValue)
                    {
                        var phuLieuEntity = CreatePhuLieuEntityFromNm(
                            nm, 
                            request.NgaySX, 
                            request.Ca, 
                            request.LoaiBM, 
                            request.Scope, 
                            isChuyenCa: isMovedToSlot);
                        _context.PhuLieu_HRC2s.Add(phuLieuEntity);
                    }
                }
            }

            // Lưu thay đổi; bỏ qua lỗi cạnh tranh lạc quan (optimistic concurrency) trong quá trình sync
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // Trường hợp hàng dữ liệu đã bị sửa/xóa bởi tiến trình khác trong lúc sync.
                // Với nghiệp vụ đồng bộ từ NM, có thể an toàn bỏ qua và tiếp tục,
                // vì lần gọi filter tiếp theo sẽ luôn lấy trạng thái mới nhất từ NM và DLNM_HRC2.
            }
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
        /// Tạo 1 PhuLieu_HRC2 entity từ HRC2_NM (mỗi record NM = 1 phụ liệu)
        /// </summary>
        private PhuLieu_HRC2 CreatePhuLieuEntityFromNm(HRC2_NM nm, DateTime? ngay, int? ca, string? bieuMau, int? scope, bool isChuyenCa = false)
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
                TenHienThi = null // Sẽ được map sau nếu có
            };
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
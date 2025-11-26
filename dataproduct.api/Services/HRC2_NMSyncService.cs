using dataproduct.api.DTOs;
using dataproduct.api.Models;
using dataproduct.api.Repositories;
using dataproduct.api.ResponseModels;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Services
{
    public class HRC2_NMSyncService
    {
        private readonly ProductFormContext _context;
        public HRC2_NMSyncService (ProductFormContext context)
        {
            _context = context;
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

            // Xử lý từng mẻ
            foreach (var productId in productIds)
            {
                if (string.IsNullOrWhiteSpace(productId))
                {
                    continue;
                }

                var existingRecords = await _context.DLNM_HRC2s
                    .Where(x => x.IsNM == true && x.MeThoi == productId)
                    .ToListAsync();

                // Kiểm tra xem mẻ này có đã chuyển đến ca/ngày hiện tại không
                var movedRecordsInSlot = existingRecords
                    .Where(x =>
                        x.IsChuyenCa == true &&
                        x.Ngay == request.NgaySX &&
                        x.Ca == request.Ca &&
                        x.Scope == request.Scope &&
                        AreEqual(x.BieuMau, request.LoaiBM))
                    .ToList();

                if (movedRecordsInSlot.Any())
                {
                    // Mẻ đã chuyển đến ca/ngày hiện tại: query riêng từ NM và cập nhật
                    var nmByMeThoi = await GetByMeThoiFromNmAsync(request.LoaiBM,request.Scope,productId);
                    _context.DLNM_HRC2s.RemoveRange(movedRecordsInSlot);

                    foreach (var nm in nmByMeThoi)
                    {
                        var movedEntity = CreateEntityFromNm(nm);
                        movedEntity.Ngay = request.NgaySX;
                        movedEntity.Ca = request.Ca;
                        movedEntity.BieuMau = request.LoaiBM;
                        movedEntity.Scope = request.Scope;
                        movedEntity.IsChuyenCa = true;
                        _context.DLNM_HRC2s.Add(movedEntity);
                    }
                }
                else
                {
                    // Mẻ chưa chuyển đến: xử lý bình thường nếu có trong NM của ca/ngày hiện tại
                    nmLookup.TryGetValue(productId, out var nmGroup);
                    if (nmGroup != null && nmGroup.Any())
                    {
                        var sameSlotRecords = existingRecords
                            .Where(x =>
                                x.IsChuyenCa != true &&
                                x.Ngay == request.NgaySX &&
                                x.Ca == request.Ca &&
                                x.Scope == request.Scope &&
                                AreEqual(x.BieuMau, request.LoaiBM))
                            .ToList();

                        if (sameSlotRecords.Any())
                        {
                            _context.DLNM_HRC2s.RemoveRange(sameSlotRecords);
                        }

                        foreach (var nm in nmGroup)
                        {
                            var entity = CreateEntityFromNm(nm);
                            _context.DLNM_HRC2s.Add(entity);
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        private static DLNM_HRC2 CreateEntityFromNm(HRC2_NM nm)
        {
            var entity = new DLNM_HRC2
            {
                IsChuyenCa = false
            };
            ApplyNmValues(entity, nm, overwriteSlot: true);
            return entity;
        }

        private static void ApplyNmValues(DLNM_HRC2 target, HRC2_NM source, bool overwriteSlot)
        {
            int? nmMaterialId = source.MATERIAL_NO.HasValue ? (int?)source.MATERIAL_NO.Value : null;

            target.REPORT_NO = (int)source.REPORT_NO;
            target.NgaySx = source.PRODUCTION_DATE;
            target.MacThep = source.GRADE_ID_PLAN;
            target.O2 = source.O2;
            target.AR_RH = source.AR_RH;
            target.N2 = source.N2;
            target.AR_BOF = source.AR_BOF;
            target.AR_LF = source.AR_LF;
            target.ID_PhuLieu = nmMaterialId;
            target.TenPhuLieu = source.DESCRIPTION_EN;
            target.KLPhuGia = source.KLPhuGia;
            target.KLGangLong = source.KLGangLong;
            target.KLThepPhe = source.KLThepPhe;
            target.MeThoi = source.PRODUCT_ID;
            target.IsNM = true;

            if (overwriteSlot)
            {
                target.Ngay = source.ShiftDate;
                target.Ca = source.Shift.HasValue ? (int?)source.Shift.Value : null;
                target.BieuMau = source.PLANT;
                target.Scope = source.PLANT_NO.HasValue ? (int?)source.PLANT_NO.Value : null;
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
    }
}
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using dataproduct.api.DTOs;
using dataproduct.api.Models;
using dataproduct.api.ResponseModels;
using dataproduct.api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public class STD_XNT_HRC2Repository : ISTD_NXT_HRC2Repository
    {
        private readonly ProductFormContext _context;

        public STD_XNT_HRC2Repository(ProductFormContext context)
        {
            _context = context;
        }

        public async Task<STD_NXT_HRC2_UpsertResponse> UpsertAsync(STD_NXT_HRC2_UpsertDto entity)
        {
           try
           {
               var existingRecord = await _context.BmPhieus
                   .FirstOrDefaultAsync(e => e.Idphieu == entity.IdPhieu);
               if (existingRecord == null)
                   throw new Exception("Phiếu không tồn tại");

               // ========== XỬ LÝ STD_XUAT_NHAP_TON_HRC2 (Details) ==========
               // Key: (Scope, Id_HeaderKey, Id_Phieu)
               var existingDetails = await _context.STD_XUAT_NHAP_TON_HRC2s
                   .Where(x => x.Id_Phieu == entity.IdPhieu)
                   .ToListAsync();

               if (entity.Details != null && entity.Details.Any())
               {
                   foreach (var detailDto in entity.Details)
                   {
                       // Tìm record hiện có theo key (Scope, Id_HeaderKey, Id_Phieu)
                       var existingDetail = existingDetails.FirstOrDefault(x =>
                           x.Scope == detailDto.Scope &&
                           x.Id_HeaderKey == detailDto.Id_HeaderKey &&
                           x.Id_Phieu == entity.IdPhieu);

                       if (existingDetail != null)
                       {
                           // Update record hiện có
                           existingDetail.NgaySX = entity.NgaySX;
                           existingDetail.Ca = entity.Ca;
                           existingDetail.BieuMau = entity.BieuMau;
                           existingDetail.ViTri = detailDto.ViTri;
                           existingDetail.TenNguyenLieu = detailDto.TenNguyenLieu;
                           existingDetail.TonDauCa = detailDto.TonDauCa;
                           existingDetail.TuongQuanDauCa = detailDto.TuongQuanDauCa;
                           existingDetail.NhapVaoTrongCa = detailDto.NhapVaoTrongCa;
                           existingDetail.MucLieu = detailDto.MucLieu;
                           existingDetail.TheTich = detailDto.TheTich;
                           existingDetail.TyTrong = detailDto.TyTrong;
                           existingDetail.TonCuoiCa = detailDto.TonCuoiCa;
                           existingDetail.TuongQuanCuoiCa = detailDto.TuongQuanCuoiCa;
                           existingDetail.TongThucTe = detailDto.TongThucTe;
                           
                           _context.STD_XUAT_NHAP_TON_HRC2s.Update(existingDetail);
                       }
                       else
                       {
                           // Insert record mới
                           var newDetail = new STD_XUAT_NHAP_TON_HRC2
                           {
                               Id_Phieu = entity.IdPhieu,
                               NgaySX = entity.NgaySX,
                               Ca = entity.Ca,
                               ViTri = detailDto.ViTri,
                               Scope = detailDto.Scope,
                               BieuMau = entity.BieuMau,
                               Id_HeaderKey = detailDto.Id_HeaderKey,
                               TenNguyenLieu = detailDto.TenNguyenLieu,
                               TonDauCa = detailDto.TonDauCa,
                               TuongQuanDauCa = detailDto.TuongQuanDauCa,
                               NhapVaoTrongCa = detailDto.NhapVaoTrongCa,
                               MucLieu = detailDto.MucLieu,
                               TheTich = detailDto.TheTich,
                               TyTrong = detailDto.TyTrong,
                               TonCuoiCa = detailDto.TonCuoiCa,
                               TuongQuanCuoiCa = detailDto.TuongQuanCuoiCa,
                               TongThucTe = detailDto.TongThucTe
                           };
                           
                           await _context.STD_XUAT_NHAP_TON_HRC2s.AddAsync(newDetail);
                       }
                   }
               }

               // Xóa các record dư thừa trong Details (có trong DB nhưng không có trong DTO)
               var detailKeysInDto = entity.Details?
                   .Select(d => (Scope: d.Scope, Id_HeaderKey: d.Id_HeaderKey))
                   .ToHashSet() ?? new HashSet<(int Scope, int Id_HeaderKey)>();

               var detailsToDelete = existingDetails.Where(existing =>
                   !detailKeysInDto.Contains((existing.Scope, existing.Id_HeaderKey)))
                   .ToList();

               if (detailsToDelete.Any())
               {
                   _context.STD_XUAT_NHAP_TON_HRC2s.RemoveRange(detailsToDelete);
               }

               // ========== XỬ LÝ STD_NXT_TOTAL_HRC2 (Summary) ==========
               // Key: (Id_HeaderKey, Id_Phieu)
               var existingSummary = await _context.STD_NXT_TOTAL_HRC2s
                   .Where(x => x.Id_Phieu == entity.IdPhieu)
                   .ToListAsync();

               if (entity.Summary != null && entity.Summary.Any())
               {
                   foreach (var summaryDto in entity.Summary)
                   {
                       // Tìm record hiện có theo key (Id_HeaderKey, Id_Phieu)
                       var existingSum = existingSummary.FirstOrDefault(x =>
                           x.Id_HeaderKey == summaryDto.Id_HeaderKey &&
                           x.Id_Phieu == entity.IdPhieu);

                       if (existingSum != null)
                       {
                           // Update record hiện có
                           existingSum.NgaySX = entity.NgaySX;
                           existingSum.Ca = entity.Ca;
                           existingSum.TenNguyenLieu = summaryDto.TenNguyenLieu;
                           existingSum.TongTonDauCa = summaryDto.TongTonDauCa;
                           existingSum.TongTonNhapTrongCa = summaryDto.TongNhapTrongCa;
                           existingSum.TongTonCuoiCa = summaryDto.TongTonCuoiCa;
                           existingSum.TongSuDung = summaryDto.TongSuDung;
                           existingSum.TongSDTrenSoSach = summaryDto.TongSDTrenSoSach;
                           existingSum.ChenhLech = summaryDto.ChenhLech;
                           
                           _context.STD_NXT_TOTAL_HRC2s.Update(existingSum);
                       }
                       else
                       {
                           // Insert record mới
                           var newSummary = new STD_NXT_TOTAL_HRC2
                           {
                               Id_Phieu = entity.IdPhieu,
                               NgaySX = entity.NgaySX,
                               Ca = entity.Ca,
                               Id_HeaderKey = summaryDto.Id_HeaderKey,
                               TenNguyenLieu = summaryDto.TenNguyenLieu,
                               TongTonDauCa = summaryDto.TongTonDauCa,
                               TongTonNhapTrongCa = summaryDto.TongNhapTrongCa,
                               TongTonCuoiCa = summaryDto.TongTonCuoiCa,
                               TongSuDung = summaryDto.TongSuDung,
                               TongSDTrenSoSach = summaryDto.TongSDTrenSoSach,
                               ChenhLech = summaryDto.ChenhLech
                           };
                           
                           await _context.STD_NXT_TOTAL_HRC2s.AddAsync(newSummary);
                       }
                   }
               }

               // Xóa các record dư thừa trong Summary (có trong DB nhưng không có trong DTO)
               var summaryKeysInDto = entity.Summary?
                   .Select(s => s.Id_HeaderKey)
                   .ToList() ?? new List<int>();

               var summaryToDelete = existingSummary.Where(existing =>
                   !summaryKeysInDto.Contains(existing.Id_HeaderKey))
                   .ToList();

               if (summaryToDelete.Any())
               {
                   _context.STD_NXT_TOTAL_HRC2s.RemoveRange(summaryToDelete);
               }

               // ========== XỬ LÝ BmKiemKePhuLieu (Kiểm kê - Snapshot) ==========
               if (entity.KiemKe != null && entity.KiemKe.Any())
               {
                   var siloRepo = new SiloRepository(_context);
                   var siloService = new SiloService(siloRepo);

                   foreach (var kiemKeDto in entity.KiemKe)
                   {
                       // ValidateBeforeSaveAsync - kiểm tra silo có chứa phụ liệu NM và mapping còn hiệu lực
                       await siloService.ValidateBeforeSaveAsync(
                           kiemKeDto.SiloId, 
                           kiemKeDto.PhuLieuNMId, 
                           kiemKeDto.NgaySX
                       );

                       // Check trùng (Key: NgaySX, Ca, HeaderKeyId, SiloId, PhuLieuNMId, Scope)
                       var exists = await _context.BmKiemKePhuLieus
                           .AnyAsync(k => 
                               k.NgaySX.Date == kiemKeDto.NgaySX.Date &&
                               k.Ca == kiemKeDto.Ca &&
                               k.ID_HeaderKey == kiemKeDto.HeaderKeyId &&
                               k.ID_Silo == kiemKeDto.SiloId &&
                               k.ID_PhuLieuNM == kiemKeDto.PhuLieuNMId &&
                               k.Scope == kiemKeDto.Scope
                           );

                       if (exists)
                       {
                           throw new InvalidOperationException(
                               $"Đã tồn tại bản ghi kiểm kê với cùng Ngày SX, Ca, HeaderKey, Silo, Phụ liệu NM và Scope."
                           );
                       }

                       // Tạo entity BmKiemKePhuLieu
                       var kiemKe = new BmKiemKePhuLieu
                       {
                           NgaySX = kiemKeDto.NgaySX,
                           Ca = kiemKeDto.Ca,
                           Scope = kiemKeDto.Scope,
                           ID_HeaderKey = kiemKeDto.HeaderKeyId,
                           ID_Silo = kiemKeDto.SiloId,
                           ID_PhuLieuNM = kiemKeDto.PhuLieuNMId,
                           TheTich = kiemKeDto.TheTich,
                           TyTrong = kiemKeDto.TyTrong,
                           NgayTao = DateTime.Now
                       };

                       // Lưu snapshot
                       await _context.BmKiemKePhuLieus.AddAsync(kiemKe);
                   }
               }

               // Lưu thay đổi
               await _context.SaveChangesAsync();

               return new STD_NXT_HRC2_UpsertResponse
               {
                   Id_Phieu = entity.IdPhieu
               };
           }
           catch(Exception ex)
           {
               throw new Exception(ex.Message);
           }
        }
        

        public async Task InitializeHRC2_STD_NXTAsync(BmPhieu phieu)
        {
            var listUsedNXT = await _context.Header_Keys
                .Where(x => x.IsUsedNXT == true)
                .ToListAsync();

            var ngaySx = phieu.NgaySX?.ToDateTime(TimeOnly.MinValue) ?? DateTime.Now;
            var ca = phieu.Ca.Value;
            var bieuMau = phieu.MaBm;
            if(listUsedNXT.Any())
            {
                foreach (var item in listUsedNXT)
                {
                    foreach (var tohop in Enum.GetValues<ToHopSTDNXT>())
                    {
                        
                        var detail = new STD_XUAT_NHAP_TON_HRC2
                        {
                            Id_Phieu = phieu.Idphieu,
                            NgaySX = ngaySx,
                            Ca = ca,
                            Scope = (int)tohop,
                            BieuMau = bieuMau,
                            Id_HeaderKey = item.Id,
                            TenNguyenLieu = item.TenHienThi,
                            TyTrong = item.TyTrong, 
                            ViTri = 1
                        };
                        await _context.STD_XUAT_NHAP_TON_HRC2s.AddAsync(detail);
                    }

                    var summary = new STD_NXT_TOTAL_HRC2
                    {
                        Id_Phieu = phieu.Idphieu,
                        NgaySX = ngaySx,
                        Ca = ca,
                        Id_HeaderKey = item.Id,
                        TenNguyenLieu = item.TenHienThi
                    };
                    await _context.STD_NXT_TOTAL_HRC2s.AddAsync(summary);
                }
                await _context.SaveChangesAsync();

                // Khởi tạo dữ liệu cho STD_NXT_Filter_Init
                await GetHRC2FilterInitAsync(new InitXuatNhapTonHRC2Request
                {
                    NgaySX = ngaySx,
                    Ca = ca,
                    IdPhieu = phieu.Idphieu,
                    HeaderKeys = listUsedNXT.Select(x => new IdHeaderKeyModel { Id_HeaderKey = x.Id }).ToList()
                });
            }
            else
            {
                throw new Exception("Không có Header Key nào được sử dụng cho STD NXT");
            }
        }

        private static DataTable ToHeaderKeyDataTable(List<IdHeaderKeyModel> data)
        {
            var table = new DataTable();
            table.Columns.Add("Id_HeaderKey", typeof(int));

            foreach (var item in data)
            {
                table.Rows.Add(item.Id_HeaderKey);
            }

            return table;
        }

        public async Task GetHRC2FilterInitAsync(InitXuatNhapTonHRC2Request request)
        {
            var parameters = new[]
            {
                new SqlParameter("@NgaySX", SqlDbType.Date) { Value = request.NgaySX.Date },
                new SqlParameter("@Ca", SqlDbType.Int) { Value = request.Ca },
                new SqlParameter("@Id_Phieu", SqlDbType.UniqueIdentifier) { Value = request.IdPhieu },
                new SqlParameter("@ListHeaderKey", SqlDbType.Structured)
                {
                    TypeName = "dbo.TT_IdHeaderKey",
                    Value = ToHeaderKeyDataTable(request.HeaderKeys)
                }
            };

            await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.sp_Init_XuatNhapTon_HRC2 @NgaySX, @Ca, @Id_Phieu, @ListHeaderKey",
                parameters
            );
        }



        public async Task<STD_NXT_HRC2_GetDetailResponse> GetByPhieuIdAsync(Guid phieuId)
        {
            var phieu = await _context.BmPhieus.FirstOrDefaultAsync(x => x.Idphieu == phieuId);
            if (phieu == null)
            {
                throw new Exception("Phiếu không tồn tại");
            }
            var details = await _context.STD_XUAT_NHAP_TON_HRC2s.Where(x => x.Id_Phieu == phieuId).ToListAsync();
            var summary = await _context.STD_NXT_TOTAL_HRC2s.Where(x => x.Id_Phieu == phieuId).ToListAsync();
            return new STD_NXT_HRC2_GetDetailResponse
            {
                Id_Phieu = phieuId,
                BieuMau = phieu.MaBm,
                NgaySX = phieu.NgaySX?.ToDateTime(TimeOnly.MinValue) ?? DateTime.Now,
                Ca = phieu.Ca.Value,
                Details = details.Select(x => new NXTDetailResponseModel
                {
                    Scope = x.Scope,
                    Id_HeaderKey = x.Id_HeaderKey,
                    TenNguyenLieu = x.TenNguyenLieu,
                    ViTri = x.ViTri,
                    TonDauCa = x.TonDauCa,
                    TuongQuanDauCa = x.TuongQuanDauCa,
                    NhapVaoTrongCa = x.NhapVaoTrongCa,
                    MucLieu = x.MucLieu,
                    TheTich = x.TheTich,
                    TyTrong = x.TyTrong,
                    TonCuoiCa = x.TonCuoiCa,
                    TuongQuanCuoiCa = x.TuongQuanCuoiCa,
                    TongThucTe = x.TongThucTe
                }).ToList(),
                Summary = summary.Select(x => new NXTSummaryResponseModel
                {
                    Id_HeaderKey = x.Id_HeaderKey,
                    TenNguyenLieu = x.TenNguyenLieu,
                    TongTonDauCa = x.TongTonDauCa,
                    TongTonNhapTrongCa = x.TongTonNhapTrongCa,
                    TongTonCuoiCa = x.TongTonCuoiCa,
                    TongSuDung = x.TongSuDung,
                    TongSDTrenSoSach = x.TongSDTrenSoSach,
                    ChenhLech = x.ChenhLech
                }).ToList()
            };
        }

        public async Task<bool> PhanBoAsync(STD_NXT_HRC2_PhanBoDto entity)
        {
            try
            {
                // ========== BƯỚC 1: Kiểm tra dữ liệu đã được lưu chưa ==========
                var details = await _context.STD_NXT_TOTAL_HRC2s
                    .Where(x => 
                        x.Id_HeaderKey == entity.Id_HeaderKey && 
                        x.NgaySX == entity.NgaySX && 
                        x.Ca == entity.Ca)
                    .FirstOrDefaultAsync();

                if (details == null)
                {
                    throw new Exception("Không tìm thấy dữ liệu tổng hợp. Vui lòng lưu trước khi phân bổ.");
                }

                // So sánh ChenhLech (cho phép sai số nhỏ do decimal)
                var chenhLechDiff = Math.Abs((details.ChenhLech ?? 0) - entity.ChenhLech);
                if (chenhLechDiff > 0.001m) // Cho phép sai số 0.001
                {
                    throw new Exception("Chênh lệch không khớp với dữ liệu đã lưu. Vui lòng lưu lại trước khi phân bổ.");
                }

                // ========== BƯỚC 2: Lấy danh sách ID_PhuLieu từ Header_Mapping ==========
                var phuLieuIds = await _context.Header_Mappings
                    .Where(m => m.ID_HeaderKey == entity.Id_HeaderKey)
                    .Select(m => m.ID_PhuLieu)
                    .Distinct()
                    .ToListAsync();

                if (!phuLieuIds.Any())
                {
                    throw new Exception($"HeaderKey {entity.Id_HeaderKey} chưa được móc nối với phụ liệu nào.");
                }

                // ========== BƯỚC 3 & 4: Kết hợp query - Lấy các PhuLieu_HRC2 có sử dụng HeaderKey này ==========
                // Tìm các PhuLieu_HRC2 có:
                // - ID_PhuLieu trong danh sách phuLieuIds (từ Header_Mapping)
                // - ID_HeaderKey = entity.Id_HeaderKey (đảm bảo đúng mapping)
                // - ID_MeThoi thuộc các mẻ trong ngày/ca (join với DLNM_HRC2s)
                // - IsPhanBo != true (CHỈ lấy phụ liệu thực tế, không lấy phân bổ cũ)
                var phuLieuRecords = await (
                    from pl in _context.PhuLieu_HRC2s
                    join dlnm in _context.DLNM_HRC2s on pl.ID_MeThoi equals dlnm.ID
                    where phuLieuIds.Contains(pl.ID_PhuLieu ?? -1) &&
                          dlnm.Ngay == entity.NgaySX &&
                          dlnm.Ca == entity.Ca &&
                          (pl.IsPhanBo != true)
                    select pl
                ).ToListAsync();

                if (!phuLieuRecords.Any())
                {
                    throw new Exception($"Không tìm thấy mẻ nào sử dụng HeaderKey {entity.Id_HeaderKey} trong ngày {entity.NgaySX:dd/MM/yyyy} ca {entity.Ca}.");
                }

                // Lấy danh sách ID_MeThoi duy nhất (mỗi mẻ chỉ phân bổ 1 lần)
                var meThoiIds = phuLieuRecords
                    .Select(x => x.ID_MeThoi)
                    .Distinct()
                    .ToList();

                // ========== BƯỚC 5: Tính khối lượng phân bổ cho mỗi mẻ ==========
                var soMe = meThoiIds.Count;
                var klPhanBo = entity.ChenhLech / soMe; // Chia đều

                // ========== BƯỚC 6: Lấy thông tin Header_Key để lấy TenHienThi ==========
                var headerKey = await _context.Header_Keys
                    .Where(k => k.Id == entity.Id_HeaderKey)
                    .Select(k => new { k.TenHienThi })
                    .FirstOrDefaultAsync();

                var tenHienThi = headerKey?.TenHienThi ?? "Phân bổ";

                // ========== BƯỚC 7: Lấy các record phân bổ cũ để upsert ==========
                // Lấy tất cả record phân bổ cũ cho HeaderKey này trong ngày/ca
                var oldPhanBoRecords = await (
                    from pl in _context.PhuLieu_HRC2s
                    join dlnm in _context.DLNM_HRC2s on pl.ID_MeThoi equals dlnm.ID
                    where pl.ID_HeaderKey == entity.Id_HeaderKey &&
                          pl.IsPhanBo == true &&
                          dlnm.Ngay == entity.NgaySX &&
                          dlnm.Ca == entity.Ca
                    select new { PhuLieu = pl, MeThoiId = dlnm.ID }
                ).ToListAsync();

                // Tạo dictionary để lookup nhanh: key = ID_MeThoi
                var oldPhanBoLookup = oldPhanBoRecords
                    .ToDictionary(x => x.MeThoiId, x => x.PhuLieu);

                // ========== BƯỚC 8: Upsert record phân bổ cho mỗi mẻ ==========
                foreach (var meThoiId in meThoiIds)
                {
                    // Lấy DLNM_HRC2 để lấy thông tin mẻ
                    var dlnm = await _context.DLNM_HRC2s
                        .Where(x => x.ID == meThoiId)
                        .FirstOrDefaultAsync();

                    if (dlnm == null) continue;

                    // Kiểm tra xem đã có record phân bổ cho mẻ này chưa
                    if (oldPhanBoLookup.TryGetValue(meThoiId, out var existingPhanBo))
                    {
                        // UPDATE: Cập nhật khối lượng phân bổ (chênh lệch mới)
                        existingPhanBo.KLPhuGia = (double)klPhanBo;
                        existingPhanBo.TenHienThi = tenHienThi;
                        // Cập nhật các trường khác từ DLNM nếu cần
                        existingPhanBo.REPORT_NO = dlnm.REPORT_NO;
                        existingPhanBo.BieuMau = dlnm.BieuMau;
                        existingPhanBo.MeThoi = dlnm.MeThoi;
                        _context.PhuLieu_HRC2s.Update(existingPhanBo);
                    }
                    else
                    {
                        // INSERT: Tạo record phân bổ mới
                        var phuLieuPhanBo = new PhuLieu_HRC2
                        {
                            REPORT_NO = dlnm.REPORT_NO,
                            BieuMau = dlnm.BieuMau,
                            MeThoi = dlnm.MeThoi,
                            ID_PhuLieu = null, // Phân bổ không có ID_PhuLieu cụ thể
                            TenPhuLieu = null,
                            KLPhuGia = (double)klPhanBo,
                            ID_HeaderKey = entity.Id_HeaderKey,
                            TenHienThi = tenHienThi,
                            ID_MeThoi = meThoiId,
                            IsPhanBo = true // ⭐ Đánh dấu là phân bổ
                        };
                        _context.PhuLieu_HRC2s.Add(phuLieuPhanBo);
                    }
                }

                // ========== BƯỚC 9: Xóa các record phân bổ cũ không còn trong danh sách mới ==========
                // (Các mẻ đã bị xóa hoặc không còn sử dụng HeaderKey này)
                var meThoiIdsSet = meThoiIds.ToHashSet();
                var phanBoToDelete = oldPhanBoRecords
                    .Where(x => !meThoiIdsSet.Contains(x.MeThoiId))
                    .Select(x => x.PhuLieu)
                    .ToList();

                if (phanBoToDelete.Any())
                {
                    _context.PhuLieu_HRC2s.RemoveRange(phanBoToDelete);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task SaveKiemKeAsync(SaveKiemKeRequest request)
        {
            try
            {
                // ValidateBeforeSaveAsync - sử dụng SiloService
                var siloRepo = new SiloRepository(_context);
                var siloService = new SiloService(siloRepo);
                await siloService.ValidateBeforeSaveAsync(request.SiloId, request.PhuLieuNMId, request.NgaySX);

                // Check trùng (BmKiemKePhuLieuRepository.Exists)
                // Key: (NgaySX, Ca, HeaderKeyId, SiloId, PhuLieuNMId, Scope)
                var exists = await _context.BmKiemKePhuLieus
                    .AnyAsync(k => 
                        k.NgaySX.Date == request.NgaySX.Date &&
                        k.Ca == request.Ca &&
                        k.ID_HeaderKey == request.HeaderKeyId &&
                        k.ID_Silo == request.SiloId &&
                        k.ID_PhuLieuNM == request.PhuLieuNMId &&
                        k.Scope == request.Scope
                    );

                if (exists)
                {
                    throw new InvalidOperationException(
                        $"Đã tồn tại bản ghi kiểm kê với cùng Ngày SX, Ca, HeaderKey, Silo, Phụ liệu NM và Scope."
                    );
                }

                // Tạo entity BmKiemKePhuLieu
                var kiemKe = new BmKiemKePhuLieu
                {
                    NgaySX = request.NgaySX,
                    Ca = request.Ca,
                    Scope = request.Scope,
                    ID_HeaderKey = request.HeaderKeyId,
                    ID_Silo = request.SiloId,
                    ID_PhuLieuNM = request.PhuLieuNMId,
                    TheTich = request.TheTich,
                    TyTrong = request.TyTrong,
                    NgayTao = DateTime.Now
                };

                // Lưu snapshot
                await _context.BmKiemKePhuLieus.AddAsync(kiemKe);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using dataproduct.api.DTOs;
using dataproduct.api.Models;
using dataproduct.api.ResponseModels;
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

            var listUsedNXT  = await _context.Header_Keys.Where(x => x.IsUsedNXT == true).ToListAsync();

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
                // var details = await _context.STD_NXT_TOTAL_HRC2s
                //     .Where(x => x.Id_HeaderKey == entity.Id_HeaderKey && x.NgaySX == entity.NgaySX && x.Ca == entity.Ca)
                //     .FirstOrDefaultAsync();
                // if (details == null || details.ChenhLech != entity.ChenhLech)
                // {
                //     throw new Exception("Vui lòng lưu trước khi phân bổ");
                // }
                // var listMeThoi = await _context.DLNM_HRC2s.Where(x => x.Ngay == entity.NgaySX && x.Ca == entity.Ca).ToListAsync();
                // if (listMeThoi.Any())
                // {
                //     // tìm 
                // }
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}

using dataproduct.api.Models;
using Microsoft.EntityFrameworkCore;
using System;
using Microsoft.Data.SqlClient;
using dataproduct.api.DTOs.CTD_Dto;

namespace dataproduct.api.Repositories
{
    public class CtdBMDucCTDRepository : ICtdBMDucCTDRepository
    {
        private readonly ProductFormContext _context;

        public CtdBMDucCTDRepository(ProductFormContext context)
        {
            _context = context;
        }
        public async Task<List<SanLuongPhoiDto>> GetSanLuongPhoiAsync( string ca,
        string kip,
        DateTime ngaySX)
        {
            return await _context.Database
             .SqlQueryRaw<SanLuongPhoiDto>(
                 "EXEC sp_CTD_GetSanLuongPhoi_ByKipNgay @p_Kip, @p_NgaySX, @p_Ca",
                 new SqlParameter("@p_Kip", kip),
                 new SqlParameter("@p_NgaySX", ngaySX),
                 new SqlParameter("@p_Ca", ca)
             )
         .ToListAsync();
        }
        public async Task<List<PhoinhapkhoDto>> GetPhoiNhapKhoAsync(string ca,string kip,DateTime ngaySX, int mayduc)
        {
            return await _context.Database
             .SqlQueryRaw<PhoinhapkhoDto>(
                 "EXEC sp_CTD_GetPhoiNhapKho_ByKipNgay @p_Kip, @p_NgaySX, @p_Ca,@p_MayDuc",
                 new SqlParameter("@p_Kip", kip),
                 new SqlParameter("@p_NgaySX", ngaySX),
                 new SqlParameter("@p_Ca", ca),
                 new SqlParameter("@p_MayDuc", mayduc)
             )
         .ToListAsync();
        }

        public Task<List<SanLuongPhoiDto>> GetSanLuongPhoiAsync(int ca, string kip, DateTime ngaySX, int? mayDuc = null, Guid? idPhieu = null)
        {
            throw new NotImplementedException();
        }

        public async Task<List<BM_SanLuongPhoi>> InsertSanLuongPhoiAsync(List<BM_SanLuongPhoi> entity)
        {
            await _context.BM_SanLuongPhoi.AddRangeAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }
        public async Task DeleteByPhieu(Guid idPhieu)
        {
            await _context.BM_SanLuongPhoi
                .Where(x => x.IdPhieu == idPhieu)
                .ExecuteDeleteAsync();
        }

        public Task DeleteByPhieuAsync(Guid idPhieu)
        {
            throw new NotImplementedException();
        }

        //public async Task<List<SanLuongPhoiDto>> GetSanLuongPhoiPDFAsync(int? ca, string kip, DateTime ngaySX, int? mayDuc = null, Guid? idPhieu = null)
        //{
        //    var query =  _context.BM_SanLuongPhoi.AsNoTracking()
        //            .Where(x => x.NgaySX == ngaySX
        //                    && x.Ca == ca
        //                    && x.Kip == kip);
        //}

        //public Task<List<SanLuongPhoiDto>> GetSanLuongPhoiAsync(int ca, string kip, DateTime ngaySX, int? mayDuc = null, Guid? idPhieu = null)
        //{
        //    throw new NotImplementedException();
        //}
    }
}
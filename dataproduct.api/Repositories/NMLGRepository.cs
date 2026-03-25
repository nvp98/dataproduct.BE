using dataproduct.api.DTOs.CTD_Dto;
using dataproduct.api.DTOs.NMLG_Dto;
using dataproduct.api.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace dataproduct.api.Repositories
{
    public class NMLGRepository : INMLGRepository
    {
        private readonly ProductFormContext _context;

        public NMLGRepository(ProductFormContext context)
        {
            _context = context;
        }
        public async Task<List<SiLoTonDto>> GetSiLoTon(int? idLoCao, int? idCa, DateTime? ngay)
        {
            return await _context.Database
                .SqlQueryRaw<SiLoTonDto>(
                    "EXEC sp_GetSiLoTon @IDLoCao, @IDCa, @Ngay",
                    new SqlParameter("@IDLoCao", (object?)idLoCao ?? DBNull.Value),
                    new SqlParameter("@IDCa", (object?)idCa ?? DBNull.Value),
                    new SqlParameter("@Ngay", (object?)ngay ?? DBNull.Value)
                )
                .ToListAsync();
        }
      
        public async Task<List<SiLo_LG>> GetSiLoWithLoCaoAsync(int? idLoCao)
        {
           return await _context.SiLo_LG
                .Where(x => idLoCao == null || x.ID_LoCao == idLoCao)
                .OrderBy(x=> x.ThuTu).ToListAsync();
        }

        public async Task<SiLo_LG> AddSiLoAsync(SiLo_LG entity)
        {
            await _context.SiLo_LG.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<SiLo_LG?> UpdateSiLoAsync(int id, SiLo_LG entity)
        {
            var existing = await _context.SiLo_LG.FindAsync(id);
            if (existing == null) return null;

            existing.ID_LoCao = entity.ID_LoCao;
            existing.TenSiLo = entity.TenSiLo;
            existing.ThuTu = entity.ThuTu;
            existing.TenNL = entity.TenNL;
            existing.TenNL_DieuChinh = entity.TenNL_DieuChinh;

            await _context.SaveChangesAsync();
            return existing;
        }
    }
}

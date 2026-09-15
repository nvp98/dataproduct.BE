using dataproduct.api.DTOs.NMLG_Dto;
using dataproduct.api.Models;
using dataproduct.api.Models.MasterData;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public interface IPhieuDieuChinhRepository
    {
        Task<List<BBGNChiTietResult>> GetBBGNAsync(DateTime ngay, int? ca, string? kip);

        Task<List<LG_PhieuDieuChinh_ChiTiet>> GetChiTietByPhieuAsync(Guid idPhieu);
        Task ReplaceChiTietAsync(Guid idPhieu, List<LG_PhieuDieuChinh_ChiTiet> entities);
    }

    // Đọc dữ liệu BBGN (Tbl_BienBanGiaoNhan/Tbl_ChiTiet_BienBanGiaoNhan/Tbl_VatTu, PRODUCTDATA)
    // qua stored procedure dbo.SP_Get_BBGN — nguồn dữ liệu cho Phiếu điều chỉnh số liệu NM.LG.
    public class PhieuDieuChinhRepository : IPhieuDieuChinhRepository
    {
        private readonly ProductDataMasterDbContext _context;

        public PhieuDieuChinhRepository(ProductDataMasterDbContext context)
        {
            _context = context;
        }

        public async Task<List<BBGNChiTietResult>> GetBBGNAsync(DateTime ngay, int? ca, string? kip)
        {
            return await _context.BBGNChiTietResults
                .FromSqlRaw(
                    "EXEC dbo.SP_Get_BBGN @p_ngay, @p_ca, @p_kip",
                    new SqlParameter("@p_ngay", ngay.Date),
                    new SqlParameter("@p_ca", (object?)ca ?? DBNull.Value),
                    new SqlParameter("@p_kip", (object?)kip ?? DBNull.Value))
                .AsNoTracking()
                .ToListAsync();
        }

        // ─── Chi tiết Phiếu điều chỉnh (LG_PhieuDieuChinh_ChiTiet) ────────────

        public async Task<List<LG_PhieuDieuChinh_ChiTiet>> GetChiTietByPhieuAsync(Guid idPhieu)
        {
            return await _context.LG_PhieuDieuChinh_ChiTiet
                .Where(x => x.IDPhieu == idPhieu && !x.IsDelete)
                .OrderBy(x => x.ThuTu)
                .ThenBy(x => x.ID)
                .AsNoTracking()
                .ToListAsync();
        }

        // Xóa + ghi mới trong cùng 1 transaction — tránh mất trắng chi tiết của phiếu
        // nếu insert lỗi giữa chừng (xem PhieuDieuChinhRepository tương tự LGNLRepository).
        public async Task ReplaceChiTietAsync(Guid idPhieu, List<LG_PhieuDieuChinh_ChiTiet> entities)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.LG_PhieuDieuChinh_ChiTiet
                    .Where(x => x.IDPhieu == idPhieu)
                    .ExecuteDeleteAsync();

                if (entities.Count > 0)
                {
                    await _context.LG_PhieuDieuChinh_ChiTiet.AddRangeAsync(entities);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}

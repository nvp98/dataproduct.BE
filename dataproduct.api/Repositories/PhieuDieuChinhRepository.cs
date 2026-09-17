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

        // ─── Danh mục NVL (LG_PhieuDieuChinh_NVL) ─────────────────────────────
        Task<List<LG_PhieuDieuChinh_NVL>> GetNvlListAsync(bool onlyActive);
        Task<LG_PhieuDieuChinh_NVL?> GetNvlByIdAsync(int id);
        Task<LG_PhieuDieuChinh_NVL> AddNvlAsync(LG_PhieuDieuChinh_NVL entity);
        Task<LG_PhieuDieuChinh_NVL?> UpdateNvlAsync(int id, LG_PhieuDieuChinh_NVL entity);
        Task<bool> DeleteNvlAsync(int id);
    }

    // GetBBGNAsync đọc từ PRODUCTDATA (Tbl_BienBanGiaoNhan/Tbl_ChiTiet_BienBanGiaoNhan/Tbl_VatTu)
    // qua stored procedure dbo.SP_Get_BBGN — nguồn dữ liệu cho Phiếu điều chỉnh số liệu NM.LG.
    // Chi tiết đã lưu (LG_PhieuDieuChinh_ChiTiet) lại nằm ở DB PRODUCT_FORM — khác DB với BBGN —
    // nên repository này cần cả 2 DbContext.
    public class PhieuDieuChinhRepository : IPhieuDieuChinhRepository
    {
        private readonly ProductDataMasterDbContext _context;
        private readonly ProductFormContext _formContext;

        public PhieuDieuChinhRepository(ProductDataMasterDbContext context, ProductFormContext formContext)
        {
            _context = context;
            _formContext = formContext;
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
            return await _formContext.LG_PhieuDieuChinh_ChiTiet
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
            await using var transaction = await _formContext.Database.BeginTransactionAsync();
            try
            {
                await _formContext.LG_PhieuDieuChinh_ChiTiet
                    .Where(x => x.IDPhieu == idPhieu)
                    .ExecuteDeleteAsync();

                if (entities.Count > 0)
                {
                    await _formContext.LG_PhieuDieuChinh_ChiTiet.AddRangeAsync(entities);
                    await _formContext.SaveChangesAsync();
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ─── Danh mục NVL (LG_PhieuDieuChinh_NVL, PRODUCTDATA) ─────────────────

        public async Task<List<LG_PhieuDieuChinh_NVL>> GetNvlListAsync(bool onlyActive)
        {
            return await _context.LG_PhieuDieuChinh_NVL
                .Where(x => !onlyActive || x.IsActive)
                .OrderBy(x => x.TenNVL)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<LG_PhieuDieuChinh_NVL?> GetNvlByIdAsync(int id)
            => await _context.LG_PhieuDieuChinh_NVL.FindAsync(id);

        public async Task<LG_PhieuDieuChinh_NVL> AddNvlAsync(LG_PhieuDieuChinh_NVL entity)
        {
            await _context.LG_PhieuDieuChinh_NVL.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<LG_PhieuDieuChinh_NVL?> UpdateNvlAsync(int id, LG_PhieuDieuChinh_NVL entity)
        {
            var existing = await _context.LG_PhieuDieuChinh_NVL.FindAsync(id);
            if (existing == null) return null;

            existing.TenNVL = entity.TenNVL;
            existing.IsActive = entity.IsActive;

            await _context.SaveChangesAsync();
            return existing;
        }

        // Xóa mềm — set IsActive=false thay vì xóa hẳn, để không phá dữ liệu chi tiết
        // đã lưu (TenNVL trong LG_PhieuDieuChinh_ChiTiet lưu text, không phải FK cứng).
        public async Task<bool> DeleteNvlAsync(int id)
        {
            var existing = await _context.LG_PhieuDieuChinh_NVL.FindAsync(id);
            if (existing == null) return false;
            existing.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}

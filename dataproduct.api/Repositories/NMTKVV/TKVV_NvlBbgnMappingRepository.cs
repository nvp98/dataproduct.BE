using dataproduct.api.DTOs.NMTKVV_Dto;
using dataproduct.api.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories.NMTKVV
{
    public class TKVV_NvlBbgnMappingRepository : ITKVV_NvlBbgnMappingRepository
    {
        private readonly ProductFormContext _context;

        public TKVV_NvlBbgnMappingRepository(ProductFormContext context)
        {
            _context = context;
        }

        // TenVatTu/MaVatTuSap/... được TKVV_NvlBbgnMappingService enrich thêm sau,
        // vì Vật tư nằm ở PRODUCTDATA — DbContext khác, không join được ở đây.
        public async Task<List<TKVVNvlBbgnMappingDto>> GetListAsync(int? tkvvNvlId)
        {
            var query = from m in _context.TKVV_NVL_BBGN_Mapping
                        join nvl in _context.TKVV_NguyenVatLieu on m.TKVV_NVL_ID equals nvl.ID into nvlG
                        from nvl in nvlG.DefaultIfEmpty()
                        where tkvvNvlId == null || m.TKVV_NVL_ID == tkvvNvlId
                        orderby m.NgayTao descending
                        select new TKVVNvlBbgnMappingDto
                        {
                            Id = m.ID,
                            TkvvNvlId = m.TKVV_NVL_ID,
                            TenNVL = nvl != null ? nvl.TenNVL : null,
                            IdVatTuBBGN = m.ID_VatTu_BBGN,
                            TrangThai = m.TrangThai,
                            GhiChu = m.GhiChu,
                            NgayTao = m.NgayTao,
                        };
            return await query.AsNoTracking().ToListAsync();
        }

        public async Task<TKVV_NVL_BBGN_Mapping?> GetByIdAsync(int id)
            => await _context.TKVV_NVL_BBGN_Mapping.FindAsync(id);

        public async Task<TKVV_NVL_BBGN_Mapping> AddAsync(TKVV_NVL_BBGN_Mapping entity)
        {
            entity.NgayTao = DateTime.Now;
            _context.TKVV_NVL_BBGN_Mapping.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<TKVV_NVL_BBGN_Mapping?> UpdateAsync(int id, bool trangThai, string? ghiChu)
        {
            var existing = await _context.TKVV_NVL_BBGN_Mapping.FindAsync(id);
            if (existing == null) return null;

            existing.TrangThai = trangThai;
            existing.GhiChu = ghiChu;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.TKVV_NVL_BBGN_Mapping.FindAsync(id);
            if (entity == null) return false;
            _context.TKVV_NVL_BBGN_Mapping.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        // SP tự join TKVV_NVL_BBGN_Mapping + chi tiết BBGN (PRODUCTDATA, qua linked server)
        // nên dùng raw ADO.NET thay vì EF FromSqlRaw, cùng pattern với
        // TKVV_BCSL_ChiPhiRepository.GetDuLieuCanAsync.
        public async Task<List<TKVVNvlBbgnDataDto>> GetNvlBbgnDataAsync(DateTime ngay, int ca, int tkvvNvlId, int scope)
        {
            var result = new List<TKVVNvlBbgnDataDto>();

            var conn = _context.Database.GetDbConnection();
            var wasOpen = conn.State == System.Data.ConnectionState.Open;
            if (!wasOpen) await conn.OpenAsync();

            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "dbo.sp_TKVV_Get_NVL_BBGN";
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.CommandTimeout = 30;
                cmd.Parameters.Add(new SqlParameter("@Ngay", ngay.Date));
                cmd.Parameters.Add(new SqlParameter("@Ca", ca.ToString()));
                cmd.Parameters.Add(new SqlParameter("@TKVV_NVL_ID", tkvvNvlId));
                cmd.Parameters.Add(new SqlParameter("@Scope", scope));

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Add(new TKVVNvlBbgnDataDto
                    {
                        MappingId = reader["Mapping_ID"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Mapping_ID"]),
                        TkvvNvlId = reader["TKVV_NVL_ID"] == DBNull.Value ? 0 : Convert.ToInt32(reader["TKVV_NVL_ID"]),
                        IdVatTuBBGN = reader["ID_VatTu_BBGN"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ID_VatTu_BBGN"]),
                        MappingTrangThai = reader["Mapping_TrangThai"] != DBNull.Value && Convert.ToInt32(reader["Mapping_TrangThai"]) == 1,
                        Kip = reader["Kip"] == DBNull.Value ? null : reader["Kip"].ToString(),
                        Ca = reader["Ca"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Ca"]),
                        IdXuongBG = reader["ID_Xuong_BG"] == DBNull.Value ? null : Convert.ToInt32(reader["ID_Xuong_BG"]),
                        IdXuongBN = reader["ID_Xuong_BN"] == DBNull.Value ? null : Convert.ToInt32(reader["ID_Xuong_BN"]),
                        IdCtBBGN = reader["ID_CT_BBGN"] == DBNull.Value ? 0 : Convert.ToInt64(reader["ID_CT_BBGN"]),
                        IdVatTu = reader["ID_VatTu"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ID_VatTu"]),
                        MaLo = reader["MaLO"] == DBNull.Value ? null : reader["MaLO"].ToString(),
                        DoAmW = reader["DoAm_W"] == DBNull.Value ? null : Convert.ToDecimal(reader["DoAm_W"]),
                        KhoiLuongBG = reader["KhoiLuong_BG"] == DBNull.Value ? null : Convert.ToDecimal(reader["KhoiLuong_BG"]),
                        KLQuyKhoBG = reader["KL_QuyKho_BG"] == DBNull.Value ? null : Convert.ToDecimal(reader["KL_QuyKho_BG"]),
                        KhoiLuongBN = reader["KhoiLuong_BN"] == DBNull.Value ? null : Convert.ToDecimal(reader["KhoiLuong_BN"]),
                        KLQuyKhoBN = reader["KL_QuyKho_BN"] == DBNull.Value ? null : Convert.ToDecimal(reader["KL_QuyKho_BN"]),
                        BBGNGhiChu = reader["BBGN_GhiChu"] == DBNull.Value ? null : reader["BBGN_GhiChu"].ToString(),
                    });
                }
            }
            finally
            {
                if (!wasOpen) await conn.CloseAsync();
            }

            return result;
        }
    }
}

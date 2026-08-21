using dataproduct.api.DTOs.NMTKVV_Dto;
using dataproduct.api.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories.NMTKVV
{
    public class TKVV_BCSL_ChiPhiRepository : ITKVV_BCSL_ChiPhiRepository
    {
        private readonly ProductFormContext _context;

        public TKVV_BCSL_ChiPhiRepository(ProductFormContext context)
        {
            _context = context;
        }

        private static readonly Dictionary<int, string> ScopeCodeMap = new()
        {
            { 1, "TK1" }, { 2, "TK2" }, { 3, "TK3" }, { 4, "TK4" },
            { 5, "VV1" }, { 6, "VV2" },
        };

        public static string ResolveScopeCode(int scope)
            => ScopeCodeMap.TryGetValue(scope, out var code) ? code : scope.ToString();

        // SP join qua linked server [SQL_OT].EMS_DATA_CAN nên dùng raw ADO.NET
        // thay vì EF FromSqlRaw (không composable qua linked server).
        public async Task<List<TKVVGiaTriNVLAutoDto>> GetGiaTriNVLAutoAsync(
            DateTime ngay, int ca, string scopeCode, string maBM)
        {
            var result = new List<TKVVGiaTriNVLAutoDto>();

            var conn = _context.Database.GetDbConnection();
            var wasOpen = conn.State == System.Data.ConnectionState.Open;
            if (!wasOpen) await conn.OpenAsync();

            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "dbo.SP_TKVV_GetGiaTriNVL_Auto";
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.CommandTimeout = 30;
                cmd.Parameters.Add(new SqlParameter("@Ngay", ngay.Date));
                cmd.Parameters.Add(new SqlParameter("@Ca", ca));
                cmd.Parameters.Add(new SqlParameter("@Scope", scopeCode));
                cmd.Parameters.Add(new SqlParameter("@MaBM", maBM));

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Add(new TKVVGiaTriNVLAutoDto
                    {
                        NguyenVatLieuID = reader["NguyenVatLieuID"] == DBNull.Value ? 0 : Convert.ToInt32(reader["NguyenVatLieuID"]),
                        MaBM            = reader["MaBM"]?.ToString() ?? string.Empty,
                        TenNVL          = reader["TenNVL"]?.ToString() ?? string.Empty,
                        DonViTinh       = reader["DonViTinh"] == DBNull.Value ? null : reader["DonViTinh"].ToString(),
                        ThuTu           = reader["ThuTu"] == DBNull.Value ? null : Convert.ToInt32(reader["ThuTu"]),
                        Scope           = reader["Scope"] == DBNull.Value ? null : reader["Scope"].ToString(),
                        TenScope        = reader["TenScope"] == DBNull.Value ? null : reader["TenScope"].ToString(),
                        GiaTri          = reader["GiaTri"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["GiaTri"]),
                        SoLuongTag      = reader["SoLuongTag"] == DBNull.Value ? 0 : Convert.ToInt32(reader["SoLuongTag"]),
                        ThoiGianTu      = reader["ThoiGianTu"] == DBNull.Value ? null : Convert.ToDateTime(reader["ThoiGianTu"]),
                        ThoiGianDen     = reader["ThoiGianDen"] == DBNull.Value ? null : Convert.ToDateTime(reader["ThoiGianDen"]),
                    });
                }
            }
            finally
            {
                if (!wasOpen) await conn.CloseAsync();
            }

            return result;
        }

        public async Task<List<TKVVDuLieuCanDto>> GetDuLieuCanAsync(
            DateTime ngay, int ca, string maBM, string loaiDuLieu, int scope)
        {
            var result = new List<TKVVDuLieuCanDto>();

            var conn = _context.Database.GetDbConnection();
            var wasOpen = conn.State == System.Data.ConnectionState.Open;
            if (!wasOpen) await conn.OpenAsync();

            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "dbo.SP_TKVV_GetDuLieuCan";
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.CommandTimeout = 30;
                cmd.Parameters.Add(new SqlParameter("@Ngay", ngay.Date));
                cmd.Parameters.Add(new SqlParameter("@Ca", ca));
                cmd.Parameters.Add(new SqlParameter("@MaBM", maBM));
                cmd.Parameters.Add(new SqlParameter("@LoaiDuLieu", loaiDuLieu));
                cmd.Parameters.Add(new SqlParameter("@Scope", scope));

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Add(new TKVVDuLieuCanDto
                    {
                        Ngay             = ngay.Date,
                        Ca               = ca,
                        MaBM             = maBM,
                        Scope            = reader["Scope"] == DBNull.Value ? null : reader["Scope"].ToString(),
                        Xuong            = reader["Xuong"] == DBNull.Value ? null : reader["Xuong"].ToString(),
                        SiloID           = reader["SiloID"] == DBNull.Value ? null : Convert.ToInt32(reader["SiloID"]),
                        MaSilo           = reader["MaSilo"] == DBNull.Value ? null : reader["MaSilo"].ToString(),
                        NguyenVatLieuID  = reader["NguyenVatLieuID"] == DBNull.Value ? 0 : Convert.ToInt32(reader["NguyenVatLieuID"]),
                        TenNVL           = reader["TenNVL"]?.ToString() ?? string.Empty,
                        DonViTinh        = reader["DonViTinh"] == DBNull.Value ? null : reader["DonViTinh"].ToString(),
                        GiaTri           = reader["GiaTri"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["GiaTri"]),
                        SoLuongSilo      = reader["SoLuongSilo"] == DBNull.Value ? 0 : Convert.ToInt32(reader["SoLuongSilo"]),
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

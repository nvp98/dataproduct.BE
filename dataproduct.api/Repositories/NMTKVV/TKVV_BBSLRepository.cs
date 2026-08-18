using dataproduct.api.DTOs.NMTKVV_Dto;
using dataproduct.api.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public class TKVV_BBSLRepository : ITKVV_BBSLRepository
    {
        private readonly ProductFormContext _context;

        public TKVV_BBSLRepository(ProductFormContext context)
        {
            _context = context;
        }

        
        private static readonly Dictionary<int, string> ScopeCodeMap = new()
        {
            { 1, "TK1" },
            { 2, "TK2" },
            { 3, "TK3" },
            { 4, "TK4" },
            { 5, "VV1" },
            { 6, "VV2" },
        };
 
        public static string ResolveScopeCode(int scope)
            => ScopeCodeMap.TryGetValue(scope, out var code) ? code : scope.ToString();

        public static int? ResolveScopeNumber(string? scope)
        {
            if (string.IsNullOrWhiteSpace(scope)) return null;
            if (int.TryParse(scope.Trim(), out var numericScope)) return numericScope;

            var normalized = scope.Trim();
            foreach (var pair in ScopeCodeMap)
            {
                if (string.Equals(pair.Value, normalized, StringComparison.OrdinalIgnoreCase))
                    return pair.Key;
            }

            return null;
        }

        // ─── Danh mục NVL ────────────────────────────────────────────────────

        public async Task<List<TKVVNguyenVatLieuDto>> GetNvlListAsync(string? maBM, string? scope)
        {
            var scopeCode = string.IsNullOrWhiteSpace(scope)
                ? null
                : (int.TryParse(scope.Trim(), out var numericScope) ? ResolveScopeCode(numericScope) : scope.Trim());

            return await _context.TKVV_NguyenVatLieu
                .Where(x => (maBM == null || x.MaBM == maBM)
                         && (scopeCode == null || x.Scope == scopeCode))
                .OrderBy(x => x.ThuTu)
                .Select(x => new TKVVNguyenVatLieuDto
                {
                    Id = x.ID,
                    MaBM = x.MaBM,
                    TenNVL = x.TenNVL,
                    DonViTinh = x.DonViTinh,
                    ThuTu = x.ThuTu,
                    TrangThai = x.TrangThai,
                    GhiChu = x.GhiChu,
                    Scope = ResolveScopeNumber(x.Scope),
                    TenScope = x.TenScope ?? x.Scope,
                })
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<TKVV_NguyenVatLieu?> GetNvlByIdAsync(int id)
            => await _context.TKVV_NguyenVatLieu.FindAsync(id);

        public async Task<TKVV_NguyenVatLieu> AddNvlAsync(TKVV_NguyenVatLieu entity)
        {
            await _context.TKVV_NguyenVatLieu.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<TKVV_NguyenVatLieu?> UpdateNvlAsync(int id, TKVV_NguyenVatLieu entity)
        {
            var existing = await _context.TKVV_NguyenVatLieu.FindAsync(id);
            if (existing == null) return null;

            existing.MaBM = entity.MaBM;
            existing.TenNVL = entity.TenNVL;
            existing.DonViTinh = entity.DonViTinh;
            existing.ThuTu = entity.ThuTu;
            existing.TrangThai = entity.TrangThai;
            existing.GhiChu = entity.GhiChu;
            existing.Scope = entity.Scope;
            existing.TenScope = entity.TenScope;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteNvlAsync(int id)
        {
            var existing = await _context.TKVV_NguyenVatLieu.FindAsync(id);
            if (existing == null) return false;
            existing.TrangThai = false;
            await _context.SaveChangesAsync();
            return true;
        }

        // ─── Mapping NVL ↔ Tag EMS + Ca ──────────────────────────────────────

        public async Task<List<TKVVMappingDto>> GetMappingListAsync()
        {
            return await (
                from m in _context.TKVV_NVL_TagMapping
                join n in _context.TKVV_NguyenVatLieu on m.NguyenVatLieuID equals n.ID
                orderby n.Scope, m.Ca, n.ThuTu
                select new TKVVMappingDto
                {
                    Id = m.ID,
                    NguyenVatLieuID = m.NguyenVatLieuID,
                    TenNVL = n.TenNVL,
                    ScopeNVL = n.Scope,
                    TagIDEMS = m.TagIDEMS,
                    Ca = m.Ca,
                    TrangThai = m.TrangThai,
                    GhiChu = m.GhiChu,
                    NgayCapNhat = m.NgayCapNhat,
                }
            ).AsNoTracking().ToListAsync();
        }

        public async Task<TKVV_NVL_TagMapping?> GetMappingByIdAsync(int id)
            => await _context.TKVV_NVL_TagMapping.FindAsync(id);

        public async Task<TKVV_NVL_TagMapping> AddMappingAsync(TKVV_NVL_TagMapping entity)
        {
            entity.NgayCapNhat = DateTime.Now;
            await _context.TKVV_NVL_TagMapping.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<TKVV_NVL_TagMapping?> UpdateMappingAsync(int id, TKVV_NVL_TagMapping entity)
        {
            var existing = await _context.TKVV_NVL_TagMapping.FindAsync(id);
            if (existing == null) return null;

            existing.NguyenVatLieuID = entity.NguyenVatLieuID;
            existing.TagIDEMS = entity.TagIDEMS;
            existing.Ca = entity.Ca;
            existing.GhiChu = entity.GhiChu;
            existing.TrangThai = entity.TrangThai;
            existing.NgayCapNhat = DateTime.Now;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteMappingAsync(int id)
        {
            var existing = await _context.TKVV_NVL_TagMapping.FindAsync(id);
            if (existing == null) return false;
            existing.TrangThai = false;
            existing.NgayCapNhat = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        // ─── Danh sách Tag PLC từ EMS (dbo.EMS_GetMappingTag) ───────────────────
        // Đọc EMS_MAPPING_TAG qua linked server — chỉ để hiển thị cho admin chọn
        // khi tạo Mapping, không lưu lại ở PRODUCT_FORM.
        // Dùng raw ADO.NET thay vì FromSqlRaw vì linked server SP không composable
        // và type cột TagIDEMS có thể không khớp với long? khi EF Core map.
        public async Task<List<EMS_MappingTag>> GetEmsTagListAsync(string? xuong, string? tagName)
        {
            var result = new List<EMS_MappingTag>();

            var conn = _context.Database.GetDbConnection();
            var wasOpen = conn.State == System.Data.ConnectionState.Open;
            if (!wasOpen) await conn.OpenAsync();

            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "dbo.EMS_GetMappingTag";
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.CommandTimeout = 30;

                var pXuong = new SqlParameter("@Xuong", (object?)xuong ?? DBNull.Value);
                var pTagID = new SqlParameter("@TagIDEMS", DBNull.Value);
                var pTagName = new SqlParameter("@TagName", (object?)tagName ?? DBNull.Value);
                cmd.Parameters.Add(pXuong);
                cmd.Parameters.Add(pTagID);
                cmd.Parameters.Add(pTagName);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Add(new EMS_MappingTag
                    {
                        ID           = reader["ID"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ID"]),
                        Xuong        = reader["Xuong"] == DBNull.Value ? null : reader["Xuong"].ToString(),
                        Loai         = reader["Loai"] == DBNull.Value ? null : reader["Loai"].ToString(),
                        TenNVL       = reader["TenNVL"] == DBNull.Value ? null : reader["TenNVL"].ToString(),
                        TenCan       = reader["TenCan"] == DBNull.Value ? null : reader["TenCan"].ToString(),
                        MaCan        = reader["MaCan"] == DBNull.Value ? null : reader["MaCan"].ToString(),
                        AddressPLC   = reader["AddressPLC"] == DBNull.Value ? null : reader["AddressPLC"].ToString(),
                        IPPLC        = reader["IPPLC"] == DBNull.Value ? null : reader["IPPLC"].ToString(),
                        LoaiDuLieu   = reader["LoaiDuLieu"] == DBNull.Value ? null : reader["LoaiDuLieu"].ToString(),
                        TagIDEMS     = reader["TagIDEMS"] == DBNull.Value ? null : Convert.ToInt64(reader["TagIDEMS"]),
                        TagName      = reader["TagName"] == DBNull.Value ? null : reader["TagName"].ToString(),
                        GhiChu       = reader["GhiChu"] == DBNull.Value ? null : reader["GhiChu"].ToString(),
                        NgayCapNhat  = reader["NgayCapNhat"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(reader["NgayCapNhat"]),
                        IsDelete     = reader["IsDelete"] == DBNull.Value ? false : Convert.ToBoolean(reader["IsDelete"]),
                    });
                }
            }
            finally
            {
                if (!wasOpen) await conn.CloseAsync();
            }

            return result;
        }

        // ─── Dữ liệu PLC thô ─────────────────────────────────────────────────

        public async Task<List<TKVVDuLieuRawDto>> GetDataByFilterAsync(
            string? scope, DateTime? ngayBatDau, DateTime? ngayKetThuc)
        {
            var query = from d in _context.TKVV_SanLuongDuLieu
                        where (scope == null || d.Scope == scope)
                           && (ngayBatDau == null || d.ThoiGian >= ngayBatDau)
                           && (ngayKetThuc == null || d.ThoiGian < ngayKetThuc)
                        orderby d.ThoiGian
                        select new TKVVDuLieuRawDto
                        {
                            Id = d.ID,
                            TagID = d.TagID,
                            GiaTriTuDong = d.GiaTriTuDong,
                            GiaTriDieuChinh = d.GiaTriDieuChinh,
                            Ngay = d.Ngay,
                            Ca = d.Ca,
                            Scope = d.Scope,
                            ThoiGian = d.ThoiGian,
                        };

            return await query.AsNoTracking().ToListAsync();
        }

        public async Task<bool> UpdateGiaTriDieuChinhAsync(long id, decimal? giaTriDieuChinh)
        {
            var existing = await _context.TKVV_SanLuongDuLieu.FindAsync(id);
            if (existing == null) return false;

            existing.GiaTriDieuChinh = giaTriDieuChinh;
            await _context.SaveChangesAsync();
            return true;
        }

        // ─── Tổng tự động (PLC) theo (Ngay, Ca, Scope toàn cục 1-6) ─────────────
        // 1 Tag = 1 BM/xưởng/ca (ca ngày và ca đêm dùng 2 Tag khác nhau), báo TỔNG
        // khối lượng cả ca — không tách theo sản phẩm. Chỉ dùng để KTV/KCS đối
        // chiếu, KHÔNG tự điền vào bảng chi tiết.
        public async Task<TKVVTongTuDongDto> GetTongTuDongAsync(DateTime ngay, int ca, int scope)
        {
            var scopeCode = ResolveScopeCode(scope);
            var ngayOnly = DateOnly.FromDateTime(ngay);

            var tagIds = await _context.TKVV_SanLuongMapping
                .Where(m => m.Scope == scopeCode
                    && m.Ca == (byte)ca
                    && m.TrangThai
                    && (m.TuNgay == null || m.TuNgay <= ngayOnly)
                    && (m.DenNgay == null || m.DenNgay >= ngayOnly))
                .Select(m => m.TagID)
                .Distinct()
                .AsNoTracking()
                .ToListAsync();

            if (tagIds.Count == 0) return new TKVVTongTuDongDto { TongTuDong = 0 };

            // Ưu tiên GiaTriDieuChinh (KTV/KCS đã chỉnh tay) nếu có, fallback GiaTriTuDong (PLC gốc)
            // — vì PLC có thể báo sai, giá trị đã chỉnh tay đáng tin hơn khi tồn tại.
            var tong = await _context.TKVV_SanLuongDuLieu
                .Where(d => d.Ngay == ngayOnly && d.Ca == (byte)ca && d.Scope == scopeCode && tagIds.Contains(d.TagID))
                .SumAsync(d => (decimal?)(d.GiaTriDieuChinh ?? d.GiaTriTuDong)) ?? 0;

            return new TKVVTongTuDongDto { TongTuDong = tong };
        }

        // ─── Mapping Cân (EMS) → Xưởng theo Ngày/Ca/Kíp ─────────────────────────

        public async Task<List<TKVVSanLuongMappingDto>> GetSanLuongMappingListAsync(string? scope)
        {
            return await _context.TKVV_SanLuongMapping
                .Where(x => scope == null || x.Scope == scope)
                .OrderBy(x => x.Scope).ThenBy(x => x.Ca).ThenBy(x => x.TagID)
                .Select(x => new TKVVSanLuongMappingDto
                {
                    Id = x.ID,
                    TagID = x.TagID,
                    Scope = x.Scope,
                    Ca = x.Ca,
                    Kip = x.Kip,
                    TuNgay = x.TuNgay,
                    DenNgay = x.DenNgay,
                    TrangThai = x.TrangThai,
                    GhiChu = x.GhiChu,
                    NgayTao = x.NgayTao,
                    NguoiTaoID = x.NguoiTaoID,
                })
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<TKVV_SanLuongMapping?> GetSanLuongMappingByIdAsync(long id)
            => await _context.TKVV_SanLuongMapping.FindAsync(id);

        public async Task<TKVV_SanLuongMapping> AddSanLuongMappingAsync(TKVV_SanLuongMapping entity)
        {
            entity.NgayTao = DateTime.Now;
            await _context.TKVV_SanLuongMapping.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<TKVV_SanLuongMapping?> UpdateSanLuongMappingAsync(long id, TKVV_SanLuongMapping entity)
        {
            var existing = await _context.TKVV_SanLuongMapping.FindAsync(id);
            if (existing == null) return null;

            existing.TagID = entity.TagID;
            existing.Scope = entity.Scope;
            existing.Ca = entity.Ca;
            existing.Kip = entity.Kip;
            existing.TuNgay = entity.TuNgay;
            existing.DenNgay = entity.DenNgay;
            existing.TrangThai = entity.TrangThai;
            existing.GhiChu = entity.GhiChu;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteSanLuongMappingAsync(long id)
        {
            var existing = await _context.TKVV_SanLuongMapping.FindAsync(id);
            if (existing == null) return false;
            existing.TrangThai = false;
            await _context.SaveChangesAsync();
            return true;
        }

        // ─── Đồng bộ dữ liệu cân/PLC thô qua SP_TKVV_GetDuLieuCan_TuMapping ─────
        // SP join TKVV_SanLuongMapping với EMS_DATA_CAN (linked server) — dùng raw ADO.NET
        // giống GetEmsTagListAsync vì linked server không composable qua FromSqlRaw.

        public async Task<int> SyncDuLieuTuEmsAsync(DateTime ngay, byte ca, string? scope)
        {
            var ngayOnly = DateOnly.FromDateTime(ngay);
            var raw = new List<(string TagID, decimal? GiaTri, string Scope, DateTime? ThoiGian)>();

            var conn = _context.Database.GetDbConnection();
            var wasOpen = conn.State == System.Data.ConnectionState.Open;
            if (!wasOpen) await conn.OpenAsync();

            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "dbo.SP_TKVV_GetDuLieuCan_TuMapping";
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.CommandTimeout = 30;
                cmd.Parameters.Add(new SqlParameter("@Ngay", ngayOnly.ToDateTime(TimeOnly.MinValue)));
                cmd.Parameters.Add(new SqlParameter("@Ca", ca));
                cmd.Parameters.Add(new SqlParameter("@Scope", (object?)scope ?? DBNull.Value));

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    raw.Add((
                        TagID: reader["TagID"] == DBNull.Value ? string.Empty : reader["TagID"].ToString()!,
                        GiaTri: reader["GiaTri"] == DBNull.Value ? null : Convert.ToDecimal(reader["GiaTri"]),
                        Scope: reader["Scope"] == DBNull.Value ? string.Empty : reader["Scope"].ToString()!,
                        ThoiGian: reader["ThoiGian"] == DBNull.Value ? null : Convert.ToDateTime(reader["ThoiGian"])
                    ));
                }
            }
            finally
            {
                if (!wasOpen) await conn.CloseAsync();
            }

            if (raw.Count == 0) return 0;

            // Bỏ qua dòng đã có sẵn (khớp TagID + ThoiGian) — tránh chèn trùng khi bấm lại nhiều lần
            var tagIds = raw.Select(x => x.TagID).Distinct().ToList();
            var existing = await _context.TKVV_SanLuongDuLieu
                .Where(x => x.Ngay == ngayOnly && x.Ca == ca && tagIds.Contains(x.TagID))
                .Select(x => new { x.TagID, x.ThoiGian })
                .AsNoTracking()
                .ToListAsync();
            var existingSet = existing.Select(x => (x.TagID, x.ThoiGian)).ToHashSet();

            var newRows = raw
                .Where(x => x.ThoiGian != null && !existingSet.Contains((x.TagID, (DateTime?)x.ThoiGian)))
                .Select(x => new TKVV_SanLuongDuLieu
                {
                    TagID = x.TagID,
                    GiaTriTuDong = x.GiaTri,
                    Ngay = ngayOnly,
                    Ca = ca,
                    Scope = x.Scope,
                    ThoiGian = x.ThoiGian,
                    NgayTao = DateTime.Now,
                })
                .ToList();

            if (newRows.Count > 0)
            {
                await _context.TKVV_SanLuongDuLieu.AddRangeAsync(newRows);
                await _context.SaveChangesAsync();
            }

            return newRows.Count;
        }

        // ─── Chi tiết sản lượng theo phiếu ─────────────────────────────────────

        // Xóa + ghi mới trong cùng 1 transaction (giống ReplaceChiTietAsync của LGNL)
        // để lỗi giữa chừng không làm mất trắng dữ liệu chi tiết của phiếu.
        public async Task ReplaceChiTietAsync(Guid idPhieu, List<TKVV_SanLuongChiTiet> entities)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.TKVV_SanLuongChiTiet
                    .Where(x => x.IDPhieu == idPhieu)
                    .ExecuteDeleteAsync();

                if (entities.Count > 0)
                {
                    await _context.TKVV_SanLuongChiTiet.AddRangeAsync(entities);
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

        public async Task<List<TKVVChiTietDto>> GetChiTietByPhieuAsync(Guid idPhieu)
        {
            return await (
                from x in _context.TKVV_SanLuongChiTiet
                join n in _context.TKVV_NguyenVatLieu on x.NguyenVatLieuID equals n.ID into ng
                from n in ng.DefaultIfEmpty()
                where x.IDPhieu == idPhieu && !x.IsDelete
                orderby x.ThuTuDong
                select new TKVVChiTietDto
                {
                    Id = x.ID,
                    IdPhieu = x.IDPhieu,
                    Scope = x.Scope,
                    Ngay = x.Ngay,
                    Ca = x.Ca,
                    NguyenVatLieuID = x.NguyenVatLieuID,
                    TenNVL = n != null ? n.TenNVL : null,
                    ThuTuDong = x.ThuTuDong,
                    ThoiGian = x.ThoiGian,
                    Loai1 = x.Loai1,
                    Loai2 = x.Loai2,
                    Loai3 = x.Loai3,
                    PhePham = x.PhePham,
                    IsEdited = x.IsEdited,
                    NguoiSuaID = x.NguoiSuaID,
                    NgaySua = x.NgaySua,
                    LyDoSua = x.LyDoSua,
                    GhiChu = x.GhiChu,
                }
            ).AsNoTracking().ToListAsync();
        }

        public async Task<BmPhieu?> GetPhieuByIdAsync(Guid idPhieu)
            => await _context.BmPhieus.FindAsync(idPhieu);
    }
}

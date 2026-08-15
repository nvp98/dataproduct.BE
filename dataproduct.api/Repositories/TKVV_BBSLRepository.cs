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

        // ─── Danh mục NVL ────────────────────────────────────────────────────

        public async Task<List<TKVVNguyenVatLieuDto>> GetNvlListAsync(string? maBM)
        {
            return await _context.TKVV_NguyenVatLieu
                .Where(x => maBM == null || x.MaBM == maBM)
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

        // ─── Mapping ─────────────────────────────────────────────────────────

        public async Task<List<TKVVMappingDto>> GetMappingListAsync(string? scope, string? tagID, int? ca)
        {
            return await (
                from m in _context.TKVV_SanLuongMapping
                where (scope == null || m.Scope == scope)
                   && (tagID == null || m.TagID == tagID)
                   && (ca == null || m.Ca == (byte)ca)
                orderby m.Scope, m.Ca, m.ThuTu
                select new TKVVMappingDto
                {
                    Id = m.ID,
                    TagID = m.TagID,
                    MaKey = m.MaKey,
                    Scope = m.Scope,
                    Ca = m.Ca,
                    ThuTu = m.ThuTu,
                    TuNgay = m.TuNgay,
                    DenNgay = m.DenNgay,
                    TrangThai = m.TrangThai,
                    GhiChu = m.GhiChu,
                    NgayTao = m.NgayTao,
                }
            ).AsNoTracking().ToListAsync();
        }

        public async Task<TKVV_SanLuongMapping?> GetMappingByIdAsync(long id)
            => await _context.TKVV_SanLuongMapping.FindAsync(id);

        public async Task<TKVV_SanLuongMapping> AddMappingAsync(TKVV_SanLuongMapping entity)
        {
            entity.NgayTao = DateTime.Now;
            await _context.TKVV_SanLuongMapping.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<TKVV_SanLuongMapping?> UpdateMappingAsync(long id, TKVV_SanLuongMapping entity)
        {
            var existing = await _context.TKVV_SanLuongMapping.FindAsync(id);
            if (existing == null) return null;

            existing.TagID = entity.TagID;
            existing.MaKey = entity.MaKey;
            existing.Scope = entity.Scope;
            existing.Ca = entity.Ca;
            existing.ThuTu = entity.ThuTu;
            existing.TuNgay = entity.TuNgay;
            existing.DenNgay = entity.DenNgay;
            existing.TrangThai = entity.TrangThai;
            existing.GhiChu = entity.GhiChu;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteMappingAsync(long id)
        {
            var existing = await _context.TKVV_SanLuongMapping.FindAsync(id);
            if (existing == null) return false;
            existing.TrangThai = false;
            await _context.SaveChangesAsync();
            return true;
        }

        // ─── Danh sách Tag PLC từ EMS (dbo.EMS_GetMappingTag) ───────────────────
        // Đọc EMS_MAPPING_TAG qua linked server SQL_OT.DATA_SANXUAT — chỉ để hiển
        // thị cho admin chọn khi tạo Mapping, không lưu lại ở PRODUCT_FORM.
        public async Task<List<EMS_MappingTag>> GetEmsTagListAsync(string? xuong, string? tagName)
        {
            return await _context.EMS_MappingTag
                .FromSqlRaw(
                    "EXEC dbo.EMS_GetMappingTag @Xuong, @TagIDEMS, @TagName",
                    new SqlParameter("@Xuong", (object?)xuong ?? DBNull.Value),
                    new SqlParameter("@TagIDEMS", DBNull.Value),
                    new SqlParameter("@TagName", (object?)tagName ?? DBNull.Value)
                )
                .ToListAsync();
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
                            MaKey = d.MaKey,
                            Value = d.Value,
                            Ngay = d.Ngay,
                            Ca = d.Ca,
                            Scope = d.Scope,
                            ThoiGian = d.ThoiGian,
                        };

            return await query.AsNoTracking().ToListAsync();
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

            var tong = await _context.TKVV_SanLuongDuLieu
                .Where(d => d.Ngay == ngayOnly && d.Ca == (byte)ca && d.Scope == scopeCode && tagIds.Contains(d.TagID))
                .SumAsync(d => (decimal?)d.Value) ?? 0;

            return new TKVVTongTuDongDto { TongTuDong = tong };
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

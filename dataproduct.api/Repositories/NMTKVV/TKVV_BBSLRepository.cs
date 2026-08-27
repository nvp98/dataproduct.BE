using dataproduct.api.DTOs.NMTKVV_Dto;
using dataproduct.api.Models;
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
            // TKVV_NguyenVatLieu.Scope lưu mã số "1".."6" — chấp nhận filter truyền vào ở
            // cả 2 dạng ("TK1" hoặc "1") rồi luôn quy về số để so khớp đúng dữ liệu trong DB.
            var scopeCode = ResolveScopeNumber(scope)?.ToString();

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
                    Scope =x.Scope,
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
        public async Task<TKVVTongTuDongDto> GetTongTuDongAsync(DateTime ngay,int ca,int scope)
        {
            //var scopeCode = ResolveScopeCode(scope);
            var ngayOnly = DateOnly.FromDateTime(ngay);

            var tong = await _context.TKVV_SanLuongDuLieu
                .Where(d =>
                    d.Ngay == ngayOnly &&
                    d.Ca == (byte)ca &&
                    d.Scope == scope.ToString())
                .SumAsync(d =>
                    (decimal?)(d.GiaTriDieuChinh ?? d.GiaTriTuDong)) ?? 0;

            return new TKVVTongTuDongDto
            {
                TongTuDong = tong
            };
        }

        // ─── Chi tiết sản lượng theo phiếu ─────────────────────────────────────
        public async Task ReplaceChiTietAsync(Guid idPhieu, List<TKVV_SanLuongChiTiet> entities)
        {
            var ownsTransaction = _context.Database.CurrentTransaction == null;
            var transaction = ownsTransaction
                ? await _context.Database.BeginTransactionAsync()
                : null;
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

                if (transaction != null) await transaction.CommitAsync();
            }
            catch
            {
                if (transaction != null) await transaction.RollbackAsync();
                throw;
            }
            finally
            {
                if (transaction != null) await transaction.DisposeAsync();
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
                    Scope = ResolveScopeNumber(x.Scope),
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


        public async Task<bool> HasDuLieuByNgayCaScopeAsync(DateTime ngay,int ca,int scope) 
        {

            var ngayOnly = DateOnly.FromDateTime(ngay);

            var scopeValue = scope.ToString();
            return await _context.TKVV_SanLuongDuLieu.AsNoTracking().AnyAsync(d => d.Ngay == ngayOnly &&
            d.Ca == (byte)ca &&
            d.Scope == scopeValue);
        }
    }
}

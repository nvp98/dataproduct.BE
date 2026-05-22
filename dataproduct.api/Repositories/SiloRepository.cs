using System;
using System.Linq;
using dataproduct.api.DTOs;
using dataproduct.api.Models;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public class SiloRepository : ISiloRepository
    {
        private readonly ProductFormContext _context;

        public SiloRepository(ProductFormContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Silo>> GetAllAsync()
        {
            var query = _context.Silos.AsQueryable();
            return await query.ToListAsync();
        }

        public async Task<Silo?> GetByIdAsync(int id)
        {
            return await _context.Silos.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task AddAsync(Silo entity)
        {
            entity.NgayTao ??= DateTime.Now;
            _context.Silos.Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Silo entity)
        {
            _context.Silos.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var item = await _context.Silos.FindAsync(id);
            if (item != null)
            {
                _context.Silos.Remove(item);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<(IEnumerable<Silo> Data, int TotalCount)> SearchMappingsWithPagingAsync(
            string? searchKey,
            int? scope,
            bool? tinhTrang,
            string? BieuMau,
            int? nhaMay,
            int page,
            int pageSize
        )
        {
            var query = _context.Silos.AsQueryable();

            // Filter theo searchKey (tìm trong TenSilo)
            if (!string.IsNullOrEmpty(searchKey))
            {
                query = query.Where(x => x.TenSilo.Contains(searchKey));
            }

            // Filter theo scope
            if (scope.HasValue)
            {
                query = query.Where(x => x.Scope == scope.Value);
            }

            // Filter theo tinhTrang
            if (tinhTrang.HasValue)
            {
                query = query.Where(x => x.TinhTrang == tinhTrang.Value);
            }

            // Filter theo BieuMau
            if (!string.IsNullOrEmpty(BieuMau))
            {
                query = query.Where(x => x.BieuMau == BieuMau);
            }

            // Filter theo NhaMay
            if (nhaMay.HasValue)
            {
                query = query.Where(x => x.NhaMay == nhaMay.Value);
            }

            var totalCount = await query.CountAsync();

            var data = await query
                .OrderByDescending(x => x.NgayTao)
                .ThenBy(x => x.TenSilo)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (data, totalCount);
        }

        public async Task<bool> ExistsByTenSiloAsync(string tenSilo, int nhaMay, string bieuMau, int? scope, int? excludeId = null)
        {
            // Luôn check: TenSilo, NhaMay, BieuMau
            var query = _context.Silos.AsQueryable()
                .Where(x => x.TenSilo == tenSilo && x.NhaMay == nhaMay && x.BieuMau == bieuMau);
            
            // Nếu có nhập scope thì check thêm scope
            if (scope.HasValue)
            {
                query = query.Where(x => x.Scope == scope.Value);
            }
            else
            {
                // Nếu không có scope thì chỉ check trong khu vực NhaMay và BieuMau (scope == null)
                query = query.Where(x => x.Scope == null);
            }
            
            if (excludeId.HasValue)
            {
                query = query.Where(x => x.Id != excludeId.Value);
            }
            return await query.AnyAsync();
        }

        public async Task<List<SiloWithPhuLieuNmDto>> GetValidSilosAsync(
            List<int> phuLieuNMIds,
            DateTime ngaySX
        )
        {
            if (phuLieuNMIds == null || !phuLieuNMIds.Any())
            {
                return new List<SiloWithPhuLieuNmDto>();
            }

            var result = await _context.MapSiloPhuLieuNMs
                .Where(m => 
                    phuLieuNMIds.Contains(m.ID_PhuLieuNM) &&
                    m.NgayBatDau <= ngaySX &&
                    (m.NgayKetThuc == null || m.NgayKetThuc >= ngaySX) &&
                    m.IsActive == true
                )
                .Join(
                    _context.Silos,
                    map => map.ID_Silo,
                    silo => silo.Id,
                    (map, silo) => new { Map = map, Silo = silo }
                )
                .Where(x => x.Silo.TinhTrang == true)
                .GroupBy(x => new { x.Silo.Id, x.Silo.TenSilo, x.Silo.TheTich })
                .Select(g => new SiloWithPhuLieuNmDto
                {
                    SiloId = g.Key.Id,
                    TenSilo = g.Key.TenSilo,
                    TheTich = g.Key.TheTich,
                    PhuLieuNMIds = g.Select(x => x.Map.ID_PhuLieuNM).Distinct().ToList()
                })
                .ToListAsync();

            return result;
        }

        public async Task<List<int>> GetPhuLieuNMInSiloAsync(
            int siloId,
            DateTime ngaySX
        )
        {
            var result = await _context.MapSiloPhuLieuNMs
                .Where(m =>
                    m.ID_Silo == siloId &&
                    m.NgayBatDau <= ngaySX &&
                    (m.NgayKetThuc == null || m.NgayKetThuc >= ngaySX) &&
                    m.IsActive == true
                )
                .Select(m => m.ID_PhuLieuNM)
                .Distinct()
                .ToListAsync();

            return result;
        }

        public async Task<bool> IsSiloContainPhuLieuNMAsync(
            int siloId,
            int phuLieuNMId,
            DateTime ngaySX
        )
        {
            var exists = await _context.MapSiloPhuLieuNMs
                .AnyAsync(m =>
                    m.ID_Silo == siloId &&
                    m.ID_PhuLieuNM == phuLieuNMId &&
                    m.NgayBatDau <= ngaySX &&
                    (m.NgayKetThuc == null || m.NgayKetThuc >= ngaySX) &&
                    m.IsActive == true
                );

            return exists;
        }

        // MapSiloPhuLieuNM methods
        public async Task<List<MapSiloPhuLieuNMResponse>> GetMappingsBySiloIdAsync(int siloId)
        {
            var result = await _context.MapSiloPhuLieuNMs
                .Where(m => m.ID_Silo == siloId)
                .Join(
                    _context.Silos,
                    map => map.ID_Silo,
                    silo => silo.Id,
                    (map, silo) => new { Map = map, Silo = silo }
                )
                .Join(
                    _context.PhuLieu_NMs,
                    x => x.Map.ID_PhuLieuNM,
                    pl => pl.ID_PhuLieu,
                    (x, pl) => new MapSiloPhuLieuNMResponse
                    {
                        Id = x.Map.ID,
                        SiloId = x.Silo.Id,
                        TenSilo = x.Silo.TenSilo,
                        PhuLieuNMId = pl.ID_PhuLieu,
                        TenPhuLieu = pl.TenPhuLieu ?? "",
                        NgayBatDau = x.Map.NgayBatDau,
                        NgayKetThuc = x.Map.NgayKetThuc,
                        IsActive = x.Map.IsActive
                    }
                )
                .OrderByDescending(x => x.NgayBatDau)
                .ToListAsync();

            return result;
        }

        public async Task<MapSiloPhuLieuNM?> GetMappingByIdAsync(int id)
        {
            return await _context.MapSiloPhuLieuNMs
                .FirstOrDefaultAsync(m => m.ID == id);
        }

        public async Task<MapSiloPhuLieuNM> AddMappingAsync(MapSiloPhuLieuNM entity)
        {
            _context.MapSiloPhuLieuNMs.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task UpdateMappingAsync(MapSiloPhuLieuNM entity)
        {
            _context.MapSiloPhuLieuNMs.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteMappingAsync(int id)
        {
            var item = await _context.MapSiloPhuLieuNMs.FindAsync(id);
            if (item != null)
            {
                _context.MapSiloPhuLieuNMs.Remove(item);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<PhuLieu_NM>> GetAllPhuLieuNMAsync()
        {
            return await _context.PhuLieu_NMs
                .Where(pl => pl.IsActive == true)
                .OrderBy(pl => pl.TenPhuLieu)
                .ToListAsync();
        }

        public async Task<(IEnumerable<PhuLieu_NM> Data, int TotalCount)> SearchPhuLieuNMWithPagingAsync(
            string? searchKey,
            int page,
            int pageSize
        )
        {
            var query = _context.PhuLieu_NMs
                .Where(pl => pl.IsActive == true);

            // Filter theo searchKey (tìm trong TenPhuLieu và ID_PhuLieu)
            if (!string.IsNullOrEmpty(searchKey))
            {
                var searchKeyLower = searchKey.ToLower();
                query = query.Where(pl =>
                    (pl.TenPhuLieu != null && pl.TenPhuLieu.ToLower().Contains(searchKeyLower)) ||
                    pl.ID_PhuLieu.ToString().Contains(searchKey)
                );
            }

            var totalCount = await query.CountAsync();

            var data = await query
                .OrderBy(pl => pl.TenPhuLieu)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (data, totalCount);
        }

        public async Task<List<DTOs.SiloByHeaderKeyDto>> GetSilosByHeaderKeyAsync(
            int headerKeyId,
            DateTime ngaySX,
            int nhaMay,
            string? bieuMau
        )
        {
            // Bước 1: Lấy ID_PhuLieu từ Header_Mapping theo ID_HeaderKey (chỉ lấy mapping active)
            var phuLieuIds = await _context.Header_Mappings
                .Where(m => m.ID_HeaderKey == headerKeyId && m.IsActive == true)
                .Select(m => m.ID_PhuLieu)
                .Distinct()
                .ToListAsync();

            if (!phuLieuIds.Any())
            {
                return new List<DTOs.SiloByHeaderKeyDto>();
            }

            // Bước 2: Lấy silo từ MapSiloPhuLieuNM theo ID_PhuLieu và ngaySX
            // Chuyển ngaySX về Date để so sánh chính xác
            var ngaySXDate = ngaySX.Date;
            
            var query = _context.MapSiloPhuLieuNMs
                .Where(m =>
                    phuLieuIds.Contains(m.ID_PhuLieuNM) &&
                    m.NgayBatDau <= ngaySXDate &&
                    (m.NgayKetThuc == null || m.NgayKetThuc >= ngaySXDate) &&
                    m.IsActive == true
                )
                .Join(
                    _context.Silos,
                    map => map.ID_Silo,
                    silo => silo.Id,
                    (map, silo) => new { Map = map, Silo = silo }
                )
                .Where(x => x.Silo.TinhTrang == true && x.Silo.NhaMay == nhaMay);

            // Lọc theo BieuMau nếu có
            if (!string.IsNullOrEmpty(bieuMau))
            {
                query = query.Where(x => x.Silo.BieuMau == bieuMau);
            }

            var result = await query
                .GroupBy(x => new { x.Silo.Id, x.Silo.TenSilo, x.Silo.TheTich })
                .Select(g => new DTOs.SiloByHeaderKeyDto
                {
                    SiloId = g.Key.Id,
                    TenSilo = g.Key.TenSilo,
                    TheTich = g.Key.TheTich
                })
                .OrderBy(x => x.TenSilo) 
                .ToListAsync();

            return result;
        }
    }
}


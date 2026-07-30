using dataproduct.api.DTOs;
using dataproduct.api.Models;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public class MaVatTuRepository : IMaVatTuRepository
    {
        private readonly ProductFormContext _context;

        public MaVatTuRepository(ProductFormContext context)
        {
            _context = context;
        }

        public async Task<(IEnumerable<MaVatTuItem> Data, int TotalCount)> SearchAsync(MaVatTuSearchRequest req)
        {
            var query = _context.MaVatTus.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(req.NhaMay))
                query = query.Where(x => x.NhaMay == req.NhaMay);
            if (!string.IsNullOrWhiteSpace(req.MacThep))
                query = query.Where(x => x.MacThep != null && x.MacThep.Contains(req.MacThep));
            if (!string.IsNullOrWhiteSpace(req.CongDoan))
                query = query.Where(x => x.CongDoan != null && x.CongDoan.Contains(req.CongDoan));
            if (!string.IsNullOrWhiteSpace(req.SearchKey))
                query = query.Where(x => x.VatTuCode.Contains(req.SearchKey)
                                      || x.TenVatTu.Contains(req.SearchKey)
                                      || (x.MacThep != null && x.MacThep.Contains(req.SearchKey))
                                      || (x.CongDoan != null && x.CongDoan.Contains(req.SearchKey)));

            var total = await query.CountAsync();
            var data = await query
                .OrderBy(x => x.NhaMay)
                .ThenBy(x => x.MacThep)
                .Skip((req.Page - 1) * req.PageSize)
                .Take(req.PageSize)
                .Select(x => new MaVatTuItem
                {
                    Id = x.Id,
                    NhaMay = x.NhaMay,
                    MacThep = x.MacThep,
                    VatTuCode = x.VatTuCode,
                    TenVatTu = x.TenVatTu,
                    IsLock = x.IsLock,
                    CongDoan = x.CongDoan,
                    KichThuoc = x.KichThuoc
                })
                .ToListAsync();

            return (data, total);
        }

        public async Task<MaVatTuItem?> GetByIdAsync(int id)
        {
            var x = await _context.MaVatTus.FindAsync(id);
            return x == null ? null : new MaVatTuItem
            {
                Id = x.Id,
                NhaMay = x.NhaMay,
                MacThep = x.MacThep,
                VatTuCode = x.VatTuCode,
                TenVatTu = x.TenVatTu,
                IsLock = x.IsLock,
                CongDoan = x.CongDoan,
                KichThuoc = x.KichThuoc
            };
        }

        public async Task<MaVatTuItem> CreateAsync(MaVatTuUpsertDto dto)
        {
            if (!string.IsNullOrWhiteSpace(dto.MacThep) &&
                await _context.MaVatTus.AnyAsync(x => x.NhaMay == dto.NhaMay && x.MacThep == dto.MacThep))
                throw new InvalidOperationException($"Nhà máy '{dto.NhaMay}' + Mác thép '{dto.MacThep}' đã tồn tại.");

            if (!string.IsNullOrWhiteSpace(dto.CongDoan) &&
                await _context.MaVatTus.AnyAsync(x => x.NhaMay == dto.NhaMay && x.CongDoan == dto.CongDoan && x.VatTuCode == dto.VatTuCode))
                throw new InvalidOperationException($"Nhà máy '{dto.NhaMay}' + Công đoạn '{dto.CongDoan}' + Mã vật tư '{dto.VatTuCode}' đã tồn tại.");

            var entity = new MaVatTu
            {
                NhaMay = dto.NhaMay.Trim(),
                MacThep = dto.MacThep?.Trim(),
                VatTuCode = dto.VatTuCode.Trim(),
                TenVatTu = dto.TenVatTu?.Trim() ?? "",
                IsLock = dto.IsLock,
                CongDoan = dto.CongDoan?.Trim(),
                KichThuoc = dto.KichThuoc?.Trim(),
                NgayTao = DateTime.Now
            };
            _context.MaVatTus.Add(entity);
            await _context.SaveChangesAsync();

            return new MaVatTuItem
            {
                Id = entity.Id,
                NhaMay = entity.NhaMay,
                MacThep = entity.MacThep,
                VatTuCode = entity.VatTuCode,
                TenVatTu = entity.TenVatTu,
                IsLock = entity.IsLock,
                CongDoan = entity.CongDoan,
                KichThuoc = entity.KichThuoc
            };
        }

        public async Task<MaVatTuBulkCreateResult> BulkCreateAsync(List<MaVatTuUpsertDto> items)
        {
            var existing = await _context.MaVatTus
                .Select(x => new { x.NhaMay, x.MacThep, x.CongDoan, x.VatTuCode })
                .ToListAsync();

            var existingMacThepKeys = new HashSet<string>(
                existing.Where(x => !string.IsNullOrEmpty(x.MacThep)).Select(x => $"{x.NhaMay}|{x.MacThep}"),
                StringComparer.OrdinalIgnoreCase);

            var existingCongDoanKeys = new HashSet<string>(
                existing.Where(x => !string.IsNullOrEmpty(x.CongDoan)).Select(x => $"{x.NhaMay}|{x.CongDoan}|{x.VatTuCode}"),
                StringComparer.OrdinalIgnoreCase);

            var result = new MaVatTuBulkCreateResult();
            var toAdd = new List<MaVatTu>();

            foreach (var dto in items)
            {
                var nhaMay = dto.NhaMay?.Trim() ?? "";
                var macThep = dto.MacThep?.Trim();
                var congDoan = dto.CongDoan?.Trim();
                var vatTuCode = dto.VatTuCode?.Trim() ?? "";
                if (string.IsNullOrEmpty(nhaMay) || string.IsNullOrEmpty(vatTuCode)) continue;

                var macThepKey = !string.IsNullOrEmpty(macThep) ? $"{nhaMay}|{macThep}" : null;
                var congDoanKey = !string.IsNullOrEmpty(congDoan) ? $"{nhaMay}|{congDoan}|{vatTuCode}" : null;

                var isDup = (macThepKey != null && existingMacThepKeys.Contains(macThepKey))
                         || (congDoanKey != null && existingCongDoanKeys.Contains(congDoanKey));
                if (isDup)
                {
                    result.Skipped++;
                    result.SkippedItems.Add($"{nhaMay}/{macThep}/{congDoan}/{vatTuCode}");
                    continue;
                }

                toAdd.Add(new MaVatTu
                {
                    NhaMay = nhaMay,
                    MacThep = string.IsNullOrEmpty(macThep) ? null : macThep,
                    VatTuCode = vatTuCode,
                    TenVatTu = dto.TenVatTu?.Trim() ?? "",
                    CongDoan = string.IsNullOrEmpty(congDoan) ? null : congDoan,
                    NgayTao = DateTime.Now
                });
                if (macThepKey != null) existingMacThepKeys.Add(macThepKey);
                if (congDoanKey != null) existingCongDoanKeys.Add(congDoanKey);
            }

            if (toAdd.Count > 0)
            {
                _context.MaVatTus.AddRange(toAdd);
                await _context.SaveChangesAsync();
                result.Created = toAdd.Count;
            }

            return result;
        }

        public async Task<bool> UpdateAsync(int id, MaVatTuUpsertDto dto)
        {
            var entity = await _context.MaVatTus.FindAsync(id);
            if (entity == null) return false;

            if (!string.IsNullOrWhiteSpace(dto.MacThep) &&
                await _context.MaVatTus.AnyAsync(x => x.NhaMay == dto.NhaMay && x.MacThep == dto.MacThep && x.Id != id))
                throw new InvalidOperationException($"Nhà máy '{dto.NhaMay}' + Mác thép '{dto.MacThep}' đã tồn tại.");

            if (!string.IsNullOrWhiteSpace(dto.CongDoan) &&
                await _context.MaVatTus.AnyAsync(x =>
                    x.NhaMay == dto.NhaMay && x.CongDoan == dto.CongDoan && x.VatTuCode == dto.VatTuCode && x.Id != id))
                throw new InvalidOperationException($"Nhà máy '{dto.NhaMay}' + Công đoạn '{dto.CongDoan}' + Mã vật tư '{dto.VatTuCode}' đã tồn tại.");

            entity.NhaMay = dto.NhaMay.Trim();
            entity.MacThep = dto.MacThep?.Trim();
            entity.VatTuCode = dto.VatTuCode.Trim();
            entity.TenVatTu = dto.TenVatTu?.Trim() ?? "";
            entity.IsLock = dto.IsLock;
            entity.CongDoan = dto.CongDoan?.Trim();
            entity.KichThuoc = dto.KichThuoc?.Trim();
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.MaVatTus.FindAsync(id);
            if (entity == null) return false;
            _context.MaVatTus.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        // Dùng nội bộ để fill MaVatTu vào HRC1_Slab sau khi sync MacThep
        public async Task<Dictionary<string, string>> GetMaVatTuMapAsync(string nhaMay, IEnumerable<string> macThepNames)
        {
            var names = macThepNames.Where(m => !string.IsNullOrEmpty(m)).Distinct().ToList();
            if (names.Count == 0) return new Dictionary<string, string>();

            var rows = await _context.MaVatTus
                .AsNoTracking()
                .Where(x => x.NhaMay == nhaMay && x.MacThep != null && names.Contains(x.MacThep))
                .ToListAsync();

            return rows
                .GroupBy(x => x.MacThep!)
                .ToDictionary(g => g.Key, g => g.First().VatTuCode);
        }
    }
}

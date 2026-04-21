using dataproduct.api.Models;
using dataproduct.api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Services
{
    public class MacThepSearchDto
    {
        public int Id { get; set; }
        public string TenMacThep { get; set; } = null!;
        public byte NhaMay { get; set; }
        public bool? IsLock { get; set; }
        public bool? IsXacNhan { get; set; }
        public int? IdMayDuc { get; set; }
        public string? TenMayDuc { get; set; }
    }

    public class MacThepService
    {
        private readonly IMacThepRepository _repo;
        private readonly ProductFormContext _context;

        public MacThepService(IMacThepRepository repo, ProductFormContext context)
        {
            _repo = repo;
            _context = context;
        }

        public Task<IEnumerable<MacThep>> GetAllAsync(byte? nhaMay, bool? isLock, string? tenMacThep, int? idMayDuc = null)
            => _repo.GetAllAsync(nhaMay, isLock, tenMacThep, idMayDuc);

        public async Task<List<MacThepSearchDto>> SearchWithMayDucAsync(
            byte? nhaMay,
            bool? isLock,
            string? tenMacThep,
            int? idMayDuc,
            int? ca = null,
            string? kip = null,
            string? maBm = null)
        {
            var query = _context.MacTheps.AsQueryable();

            if (nhaMay.HasValue)        query = query.Where(x => x.NhaMay == nhaMay.Value);
            if (isLock.HasValue)        query = query.Where(x => x.IsLock == isLock.Value);
            if (!string.IsNullOrWhiteSpace(tenMacThep)) query = query.Where(x => x.TenMacThep.Contains(tenMacThep));
            if (idMayDuc.HasValue)      query = query.Where(x => x.IdMayDuc == idMayDuc.Value);

            // Chỉ khi có filter Kíp: lấy danh sách IdPhieu theo Ca/Kip/MaBm, sau đó lọc MacThep
            // theo tập mác thép xuất hiện trong BBGN_ThepLong của các phiếu đó.
            var hasKipFilter = !string.IsNullOrWhiteSpace(kip);
            if (hasKipFilter)
            {
                var phieuQuery = _context.BmPhieus
                    .AsNoTracking()
                    .Where(x => x.IsDelete != 1 && x.IsLock != 1);

                if (!string.IsNullOrWhiteSpace(maBm))
                    phieuQuery = phieuQuery.Where(x => x.MaBm == maBm);
                if (ca.HasValue)
                    phieuQuery = phieuQuery.Where(x => x.Ca == ca.Value);
                if (!string.IsNullOrWhiteSpace(kip))
                    phieuQuery = phieuQuery.Where(x => x.Kip == kip);

                var idPhieus = await phieuQuery.Select(x => x.Idphieu).Distinct().ToListAsync();

                if (idPhieus.Count == 0)
                    return new List<MacThepSearchDto>();

                var macThepNames = await _context.BBGN_ThepLongs
                    .AsNoTracking()
                    .Where(x =>
                        idPhieus.Contains(x.IdPhieu) &&
                        x.IsGhost != true &&
                        !string.IsNullOrWhiteSpace(x.MacThep))
                    .Select(x => x.MacThep!.Trim().ToLower())
                    .Distinct()
                    .ToListAsync();

                if (macThepNames.Count == 0)
                    return new List<MacThepSearchDto>();

                query = query.Where(x => macThepNames.Contains(x.TenMacThep.ToLower()));
            }

            return await (
                from mt in query
                join md in _context.MayDucs on mt.IdMayDuc equals md.Id into mdGroup
                from md in mdGroup.DefaultIfEmpty()
                orderby mt.NhaMay, mt.TenMacThep
                select new MacThepSearchDto
                {
                    Id          = mt.Id,
                    TenMacThep  = mt.TenMacThep,
                    NhaMay      = mt.NhaMay,
                    IsLock      = mt.IsLock,
                    IsXacNhan   = mt.IsXacNhan,
                    IdMayDuc    = mt.IdMayDuc,
                    TenMayDuc   = md != null ? md.TenMayDuc : null
                }
            ).ToListAsync();
        }

        public Task<MacThep?> GetByIdAsync(int id)
            => _repo.GetByIdAsync(id);

        public async Task<MacThep> CreateAsync(MacThep entity)
        {
            if (await _repo.ExistsByTenAsync(entity.TenMacThep, entity.NhaMay))
                throw new InvalidOperationException("Tên mác thép đã tồn tại trong nhà máy này.");

            await _repo.AddAsync(entity);
            return entity;
        }

        public async Task<bool> UpdateAsync(int id, MacThep entity)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return false;

            if (await _repo.ExistsByTenAsync(entity.TenMacThep, entity.NhaMay, id))
                throw new InvalidOperationException("Tên mác thép đã tồn tại trong nhà máy này.");

            existing.TenMacThep = entity.TenMacThep;
            existing.NhaMay = entity.NhaMay;
            existing.IsLock = entity.IsLock;
            existing.IsXacNhan = entity.IsXacNhan;
            existing.IdMayDuc = entity.IdMayDuc;
            await _repo.UpdateAsync(existing);
            return true;
        }

        public async Task<bool?> ToggleXacNhanAsync(int id)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return null;

            existing.IsXacNhan = existing.IsXacNhan == true ? false : true;
            await _repo.UpdateAsync(existing);
            return existing.IsXacNhan;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return false;
            await _repo.DeleteAsync(id);
            return true;
        }
    }
}


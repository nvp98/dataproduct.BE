using dataproduct.api.Models;
using dataproduct.api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Services
{
    public class MacThepMayDucInfo
    {
        public int IdMayDuc { get; set; }
        public string TenMayDuc { get; set; } = null!;
    }

    public class MacThepSearchDto
    {
        public int Id { get; set; }
        public string TenMacThep { get; set; } = null!;
        public byte NhaMay { get; set; }
        public bool? IsLock { get; set; }
        public bool? IsXacNhan { get; set; }
        public List<MacThepMayDucInfo> MayDucs { get; set; } = new();
        public int? Id_NhomPhanLoaiMacThep {get;set;}
        public string? TenNhom {get;set;}
        public DateTime? NgayTao {get;set;}
    }

    public class MacThepSearchRequestDto
    {
        public string? SearchKey { get; set; }
        public byte? NhaMay { get; set; }
        public bool? IsLock { get; set; }
        public List<int>? IdMayDucs { get; set; }
        public bool? IsXacNhan { get; set; }
        public int? Ca { get; set; }
        public string? Kip { get; set; }
        public string? MaBm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 30;
    }

    public class NhomPhanLoaiMacThepSearchRequestDto
    {
        public string? searchText { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 30;
    }

    public class MacThepUpsertDto
    {
        public string TenMacThep { get; set; } = null!;
        public byte NhaMay { get; set; }
        public bool? IsLock { get; set; }
        public bool? IsXacNhan { get; set; }
        public List<int> IdMayDucs { get; set; } = new();
        public int Id_NhomPhanLoaiMacThep {get;set;}
    }

    public class NhomMacThepUpsertDto
    {
        public string TenNhom { get; set; } = null!;
    }

    public class MacThepService
    {
        private readonly IMacThepRepository _repo;
        private readonly IMacThep_MayDucRepository _mappingRepo;
        private readonly ProductFormContext _context;

        public MacThepService(IMacThepRepository repo, IMacThep_MayDucRepository mappingRepo, ProductFormContext context)
        {
            _repo = repo;
            _mappingRepo = mappingRepo;
            _context = context;
        }

        public Task<IEnumerable<MacThep>> GetAllAsync(byte? nhaMay, bool? isLock, string? tenMacThep, List<int>? idMayDucs = null)
            => _repo.GetAllAsync(nhaMay, isLock, tenMacThep, idMayDucs);

        public async Task<List<MacThepSearchDto>> SearchWithMayDucAsync(
            byte? nhaMay,
            bool? isLock,
            string? tenMacThep,
            List<int>? idMayDucs,
            bool? isXacNhan = null,
            int? ca = null,
            string? kip = null,
            string? maBm = null)
        {
            var query = _context.MacTheps.AsQueryable();

            if (nhaMay.HasValue)        query = query.Where(x => x.NhaMay == nhaMay.Value);
            if (isLock.HasValue)        query = query.Where(x => x.IsLock == isLock.Value);
            if (isXacNhan.HasValue)     query = isXacNhan.Value
                ? query.Where(x => x.IsXacNhan == true)
                : query.Where(x => x.IsXacNhan == false || x.IsXacNhan == null);
            if (!string.IsNullOrWhiteSpace(tenMacThep)) query = query.Where(x => x.TenMacThep.Contains(tenMacThep));
            if (idMayDucs != null && idMayDucs.Count > 0)
                query = query.Where(x => _context.MacThep_MayDucs.Any(m => m.IdMacThep == x.Id && idMayDucs.Contains(m.IdMayDuc)));

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

            var macTheps = await (
                from x in query

                join n in _context.NhomPhanLoaiMacTheps
                    on x.Id_NhomPhanLoaiMacThep equals n.Id into gj

                from nhom in gj.DefaultIfEmpty()

                orderby x.NgayTao descending

                select new MacThepSearchDto
                {
                    Id = x.Id,
                    TenMacThep = x.TenMacThep,
                    NhaMay = x.NhaMay,
                    IsLock = x.IsLock,
                    IsXacNhan = x.IsXacNhan,

                    Id_NhomPhanLoaiMacThep = x.Id_NhomPhanLoaiMacThep,
                    TenNhom = nhom != null ? nhom.TenNhom : null,
                    NgayTao = x.NgayTao
                }
            ).ToListAsync();

            if (macTheps.Count == 0) return macTheps;

            var ids = macTheps.Select(x => x.Id).ToList();
            var mayDucMap = await (
                from m in _context.MacThep_MayDucs
                join md in _context.MayDucs on m.IdMayDuc equals md.Id
                where ids.Contains(m.IdMacThep)
                select new { m.IdMacThep, m.IdMayDuc, md.TenMayDuc }
            ).ToListAsync();

            var grouped = mayDucMap.GroupBy(x => x.IdMacThep)
                .ToDictionary(g => g.Key, g => g.Select(x => new MacThepMayDucInfo
                {
                    IdMayDuc   = x.IdMayDuc,
                    TenMayDuc  = x.TenMayDuc
                }).ToList());

            foreach (var mt in macTheps)
            {
                if (grouped.TryGetValue(mt.Id, out var list))
                    mt.MayDucs = list;
            }

            return macTheps;
        }

        public Task<MacThep?> GetByIdAsync(int id)
            => _repo.GetByIdAsync(id);

        public async Task<MacThep> CreateAsync(MacThepUpsertDto dto)
        {
            if (await _repo.ExistsByTenAndMayDucAsync(dto.TenMacThep, dto.IdMayDucs))
                throw new InvalidOperationException("Tên mác thép đã được gán cho một hoặc nhiều máy đúc được chọn.");

            var entity = new MacThep
            {
                TenMacThep = dto.TenMacThep,
                NhaMay     = dto.NhaMay,
                IsLock     = dto.IsLock,
                IsXacNhan  = dto.IsXacNhan,
                Id_NhomPhanLoaiMacThep = dto.Id_NhomPhanLoaiMacThep,
                NgayTao    = DateTime.Now
            };
            await _repo.AddAsync(entity);

            if (dto.IdMayDucs.Count > 0)
                await _mappingRepo.SetMayDucsAsync(entity.Id, dto.IdMayDucs);

            return entity;
        }

        public async Task<bool> UpdateAsync(int id, MacThepUpsertDto dto)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return false;

            if (await _repo.ExistsByTenAndMayDucAsync(dto.TenMacThep, dto.IdMayDucs, id))
                throw new InvalidOperationException("Tên mác thép đã được gán cho một hoặc nhiều máy đúc được chọn.");

            existing.TenMacThep = dto.TenMacThep;
            existing.NhaMay     = dto.NhaMay;
            existing.IsLock     = dto.IsLock;
            existing.Id_NhomPhanLoaiMacThep = dto.Id_NhomPhanLoaiMacThep;
            await _repo.UpdateAsync(existing);
            await _mappingRepo.SetMayDucsAsync(id, dto.IdMayDucs);
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

        public async Task<bool> SetMayDucsAsync(int id, List<int> idMayDucs)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return false;

            if (await _repo.ExistsByTenAndMayDucAsync(existing.TenMacThep, idMayDucs, id))
                throw new InvalidOperationException("Tên mác thép đã được gán cho một hoặc nhiều máy đúc được chọn.");

            await _mappingRepo.SetMayDucsAsync(id, idMayDucs);
            return true;
        }

        public async Task<List<MacThepMayDucInfo>> GetMayDucsAsync(int id)
        {
            var mappings = await _mappingRepo.GetByMacThepIdAsync(id);
            if (mappings.Count == 0) return new List<MacThepMayDucInfo>();

            var mayDucIds = mappings.Select(x => x.IdMayDuc).ToList();
            return await _context.MayDucs
                .Where(x => mayDucIds.Contains(x.Id))
                .Select(x => new MacThepMayDucInfo { IdMayDuc = x.Id, TenMayDuc = x.TenMayDuc })
                .ToListAsync();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return false;
            await _repo.DeleteAsync(id);
            return true;
        }

        public async Task<NhomPhanLoaiMacThep> CreateNhomMacThepAsync(NhomMacThepUpsertDto dto)
        {
            if (await _context.NhomPhanLoaiMacTheps.AnyAsync(x => x.TenNhom == dto.TenNhom))
                throw new InvalidOperationException("Tên nhóm phân loạiloại mác thép đã tồn tại");

            var entity = new NhomPhanLoaiMacThep
            {
                TenNhom = dto.TenNhom
            };
            await _context.NhomPhanLoaiMacTheps.AddAsync(entity);
            _context.SaveChanges();
            return entity;
        }

        public async Task<List<NhomPhanLoaiMacThep>> SearchNhomPhanLoaiMacThepAsync(NhomPhanLoaiMacThepSearchRequestDto dto)
        {
            var query = _context.NhomPhanLoaiMacTheps.AsQueryable();

            if (!string.IsNullOrWhiteSpace(dto.searchText))
                query = query.Where(x => x.TenNhom.Contains(dto.searchText));

            var nhomPhanLoai = await query
                .OrderBy(x => x.TenNhom)
                .ToListAsync();

            return nhomPhanLoai;
        }
    }
}


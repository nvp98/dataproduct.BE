using dataproduct.api.Models;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public class HRC1_BCChuanHoaRepository : IHRC1_BCChuanHoaRepository
    {
        private static readonly string[] KeyTraCuu = ["XuongNhan", "BoPhanNhan", "XuongGiao", "BoPhanGiao", "BP_BenGiao", "NoiDung1", "BP_DoiTac"];

        private readonly ProductFormContext _context;

        public HRC1_BCChuanHoaRepository(ProductFormContext context)
        {
            _context = context;
        }

        public Task<List<HRC1_BC_XuongChuanHoa>> GetXuongAsync()
            => _context.HRC1_BC_XuongChuanHoas.AsNoTracking().OrderBy(x => x.ThuTu).ThenBy(x => x.ID).ToListAsync();

        public Task<HRC1_BC_XuongChuanHoa?> GetXuongByIdAsync(int id)
            => _context.HRC1_BC_XuongChuanHoas.FirstOrDefaultAsync(x => x.ID == id);

        public Task<List<HRC1_BC_NVLChuanHoa>> GetNvlAsync()
            => _context.HRC1_BC_NVLChuanHoas.AsNoTracking().OrderBy(x => x.ThuTu).ThenBy(x => x.ID).ToListAsync();

        public Task<HRC1_BC_NVLChuanHoa?> GetNvlByIdAsync(int id)
            => _context.HRC1_BC_NVLChuanHoas.FirstOrDefaultAsync(x => x.ID == id);

        public async Task AddAsync<T>(T entity) where T : class => await _context.Set<T>().AddAsync(entity);

        public Task SaveAsync() => _context.SaveChangesAsync();

        public void Remove<T>(T entity) where T : class => _context.Set<T>().Remove(entity);

        public async Task<List<(DateOnly NgaySX, byte CaSX, string? Kip, string Lsx)>> GetLsxTheoCaAsync(string maModule, DateOnly tuNgay, DateOnly denNgay)
        {
            var rows = await _context.KDL_DoiTuongs.AsNoTracking()
                .Where(d => d.MaModule == maModule && !d.IsDelete && d.NgaySX >= tuNgay && d.NgaySX <= denNgay
                            && d.CaSX != null && d.LenhSanXuat != null && d.LenhSanXuat != "")
                .Select(d => new { d.NgaySX, d.CaSX, d.Kip, d.LenhSanXuat })
                .Distinct()
                .ToListAsync();
            return rows.Select(r => (r.NgaySX, r.CaSX ?? (byte)0, r.Kip, (r.LenhSanXuat ?? "").Trim())).ToList();
        }

        public Task<List<HRC1_BC_PhanBoDieuChinh>> GetPhanBoDieuChinhAsync(DateOnly tuNgay, DateOnly denNgay)
            => _context.HRC1_BC_PhanBoDieuChinhs.Where(x => x.NgaySX >= tuNgay && x.NgaySX <= denNgay).ToListAsync();

        public async Task<List<HRC1_GNDongTraCuu>> GetDongTraCuuAsync(string maModule, DateOnly tuNgay, DateOnly denNgay)
        {
            var doiTuongs = await _context.KDL_DoiTuongs.AsNoTracking()
                .Where(d => d.MaModule == maModule && !d.IsDelete && d.NgaySX >= tuNgay && d.NgaySX <= denNgay)
                .Select(d => new { d.IdDoiTuong, d.Mac, d.MayDuc })
                .ToListAsync();
            if (doiTuongs.Count == 0) return [];

            var keyIds = await _context.KDL_Keys.AsNoTracking()
                .Where(k => k.MaModule == maModule && KeyTraCuu.Contains(k.MaKey))
                .ToDictionaryAsync(k => k.IdKey, k => k.MaKey);
            var idKeys = keyIds.Keys.ToList();

            var giaTri = await _context.KDL_GiaTris.AsNoTracking()
                .Where(g => g.NgaySX >= tuNgay && g.NgaySX <= denNgay && idKeys.Contains(g.IdKey))
                .Select(g => new { g.IdDoiTuong, g.IdKey, g.Nguon, g.GiaTriText })
                .ToListAsync();
            // Nhiều nguồn cùng key → lấy nguồn ưu tiên cao nhất (điều chỉnh > nhập tay > tự động)
            var theoDoiTuong = giaTri
                .GroupBy(g => g.IdDoiTuong)
                .ToDictionary(g => g.Key, g => g.GroupBy(x => keyIds[x.IdKey])
                    .ToDictionary(x => x.Key, x => x.OrderByDescending(v => v.Nguon).First().GiaTriText, StringComparer.OrdinalIgnoreCase));

            return doiTuongs.Select(d =>
            {
                var v = theoDoiTuong.GetValueOrDefault(d.IdDoiTuong) ?? new Dictionary<string, string?>();
                return new HRC1_GNDongTraCuu
                {
                    VatTu = d.Mac,
                    BpNhan = d.MayDuc,
                    XuongNhan = v.GetValueOrDefault("XuongNhan"),
                    BoPhanNhan = v.GetValueOrDefault("BoPhanNhan"),
                    XuongGiao = v.GetValueOrDefault("XuongGiao"),
                    BoPhanGiao = v.GetValueOrDefault("BoPhanGiao"),
                    BpGiao = v.GetValueOrDefault("BP_BenGiao"),
                    NoiDung1 = v.GetValueOrDefault("NoiDung1"),
                    BpDoiTac = v.GetValueOrDefault("BP_DoiTac") ?? d.MayDuc,
                };
            }).ToList();
        }
    }
}

using dataproduct.api.Models;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public class DPV_LenhSanXuatRepository : IDPV_LenhSanXuatRepository
    {
        private readonly ProductFormContext _context;

        public DPV_LenhSanXuatRepository(ProductFormContext context)
        {
            _context = context;
        }

        public async Task<List<DPV_LenhSanXuat>> SearchAsync(string? mac, string? kichThuoc, int? mayDuc, int? idXuong, string? searchKey)
        {
            var query = _context.DPV_LenhSanXuats.Where(x => !x.IsDelete);

            if (!string.IsNullOrWhiteSpace(mac))
                query = query.Where(x => x.Mac.Contains(mac));
            if (!string.IsNullOrWhiteSpace(kichThuoc))
                query = query.Where(x => x.KichThuoc.Contains(kichThuoc));
            if (mayDuc.HasValue)
                query = query.Where(x => x.MayDuc == mayDuc.Value);
            if (idXuong.HasValue)
                query = query.Where(x => x.IdXuong == idXuong.Value);
            if (!string.IsNullOrWhiteSpace(searchKey))
                query = query.Where(x => (x.MaLenh != null && x.MaLenh.Contains(searchKey))
                                      || (x.LenhSanXuat != null && x.LenhSanXuat.Contains(searchKey)));

            return await query
                .OrderBy(x => x.Mac).ThenBy(x => x.KichThuoc).ThenBy(x => x.MayDuc)
                .ToListAsync();
        }

        public Task<List<DPV_LenhSanXuat>> GetAllActiveAsync()
            => _context.DPV_LenhSanXuats.Where(x => !x.IsDelete).ToListAsync();

        public Task<DPV_LenhSanXuat?> GetByIdAsync(int id)
            => _context.DPV_LenhSanXuats.FirstOrDefaultAsync(x => x.Id == id && !x.IsDelete);

        public Task<bool> ExistsToHopAsync(string mac, string kichThuoc, int mayDuc, int? excludeId = null)
        {
            var query = _context.DPV_LenhSanXuats.Where(x =>
                !x.IsDelete && x.Mac == mac && x.KichThuoc == kichThuoc && x.MayDuc == mayDuc);
            if (excludeId.HasValue)
                query = query.Where(x => x.Id != excludeId.Value);
            return query.AnyAsync();
        }

        // Tổ hợp (Mác, KT, Máy đúc) có trong dữ liệu mẻ (BK_PhoiThep) của khoảng ngày
        public async Task<List<(string Mac, string KichThuoc, int MayDuc)>> GetToHopTuDuLieuMeAsync(DateOnly tuNgay, DateOnly denNgay)
        {
            // Dòng ND (ngắn dài) mang KT ngắn dài, không phải KT của mẻ → bỏ; KT chính = KT nhiều thanh nhất của mẻ
            // (cùng quy tắc với HRC1DPV_BKMNguon của Kho dữ liệu động)
            var rows = await _context.BkPhoiThep
                .Where(x => x.NgaySx >= tuNgay && x.NgaySx <= denNgay &&
                            x.Mac != null && x.Mac != "" &&
                            x.KichThuoc != null && x.KichThuoc != "" &&
                            x.MayDuc != null && x.TenLoai != "ND")
                .Select(x => new { x.Me, x.Mac, x.KichThuoc, x.MayDuc, x.SoThanh })
                .ToListAsync();

            return rows
                .GroupBy(x => x.Me)
                .Select(me =>
                {
                    var ktChinh = me.GroupBy(x => x.KichThuoc!.Trim())
                                    .OrderByDescending(g => g.Sum(x => x.SoThanh ?? 0))
                                    .First().Key;
                    var dau = me.First();
                    return (dau.Mac!.Trim(), ktChinh, dau.MayDuc!.Value);
                })
                .Distinct()
                .ToList();
        }

        public async Task AddAsync(DPV_LenhSanXuat entity)
        {
            _context.DPV_LenhSanXuats.Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task AddRangeAsync(IEnumerable<DPV_LenhSanXuat> entities)
        {
            _context.DPV_LenhSanXuats.AddRange(entities);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(DPV_LenhSanXuat entity)
        {
            _context.DPV_LenhSanXuats.Update(entity);
            await _context.SaveChangesAsync();
        }
    }
}

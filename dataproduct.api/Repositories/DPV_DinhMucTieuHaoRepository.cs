using dataproduct.api.Models;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public class DinhMucTieuHaoRepository : IDinhMucTieuHaoRepository
    {
        private readonly ProductFormContext _context;

        public DinhMucTieuHaoRepository(ProductFormContext context)
        {
            _context = context;
        }

        public async Task<List<DinhMucTieuHao>> SearchAsync(int? idPB, int? idXuong, string? baoCao, string? chiTieu, DateOnly? ngayApDung)
        {
            var query = _context.DinhMucTieuHaos.Where(x => !x.IsDelete);

            if (idPB.HasValue)
                query = query.Where(x => x.IdPB == idPB.Value);
            if (idXuong.HasValue)
                query = query.Where(x => x.IdXuong == idXuong.Value);
            if (!string.IsNullOrWhiteSpace(baoCao))
                query = query.Where(x => x.BaoCao != null && x.BaoCao.Contains(baoCao));
            if (!string.IsNullOrWhiteSpace(chiTieu))
                query = query.Where(x => x.ChiTieu.Contains(chiTieu));
            if (ngayApDung.HasValue)
                query = query.Where(x =>
                    (x.TuNgay == null || x.TuNgay <= ngayApDung.Value) &&
                    (x.DenNgay == null || x.DenNgay >= ngayApDung.Value));

            return await query
                .OrderBy(x => x.BaoCao)
                .ThenBy(x => x.ChiTieu)
                .ThenBy(x => x.TuNgay)
                .ToListAsync();
        }

        public Task<DinhMucTieuHao?> GetByIdAsync(int id)
            => _context.DinhMucTieuHaos.FirstOrDefaultAsync(x => x.Id == id && !x.IsDelete);

        public async Task AddAsync(DinhMucTieuHao entity)
        {
            _context.DinhMucTieuHaos.Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(DinhMucTieuHao entity)
        {
            _context.DinhMucTieuHaos.Update(entity);
            await _context.SaveChangesAsync();
        }
    }
}

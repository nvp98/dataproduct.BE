using dataproduct.api.Models;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public class CtdSoTheoDoiRepository : ICtdSoTheoDoiRepository
    {
        private readonly ProductFormContext _context;

        public CtdSoTheoDoiRepository(ProductFormContext context)
        {
            _context = context;
        }

        public async Task AddSoTheoDoiListAsync(List<CtdSoTheoDoi> entities)
        {
            if (entities.Count == 0)
                return;

            await _context.CtdSoTheoDois.AddRangeAsync(entities);
            await _context.SaveChangesAsync();
        }

        public async Task AddDienBienListAsync(List<CtdStdDienBien> entities)
        {
            if (entities.Count == 0)
                return;

            await _context.CtdStdDienBiens.AddRangeAsync(entities);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteSoTheoDoiByPhieuIdAsync(Guid phieuId)
        {
            var entities = await _context.CtdSoTheoDois
                .Where(x => x.Idphieu == phieuId)
                .ToListAsync();

            if (entities.Count == 0)
                return;

            _context.CtdSoTheoDois.RemoveRange(entities);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteDienBienByPhieuIdAsync(Guid phieuId)
        {
            var entities = await _context.CtdStdDienBiens
                .Where(x => x.Idphieu == phieuId)
                .ToListAsync();

            if (entities.Count == 0)
                return;

            _context.CtdStdDienBiens.RemoveRange(entities);
            await _context.SaveChangesAsync();
        }

        public async Task<List<CtdSoTheoDoi>> GetSoTheoDoiByPhieuIdAsync(Guid phieuId)
        {
            return await _context.CtdSoTheoDois
                .AsNoTracking()
                .Where(x => x.Idphieu == phieuId)
                // .OrderBy(x => x.LoaiMacPhoi)
                .ToListAsync();
        }

        public async Task<List<CtdStdDienBien>> GetDienBienByPhieuIdAsync(Guid phieuId)
        {
            return await _context.CtdStdDienBiens
                .AsNoTracking()
                .Where(x => x.Idphieu == phieuId)
                .OrderBy(x => x.TuGio)
                .ToListAsync();
        }
    }
}

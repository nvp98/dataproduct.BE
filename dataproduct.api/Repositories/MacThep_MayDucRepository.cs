using dataproduct.api.Models;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public class MacThep_MayDucRepository : IMacThep_MayDucRepository
    {
        private readonly ProductFormContext _context;

        public MacThep_MayDucRepository(ProductFormContext context)
        {
            _context = context;
        }

        public Task<List<MacThep_MayDuc>> GetByMacThepIdAsync(int idMacThep)
            => _context.MacThep_MayDucs.Where(x => x.IdMacThep == idMacThep).ToListAsync();

        public Task<List<MacThep_MayDuc>> GetByMacThepIdsAsync(List<int> idMacTheps)
            => _context.MacThep_MayDucs.Where(x => idMacTheps.Contains(x.IdMacThep)).ToListAsync();

        public async Task SetMayDucsAsync(int idMacThep, List<int> idMayDucs)
        {
            var existing = await _context.MacThep_MayDucs
                .Where(x => x.IdMacThep == idMacThep)
                .ToListAsync();

            _context.MacThep_MayDucs.RemoveRange(existing);

            if (idMayDucs.Count > 0)
            {
                var newMappings = idMayDucs.Distinct().Select(id => new MacThep_MayDuc
                {
                    IdMacThep = idMacThep,
                    IdMayDuc = id
                });
                _context.MacThep_MayDucs.AddRange(newMappings);
            }

            await _context.SaveChangesAsync();
        }
    }
}

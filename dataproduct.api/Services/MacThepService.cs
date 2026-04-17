using dataproduct.api.Models;
using dataproduct.api.Repositories;

namespace dataproduct.api.Services
{
    public class MacThepService
    {
        private readonly IMacThepRepository _repo;

        public MacThepService(IMacThepRepository repo)
        {
            _repo = repo;
        }

        public Task<IEnumerable<MacThep>> GetAllAsync(byte? nhaMay, bool? isLock, string? tenMacThep)
            => _repo.GetAllAsync(nhaMay, isLock, tenMacThep);

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
            await _repo.UpdateAsync(existing);
            return true;
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


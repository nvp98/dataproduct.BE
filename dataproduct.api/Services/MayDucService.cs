using dataproduct.api.Models;
using dataproduct.api.Repositories;

namespace dataproduct.api.Services
{
    public class MayDucService
    {
        private readonly IMayDucRepository _repo;

        public MayDucService(IMayDucRepository repo)
        {
            _repo = repo;
        }

        public Task<IEnumerable<MayDuc>> GetAllAsync(byte? nhaMay, bool? isLock, string? tenMayDuc)
            => _repo.GetAllAsync(nhaMay, isLock, tenMayDuc);

        public Task<MayDuc?> GetByIdAsync(int id)
            => _repo.GetByIdAsync(id);

        public async Task<MayDuc> CreateAsync(MayDuc entity)
        {
            if (await _repo.ExistsByTenAsync(entity.TenMayDuc, entity.NhaMay))
                throw new InvalidOperationException("Tên máy đúc đã tồn tại trong nhà máy này.");

            await _repo.AddAsync(entity);
            return entity;
        }

        public async Task<bool> UpdateAsync(int id, MayDuc entity)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return false;

            if (await _repo.ExistsByTenAsync(entity.TenMayDuc, entity.NhaMay, id))
                throw new InvalidOperationException("Tên máy đúc đã tồn tại trong nhà máy này.");

            existing.TenMayDuc   = entity.TenMayDuc;
            existing.NhaMay      = entity.NhaMay;
            existing.IsLock      = entity.IsLock;
            existing.LoaiMayDuc  = entity.LoaiMayDuc;
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


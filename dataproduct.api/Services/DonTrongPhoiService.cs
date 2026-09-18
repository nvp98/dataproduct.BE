using dataproduct.api.Models;
using dataproduct.api.Repositories;

namespace dataproduct.api.Services
{
    public class DonTrongPhoiService
    {
        private readonly IDonTrongPhoiRepository _repo;

        public DonTrongPhoiService(IDonTrongPhoiRepository repo)
        {
            _repo = repo;
        }

        public Task<IEnumerable<DonTrongPhoi>> GetAllAsync(string? macPhoi, string? mac, string? kichThuoc)
            => _repo.GetAllAsync(macPhoi, mac, kichThuoc);

        public Task<DonTrongPhoi?> GetByIdAsync(int id)
            => _repo.GetByIdAsync(id);

        public async Task<DonTrongPhoi> CreateAsync(DonTrongPhoi entity)
        {
            if (await _repo.ExistsAsync(entity.MacPhoi, entity.Mac, entity.KichThuoc))
                throw new InvalidOperationException("Đã tồn tại bản ghi với Mác phôi, Mác thép và Kích thước này.");

            await _repo.AddAsync(entity);
            return entity;
        }

        public async Task<bool> UpdateAsync(int id, DonTrongPhoi entity)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return false;

            if (await _repo.ExistsAsync(entity.MacPhoi, entity.Mac, entity.KichThuoc, id))
                throw new InvalidOperationException("Đã tồn tại bản ghi với Mác phôi, Mác thép và Kích thước này.");

            existing.MacPhoi   = entity.MacPhoi;
            existing.DonTrong  = entity.DonTrong;
            existing.Mac       = entity.Mac;
            existing.KichThuoc = entity.KichThuoc;
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

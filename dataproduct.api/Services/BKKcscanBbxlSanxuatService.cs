using dataproduct.api.Models;
using dataproduct.api.Repositories;

namespace dataproduct.api.Services
{
    public class BKKcscanBbxlSanxuatService
    {
        private readonly IBKKcscanBbxlSanxuatRepository _repo;

        public BKKcscanBbxlSanxuatService(IBKKcscanBbxlSanxuatRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<BkKcscanBbxlSanxuat>> GetAllAsync(DateOnly? ngaySX, string? ca, DateOnly? ngayXL, string? caXL, string? order, int? xuongCan)
        {
            return await _repo.GetAllAsync(ngaySX, ca, ngayXL, caXL, order, xuongCan);
        }

        public async Task<BkKcscanBbxlSanxuat?> GetByIdAsync(long id)
        {
            return await _repo.GetByIdAsync(id);
        }
    }
}

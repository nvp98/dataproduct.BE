using dataproduct.api.Models;

namespace dataproduct.api.Repositories
{
    public interface IPhieuRepository
    {
        Task<IEnumerable<BmPhieu>> GetAllAsync();
        Task<BmPhieu?> GetByIdAsync(Guid id);
        Task AddAsync(BmPhieu entity);
        Task UpdateAsync(BmPhieu entity);
        Task DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
    }
}

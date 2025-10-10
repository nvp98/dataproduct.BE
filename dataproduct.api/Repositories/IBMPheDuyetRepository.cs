using dataproduct.api.Models;

namespace dataproduct.api.Repositories
{
    public interface IBMPheDuyetRepository
    {
        Task<IEnumerable<BmPheDuyet>> GetAllAsync();
        Task<BmPheDuyet?> GetByIdAsync(int id);
        Task AddAsync(BmPheDuyet entity);
        Task UpdateAsync(BmPheDuyet entity);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
    }
}

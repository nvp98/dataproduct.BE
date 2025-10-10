using dataproduct.api.Models;

namespace dataproduct.api.Repositories
{
    public interface IBKPhoiThepRepository
    {
        Task<IEnumerable<BkPhoiThep>> GetAllAsync();
        Task<BkPhoiThep?> GetByIdAsync(int id);
        Task AddAsync(BkPhoiThep entity);
        Task UpdateAsync(BkPhoiThep entity);
        Task DeleteAsync(int id);
    }
}

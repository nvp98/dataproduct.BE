using dataproduct.api.Models;

namespace dataproduct.api.Repositories
{
    public interface IBMPheDuyetRepository
    {
        Task<IEnumerable<BmPheDuyet>> GetAllAsync(int? NguoiDuyetID);
        Task<IEnumerable<BmPheDuyet>?> GetByIdAsync(Guid id);
        Task AddAsync(BmPheDuyet entity);
        Task UpdateAsync(BmPheDuyet entity);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task AddListAsync(List<BmPheDuyet> pheDuyetList, Guid idphieu);
    }
}

using dataproduct.api.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace dataproduct.api.Repositories
{
    public interface IPhieuRepository
    {
        Task<IEnumerable<BmPhieu>> GetAllAsync(string? MaBM, int? NguoiTaoID);
        Task<BmPhieu?> GetByIdAsync(Guid id);
        Task<BmPhieu> AddAsync([FromBody] JsonElement formData);
        Task UpdateAsync(BmPhieu entity);
        Task DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
    }
}

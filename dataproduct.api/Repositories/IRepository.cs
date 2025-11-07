using dataproduct.api.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace dataproduct.api.Repositories
{
    public interface IBKNguyenLieuRepository
    {
        Task<IEnumerable<BkNguyenLieu>> GetAllAsync(DateOnly? NgaySX, int? Ca, string? Kip);
        Task<BkNguyenLieu?> GetByIdAsync(int id);
        Task AddAsync(BkNguyenLieu entity);
        Task UpdateAsync(BkNguyenLieu entity);
        Task DeleteAsync(int id);
    }
    public interface IPhieuRepository
    {
        Task<IEnumerable<BmPhieu>> GetAllAsync(string? MaBM, int? NguoiTaoID);
        Task<BmPhieu?> GetByIdAsync(Guid id);
        Task<BmPhieu> AddAsync([FromBody] JsonElement formData);
        Task UpdateAsync(BmPhieu entity);
        Task DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
    }
    public interface IBMPheDuyetRepository
    {
        Task<IEnumerable<BmPheDuyet>> GetAllAsync(int? NguoiDuyetID, int? isCheckDuyet);
        Task<BmPheDuyet> GetByIdAsync(int? id);
        Task<IEnumerable<BmPheDuyet>?> GetByIdPhieuAsync(Guid id);
        Task AddAsync(BmPheDuyet entity);
        Task UpdateAsync(BmPheDuyet entity);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task AddListAsync(List<BmPheDuyet> pheDuyetList, Guid idphieu);
    }
    public interface IBKPhoiThepRepository
    {
        Task<IEnumerable<BkPhoiThep>> GetAllAsync(DateOnly? NgaySX, int? Ca, string? Kip);
        Task<BkPhoiThep?> GetByIdAsync(int id);
        Task AddAsync(BkPhoiThep entity);
        Task UpdateAsync(BkPhoiThep entity);
        Task DeleteAsync(int id);
    }
    public interface IDLNMHRC2Repository
    {
        Task<IEnumerable<DLNM_HRC2>> GetAllAsync(DateTime? NgaySX, int? Ca, string? BieuMau, int? Scope);
        Task<DLNM_HRC2?> GetByIdAsync(int id);
        Task<IEnumerable<DLNM_HRC2>> GetByReportNoAsync(int reportNo);
        Task AddAsync(DLNM_HRC2 entity);
        Task UpdateAsync(DLNM_HRC2 entity);
        Task DeleteAsync(int id);
        Task<(IEnumerable<DLNM_HRC2> Data, int TotalCount)> SearchWithPagingAsync(DateTime? NgaySX, int? Ca, string? LoaiBM, int? Scope, int page, int pageSize);
    }
}

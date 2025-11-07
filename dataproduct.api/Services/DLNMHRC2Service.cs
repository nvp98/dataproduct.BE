using dataproduct.api.DTOs;
using dataproduct.api.Models;
using dataproduct.api.Repositories;

namespace dataproduct.api.Services
{
    public class DLNMHRC2Service
    {
        private readonly IDLNMHRC2Repository _repo;

        public DLNMHRC2Service(IDLNMHRC2Repository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<DLNM_HRC2>> GetAllAsync(DateTime? Ngay, int? Ca, string? BieuMau, int? Scope)
        {
            return  await _repo.GetAllAsync(Ngay,Ca,BieuMau,Scope);
        }

        public async Task<DLNM_HRC2?> GetByIdAsync(int id)
        {
            var x = await _repo.GetByIdAsync(id);
            if (x == null) return null;
            return x;
            
        }

        public async Task<IEnumerable<DLNM_HRC2>> GetByReportNoAsync(int reportNo)
        {
            return await _repo.GetByReportNoAsync(reportNo);
        }

        public async Task<DLNM_HRC2> CreateAsync(DLNM_HRC2 entity)
        {
            await _repo.AddAsync(entity);
            return entity;
        }

        public async Task<bool> UpdateAsync(int id, DLNM_HRC2 entity)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return false;
            entity.Id = id;
            await _repo.UpdateAsync(entity);
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return false;
            await _repo.DeleteAsync(id);
            return true;
        }

        public async Task<PagedResult<DLNM_HRC2>> SearchWithPagingAsync(DateTime? NgaySX, int? Ca, string? LoaiBM, int? Scope, int page, int pageSize)
        {
            var (data, totalCount) = await _repo.SearchWithPagingAsync(NgaySX, Ca, LoaiBM, Scope, page, pageSize);
            
            return new PagedResult<DLNM_HRC2>
            {
                Data = data.ToList(),
                TotalRecords = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
    }
}

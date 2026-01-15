using dataproduct.api.DTOs;
using dataproduct.api.Models;
using dataproduct.api.Repositories;
using dataproduct.api.ResponseModels;

namespace dataproduct.api.Services
{
    public class DLNMHRC2Service
    {
        private readonly IDLNMHRC2Repository _repo;
        private readonly HRC2_NMSyncService _hrc2NMSyncService;

        public DLNMHRC2Service(IDLNMHRC2Repository repo, HRC2_NMSyncService hrc2NMSyncService)
        {
            _repo = repo;
            _hrc2NMSyncService = hrc2NMSyncService;
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

        public async Task<HRC2DetailByReportNoModel?> GetByReportNoAsync(int reportNo)
        {
            return await _repo.GetByReportNoAsync(reportNo);
        }

        // public async Task<IEnumerable<HRC2DetailByReportNoModel>> FilterAsync(SyncFromNM_HRC2_Request request)
        // {
        //     await _hrc2NMSyncService.SyncHRC2FromNMAsync(request);

        //     var allData = await _repo.GetAllAsync(request.NgaySX , request.Ca, request.LoaiBM, request.Scope);
        //     var reportNos = allData
        //         .Select(x => (int?)x.REPORT_NO)
        //         .Where(x => x.HasValue && x.Value != 0)
        //         .Select(x => x!.Value)
        //         .Distinct()
        //         .ToList();

        //     var result = new List<HRC2DetailByReportNoModel>();
        //     foreach (var reportNo in reportNos)
        //     {
        //         var detail = await _repo.GetByReportNoAsync(reportNo);
        //         if (detail != null)
        //         {
        //             result.Add(detail);
        //         }
        //     }

        //     return result;
        // }

        public async Task<IEnumerable<HRC2GroupedByReportNoModel>> FilterGroupedAsync(SyncFromNM_HRC2_Request request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            await _hrc2NMSyncService.SyncHRC2FromNMAsync(request);
            var allData = await _repo.GetAllAsync(request.NgaySX, request.Ca, request.LoaiBM, request.Scope);
            var ids = allData
                .Select(x => (int?)x.ID)
                .Where(x => x.HasValue && x.Value != 0)
                .Select(x => x!.Value)
                .ToList();

            var result = new List<HRC2GroupedByReportNoModel>();
            foreach (var id in ids)
            {
                var detail = await _repo.GetByIdGroupedAsync(id);
                if (detail != null)
                {
                    result.Add(detail);
                }
            }

            return result;
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
            entity.ID = id;
            await _repo.UpdateAsync(entity);
            return true;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return false;
            await _repo.DeleteAsync(id);
            return true;
        }

        public async Task<PagedResult<DLNM_HRC2>> SearchWithPagingAsync(DateTime? NgaySX, int? Ca, string? LoaiBM, int? Scope, string? searchText, int page, int pageSize)
        {
            var (data, totalCount) = await _repo.SearchWithPagingAsync(NgaySX, Ca, LoaiBM, Scope, searchText, page, pageSize);
            
            return new PagedResult<DLNM_HRC2>
            {
                Data = data.ToList(),
                TotalRecords = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<bool> ChuyenMeThoiAsync(ChuyenMeThoiRequest request)
        {
            return await _repo.ChuyenMeThoiAsync(request);
        }

        public async Task<IEnumerable<FilterSTD_NXTResponse>> FilterSTD_NXTAsync(FilterSTD_NXTRequest request)
        {
            return await _repo.GetHRC2GroupedByMaterialAsync(request.NgaySX, request.Ca);
        }

    }
}

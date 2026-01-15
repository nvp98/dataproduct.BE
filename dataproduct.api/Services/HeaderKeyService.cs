using dataproduct.api.DTOs;
using dataproduct.api.Models;
using dataproduct.api.Repositories;
using dataproduct.api.ResponseModels;

namespace dataproduct.api.Services
{
    public class HeaderKeyService
    {
        private readonly IHeaderKeyRepository _repo;

        public HeaderKeyService(IHeaderKeyRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<Header_Key>> GetAllAsync()
        {
            return  await _repo.GetAllAsync();
        }

        public async Task<Header_Key?> GetByIdAsync(int id)
        {
            var x = await _repo.GetByIdAsync(id);
            if (x == null) return null;
            return x;
            
        }

        public async Task<Header_Key> CreateAsync(Header_Key entity)
        {
            if (await _repo.ExistsByTenHienThiAsync(entity.TenHienThi))
            {
                throw new InvalidOperationException("Tên hiển thị đã tồn tại.");
            }
            await _repo.AddAsync(entity);
            return entity;
        }

        public async Task<bool> UpdateAsync(int id, Header_Key entity)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return false;
            if (await _repo.ExistsByTenHienThiAsync(entity.TenHienThi, id))
            {
                throw new InvalidOperationException("Tên hiển thị đã tồn tại.");
            }
            // Update existing entity thay vì tạo entity mới để tránh tracking conflict
            existing.TenHienThi = entity.TenHienThi;
            existing.LoaiPhieu = entity.LoaiPhieu;
            existing.Mota = entity.Mota;
            existing.IsActive = entity.IsActive;
            existing.ThuTu = entity.ThuTu;
            existing.IsUsedNXT = entity.IsUsedNXT;
            // KeyGuid không được thay đổi khi update
            await _repo.UpdateAsync(existing);
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            if (await _repo.IsInUseAsync(id))
            {
                throw new InvalidOperationException("Header Key đang được sử dụng, không thể xóa.");
            }
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return false;
            await _repo.DeleteAsync(id);
            return true;
        }

        public async Task<PagedResult<HeaderKey_ResponseModel>> SearchWithPagingAsync(string? searchKey, string? LoaiPhieu, int page, int pageSize)
        {
            var (data, totalCount) = await _repo.SearchWithPagingAsync(searchKey, LoaiPhieu, page, pageSize);
            return new PagedResult<HeaderKey_ResponseModel>
            {
                Data = data.ToList(),
                TotalRecords = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        // Search theo Header_Mappings: mỗi ID_PhuLieu trả về 1 dòng với 1 HeaderKey
        public async Task<PagedResult<HeaderKeyMapping_ResponseModel>> SearchMappingsWithPagingAsync(
            string? searchKey,
            string? LoaiPhieu,
            string? TrangThai,
            bool? IsUsedNXT,
            DateTime? FromDate,
            DateTime? ToDate,
            string? SortThuTu,
            int page,
            int pageSize
        )
        {
            var (data, totalCount) = await _repo.SearchMappingsWithPagingAsync(
                searchKey,
                LoaiPhieu,
                TrangThai,
                IsUsedNXT,
                FromDate,
                ToDate,
                SortThuTu,
                page,
                pageSize
            );
            return new PagedResult<HeaderKeyMapping_ResponseModel>
            {
                Data = data.ToList(),
                TotalRecords = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
    }
}

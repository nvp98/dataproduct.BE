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
            await EnsureThuTuUniqueAsync(entity, excludeId: null);
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
            await EnsureThuTuUniqueAsync(entity, excludeId: id);
            // Update existing entity thay vì tạo entity mới để tránh tracking conflict
            existing.TenHienThi = entity.TenHienThi;
            existing.LoaiPhieu = entity.LoaiPhieu;
            existing.Mota = entity.Mota;
            existing.IsActive = entity.IsActive;
            existing.IsUsedNXT = entity.IsUsedNXT;
            existing.IsUsedThongKe = entity.IsUsedThongKe;
            existing.LoaiThongKe = entity.LoaiThongKe;
            existing.ThuTu_TK_BOF = entity.ThuTu_TK_BOF;
            existing.ThuTu_TK_LF = entity.ThuTu_TK_LF;
            existing.ThuTu_TK_RH = entity.ThuTu_TK_RH;
            existing.IsUsed_Excel = entity.IsUsed_Excel;
            existing.LoaiExcel = entity.LoaiExcel;
            existing.ThuTu_Excel_BOF = entity.ThuTu_Excel_BOF;
            existing.ThuTu_Excel_LF = entity.ThuTu_Excel_LF;
            existing.ThuTu_Excel_RH = entity.ThuTu_Excel_RH;
            existing.ID_NhomKey = entity.ID_NhomKey;
            existing.MaVatTuChiPhi = entity.MaVatTuChiPhi;
            // KeyGuid không được thay đổi khi update
            await _repo.UpdateAsync(existing);
            return true;
        }

        /// <summary>Chặn 2 Header_Key khác nhau dùng chung 1 số thứ tự trong cùng 1 cột
        /// (mỗi cột TT_TK_BOF/TT_TK_LF/TT_TK_RH/TT_Excel_BOF/TT_Excel_LF/TT_Excel_RH là 1 không gian số độc lập).</summary>
        private async Task EnsureThuTuUniqueAsync(Header_Key entity, int? excludeId)
        {
            var checks = new (int? Value, ThuTuColumn Column, string Label)[]
            {
                (entity.ThuTu_TK_BOF, ThuTuColumn.TK_BOF, "Thứ tự Thống kê BOF"),
                (entity.ThuTu_TK_LF, ThuTuColumn.TK_LF, "Thứ tự Thống kê LF"),
                (entity.ThuTu_TK_RH, ThuTuColumn.TK_RH, "Thứ tự Thống kê RH"),
                (entity.ThuTu_Excel_BOF, ThuTuColumn.Excel_BOF, "Thứ tự Excel BOF"),
                (entity.ThuTu_Excel_LF, ThuTuColumn.Excel_LF, "Thứ tự Excel LF"),
                (entity.ThuTu_Excel_RH, ThuTuColumn.Excel_RH, "Thứ tự Excel RH"),
            };

            foreach (var (value, column, label) in checks)
            {
                if (!value.HasValue) continue;
                var conflict = await _repo.FindDuplicateThuTuAsync(column, value.Value, excludeId);
                if (conflict != null)
                    throw new InvalidOperationException(
                        $"{label} = {value.Value} đã được dùng bởi Header Key \"{conflict.TenHienThi}\". Vui lòng chọn số khác.");
            }
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
            bool? IsUsedThongKe,
            DateTime? FromDate,
            DateTime? ToDate,
            int? IdNhom,
            bool? chuaMappingNM,
            string? SortBy,
            string? SortOrder,
            int page,
            int pageSize
        )
        {
            var (data, totalCount) = await _repo.SearchMappingsWithPagingAsync(
                searchKey,
                LoaiPhieu,
                TrangThai,
                IsUsedNXT,
                IsUsedThongKe,
                FromDate,
                ToDate,
                IdNhom,
                chuaMappingNM,
                SortBy,
                SortOrder,
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

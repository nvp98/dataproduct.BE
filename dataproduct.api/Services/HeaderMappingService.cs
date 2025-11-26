using dataproduct.api.DTOs;
using dataproduct.api.Models;
using dataproduct.api.Repositories;

namespace dataproduct.api.Services
{
    public class HeaderMappingService
    {
        private readonly IHeaderMappingRepository _repo;

        public HeaderMappingService(IHeaderMappingRepository repo)
        {
            _repo = repo;
        }

        public async Task<Header_Mapping> CreateAsync(HeaderMappingRequestDto dto)
        {
            if (await _repo.ExistsAsync(dto.ID_PhuLieu, dto.ID_HeaderKey))
            {
                throw new InvalidOperationException("Phụ liệu này đã được móc nối với Header Key đã chọn.");
            }

            var entity = new Header_Mapping
            {
                ID_PhuLieu = dto.ID_PhuLieu,
                ID_HeaderKey = dto.ID_HeaderKey,
                TenNguonDuLieu = string.IsNullOrWhiteSpace(dto.TenNguonDuLieu) ? string.Empty : dto.TenNguonDuLieu.Trim(),
                IsActive = true,
                NgayTao = DateTime.Now
            };

            return await _repo.AddAsync(entity);
        }

        public async Task<bool> UpdateAsync(int id, HeaderMappingRequestDto dto)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null)
            {
                return false;
            }

            if (await _repo.ExistsAsync(dto.ID_PhuLieu, dto.ID_HeaderKey, id))
            {
                throw new InvalidOperationException("Phụ liệu này đã được móc nối với Header Key đã chọn.");
            }

            existing.ID_PhuLieu = dto.ID_PhuLieu;
            existing.ID_HeaderKey = dto.ID_HeaderKey;
            existing.TenNguonDuLieu = string.IsNullOrWhiteSpace(dto.TenNguonDuLieu)
                ? existing.TenNguonDuLieu
                : dto.TenNguonDuLieu.Trim();

            await _repo.UpdateAsync(existing);
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null)
            {
                return false;
            }

            if (existing.ID_HeaderKey != null)
            {
                await _repo.DeleteByHeaderKeyAsync(existing.ID_HeaderKey);
            }
            else
            {
                await _repo.DeleteAsync(id);
            }

            return true;
        }
    }
}


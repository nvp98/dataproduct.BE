using System.Linq;
using dataproduct.api.DTOs;
using dataproduct.api.Models;
using dataproduct.api.Repositories;

namespace dataproduct.api.Services
{
    public class SiloService
    {
        private readonly ISiloRepository _repo;

        public SiloService(ISiloRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<Silo>> GetAllAsync()
        {
            return await _repo.GetAllAsync();
        }

        public async Task<Silo?> GetByIdAsync(int id)
        {
            var x = await _repo.GetByIdAsync(id);
            if (x == null) return null;
            return x;
        }

        public async Task<Silo> CreateAsync(Silo entity)
        {
            if (await _repo.ExistsByTenSiloAsync(entity.TenSilo, entity.NhaMay, entity.BieuMau, entity.Scope))
            {
                throw new InvalidOperationException("Tên silo đã tồn tại trong nhà máy và biểu mẫu này" + (entity.Scope.HasValue ? $" với scope {entity.Scope.Value}" : " (không có scope)") + ".");
            }
            await _repo.AddAsync(entity);
            return entity;
        }

        public async Task<bool> UpdateAsync(int id, Silo entity)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return false;
            if (await _repo.ExistsByTenSiloAsync(entity.TenSilo, entity.NhaMay, entity.BieuMau, entity.Scope, id))
            {
                throw new InvalidOperationException("Tên silo đã tồn tại trong nhà máy và biểu mẫu này" + (entity.Scope.HasValue ? $" với scope {entity.Scope.Value}" : " (không có scope)") + ".");
            }
            // Update existing entity thay vì tạo entity mới để tránh tracking conflict
            existing.TenSilo = entity.TenSilo;
            existing.TheTich = entity.TheTich;
            existing.BieuMau = entity.BieuMau;
            existing.Scope = entity.Scope;
            existing.TinhTrang = entity.TinhTrang;
            existing.NhaMay = entity.NhaMay;
            // NgayTao không được thay đổi khi update
            await _repo.UpdateAsync(existing);
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return false;
            await _repo.DeleteAsync(id);
            return true;
        }

        public async Task<PagedResult<Silo>> SearchMappingsWithPagingAsync(
            string? searchKey,
            int? scope,
            bool? tinhTrang,
            string? BieuMau,
            int? nhaMay,
            int page,
            int pageSize
        )
        {
            var (data, totalCount) = await _repo.SearchMappingsWithPagingAsync(
                searchKey,
                scope,
                tinhTrang,
                BieuMau,
                nhaMay,
                page,
                pageSize
            );
            return new PagedResult<Silo>
            {
                Data = data.ToList(),
                TotalRecords = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<SelectSiloResponse> ResolveSiloAsync(
            List<int> phuLieuNMIds,
            DateTime ngaySX
        )
        {
            if (phuLieuNMIds == null || !phuLieuNMIds.Any())
            {
                throw new InvalidOperationException("Danh sách phụ liệu NM không được rỗng.");
            }

            // Gọi GetValidSilosAsync để lấy danh sách silo hợp lệ
            var validSilos = await _repo.GetValidSilosAsync(phuLieuNMIds, ngaySX);

            // ❌ 0 silo → throw exception
            if (!validSilos.Any())
            {
                throw new InvalidOperationException("Không tìm thấy silo nào chứa các phụ liệu NM này trong ngày sản xuất.");
            }

            // ✔ 1 silo: auto chọn silo
            if (validSilos.Count == 1)
            {
                var silo = validSilos.First();
                var response = new SelectSiloResponse
                {
                    IsAutoSelected = true,
                    SiloId = silo.SiloId,
                    Options = new List<SiloOptionDto>()
                };

                // Tìm giao của PhuLieuNMIds trong silo với phuLieuNMIds đầu vào
                var commonPhuLieuIds = silo.PhuLieuNMIds.Intersect(phuLieuNMIds).ToList();

                // Nếu silo chỉ có 1 NM trong danh sách hợp lệ → auto chọn NM
                if (commonPhuLieuIds.Count == 1)
                {
                    response.PhuLieuNMId = commonPhuLieuIds.First();
                }
                // Nếu nhiều NM → để FE chọn (PhuLieuNMId = null)

                return response;
            }

            // ⚠ >1 silo: trả danh sách option cho FE
            var options = validSilos.Select(s => new SiloOptionDto
            {
                SiloId = s.SiloId,
                TenSilo = s.TenSilo,
                TheTich = s.TheTich,
                PhuLieuNMIds = s.PhuLieuNMIds.Intersect(phuLieuNMIds).ToList(),
                IsAuto = false
            }).ToList();

            return new SelectSiloResponse
            {
                IsAutoSelected = false,
                SiloId = null,
                PhuLieuNMId = null,
                Options = options
            };
        }

        public async Task<SelectPhuLieuNMResponse> ResolvePhuLieuWhenChangeSiloAsync(
            int siloId,
            List<int> phuLieuNMIds,
            DateTime ngaySX
        )
        {
            if (phuLieuNMIds == null || !phuLieuNMIds.Any())
            {
                throw new InvalidOperationException("Danh sách phụ liệu NM không được rỗng.");
            }

            // Lấy danh sách NM trong silo
            var phuLieuInSilo = await _repo.GetPhuLieuNMInSiloAsync(siloId, ngaySX);

            // Giao với phuLieuNMIds hợp lệ
            var validPhuLieuIds = phuLieuInSilo.Intersect(phuLieuNMIds).ToList();

            var response = new SelectPhuLieuNMResponse
            {
                PhuLieuNMIds = validPhuLieuIds
            };

            // Nếu 1 NM → auto
            if (validPhuLieuIds.Count == 1)
            {
                response.IsAutoSelected = true;
                response.PhuLieuNMId = validPhuLieuIds.First();
            }
            // Nếu >1 NM → FE cho chọn (IsAutoSelected = false, PhuLieuNMId = null)

            return response;
        }

        public async Task ValidateBeforeSaveAsync(
            int siloId,
            int phuLieuNMId,
            DateTime ngaySX
        )
        {
            // Kiểm tra silo đang chứa NM và mapping còn hiệu lực
            var isValid = await _repo.IsSiloContainPhuLieuNMAsync(siloId, phuLieuNMId, ngaySX);

            if (!isValid)
            {
                throw new InvalidOperationException(
                    $"Silo ID {siloId} không chứa phụ liệu NM ID {phuLieuNMId} hoặc mapping đã hết hiệu lực vào ngày {ngaySX:yyyy-MM-dd}."
                );
            }
        }

        // MapSiloPhuLieuNM methods
        public async Task<List<MapSiloPhuLieuNMResponse>> GetMappingsBySiloIdAsync(int siloId)
        {
            return await _repo.GetMappingsBySiloIdAsync(siloId);
        }

        public async Task<MapSiloPhuLieuNMResponse> CreateMappingAsync(MapSiloPhuLieuNMPayload payload)
        {
            // Validate silo exists
            var silo = await _repo.GetByIdAsync(payload.SiloId);
            if (silo == null)
            {
                throw new InvalidOperationException($"Không tìm thấy Silo với ID {payload.SiloId}");
            }

            // Validate PhuLieu_NM exists (cần check trong context)
            // Tạm thời bỏ qua validation này, sẽ validate ở controller nếu cần

            var entity = new MapSiloPhuLieuNM
            {
                ID_Silo = payload.SiloId,
                ID_PhuLieuNM = payload.PhuLieuNMId,
                NgayBatDau = payload.NgayBatDau,
                NgayKetThuc = payload.NgayKetThuc,
                IsActive = payload.IsActive
            };

            var created = await _repo.AddMappingAsync(entity);
            
            // Fetch full response with names
            var mappings = await _repo.GetMappingsBySiloIdAsync(payload.SiloId);
            return mappings.FirstOrDefault(m => m.Id == created.ID) 
                ?? throw new InvalidOperationException("Không thể tạo mapping");
        }

        public async Task<bool> UpdateMappingAsync(int id, MapSiloPhuLieuNMPayload payload)
        {
            var existing = await _repo.GetMappingByIdAsync(id);
            if (existing == null) return false;

            existing.ID_Silo = payload.SiloId;
            existing.ID_PhuLieuNM = payload.PhuLieuNMId;
            existing.NgayBatDau = payload.NgayBatDau;
            existing.NgayKetThuc = payload.NgayKetThuc;
            existing.IsActive = payload.IsActive;

            await _repo.UpdateMappingAsync(existing);
            return true;
        }

        public async Task<bool> DeleteMappingAsync(int id)
        {
            var existing = await _repo.GetMappingByIdAsync(id);
            if (existing == null) return false;
            await _repo.DeleteMappingAsync(id);
            return true;
        }

        public async Task<List<PhuLieu_NM>> GetAllPhuLieuNMAsync()
        {
            return await _repo.GetAllPhuLieuNMAsync();
        }

        public async Task<PagedResult<PhuLieu_NM>> SearchPhuLieuNMWithPagingAsync(
            string? searchKey,
            int page,
            int pageSize
        )
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100; // Giới hạn tối đa 100 records mỗi trang

            var (data, totalCount) = await _repo.SearchPhuLieuNMWithPagingAsync(
                searchKey,
                page,
                pageSize
            );

            return new PagedResult<PhuLieu_NM>
            {
                Data = data.ToList(),
                TotalRecords = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<List<DTOs.SiloByHeaderKeyDto>> GetSilosByHeaderKeyAsync(
            int headerKeyId,
            DateTime ngaySX,
            int nhaMay,
            string? bieuMau
        )
        {
            return await _repo.GetSilosByHeaderKeyAsync(headerKeyId, ngaySX, nhaMay, bieuMau);
        }
    }
}



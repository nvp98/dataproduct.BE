using dataproduct.api.DTOs.NMTKVV_Dto;
using dataproduct.api.Models;
using dataproduct.api.Repositories.NMTKVV;

namespace dataproduct.api.Services.NMTKVV
{
    public class TKVV_NvlBbgnMappingService
    {
        private readonly ITKVV_NvlBbgnMappingRepository _mappingRepo;
        private readonly ITKVV_VatTuLookupRepository _vatTuRepo;

        public TKVV_NvlBbgnMappingService(
            ITKVV_NvlBbgnMappingRepository mappingRepo,
            ITKVV_VatTuLookupRepository vatTuRepo)
        {
            _mappingRepo = mappingRepo;
            _vatTuRepo = vatTuRepo;
        }

        public Task<VatTuLookupResultDto> SearchVatTuAsync(string? searchKey, int page, int pageSize)
            => _vatTuRepo.SearchAsync(searchKey, page, pageSize);

        // Gộp dữ liệu 2 DB ở tầng service: mapping (PRODUCT_FORM) + tên Vật tư (PRODUCTDATA).
        public async Task<List<TKVVNvlBbgnMappingDto>> GetListAsync(int? tkvvNvlId)
        {
            var rows = await _mappingRepo.GetListAsync(tkvvNvlId);
            if (rows.Count == 0) return rows;

            var vatTuMap = await _vatTuRepo.GetByIdsAsync(rows.Select(r => r.IdVatTuBBGN));
            foreach (var row in rows)
            {
                if (vatTuMap.TryGetValue(row.IdVatTuBBGN, out var vt))
                {
                    row.TenVatTu = vt.TenVatTu;
                    row.MaVatTuSap = vt.MaVatTuSap;
                    row.TenVatTuSap = vt.TenVatTuSap;
                    row.DonViTinh = vt.DonViTinh;
                }
            }
            return rows;
        }

        public Task<TKVV_NVL_BBGN_Mapping> AddAsync(CreateTKVVNvlBbgnMappingDto dto)
            => _mappingRepo.AddAsync(new TKVV_NVL_BBGN_Mapping
            {
                TKVV_NVL_ID = dto.TkvvNvlId,
                ID_VatTu_BBGN = dto.IdVatTuBBGN,
                GhiChu = dto.GhiChu,
                TrangThai = true,
            });

        public Task<TKVV_NVL_BBGN_Mapping?> UpdateAsync(int id, UpdateTKVVNvlBbgnMappingDto dto)
            => _mappingRepo.UpdateAsync(id, dto.TrangThai, dto.GhiChu);

        public Task<bool> DeleteAsync(int id)
            => _mappingRepo.DeleteAsync(id);
    }
}

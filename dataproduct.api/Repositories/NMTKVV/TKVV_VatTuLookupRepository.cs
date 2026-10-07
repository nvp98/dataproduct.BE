using dataproduct.api.DTOs.NMTKVV_Dto;
using dataproduct.api.Models.MasterData;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories.NMTKVV
{
    // Đọc danh mục Vật tư SAP từ PRODUCTDATA.Tbl_VatTu — DB khác với PRODUCT_FORM
    // (kết nối riêng, khác server) nên không join được bằng LINQ/SQL trực tiếp với
    // TKVV_NVL_BBGN_Mapping; việc gộp dữ liệu thực hiện ở TKVV_NvlBbgnMappingService.
    public class TKVV_VatTuLookupRepository : ITKVV_VatTuLookupRepository
    {
        private readonly ProductDataMasterDbContext _context;

        public TKVV_VatTuLookupRepository(ProductDataMasterDbContext context)
        {
            _context = context;
        }

        private static VatTuLookupDto ToDto(Tbl_VatTu x) => new()
        {
            IdVatTu = x.ID_VatTu,
            TenVatTu = x.TenVatTu,
            MaVatTuSap = x.MaVatTu_Sap,
            TenVatTuSap = x.TenVatTu_Sap,
            DonViTinh = x.DonViTinh,
            IdNhomVatTu = x.ID_NhomVatTu,
            PhongBan = x.PhongBan,
            IdTrangThai = x.ID_TrangThai,
        };

        public async Task<VatTuLookupResultDto> SearchAsync(string? searchKey, int page, int pageSize)
        {
            var query = _context.Tbl_VatTu.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchKey))
            {
                var key = searchKey.Trim();
                query = query.Where(x =>
                    (x.TenVatTu != null && x.TenVatTu.Contains(key)) ||
                    (x.MaVatTu_Sap != null && x.MaVatTu_Sap.Contains(key)) ||
                    (x.TenVatTu_Sap != null && x.TenVatTu_Sap.Contains(key)));
            }

            var total = await query.CountAsync();
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 50 : pageSize;

            var data = await query
                .OrderBy(x => x.TenVatTu)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => ToDto(x))
                .ToListAsync();

            return new VatTuLookupResultDto
            {
                Data = data,
                TotalRecords = total,
                Page = page,
                PageSize = pageSize,
            };
        }

        public async Task<Dictionary<int, VatTuLookupDto>> GetByIdsAsync(IEnumerable<int> ids)
        {
            var idList = ids.Distinct().ToList();
            if (idList.Count == 0) return new Dictionary<int, VatTuLookupDto>();

            var rows = await _context.Tbl_VatTu
                .AsNoTracking()
                .Where(x => idList.Contains(x.ID_VatTu))
                .ToListAsync();

            return rows.ToDictionary(x => x.ID_VatTu, ToDto);
        }
    }
}

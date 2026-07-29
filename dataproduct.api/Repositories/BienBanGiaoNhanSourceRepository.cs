using dataproduct.api.Models.MasterData;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    // Đọc BBGN (Tbl_BienBanGiaoNhan/Tbl_ChiTiet_BienBanGiaoNhan, PRODUCTDATA) qua stored procedure
    // sp_LG_PhanBo_TongNhanVe_BBGN — dùng chung cho QHLC (ID_VatTu=470) và Than cốc <10mm (ID_VatTu=484).
    // SP không lọc theo Xưởng — việc map ID_Xuong -> IDLoCao (LG_PB_Map_Xuong_LoCao, ở PRODUCT_FORM,
    // khác database) và lọc/gộp theo lò cao thực hiện ở tầng C# (PhanBoService).
    public class BienBanGiaoNhanSourceRepository : IBienBanGiaoNhanSourceRepository
    {
        private readonly ProductDataMasterDbContext _context;

        public BienBanGiaoNhanSourceRepository(ProductDataMasterDbContext context)
        {
            _context = context;
        }

        public async Task<List<(int IdXuong, byte? Ca, decimal KhoiLuong)>> GetTongNhanVeAsync(
            DateTime ngay, int idVatTu, IEnumerable<int> idXuongList)
        {
            var idXuongs = idXuongList.ToList();

            var raw = await _context.TongNhanVeBbgnResults
                .FromSqlRaw(
                    "EXEC dbo.sp_LG_PhanBo_TongNhanVe_BBGN @Ngay, @IdVatTu",
                    new SqlParameter("@Ngay", ngay.Date),
                    new SqlParameter("@IdVatTu", idVatTu))
                .AsNoTracking()
                .ToListAsync();

            return raw
                .Where(r => idXuongs.Contains(r.IdXuong))
                .Select(r => (r.IdXuong, ParseCa(r.Ca), (decimal)r.KhoiLuong))
                .ToList();
        }

        // Ca lưu dạng nchar(1) chứa text "1"/"2" trên Tbl_BienBanGiaoNhan
        private static byte? ParseCa(string? ca) => byte.TryParse(ca?.Trim(), out var v) ? v : null;
    }
}

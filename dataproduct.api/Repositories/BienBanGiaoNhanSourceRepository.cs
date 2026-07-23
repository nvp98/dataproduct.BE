using dataproduct.api.Models.MasterData;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    // Đọc Tbl_BienBanGiaoNhan/Tbl_ChiTiet_BienBanGiaoNhan (PRODUCTDATA) — dùng chung cho
    // QHLC (ID_VatTu=470) và Than cốc <10mm (ID_VatTu=484). Không LINQ-join chéo sang
    // ProductFormContext (LG_Map_Xuong_LoCao) ở đây — việc map ID_Xuong -> IDLoCao gộp ở Service.
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

            var query = from bb in _context.Tbl_BienBanGiaoNhan
                        join ct in _context.Tbl_ChiTiet_BienBanGiaoNhan on bb.ID_BBGN equals ct.ID_BBGN
                        where bb.ThoiGianXuLyBG != null
                            && bb.ThoiGianXuLyBG.Value.Date == ngay.Date
                            && bb.ID_TrangThai_BBGN == 1
                            && bb.ID_Xuong_BG != null && idXuongs.Contains(bb.ID_Xuong_BG.Value)
                            && ct.ID_VatTu == idVatTu
                        group ct by new { bb.ID_Xuong_BG, bb.Ca } into g
                        select new { g.Key.ID_Xuong_BG, g.Key.Ca, KhoiLuong = g.Sum(x => x.KL_QuyKho_BG ?? 0) };

            var rows = await query.AsNoTracking().ToListAsync();

            return rows
                .Select(r => (r.ID_Xuong_BG!.Value, ParseCa(r.Ca), (decimal)r.KhoiLuong))
                .ToList();
        }

        // Ca lưu dạng nchar(1) chứa text "1"/"2" trên Tbl_BienBanGiaoNhan
        private static byte? ParseCa(string? ca) => byte.TryParse(ca?.Trim(), out var v) ? v : null;
    }
}

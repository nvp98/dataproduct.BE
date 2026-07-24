using dataproduct.api.DTOs.NMLG_Dto;
using dataproduct.api.Models;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public class NapLieuPhanBoRepository : INapLieuPhanBoRepository
    {
        private readonly ProductFormContext _context;

        public NapLieuPhanBoRepository(ProductFormContext context)
        {
            _context = context;
        }

        // Dùng GiaTri (giá trị từng dòng chi tiết, không lặp lại) — KHÔNG dùng QuyKho vì QuyKho được
        // tính 1 lần cho cả phiếu rồi lưu LẶP LẠI trên mọi dòng cùng (IDPhieu, IDNVL), SUM trực tiếp
        // sẽ nhân giá trị thật lên theo số dòng chi tiết trong phiếu.
        public async Task<List<NapLieuTheoNvlDto>> GetNapLieuAsync(DateTime ngay, int idLoCao)
        {
            return await _context.LG_NL_ChiTiet
                .Where(x => x.Ngay == ngay.Date && x.IDLoCao == idLoCao)
                .GroupBy(x => new { x.IDCa, x.IDNVL })
                .Select(g => new NapLieuTheoNvlDto
                {
                    Ngay = ngay.Date,
                    Ca = (byte?)g.Key.IDCa,
                    IdLoCao = idLoCao,
                    IdNvl = g.Key.IDNVL,
                    KhoiLuongNapLieu = g.Sum(x => x.GiaTri ?? 0)
                })
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<TongNhanVeDto>> GetNapLieuTheoNvlListAsync(DateTime ngay, IEnumerable<int> idNvlList)
        {
            var ids = idNvlList.ToList();
            return await _context.LG_NL_ChiTiet
                .Where(x => x.Ngay == ngay.Date && ids.Contains(x.IDNVL))
                .GroupBy(x => new { x.IDCa, x.IDLoCao })
                .Select(g => new TongNhanVeDto
                {
                    Ngay = ngay.Date,
                    Ca = (byte?)g.Key.IDCa,
                    IdLoCao = g.Key.IDLoCao ?? 0,
                    KhoiLuongNhanVe = g.Sum(x => x.GiaTri ?? 0)
                })
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Dictionary<int, int>> GetMapNvlCvhLoCaoAsync()
        {
            return await _context.LG_Map_NvlCVH_LoCao
                .AsNoTracking()
                .ToDictionaryAsync(x => x.IDLoCao, x => x.IDNVL);
        }
    }
}

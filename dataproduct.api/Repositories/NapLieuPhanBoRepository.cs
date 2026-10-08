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
        // apDungQuyKho = false: KhoiLuongNapLieu trả về cũng là giá trị chưa quy khô (giữ để dùng sau này).
        // Luôn trả kèm KhoiLuongNapLieuTruocQuyKho (chưa quy khô) dùng cho tính tỷ lệ nhóm PP1 Than cốc.
        public async Task<List<NapLieuTheoNvlDto>> GetNapLieuAsync(DateTime ngay, int idLoCao, bool apDungQuyKho = true)
        {
            var raw = await _context.LG_NL_ChiTiet
                .Where(x => x.Ngay == ngay.Date && x.IDLoCao == idLoCao)
                .GroupBy(x => new { x.IDCa, x.IDNVL })
                .Select(g => new
                {
                    g.Key.IDCa,
                    g.Key.IDNVL,
                    KhoiLuongQuyKho = g.Sum(x => (x.ManualGiaTri ? (x.GiaTri ?? 0m) : (x.GiaTri_Goc ?? x.GiaTri ?? 0m)) * (100m - (x.DoAm ?? 0m)) / 100m),
                    KhoiLuongChuaQuyKho = g.Sum(x => x.ManualGiaTri ? (x.GiaTri ?? 0m) : (x.GiaTri_Goc ?? x.GiaTri ?? 0m))
                })
                .AsNoTracking()
                .ToListAsync();

            return raw.Select(g => new NapLieuTheoNvlDto
            {
                Ngay = ngay.Date,
                Ca = (byte?)g.IDCa,
                IdLoCao = idLoCao,
                IdNvl = g.IDNVL,
                KhoiLuongNapLieu = apDungQuyKho ? g.KhoiLuongQuyKho : g.KhoiLuongChuaQuyKho,
                KhoiLuongNapLieuTruocQuyKho = g.KhoiLuongChuaQuyKho
            }).ToList();
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
                    KhoiLuongNhanVe = g.Sum(x => (x.ManualGiaTri ? (x.GiaTri ?? 0m) : (x.GiaTri_Goc ?? x.GiaTri ?? 0m)) * (100m - (x.DoAm ?? 0m)) / 100m)
                })
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Dictionary<int, int>> GetMapNvlCvhLoCaoAsync()
        {
            return await _context.LG_PB_Map_NvlCVH_LoCao
                .AsNoTracking()
                .ToDictionaryAsync(x => x.IDLoCao, x => x.IDNVL);
        }
    }
}

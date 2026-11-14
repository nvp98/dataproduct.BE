using dataproduct.api.Models;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Services.Kdl.HRC1DPV
{
    /// <summary>
    /// BCSX HRC1 Đúc phôi vuông — nguồn thép lỏng (HRC1_MeThep) cho các mẻ đã có từ BKM.
    /// Mã gốc: "TL|{PhanLoai}" = KlThepLong × 1000 (kg); "GHICHU" = đường đi (Tinh luyện / Lên thẳng).
    /// Mẻ có nhiều dòng (bỏ IsGhost): lấy dòng có khối lượng, Id mới nhất.
    /// </summary>
    public class HRC1DPV_MeThepNguon : IKdlNguon
    {
        private readonly ProductFormContext _context;

        public HRC1DPV_MeThepNguon(ProductFormContext context)
        {
            _context = context;
        }

        public string MaModule => HRC1DPV_BKMNguon.Module;
        public string MaNguon => "METHEP";
        public bool CungCapDoiTuong => false;

        public async Task<KdlKetQuaDoc> LayAsync(KdlNguonContext ctx, CancellationToken ct)
        {
            var kq = new KdlKetQuaDoc();
            var maMes = ctx.DoiTuongs.Select(x => x.Me).Distinct().ToList();
            if (maMes.Count == 0) return kq;

            var rows = await _context.HRC1_MeTheps.AsNoTracking()
                .Where(x => x.MaMe != null && maMes.Contains(x.MaMe) && x.IsGhost != true)
                .Select(x => new { x.Id, x.MaMe, x.KlThepLong, x.PhanLoai, x.DichChuyen })
                .ToListAsync(ct);

            foreach (var g in rows.GroupBy(x => x.MaMe!.Trim()))
            {
                var me = g.OrderByDescending(x => x.KlThepLong.HasValue).ThenByDescending(x => x.Id).First();
                var dichChuyen = me.DichChuyen ?? g.Select(x => x.DichChuyen).FirstOrDefault(x => x != null);

                if (me.KlThepLong.HasValue)
                {
                    var phanLoai = string.IsNullOrWhiteSpace(me.PhanLoai) ? "CHUA_PHAN_LOAI" : me.PhanLoai.Trim();
                    kq.GiaTris.Add(new KdlDongDuLieu(g.Key, $"TL|{phanLoai}", me.KlThepLong.Value * 1000));
                }

                var ghiChu = dichChuyen switch
                {
                    "tinh_luyen" => "Tinh luyện",
                    "len_thang" => "Lên thẳng",
                    null or "" => null,
                    _ => dichChuyen,
                };
                if (ghiChu != null)
                    kq.GiaTris.Add(new KdlDongDuLieu(g.Key, "GHICHU", null, ghiChu));
            }
            return kq;
        }
    }
}

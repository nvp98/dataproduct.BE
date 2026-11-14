using dataproduct.api.Models;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Services.Kdl.HRC1DPV
{
    /// <summary>
    /// BCSX HRC1 Đúc phôi vuông — nguồn điện theo ca (DuLieu_DongLuc, kéo từ EMS, MaXuong = DPV).
    /// Điện là số của cả ca: gắn vào mẻ đầu tiên của ca (mẻ A→Z) giống Excel (1 số / ca, cột Điện sheet CSDL),
    /// nên cộng theo ca / kíp / ngày vẫn đúng. Ca không có mẻ nào thì không có chỗ gắn → bỏ qua.
    /// Mã gốc = MaHeader của tag động lực (DPV_DienKhi, DPV_DienNM, DPV_DienTong).
    /// </summary>
    public class HRC1DPV_DienNguon : IKdlNguon
    {
        private const string MaXuongDongLuc = "DPV";

        private readonly ProductFormContext _context;

        public HRC1DPV_DienNguon(ProductFormContext context)
        {
            _context = context;
        }

        public string MaModule => HRC1DPV_BKMNguon.Module;
        public string MaNguon => "EMS";
        public bool CungCapDoiTuong => false;

        public async Task<KdlKetQuaDoc> LayAsync(KdlNguonContext ctx, CancellationToken ct)
        {
            var kq = new KdlKetQuaDoc();
            var meDauCa = ctx.DoiTuongs
                .Where(x => x.CaSX.HasValue)
                .GroupBy(x => (x.NgaySX, CaSX: x.CaSX!.Value))
                .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Me, StringComparer.Ordinal).First().Me);
            if (meDauCa.Count == 0) return kq;

            var rows = await _context.DuLieu_DongLucs.AsNoTracking()
                .Where(x => x.MaXuong == MaXuongDongLuc && x.Ngay >= ctx.TuNgay && x.Ngay <= ctx.DenNgay && x.GiaTri != null)
                .Select(x => new { x.Ngay, x.CaSX, x.MaHeader, x.GiaTri })
                .ToListAsync(ct);

            foreach (var r in rows)
                if (meDauCa.TryGetValue((r.Ngay, r.CaSX), out var me))
                    kq.GiaTris.Add(new KdlDongDuLieu(me, r.MaHeader, r.GiaTri));
            return kq;
        }
    }
}

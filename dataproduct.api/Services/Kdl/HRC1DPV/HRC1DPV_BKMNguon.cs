using dataproduct.api.Models;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Services.Kdl.HRC1DPV
{
    /// <summary>
    /// BCSX HRC1 Đúc phôi vuông — nguồn BKM (BK_PhoiThep), cung cấp đối tượng = mẻ.
    /// Header: Ngày/Ca/Kíp/Mác/Máy đúc của mẻ; KichThuoc = KT chính (dòng không phải ND, nhiều thanh nhất);
    /// LenhSanXuat = LSX tra theo (Mác, KT chính, Máy đúc) trong bảng LenhSanXuat.
    /// Mã gốc: "ST|{NONG|NGUOI}|{TenLoai}" (số thanh), "KL|…" (khối lượng kg), "KT|NGUOI|ND" (KT ngắn dài).
    /// PhanLoaiPhoi: 1 = nóng, 2 = nguội, khác = "KXD" (không map → báo trong nhật ký).
    /// </summary>
    public class HRC1DPV_BKMNguon : IKdlNguon
    {
        public const string Module = "HRC1_DPV";
        private const string LoaiNganDai = "ND";

        private readonly ProductFormContext _context;

        public HRC1DPV_BKMNguon(ProductFormContext context)
        {
            _context = context;
        }

        public string MaModule => Module;
        public string MaNguon => "BKM";
        public bool CungCapDoiTuong => true;

        public async Task<KdlKetQuaDoc> LayAsync(KdlNguonContext ctx, CancellationToken ct)
        {
            var rows = await _context.BkPhoiThep.AsNoTracking()
                .Where(x => x.NgaySx >= ctx.TuNgay && x.NgaySx <= ctx.DenNgay && x.Me != null && x.Me != "")
                .Select(x => new { x.Me, x.NgaySx, x.Ca, x.Kip, x.Mac, x.MayDuc, x.KichThuoc, x.TenLoai, x.PhanLoaiPhoi, x.SoThanh, x.TongKhoiLuog })
                .ToListAsync(ct);

            var lsx = (await _context.DPV_LenhSanXuats.AsNoTracking()
                    .Where(x => !x.IsDelete && x.LenhSanXuat != null && x.LenhSanXuat != "")
                    .Select(x => new { x.Mac, x.KichThuoc, x.MayDuc, x.LenhSanXuat })
                    .ToListAsync(ct))
                .GroupBy(x => (Mac: x.Mac.Trim().ToUpperInvariant(), Kt: x.KichThuoc.Trim().ToUpperInvariant(), x.MayDuc))
                .ToDictionary(g => g.Key, g => g.First().LenhSanXuat);

            var kq = new KdlKetQuaDoc();
            foreach (var me in rows.GroupBy(x => x.Me.Trim()))
            {
                var chinh = me.Where(x => !string.Equals(x.TenLoai?.Trim(), LoaiNganDai, StringComparison.OrdinalIgnoreCase)).ToList();
                if (chinh.Count == 0) chinh = me.ToList();

                var ktChinh = chinh
                    .Where(x => !string.IsNullOrWhiteSpace(x.KichThuoc))
                    .GroupBy(x => x.KichThuoc!.Trim())
                    .OrderByDescending(g => g.Sum(x => x.SoThanh ?? 0))
                    .Select(g => g.Key)
                    .FirstOrDefault();
                var dau = chinh[0];
                var mac = dau.Mac?.Trim();

                string? lenhSanXuat = null;
                if (mac != null && ktChinh != null && dau.MayDuc.HasValue)
                    lsx.TryGetValue((mac.ToUpperInvariant(), ktChinh.ToUpperInvariant(), dau.MayDuc.Value), out lenhSanXuat);

                kq.DoiTuongs.Add(new KdlDoiTuongVao
                {
                    Me = me.Key,
                    NgaySX = dau.NgaySx,
                    CaSX = (byte)dau.Ca,
                    Kip = dau.Kip?.Trim(),
                    Mac = mac,
                    MayDuc = dau.MayDuc?.ToString(),
                    KichThuoc = ktChinh,
                    LenhSanXuat = lenhSanXuat,
                });

                foreach (var r in me)
                {
                    var nhom = r.PhanLoaiPhoi switch { 1 => "NONG", 2 => "NGUOI", _ => "KXD" };
                    var loai = r.TenLoai?.Trim() ?? "";
                    kq.GiaTris.Add(new KdlDongDuLieu(me.Key, $"ST|{nhom}|{loai}", r.SoThanh ?? 0));
                    kq.GiaTris.Add(new KdlDongDuLieu(me.Key, $"KL|{nhom}|{loai}", (decimal)(r.TongKhoiLuog ?? 0)));
                    if (string.Equals(loai, LoaiNganDai, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(r.KichThuoc))
                        kq.GiaTris.Add(new KdlDongDuLieu(me.Key, $"KT|{nhom}|{loai}", null, r.KichThuoc.Trim()));
                }
            }
            return kq;
        }
    }
}

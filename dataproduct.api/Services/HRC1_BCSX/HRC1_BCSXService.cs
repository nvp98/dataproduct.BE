using System.Text.Json;
using System.Text.Json.Serialization;
using dataproduct.api.Models;
using dataproduct.api.Models.MasterData;
using dataproduct.api.Repositories;
using dataproduct.api.Services.Kdl;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Services
{
    /// <summary>Dữ liệu gợi ý cho 1 Biên bản chốt số liệu (lấy từ Kho dữ liệu động) + cảnh báo.</summary>
    public class HRC1_BCSXGoiY : HRC1_BCSXChiTiet
    {
        public List<string> CanhBao { get; set; } = [];
    }

    /// <summary>
    /// Biên bản chốt số liệu Xưởng Đúc phôi vuông (BM.14/QT.05.11, MaBM = HRC1_BCSX_DPV).
    /// - Gợi ý số liệu theo ngày từ form CSDL HRC1_DPV_CSDL (Kho dữ liệu động) + lịch kíp Tbl_Kip.
    /// - Ghi 4 bảng HRC1_BC_* từ DataJson của phiếu (gọi bởi HRC1_BCSXJsonInitializer khi lưu phiếu).
    /// Tài liệu: docs/BCSX_HRC1_DucVuong/02-BienBan-ChotSoLieu.md
    /// </summary>
    public class HRC1_BCSXService
    {
        public const string MaBM = "HRC1_BCSX_DPV";
        public const string MaFormCSDL = "HRC1_DPV_CSDL";

        // Khóa mảng trong DataJson (FE gửi đúng tên này)
        private const string KhoaThepLong = "thepLong";
        private const string KhoaDien = "dien";
        private const string KhoaNguyenVatLieu = "nguyenVatLieu";
        private const string KhoaThanhPham = "thanhPham";

        private static readonly JsonSerializerOptions JsonDong = new()
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
        };

        private readonly IHRC1_BCSXRepository _repo;
        private readonly KdlXemService _kdl;
        private readonly ProductDataMasterDbContext _master;

        public HRC1_BCSXService(IHRC1_BCSXRepository repo, KdlXemService kdl, ProductDataMasterDbContext master)
        {
            _repo = repo;
            _kdl = kdl;
            _master = master;
        }

        public Task<HRC1_BCSXChiTiet> GetChiTietAsync(Guid idPhieu) => _repo.GetByPhieuAsync(idPhieu);

        // ── Gợi ý từ Kho dữ liệu ───────────────────────────────────────────────

        /// <summary>
        /// Số liệu gợi ý của ngày <paramref name="ngaySX"/> (kỳ 08h ngày N → 08h ngày N+1) từ CSDL HRC1_DPV:
        /// Thép lỏng (Kíp × Mác × LSX: số mẻ, Xuất = Σ TL_Tong), Điện (Σ Dien theo kíp), Thành phẩm (theo mẻ + dòng ngắn dài).
        /// NVL không có nguồn → trống. Dòng gợi ý có Nguon = 1.
        /// </summary>
        public async Task<HRC1_BCSXGoiY> GetGoiYAsync(DateOnly ngaySX)
        {
            var kq = new HRC1_BCSXGoiY();
            var dongs = await _kdl.GetTatCaDongAsync(MaFormCSDL, new KdlLoc { TuNgay = ngaySX, DenNgay = ngaySX });
            if (dongs == null)
            {
                kq.CanhBao.Add($"Không tìm thấy form {MaFormCSDL} trong Kho dữ liệu.");
                return kq;
            }
            if (dongs.Count == 0)
            {
                kq.CanhBao.Add($"Chưa có dữ liệu CSDL Đúc phôi vuông ngày {ngaySX:dd/MM/yyyy} — hãy Đồng bộ ở màn hình CSDL rồi lấy lại.");
                return kq;
            }

            // Lịch kíp trực: (TenCa) → (ID_Kip, TenKip)
            var lichKip = await _master.Tbl_Kip.AsNoTracking()
                .Where(k => k.NgayLamViec == ngaySX)
                .ToListAsync();
            Tbl_Kip? TraKip(byte? ca) => ca == null ? null : lichKip.FirstOrDefault(k => k.TenCa == ca.Value.ToString());

            // Thứ tự biên bản: ca 1 trước ca 2, trong ca theo mẻ A→Z
            var theoCa = dongs.OrderBy(d => d.CaSX ?? 9).ThenBy(d => d.Me, StringComparer.OrdinalIgnoreCase).ToList();

            void GanKip(HRC1_BC_DongBase x, KdlDong d)
            {
                var k = TraKip(d.CaSX);
                x.NgaySX = ngaySX;
                x.CaSX = d.CaSX is 1 or 2 ? d.CaSX : null;
                x.IdKip = k?.ID_Kip;
                x.Kip = ChuanKip(d.Kip) ?? ChuanKip(k?.TenKip);
                x.Nguon = KdlConst.Nguon.TuDong;
                if (k != null && !string.IsNullOrWhiteSpace(d.Kip) && !string.Equals(ChuanKip(d.Kip), ChuanKip(k.TenKip), StringComparison.OrdinalIgnoreCase))
                    kq.CanhBao.Add($"Mẻ {d.Me}: kíp trên BKM ({d.Kip}) khác lịch kíp ca {d.CaSX} ({k.TenKip}).");
            }

            // I. Thép lỏng — Kíp × Mác × LSX
            foreach (var g in theoCa.GroupBy(d => (d.CaSX, Kip: ChuanKip(d.Kip), d.Mac, d.LenhSanXuat)))
            {
                var dau = g.First();
                var x = new HRC1_BC_ThepLong
                {
                    Mac = dau.Mac ?? "",
                    SoLuongMe = g.Count(),
                    MayDuc = g.Select(d => SoNguyen(d.MayDuc)).Where(m => m != null)
                              .GroupBy(m => m).OrderByDescending(m => m.Count()).Select(m => m.Key).FirstOrDefault(),
                    Xuat = TongKey(g, "TL_Tong"),
                    LenhSanXuat = dau.LenhSanXuat,
                };
                GanKip(x, dau);
                if (string.IsNullOrWhiteSpace(x.LenhSanXuat))
                    kq.CanhBao.Add($"Thép lỏng {x.Mac} kíp {x.Kip}: chưa có LSX (điền ở màn hình LSX Đúc phôi vuông rồi Đồng bộ).");
                kq.ThepLong.Add(x);
            }

            // II. Điện — Σ Dien theo kíp (điện cả ca gắn vào mẻ đầu ca)
            foreach (var g in theoCa.GroupBy(d => (d.CaSX, Kip: ChuanKip(d.Kip))))
            {
                var dien = TongKey(g, "Dien");
                if (dien == null) continue;
                var x = new HRC1_BC_Dien { SoLieu = dien };
                GanKip(x, g.First());
                kq.Dien.Add(x);
            }
            if (kq.Dien.Count == 0)
                kq.CanhBao.Add("Chưa có số điện (tag DPV_DienTong) cho ngày này.");

            // IV. Thành phẩm — 1 dòng / mẻ; mẻ có ngắn dài thêm 1 dòng KT ngắn dài (xếp cuối kíp)
            foreach (var g in theoCa.GroupBy(d => (d.CaSX, Kip: ChuanKip(d.Kip))))
            {
                var nganDai = new List<HRC1_BC_ThanhPham>();
                foreach (var d in g)
                {
                    var tp = new HRC1_BC_ThanhPham
                    {
                        Me = d.Me,
                        Mac = d.Mac,
                        KichThuoc = d.KichThuoc,
                        MayDuc = SoNguyen(d.MayDuc),
                        LenhSanXuat = d.LenhSanXuat,
                        Nguoi_SoThanh = SoNguyenKey(d, "TongSoThanhNguoiTruND"),
                        Nguoi_Loai1 = Key(d, "Nguoi_KhoiLuongL1"),
                        // Loại II BM của biên bản gồm cả giao thoa
                        Nguoi_Loai2BM = Cong(Key(d, "Nguoi_KhoiLuongL2BM"), Key(d, "Nguoi_KhoiLuongL2GiaoCa")),
                        Nguoi_Loai2TPHH = Key(d, "Nguoi_KhoiLuongL2TPHH"),
                        Nguoi_Loai3 = Key(d, "Nguoi_KhoiLuongL3"),
                        Nong_SoThanh = SoNguyenKey(d, "TongSoThanhNong"),
                        Nong_Loai1 = Key(d, "Nong_KhoiLuongL1"),
                        Nong_Loai2BM = Key(d, "Nong_KhoiLuongL2BM"),
                        Nong_Loai2TPHH = Key(d, "Nong_KhoiLuongL2TPHH"),
                        Nong_Loai3 = Key(d, "Nong_KhoiLuongL3"),
                    };
                    GanKip(tp, d);
                    kq.ThanhPham.Add(tp);

                    var stND = SoNguyenKey(d, "Nguoi_SoThanhL2ND");
                    var klND = Key(d, "Nguoi_KhoiLuongL2ND");
                    if ((stND ?? 0) != 0 || (klND ?? 0) != 0)
                    {
                        var ktND = d.GiaTri.GetValueOrDefault("Nguoi_KichThuocND") as string;
                        if (string.IsNullOrWhiteSpace(ktND))
                            kq.CanhBao.Add($"Mẻ {d.Me}: có phôi ngắn dài nhưng thiếu kích thước ngắn dài.");
                        var nd = new HRC1_BC_ThanhPham
                        {
                            Me = d.Me,
                            Mac = d.Mac,
                            KichThuoc = string.IsNullOrWhiteSpace(ktND) ? $"{d.KichThuoc} (ND)" : ktND.Trim(),
                            MayDuc = SoNguyen(d.MayDuc),
                            LenhSanXuat = d.LenhSanXuat,
                            LaNganDai = true,
                            Nguoi_SoThanh = stND,
                            Nguoi_Loai1 = 0,
                            Nguoi_Loai2BM = klND,
                        };
                        GanKip(nd, d);
                        nganDai.Add(nd);
                    }
                }
                kq.ThanhPham.AddRange(nganDai);
            }

            if (lichKip.Count == 0)
                kq.CanhBao.Add($"Chưa có lịch kíp (Tbl_Kip) ngày {ngaySX:dd/MM/yyyy} — kíp lấy theo BKM.");

            kq.CanhBao = kq.CanhBao.Distinct().ToList();
            return kq;
        }

        // ── Ghi bảng phụ từ DataJson (hook sau khi lưu phiếu) ──────────────────

        /// <summary>
        /// Đọc 4 mảng thepLong / dien / nguyenVatLieu / thanhPham trong DataJson → thay toàn bộ dòng của phiếu ở 4 bảng HRC1_BC_*.
        /// Kíp / ca / ngày lấy theo từng dòng; ngày mặc định = NgaySX của phiếu. Trả cảnh báo cho FE.
        /// </summary>
        public async Task<List<string>> InsertFromPhieuJsonAsync(BmPhieu phieu)
        {
            var canhBao = new List<string>();
            if (string.IsNullOrWhiteSpace(phieu.DataJson)) return canhBao;

            using var doc = JsonDocument.Parse(phieu.DataJson);
            var root = doc.RootElement;

            var ngaySX = phieu.NgaySX ?? DocNgay(root);
            if (ngaySX == null)
            {
                canhBao.Add("Phiếu chưa có Ngày — chưa lưu được chi tiết biên bản.");
                return canhBao;
            }

            var lichKip = await _master.Tbl_Kip.AsNoTracking().Where(k => k.NgayLamViec == ngaySX).ToListAsync();
            var nguoi = phieu.NguoiTaoId?.ToString();
            var bayGio = DateTime.Now;

            void ChuanHoa(HRC1_BC_DongBase x)
            {
                x.PhieuID = phieu.Idphieu;
                x.NgaySX = ngaySX.Value;
                x.Kip = ChuanKip(x.Kip);
                if (x.CaSX is not (1 or 2)) x.CaSX = null;
                // Thiếu ca → suy từ kíp theo lịch; có ca → lấy IdKip theo lịch
                if (x.CaSX == null && x.Kip != null)
                {
                    var k = lichKip.FirstOrDefault(l => string.Equals(ChuanKip(l.TenKip), x.Kip, StringComparison.OrdinalIgnoreCase));
                    if (k != null && byte.TryParse(k.TenCa, out var ca)) { x.CaSX = ca; x.IdKip = k.ID_Kip; }
                }
                else if (x.CaSX != null)
                    x.IdKip ??= lichKip.FirstOrDefault(l => l.TenCa == x.CaSX.Value.ToString())?.ID_Kip;
                if (x.Nguon is not (1 or 3 or 4)) x.Nguon = KdlConst.Nguon.NhapTay;
                x.GhiChu = RongThanhNull(x.GhiChu);
                x.IsDelete = false;
                x.NguoiTao = nguoi;
                x.NgayTao = bayGio;
                x.NguoiCapNhat = nguoi;
                x.NgayCapNhat = bayGio;
            }

            var data = new HRC1_BCSXChiTiet
            {
                ThepLong = DocMang<HRC1_BC_ThepLong>(root, KhoaThepLong)
                    .Where(x => !string.IsNullOrWhiteSpace(x.Mac)).ToList(),
                Dien = DocMang<HRC1_BC_Dien>(root, KhoaDien)
                    .Where(x => x.SoLieu != null || !string.IsNullOrWhiteSpace(x.Kip)).ToList(),
                NguyenVatLieu = DocMang<HRC1_BC_NguyenVatLieu>(root, KhoaNguyenVatLieu)
                    .Where(x => !string.IsNullOrWhiteSpace(x.NoiDung)).ToList(),
                ThanhPham = DocMang<HRC1_BC_ThanhPham>(root, KhoaThanhPham)
                    .Where(x => !string.IsNullOrWhiteSpace(x.Me)).ToList(),
            };

            foreach (var x in data.ThepLong)
            {
                ChuanHoa(x);
                x.Mac = x.Mac.Trim();
                x.DonViTinh = RongThanhNull(x.DonViTinh) ?? "kg";
                x.LenhSanXuat = RongThanhNull(x.LenhSanXuat);
            }
            foreach (var x in data.Dien)
            {
                ChuanHoa(x);
                x.NoiDung = RongThanhNull(x.NoiDung) ?? "Điện";
                x.DonViTinh = RongThanhNull(x.DonViTinh) ?? "Kwh";
            }
            foreach (var x in data.NguyenVatLieu)
            {
                ChuanHoa(x);
                x.NoiDung = x.NoiDung.Trim();
                x.DonViTinh = RongThanhNull(x.DonViTinh) ?? "Kg";
                x.BoPhanLienQuan = RongThanhNull(x.BoPhanLienQuan);
                x.LenhSanXuat = RongThanhNull(x.LenhSanXuat);
            }
            foreach (var x in data.ThanhPham)
            {
                ChuanHoa(x);
                x.Me = x.Me.Trim();
                x.KichThuoc = RongThanhNull(x.KichThuoc);
                x.LenhSanXuat = RongThanhNull(x.LenhSanXuat);
                x.Tong = null;   // cột tính trong DB
            }

            // Trùng khóa → báo rõ thay vì để DB ném lỗi unique
            var trung = data.ThepLong.GroupBy(x => (x.Kip, x.Mac.ToUpperInvariant(), x.LenhSanXuat)).Where(g => g.Count() > 1)
                .Select(g => $"Thép lỏng trùng dòng: kíp {g.Key.Kip}, mác {g.First().Mac}, LSX {g.Key.LenhSanXuat}.")
                .Concat(data.Dien.GroupBy(x => (x.Kip, x.NoiDung)).Where(g => g.Count() > 1)
                    .Select(g => $"Điện trùng dòng: kíp {g.Key.Kip}, {g.Key.NoiDung}."))
                .Concat(data.ThanhPham.GroupBy(x => (x.Me.ToUpperInvariant(), x.KichThuoc)).Where(g => g.Count() > 1)
                    .Select(g => $"Thành phẩm trùng dòng: mẻ {g.First().Me}, KT {g.Key.KichThuoc}."))
                .ToList();
            if (trung.Count > 0)
            {
                canhBao.Add("Chưa lưu được chi tiết biên bản: " + string.Join(" ", trung));
                return canhBao;
            }

            await _repo.ReplaceByPhieuAsync(phieu.Idphieu, data);

            if (data.ThepLong.Count + data.Dien.Count + data.NguyenVatLieu.Count + data.ThanhPham.Count == 0)
                canhBao.Add("Biên bản chưa có dòng số liệu nào.");
            return canhBao;
        }

        // ── Helpers ─────────────────────────────────────────────────────────────

        private static List<T> DocMang<T>(JsonElement root, string khoa)
        {
            foreach (var p in root.EnumerateObject())
                if (string.Equals(p.Name, khoa, StringComparison.OrdinalIgnoreCase) && p.Value.ValueKind == JsonValueKind.Array)
                    return p.Value.EnumerateArray()
                        .Where(e => e.ValueKind == JsonValueKind.Object)
                        .Select(e => e.Deserialize<T>(JsonDong))
                        .Where(x => x != null)
                        .Select(x => x!)
                        .ToList();
            return [];
        }

        private static DateOnly? DocNgay(JsonElement root)
        {
            foreach (var k in new[] { "NgaySX", "ngaySX", "ngay" })
                if (root.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String
                    && DateTime.TryParse(v.GetString(), out var dt))
                    return DateOnly.FromDateTime(dt);
            return null;
        }

        private static string? ChuanKip(string? kip)
        {
            var k = kip?.Trim();
            return string.IsNullOrEmpty(k) ? null : k[..1].ToUpperInvariant();
        }

        private static string? RongThanhNull(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        private static int? SoNguyen(string? s) => int.TryParse(s?.Trim(), out var n) ? n : null;

        private static decimal? Key(KdlDong d, string maKey)
            => d.GiaTri.TryGetValue(maKey, out var v) && v is decimal so ? so : null;

        private static int? SoNguyenKey(KdlDong d, string maKey)
            => Key(d, maKey) is decimal so ? (int)Math.Round(so) : null;

        private static decimal? Cong(decimal? a, decimal? b) => a == null && b == null ? null : (a ?? 0) + (b ?? 0);

        private static decimal? TongKey(IEnumerable<KdlDong> ds, string maKey)
        {
            decimal? tong = null;
            foreach (var d in ds)
                if (Key(d, maKey) is decimal so) tong = (tong ?? 0) + so;
            return tong;
        }
    }
}

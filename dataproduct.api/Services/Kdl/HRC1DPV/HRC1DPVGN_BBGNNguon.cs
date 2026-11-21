using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using dataproduct.api.Models;
using dataproduct.api.Models.MasterData;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Services.Kdl.HRC1DPV
{
    /// <summary>
    /// BCSX HRC1 Đúc phôi vuông — nguồn Giao nhận (module HRC1_DPV_GN): API BM_11/ThongKeSoLieuData,
    /// lọc bên giao NM.HRC1 – X.ĐPV (tham số ở appsettings "KdlBbgn"). 1 đối tượng = 1 dòng chi tiết biên bản:
    ///   Me = "{id}|{maLo}" (+ "|2", "|3"… khi trùng trong cùng phiếu) · Mac = tenVatTu · KichThuoc = maLo · MayDuc = BP bên nhận (viết tắt).
    /// Mã gốc = tên trường API (klBN, klBG, xuongBN…); "BP_BG" = BP bên giao, "NOIDUNG1" = Nội dung 1 — tra 2 bảng
    /// HRC1_BC_XuongChuanHoa / HRC1_BC_NVLChuanHoa (theo ID xưởng/BP qua Tbl_Xuong + Tbl_PhongBan trước, không ra ID thì theo tên);
    /// không tra được → "N/A" + cảnh báo vào KDL_SyncLog.
    /// Nghiệp vụ: docs/BCSX_HRC1_DucVuong/03-GiaoNhan-Form-Dong.md
    /// </summary>
    public class HRC1DPVGN_BBGNNguon : IKdlNguon
    {
        public const string Module = "HRC1_DPV_GN";
        public const string KhongTraDuoc = "N/A";

        private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };
        private static readonly Regex _nhieuKhoangTrang = new(@"\s+", RegexOptions.Compiled);

        private readonly IHttpClientFactory _httpFactory;
        private readonly IConfiguration _configuration;
        private readonly ProductFormContext _context;
        private readonly ProductDataMasterDbContext _master;

        public HRC1DPVGN_BBGNNguon(IHttpClientFactory httpFactory, IConfiguration configuration,
            ProductFormContext context, ProductDataMasterDbContext master)
        {
            _httpFactory = httpFactory;
            _configuration = configuration;
            _context = context;
            _master = master;
        }

        public string MaModule => Module;
        public string MaNguon => "BBGN";
        public bool CungCapDoiTuong => true;

        public const string ChieuGiao = "Giao";
        public const string ChieuNhan = "Nhận";

        public async Task<KdlKetQuaDoc> LayAsync(KdlNguonContext ctx, CancellationToken ct)
        {
            var kq = new KdlKetQuaDoc();
            // 2 chiều: X.ĐPV là bên giao + bên nhận. 1 phiếu chỉ thuộc 1 chiều (id khác nhau) → bỏ trùng theo id cho chắc.
            var cfg = _configuration.GetSection("KdlBbgn");
            var giao = await DocApiAsync(ctx.TuNgay, ctx.DenNgay, ThamSoChieu(cfg, "Giao"), ct);
            var nhan = await DocApiAsync(ctx.TuNgay, ctx.DenNgay, ThamSoChieu(cfg, "Nhan"), ct);
            var idGiao = giao.Select(d => d.Id).ToHashSet();
            giao.ForEach(d => d.Chieu = ChieuGiao);
            nhan.ForEach(d => d.Chieu = ChieuNhan);
            var dong = giao.Concat(nhan.Where(d => !idGiao.Contains(d.Id))).ToList();
            if (dong.Count == 0) return kq;

            var traCuu = await TraCuu.TaoAsync(_context, _master, ct);

            // Khóa dòng: {id}|{maLo}; trùng trong cùng phiếu → đánh số theo thứ tự cố định để đồng bộ lại ra đúng dòng cũ
            var theoKhoa = dong
                .GroupBy(d => $"{d.Id}|{Cat(d.MaLo?.Trim(), 40)}")
                .SelectMany(g => g
                    .OrderBy(d => d.TenVatTu, StringComparer.Ordinal)
                    .ThenBy(d => So(d.KlBG) ?? 0).ThenBy(d => So(d.KlBN) ?? 0)
                    .ThenBy(d => d.GhiChu, StringComparer.Ordinal)
                    .Select((d, i) => (Me: i == 0 ? g.Key : $"{g.Key}|{i + 1}", Dong: d)));

            var boQuaNgay = 0;
            foreach (var (me, d) in theoKhoa)
            {
                if (!DateOnly.TryParseExact(d.Ngay?.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var ngay)
                    || ngay < ctx.TuNgay || ngay > ctx.DenNgay)
                {
                    boQuaNgay++;
                    continue;
                }

                var bpNhan = traCuu.VietTat(d.XuongBN, d.PbBN, kq.CanhBao);
                var bpGiao = traCuu.VietTat(d.XuongBG, d.PbBG, kq.CanhBao);
                // BP đối tác = xưởng phía bên kia của X.ĐPV; Nội dung 1 tra theo (vật tư, BP đối tác)
                var bpDoiTac = d.Chieu == ChieuNhan ? bpGiao : bpNhan;
                var noiDung1 = bpDoiTac == KhongTraDuoc ? KhongTraDuoc : traCuu.Nhom1(d.TenVatTu, bpDoiTac, kq.CanhBao);

                kq.DoiTuongs.Add(new KdlDoiTuongVao
                {
                    Me = me,
                    NgaySX = ngay,
                    CaSX = byte.TryParse(d.Ca?.Trim(), out var ca) && (ca == 1 || ca == 2) ? ca : (byte?)null,
                    Kip = Rong(d.Kip)?.Substring(0, 1).ToUpperInvariant(),
                    Mac = Cat(Rong(d.TenVatTu), 50),
                    KichThuoc = Cat(Rong(d.MaLo), 50),
                    MayDuc = Cat(bpNhan, 30),
                });

                ThemSo(kq, me, "klBN", d.KlBN);
                ThemSo(kq, me, "klQuyKhoBN", d.KlQuyKhoBN);
                ThemSo(kq, me, "klBG", d.KlBG);
                ThemSo(kq, me, "klQuyKhoBG", d.KlQuyKhoBG);
                ThemChu(kq, me, "dvt", d.Dvt);
                ThemChu(kq, me, "xuongBN", d.XuongBN);
                ThemChu(kq, me, "pbBN", d.PbBN);
                ThemChu(kq, me, "xuongBG", d.XuongBG);
                ThemChu(kq, me, "pbBG", d.PbBG);
                ThemChu(kq, me, "ghiChu", d.GhiChu);
                ThemChu(kq, me, "soPhieu", d.SoPhieu);
                ThemChu(kq, me, "trangThai", d.TrangThai);
                ThemChu(kq, me, "BP_BG", bpGiao);
                ThemChu(kq, me, "NOIDUNG1", noiDung1);
                ThemChu(kq, me, "CHIEU", d.Chieu);
                ThemChu(kq, me, "BP_DOITAC", bpDoiTac);
            }

            if (boQuaNgay > 0) kq.CanhBao.Add($"{boQuaNgay} dòng bỏ qua vì ngày không hợp lệ / ngoài khoảng đồng bộ");
            return kq;
        }

        // ─── Đọc API (lặp hết các trang) ────────────────────────────────────────

        /// <summary>
        /// Tham số lọc 1 chiều: mục con "Giao" / "Nhan" của KdlBbgn (vd ID_PhongBanBG, ID_XuongBG). Thiếu thì dùng mặc định
        /// X.ĐPV (60 / 156); chiều Giao còn đọc khóa cũ ID_PhongBanBG / ID_XuongBG ở cấp KdlBbgn.
        /// </summary>
        private static Dictionary<string, string?> ThamSoChieu(IConfigurationSection cfg, string chieu)
        {
            var sec = cfg.GetSection(chieu);
            var thamSo = sec.GetChildren().ToDictionary(c => c.Key, c => c.Value, StringComparer.OrdinalIgnoreCase);
            if (thamSo.Count == 0)
                thamSo = chieu == "Giao"
                    ? new(StringComparer.OrdinalIgnoreCase) { ["ID_PhongBanBG"] = cfg["ID_PhongBanBG"] ?? "60", ["ID_XuongBG"] = cfg["ID_XuongBG"] ?? "156" }
                    : new(StringComparer.OrdinalIgnoreCase) { ["ID_PhongBanBN"] = "60", ["ID_XuongBN"] = "156" };
            thamSo["trangThaiList"] = cfg["TrangThaiList"] ?? "0,1";
            return thamSo;
        }

        private async Task<List<BbgnDong>> DocApiAsync(DateOnly tuNgay, DateOnly denNgay, Dictionary<string, string?> thamSo, CancellationToken ct)
        {
            var cfg = _configuration.GetSection("KdlBbgn");
            var baseUrl = cfg["BaseUrl"] ?? "https://report.hoaphatdungquat.vn/BM_11/ThongKeSoLieuData";
            var pageSize = cfg.GetValue("PageSize", 500);

            var client = _httpFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(cfg.GetValue("TimeoutSeconds", 120));

            var ketQua = new List<BbgnDong>();
            for (var page = 1; ; page++)
            {
                var query = new List<string>
                {
                    $"begind={tuNgay:yyyy-MM-dd}",
                    $"endd={denNgay:yyyy-MM-dd}",
                    $"page={page}",
                    $"pageSize={pageSize}",
                };
                query.AddRange(thamSo.Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
                    .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value!)}"));

                var resp = await client.GetFromJsonAsync<BbgnTrang>($"{baseUrl.TrimEnd('/')}?{string.Join("&", query)}", _json, ct);
                if (resp?.Data == null || resp.Data.Count == 0) break;
                ketQua.AddRange(resp.Data);
                if (page >= resp.TotalPages) break;
            }
            return ketQua;
        }

        // ─── Tra cứu 2 bảng chuẩn hóa ──────────────────────────────────────────

        private sealed class TraCuu
        {
            private Dictionary<(string Xuong, string PhongBan), (int IdXuong, int? IdPhongBan)> _idMaster = [];
            private List<HRC1_BC_XuongChuanHoa> _xuong = [];
            private List<HRC1_BC_NVLChuanHoa> _nvl = [];
            private readonly HashSet<string> _daBao = new(StringComparer.OrdinalIgnoreCase);

            public static async Task<TraCuu> TaoAsync(ProductFormContext context, ProductDataMasterDbContext master, CancellationToken ct)
            {
                var t = new TraCuu();

                // Tên xưởng + tên phòng ban (master) → ID; trùng tên thì ưu tiên xưởng đang hoạt động, có phòng ban
                var xuongs = await master.Tbl_Xuong.AsNoTracking()
                    .Select(x => new { x.ID_Xuong, x.TenXuong, x.ID_PhongBan, x.ID_TrangThai })
                    .ToListAsync(ct);
                var phongBans = await master.Tbl_PhongBan.AsNoTracking()
                    .Select(p => new { p.ID_PhongBan, p.TenPhongBan })
                    .ToDictionaryAsync(p => p.ID_PhongBan, p => p.TenPhongBan, ct);
                t._idMaster = xuongs
                    .Where(x => x.ID_PhongBan.HasValue && !string.IsNullOrWhiteSpace(x.TenXuong) && phongBans.ContainsKey(x.ID_PhongBan.Value))
                    .GroupBy(x => (ChuanHoa(x.TenXuong), ChuanHoa(phongBans[x.ID_PhongBan!.Value])))
                    .ToDictionary(g => g.Key, g =>
                    {
                        var x = g.OrderByDescending(v => v.ID_TrangThai == 1).ThenByDescending(v => v.ID_Xuong).First();
                        return (x.ID_Xuong, x.ID_PhongBan);
                    });

                t._xuong = await context.HRC1_BC_XuongChuanHoas.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.ThuTu).ToListAsync(ct);
                t._nvl = await context.HRC1_BC_NVLChuanHoas.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.ThuTu).ThenBy(x => x.ID).ToListAsync(ct);
                return t;
            }

            /// <summary>Xưởng + bộ phận → mã viết tắt: theo ID (qua master) trước, không ra thì theo tên.</summary>
            public string VietTat(string? xuong, string? phongBan, List<string> canhBao)
            {
                var kx = ChuanHoa(xuong);
                var kp = ChuanHoa(phongBan);
                if (kx.Length == 0) return KhongTraDuoc;

                int? idXuong = null;
                if (_idMaster.TryGetValue((kx, kp), out var id))
                {
                    idXuong = id.IdXuong;
                    var theoId = _xuong.FirstOrDefault(x => x.ID_Xuong == id.IdXuong && (x.ID_PhongBan == null || x.ID_PhongBan == id.IdPhongBan));
                    if (theoId != null) return theoId.VietTat;
                }

                var theoTen = _xuong.FirstOrDefault(x => ChuanHoa(x.Xuong) == kx && ChuanHoa(x.BoPhan) == kp);
                if (theoTen != null) return theoTen.VietTat;

                var ten = $"{Gon(xuong)} / {Gon(phongBan)}{(idXuong.HasValue ? $" (ID_Xuong {idXuong})" : "")}";
                if (_daBao.Add("X:" + ten)) canhBao.Add($"Không tra được xưởng trong HRC1_BC_XuongChuanHoa: {ten}");
                return KhongTraDuoc;
            }

            /// <summary>Vật tư + BP bên nhận → Nhóm 1; nhiều dòng → dòng ThuTu nhỏ nhất + cảnh báo trùng.</summary>
            public string Nhom1(string? tenVatTu, string bpNhan, List<string> canhBao)
            {
                var kv = ChuanHoa(tenVatTu);
                var kb = ChuanHoa(bpNhan);
                var khop = _nvl.Where(x => ChuanHoa(x.TenVatTu) == kv && ChuanHoa(x.XuongVietTat) == kb).ToList();
                var ten = $"{Gon(tenVatTu)} / {bpNhan}";
                if (khop.Count == 0)
                {
                    if (_daBao.Add("N:" + ten)) canhBao.Add($"Không tra được Nội dung 1 trong HRC1_BC_NVLChuanHoa: {ten}");
                    return KhongTraDuoc;
                }
                if (khop.Select(x => x.Nhom1).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1 && _daBao.Add("T:" + ten))
                    canhBao.Add($"Trùng mapping HRC1_BC_NVLChuanHoa: {ten} → {string.Join(" / ", khop.Select(x => x.Nhom1))} (đang lấy \"{khop[0].Nhom1}\")");
                return khop[0].Nhom1;
            }
        }

        // ─── Tiện ích ───────────────────────────────────────────────────────────

        /// <summary>Khóa so khớp: bỏ khoảng trắng thừa, không phân biệt hoa thường.</summary>
        internal static string ChuanHoa(string? s) => Gon(s).ToLowerInvariant();

        /// <summary>Bỏ khoảng trắng đầu/cuối và gộp khoảng trắng liên tiếp.</summary>
        internal static string Gon(string? s) => string.IsNullOrWhiteSpace(s) ? "" : _nhieuKhoangTrang.Replace(s.Trim(), " ");

        private static string? Rong(string? s) => string.IsNullOrWhiteSpace(s) ? null : Gon(s);

        private static string? Cat(string? s, int max) => s == null || s.Length <= max ? s : s[..max];

        private static decimal? So(string? s)
            => decimal.TryParse(s?.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : null;

        private static void ThemSo(KdlKetQuaDoc kq, string me, string maGoc, string? giaTri)
        {
            var v = So(giaTri);
            if (v.HasValue) kq.GiaTris.Add(new KdlDongDuLieu(me, maGoc, v));
        }

        private static void ThemChu(KdlKetQuaDoc kq, string me, string maGoc, string? giaTri)
        {
            var v = Rong(giaTri);
            if (v != null) kq.GiaTris.Add(new KdlDongDuLieu(me, maGoc, null, Cat(v, 500)));
        }

        // ─── DTO API ────────────────────────────────────────────────────────────

        private sealed class BbgnTrang
        {
            public List<BbgnDong> Data { get; set; } = [];
            public int TotalPages { get; set; }
        }

        private sealed class BbgnDong
        {
            public int Id { get; set; }
            public string? SoPhieu { get; set; }
            public string? Ngay { get; set; }
            public string? Kip { get; set; }
            public string? Ca { get; set; }
            public string? PbBG { get; set; }
            public string? XuongBG { get; set; }
            public string? PbBN { get; set; }
            public string? XuongBN { get; set; }
            public string? TenVatTu { get; set; }
            public string? Dvt { get; set; }
            public string? MaLo { get; set; }
            public string? KlBG { get; set; }
            public string? KlQuyKhoBG { get; set; }
            public string? KlBN { get; set; }
            public string? KlQuyKhoBN { get; set; }
            public string? TrangThai { get; set; }
            public string? GhiChu { get; set; }
            /// <summary>Gán sau khi đọc: "Giao" | "Nhận" (không có trong API).</summary>
            [System.Text.Json.Serialization.JsonIgnore]
            public string Chieu { get; set; } = ChieuGiao;
        }
    }
}

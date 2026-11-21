using ClosedXML.Excel;
using dataproduct.api.Models;
using dataproduct.api.Repositories;
using dataproduct.api.Services.Kdl.HRC1DPV;

namespace dataproduct.api.Services
{
    /// <summary>1 khối NVL được phân bổ = 1 dòng Mapping NVL có cờ Cấu hình phân bổ: (Nhóm 1, Xưởng viết tắt).</summary>
    public record HRC1_PhanBoKhoi(string Ma, string Nhom1, string Xuong);

    public class HRC1_PhanBoO
    {
        /// <summary>Σ Số liệu sau điều chỉnh (kg) chiều Giao của khối trong ca (giống nhau ở mọi dòng của ca).</summary>
        public decimal TongCa { get; set; }
        public decimal PhanBo { get; set; }
        public decimal? DieuChinh { get; set; }
        public string? LyDo { get; set; }
        public decimal PhanBoCuoi => PhanBo + (DieuChinh ?? 0);
        /// <summary>Σ phân bổ cuối của ca − Tổng ca (giống nhau ở mọi dòng của ca).</summary>
        public decimal Check { get; set; }
    }

    public class HRC1_PhanBoDong
    {
        public DateOnly NgaySX { get; set; }
        public byte CaSX { get; set; }
        public string? Kip { get; set; }
        /// <summary>vd "1N1" = ngày 1, ca Ngày, lệnh thứ 1.</summary>
        public string MaDong { get; set; } = "";
        /// <summary>"" = ca chưa có LSX (dòng gánh tổng ca để Check báo lệch).</summary>
        public string LenhSanXuat { get; set; } = "";
        public int SoLenhTrongCa { get; set; }
        /// <summary>Dòng đầu của ca + số dòng của ca — để gộp ô Tổng ca.</summary>
        public bool DauCa { get; set; }
        public int SoDongCa { get; set; }
        public string? GhiChu { get; set; }
        public Dictionary<string, HRC1_PhanBoO> O { get; set; } = [];
    }

    public class HRC1_PhanBoKetQua
    {
        public List<HRC1_PhanBoKhoi> Khoi { get; set; } = [];
        public List<HRC1_PhanBoDong> Dong { get; set; } = [];
    }

    public class HRC1_PhanBoDieuChinhPayload
    {
        public DateOnly NgaySX { get; set; }
        public byte CaSX { get; set; }
        public string? Kip { get; set; }
        public string? LenhSanXuat { get; set; }
        public string? GhiChu { get; set; }
        public List<HRC1_PhanBoDieuChinhO> DieuChinh { get; set; } = [];
    }

    public class HRC1_PhanBoDieuChinhO
    {
        public string? Nhom1 { get; set; }
        public string? XuongVietTat { get; set; }
        public decimal? DieuChinh { get; set; }
        public string? LyDo { get; set; }
    }

    /// <summary>
    /// Phân bổ BBGN (trang Giao nhận Xưởng ĐPV): chia Σ Số liệu sau ĐC (kg, chiều Giao) của mỗi khối NVL có cờ Cấu hình phân bổ
    /// cho các LSX của ca (LSX của mẻ trong CSDL Đúc phôi vuông, KDL HRC1_DPV). Chia đều làm tròn số nguyên, lệnh cuối = tổng − các lệnh trước.
    /// Điều chỉnh tay + lý do lưu HRC1_BC_PhanBoDieuChinh. Kết quả dùng cho vùng III Biên bản chốt (GetDongPhanBoAsync).
    /// </summary>
    public class HRC1_GNPhanBoService
    {
        public const string ModuleCSDL = "HRC1_DPV";

        private readonly IHRC1_BCChuanHoaRepository _repo;
        private readonly HRC1_GNTongHopNhomService _tongHopNhom;

        public HRC1_GNPhanBoService(IHRC1_BCChuanHoaRepository repo, HRC1_GNTongHopNhomService tongHopNhom)
        {
            _repo = repo;
            _tongHopNhom = tongHopNhom;
        }

        /// <summary>Chia đều <paramref name="tong"/> cho <paramref name="soLenh"/> lệnh: làm tròn số nguyên, lệnh cuối = tổng − các lệnh trước.</summary>
        public static decimal[] ChiaDeu(decimal tong, int soLenh)
        {
            if (soLenh <= 0) return [];
            var moi = Math.Round(tong / soLenh, 0, MidpointRounding.AwayFromZero);
            var kq = Enumerable.Repeat(moi, soLenh).ToArray();
            kq[^1] = tong - moi * (soLenh - 1);
            return kq;
        }

        private static string K(string? s) => HRC1DPVGN_BBGNNguon.ChuanHoa(s);

        public async Task<HRC1_PhanBoKetQua> GetAsync(DateOnly tuNgay, DateOnly denNgay)
        {
            var khoi = (await _repo.GetNvlAsync())
                .Where(x => x.IsActive && x.CauHinhPhanBo)
                .GroupBy(x => (K(x.Nhom1), K(x.XuongVietTat)))
                .Select(g => g.First())
                .Select(x => new HRC1_PhanBoKhoi($"{x.Nhom1}|{x.XuongVietTat}", x.Nhom1, x.XuongVietTat))
                .ToList();
            var kq = new HRC1_PhanBoKetQua { Khoi = khoi };

            // Tổng ca theo khối (chiều Giao) + kíp của ca từ dữ liệu giao nhận
            var th = await _tongHopNhom.GetAsync(tuNgay, denNgay);
            var cotTheoKhoi = khoi.ToDictionary(k => k.Ma, k => th.Cot
                .Where(c => c.Chieu == HRC1DPVGN_BBGNNguon.ChieuGiao && K(c.Nhom1) == K(k.Nhom1) && K(c.Xuong) == K(k.Xuong))
                .Select(c => c.Ma).ToList());
            var tongCa = new Dictionary<(DateOnly, byte), Dictionary<string, decimal>>();
            var kipCa = new Dictionary<(DateOnly, byte), string?>();
            foreach (var d in th.Dong.Where(d => d.CaSX.HasValue))
            {
                var ca = (d.NgaySX, d.CaSX!.Value);
                kipCa.TryAdd(ca, d.Kip);
                var t = tongCa.TryGetValue(ca, out var v) ? v : tongCa[ca] = [];
                foreach (var k in khoi)
                    t[k.Ma] = t.GetValueOrDefault(k.Ma) + cotTheoKhoi[k.Ma].Sum(m => d.GiaTri.GetValueOrDefault(m));
            }

            var lsx = (await _repo.GetLsxTheoCaAsync(ModuleCSDL, tuNgay, denNgay))
                .GroupBy(x => (x.NgaySX, x.CaSX))
                .ToDictionary(g => g.Key, g => (Kip: g.Select(x => x.Kip).FirstOrDefault(k => !string.IsNullOrWhiteSpace(k)),
                                                Lsx: g.Select(x => x.Lsx).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList()));
            var dieuChinh = (await _repo.GetPhanBoDieuChinhAsync(tuNgay, denNgay))
                .ToDictionary(x => (x.NgaySX, x.CaSX, K(x.LenhSanXuat), K(x.Nhom1), K(x.XuongVietTat)));

            // Ca hiển thị = ca có LSX hoặc có NVL phân bổ ≠ 0
            var cas = lsx.Keys
                .Concat(tongCa.Where(t => t.Value.Values.Any(v => v != 0)).Select(t => t.Key))
                .Distinct().OrderBy(c => c.Item1).ThenBy(c => c.Item2);

            foreach (var ca in cas)
            {
                var (ngay, caSX) = ca;
                var dsLsx = lsx.TryGetValue(ca, out var l) ? l.Lsx : [];
                var kip = kipCa.GetValueOrDefault(ca) ?? (lsx.TryGetValue(ca, out var l2) ? l2.Kip : null);
                var tong = tongCa.GetValueOrDefault(ca) ?? [];
                var lenhs = dsLsx.Count > 0 ? dsLsx : [""];   // chưa có LSX: 1 dòng gánh tổng để Check báo lệch
                var dongs = lenhs.Select((x, i) => new HRC1_PhanBoDong
                {
                    NgaySX = ngay, CaSX = caSX, Kip = kip, LenhSanXuat = x, SoLenhTrongCa = dsLsx.Count,
                    MaDong = $"{ngay.Day}{(caSX == 1 ? "N" : "Đ")}{i + 1}", DauCa = i == 0, SoDongCa = lenhs.Count,
                    GhiChu = dieuChinh.GetValueOrDefault((ngay, caSX, K(x), "", ""))?.GhiChu,
                }).ToList();

                foreach (var k in khoi)
                {
                    var t = tong.GetValueOrDefault(k.Ma);
                    var chia = dsLsx.Count > 0 ? ChiaDeu(t, dsLsx.Count) : [0m];
                    for (var i = 0; i < dongs.Count; i++)
                    {
                        var dc = dieuChinh.GetValueOrDefault((ngay, caSX, K(dongs[i].LenhSanXuat), K(k.Nhom1), K(k.Xuong)));
                        dongs[i].O[k.Ma] = new HRC1_PhanBoO { TongCa = t, PhanBo = chia[i], DieuChinh = dc?.DieuChinh, LyDo = dc?.LyDo };
                    }
                    var check = dongs.Sum(d => d.O[k.Ma].PhanBoCuoi) - t;
                    dongs.ForEach(d => d.O[k.Ma].Check = check);
                }
                kq.Dong.AddRange(dongs);
            }
            return kq;
        }

        /// <summary>Số phân bổ cuối theo (kíp, LSX, khối) của 1 ngày — nguồn vùng III Biên bản chốt cho NVL có cờ phân bổ.</summary>
        public async Task<List<(byte CaSX, string? Kip, string Lsx, string Nhom1, string Xuong, decimal SoLieu)>> GetDongPhanBoAsync(DateOnly ngaySX)
        {
            var kq = await GetAsync(ngaySX, ngaySX);
            return kq.Dong
                .Where(d => d.LenhSanXuat.Length > 0)
                .SelectMany(d => kq.Khoi.Select(k => (d.CaSX, d.Kip, d.LenhSanXuat, k.Nhom1, k.Xuong, d.O[k.Ma].PhanBoCuoi)))
                .Where(x => x.Item6 != 0)
                .ToList();
        }

        /// <summary>Lưu điều chỉnh + lý do của các khối + ghi chú của 1 dòng LSX. Trống hết → xóa.</summary>
        public async Task LuuDieuChinhAsync(HRC1_PhanBoDieuChinhPayload p, string? nguoi)
        {
            var lsx = HRC1DPVGN_BBGNNguon.Gon(p.LenhSanXuat);
            if (lsx.Length == 0) throw new InvalidOperationException("Ca chưa có LSX — không điều chỉnh được (bổ sung LSX ở CSDL Đúc phôi vuông rồi Đồng bộ).");
            if (lsx.Length > 30) throw new InvalidOperationException("LSX tối đa 30 ký tự.");
            if (p.CaSX is not (1 or 2)) throw new InvalidOperationException("Ca không hợp lệ.");

            var daCo = (await _repo.GetPhanBoDieuChinhAsync(p.NgaySX, p.NgaySX))
                .Where(x => x.CaSX == p.CaSX && K(x.LenhSanXuat) == K(lsx)).ToList();
            var them = new List<HRC1_BC_PhanBoDieuChinh>();

            void Ghi(string nhom1, string xuong, decimal? dieuChinh, string? lyDo, string? ghiChu)
            {
                lyDo = HRC1DPVGN_BBGNNguon.Gon(lyDo);
                ghiChu = HRC1DPVGN_BBGNNguon.Gon(ghiChu);
                if (dieuChinh == 0) dieuChinh = null;
                if (dieuChinh.HasValue && lyDo.Length == 0)
                    throw new InvalidOperationException($"Nhập lý do cho điều chỉnh {nhom1} ({xuong}).");
                if (lyDo.Length > 500 || ghiChu.Length > 500) throw new InvalidOperationException("Lý do / ghi chú tối đa 500 ký tự.");

                var e = daCo.FirstOrDefault(x => K(x.Nhom1) == K(nhom1) && K(x.XuongVietTat) == K(xuong));
                var trong = !dieuChinh.HasValue && lyDo.Length == 0 && ghiChu.Length == 0;
                if (trong)
                {
                    if (e != null) _repo.Remove(e);
                    return;
                }
                if (e == null)
                {
                    e = new HRC1_BC_PhanBoDieuChinh
                    {
                        NgaySX = p.NgaySX, CaSX = p.CaSX, LenhSanXuat = lsx, Nhom1 = nhom1, XuongVietTat = xuong,
                        NguoiTao = nguoi, NgayTao = DateTime.Now,
                    };
                    them.Add(e);
                }
                else
                {
                    e.NguoiCapNhat = nguoi;
                    e.NgayCapNhat = DateTime.Now;
                }
                e.Kip = string.IsNullOrWhiteSpace(p.Kip) ? null : p.Kip.Trim()[..1].ToUpperInvariant();
                e.DieuChinh = dieuChinh;
                e.LyDo = lyDo.Length == 0 ? null : lyDo;
                e.GhiChu = ghiChu.Length == 0 ? null : ghiChu;
            }

            Ghi("", "", null, null, p.GhiChu);   // ghi chú dòng
            foreach (var o in p.DieuChinh)
            {
                var nhom1 = HRC1DPVGN_BBGNNguon.Gon(o.Nhom1);
                var xuong = HRC1DPVGN_BBGNNguon.Gon(o.XuongVietTat);
                if (nhom1.Length == 0) continue;
                Ghi(nhom1, xuong, o.DieuChinh, o.LyDo, null);
            }
            foreach (var e in them) await _repo.AddAsync(e);
            await _repo.SaveAsync();
        }

        // ─── Excel ──────────────────────────────────────────────────────────────

        public async Task<byte[]> ExportExcelAsync(DateOnly tuNgay, DateOnly denNgay)
        {
            var kq = await GetAsync(tuNgay, denNgay);
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("PhanBoBBGN");
            string[] dau = ["Ngày", "Ca", "Kíp", "Mã dòng", "LSX", "Số lệnh trong ca"];
            string[] tenKhoi = ["", "Phân bổ", "Check", "Điều chỉnh", "Lý do"];
            const int cotKhoi = 5;
            var cotGhiChu = dau.Length + kq.Khoi.Count * cotKhoi + 1;
            var vang = XLColor.FromHtml("#FFFF00");

            ws.Cell(1, 1).Value = "PHÂN BỔ BBGN";
            ws.Range(1, 1, 1, cotGhiChu).Merge().Style.Font.SetBold().Font.SetFontSize(14).Fill.SetBackgroundColor(vang)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            ws.Cell(2, 1).Value = $"Giao nhận Xưởng Đúc phôi vuông — từ {tuNgay:dd/MM/yyyy} đến {denNgay:dd/MM/yyyy} — kg (Số liệu sau điều chỉnh, chia đều theo LSX, làm tròn số nguyên)";
            ws.Range(2, 1, 2, cotGhiChu).Merge().Style.Font.SetItalic();

            const int h = 4, d0 = 5;
            for (var i = 0; i < dau.Length; i++) ws.Cell(h, i + 1).Value = dau[i];
            for (var k = 0; k < kq.Khoi.Count; k++)
                for (var j = 0; j < cotKhoi; j++)
                {
                    var c = ws.Cell(h, dau.Length + 1 + k * cotKhoi + j);
                    c.Value = j == 0 ? $"{kq.Khoi[k].Nhom1}\n({kq.Khoi[k].Xuong})" : $"{tenKhoi[j]} {kq.Khoi[k].Nhom1}";
                    c.Style.Fill.SetBackgroundColor(j == 0 ? XLColor.FromHtml(k % 2 == 0 ? "#FFFF00" : "#5B9BD5") : XLColor.FromHtml("#DDEBF7"));
                }
            ws.Cell(h, cotGhiChu).Value = "Ghi chú";
            var hdr = ws.Range(h, 1, h, cotGhiChu);
            hdr.Style.Font.SetBold().Alignment.SetWrapText().Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
            ws.Row(h).Height = 45;

            var r = d0;
            foreach (var d in kq.Dong)
            {
                ws.Cell(r, 1).Value = d.NgaySX.ToDateTime(TimeOnly.MinValue);
                ws.Cell(r, 1).Style.DateFormat.Format = "dd/MM/yyyy";
                ws.Cell(r, 2).Value = d.CaSX == 1 ? "N" : "Đ";
                ws.Cell(r, 3).Value = d.Kip ?? "";
                ws.Cell(r, 4).Value = d.MaDong;
                ws.Cell(r, 5).Value = d.LenhSanXuat.Length > 0 ? d.LenhSanXuat : "(chưa có LSX)";
                ws.Cell(r, 6).Value = d.SoLenhTrongCa;
                for (var k = 0; k < kq.Khoi.Count; k++)
                {
                    var o = d.O[kq.Khoi[k].Ma];
                    var c0 = dau.Length + 1 + k * cotKhoi;
                    if (d.DauCa)
                    {
                        So(ws.Cell(r, c0), o.TongCa);
                        if (d.SoDongCa > 1) ws.Range(r, c0, r + d.SoDongCa - 1, c0).Merge();
                    }
                    So(ws.Cell(r, c0 + 1), o.PhanBoCuoi);
                    if (o.DieuChinh.HasValue) ws.Cell(r, c0 + 1).Style.Fill.SetBackgroundColor(vang);
                    So(ws.Cell(r, c0 + 2), o.Check);
                    if (o.Check != 0) ws.Cell(r, c0 + 2).Style.Font.SetFontColor(XLColor.Red).Font.SetBold();
                    if (o.DieuChinh.HasValue) So(ws.Cell(r, c0 + 3), o.DieuChinh.Value);
                    ws.Cell(r, c0 + 4).Value = o.LyDo ?? "";
                }
                ws.Cell(r, cotGhiChu).Value = d.GhiChu ?? "";
                r++;
            }

            var bang = ws.Range(h, 1, Math.Max(r - 1, h), cotGhiChu);
            bang.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);
            ws.Range(d0, 1, Math.Max(r - 1, d0), cotGhiChu).Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
            ws.Range(d0, 2, Math.Max(r - 1, d0), 6).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            ws.Column(1).Width = 12;
            ws.Columns(2, 4).Width = 7;
            ws.Column(5).Width = 16;
            ws.Column(6).Width = 9;
            for (var c = dau.Length + 1; c < cotGhiChu; c++) ws.Column(c).Width = (c - dau.Length - 1) % cotKhoi == 4 ? 22 : 12;
            ws.Column(cotGhiChu).Width = 25;
            ws.SheetView.FreezeRows(h);
            ws.SheetView.FreezeColumns(6);

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        private static void So(IXLCell c, decimal v)
        {
            c.Value = v;
            c.Style.NumberFormat.Format = "#,##0;-#,##0;0";
        }
    }
}

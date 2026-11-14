using System.Globalization;
using System.Text.Json;
using ClosedXML.Excel;
using dataproduct.api.Models;
using dataproduct.api.Repositories;

namespace dataproduct.api.Services.Kdl
{
    /// <summary>
    /// 1 nút trong KDL_Form.CotJson. Lá: "key" (KDL_Key.MaKey) hoặc "field" (trường header: KdlConst.TruongDoiTuong).
    /// Nhóm: "label" + "children" (header nhiều tầng).
    /// </summary>
    public class KdlCot
    {
        public string? Key { get; set; }
        public string? Field { get; set; }
        public string? Label { get; set; }
        public int? Width { get; set; }
        /// <summary>Số chữ số thập phân khi hiển thị (mặc định KDL_Key.SoLe).</summary>
        public int? Decimals { get; set; }
        /// <summary>Cho phép nhập / sửa tay trên form (ghi Nguon = nhập tay).</summary>
        public bool? Editable { get; set; }
        public bool? Hidden { get; set; }
        /// <summary>false = không xuất Excel.</summary>
        public bool? Excel { get; set; }
        /// <summary>"left" | "right": cố định cột khi cuộn ngang (Excel: cố định các cột "left" liền nhau từ cột đầu).</summary>
        public string? Fixed { get; set; }
        public List<KdlCot>? Children { get; set; }
    }

    public class KdlKeyDto
    {
        public string MaKey { get; set; } = null!;
        public string? TenKey { get; set; }
        public string? DonVi { get; set; }
        public byte KieuDL { get; set; }
        public byte CachLay { get; set; }
        public byte KieuTongHop { get; set; }
        public byte SoLe { get; set; }
        public string? CongThuc { get; set; }
        public string? MaNguon { get; set; }
    }

    public class KdlCauTruc
    {
        public string MaForm { get; set; } = null!;
        public string TenForm { get; set; } = null!;
        public string MaModule { get; set; } = null!;
        public string? MaBM { get; set; }
        public List<KdlCot> Cot { get; set; } = [];
        public Dictionary<string, KdlKeyDto> Keys { get; set; } = [];
        public List<string> CanhBao { get; set; } = [];
    }

    public class KdlDong
    {
        public int IdDoiTuong { get; set; }
        public string Me { get; set; } = null!;
        public DateOnly NgaySX { get; set; }
        public byte? CaSX { get; set; }
        public string? Kip { get; set; }
        public string? MayDuc { get; set; }
        public string? Mac { get; set; }
        public string? KichThuoc { get; set; }
        public string? LenhSanXuat { get; set; }
        /// <summary>Giá trị đang hiệu lực theo MaKey (số hoặc chuỗi).</summary>
        public Dictionary<string, object?> GiaTri { get; set; } = [];
        /// <summary>Key đang hiển thị giá trị nhập tay / điều chỉnh (ưu tiên hơn số tự động).</summary>
        public List<string> ThuCong { get; set; } = [];
    }

    public class KdlNhom
    {
        public Dictionary<string, object?> Nhom { get; set; } = [];
        public int SoDoiTuong { get; set; }
        public Dictionary<string, decimal?> GiaTri { get; set; } = [];
    }

    /// <summary>Xem / tổng hợp / xuất Excel / nhập tay dữ liệu Kho dữ liệu động theo cấu hình form.</summary>
    public class KdlXemService
    {
        private const int ToiDaXuatExcel = 50000;
        private static readonly JsonSerializerOptions JsonCot = new() { PropertyNameCaseInsensitive = true };

        private readonly IKdlRepository _repo;
        private readonly KdlTinhToanService _tinhToan;
        private readonly KdlCongThucHelper _congThuc;

        public KdlXemService(IKdlRepository repo, KdlTinhToanService tinhToan, KdlCongThucHelper congThuc)
        {
            _repo = repo;
            _tinhToan = tinhToan;
            _congThuc = congThuc;
        }

        // ── Cấu trúc ───────────────────────────────────────────────────────────

        public Task<List<KDL_Form>> GetFormsAsync(string? maModule) => _repo.GetFormsAsync(maModule);

        public async Task<List<KdlKeyDto>> GetKeysAsync(string maModule)
            => (await _repo.GetKeysAsync(maModule, chiActive: false)).Select(ToDto).ToList();

        public async Task<KdlCauTruc?> GetCauTrucAsync(string maForm)
            => (await NapFormAsync(maForm))?.CauTruc;

        private sealed record FormDaNap(KdlCauTruc CauTruc, List<KDL_Key> KeysModule, List<KdlCot> La);

        private async Task<FormDaNap?> NapFormAsync(string maForm)
        {
            var form = await _repo.GetFormAsync(maForm);
            if (form == null) return null;

            var cot = JsonSerializer.Deserialize<List<KdlCot>>(form.CotJson, JsonCot) ?? [];
            var keys = await _repo.GetKeysAsync(form.MaModule);
            var theoMa = keys.ToDictionary(k => k.MaKey, StringComparer.OrdinalIgnoreCase);
            var la = LayLa(cot).ToList();

            var ct = new KdlCauTruc { MaForm = form.MaForm, TenForm = form.TenForm, MaModule = form.MaModule, MaBM = form.MaBM, Cot = cot };
            foreach (var c in la)
            {
                if (c.Key != null)
                {
                    if (theoMa.TryGetValue(c.Key, out var k)) ct.Keys[k.MaKey] = ToDto(k);
                    else ct.CanhBao.Add($"Cột \"{c.Label ?? c.Key}\": key {c.Key} không có (hoặc đã ngưng) trong KDL_Key.");
                }
                else if (c.Field == null || !KdlConst.TruongDoiTuong.Contains(c.Field, StringComparer.OrdinalIgnoreCase))
                    ct.CanhBao.Add($"Cột \"{c.Label}\": thiếu key hoặc field không hợp lệ ({c.Field}).");
            }
            return new FormDaNap(ct, keys, la);
        }

        private static IEnumerable<KdlCot> LayLa(IEnumerable<KdlCot> cot)
        {
            foreach (var c in cot)
            {
                if (c.Children is { Count: > 0 })
                    foreach (var con in LayLa(c.Children)) yield return con;
                else yield return c;
            }
        }

        /// <summary>Key cần đọc: key của cột + key mà công thức TINH_KHI_XEM tham chiếu.</summary>
        private List<KDL_Key> KeyCanDoc(FormDaNap f)
        {
            var theoMa = f.KeysModule.ToDictionary(k => k.MaKey, StringComparer.OrdinalIgnoreCase);
            var can = new HashSet<string>(f.CauTruc.Keys.Keys, StringComparer.OrdinalIgnoreCase);
            var hangDoi = new Queue<string>(can);
            while (hangDoi.Count > 0)
            {
                if (!theoMa.TryGetValue(hangDoi.Dequeue(), out var k)) continue;
                if (k.CachLay != KdlConst.CachLay.TinhKhiXem || string.IsNullOrWhiteSpace(k.CongThuc)) continue;
                foreach (var t in _congThuc.LayKeyThamChieu(k.CongThuc))
                    if (can.Add(t)) hangDoi.Enqueue(t);
            }
            return f.KeysModule.Where(k => can.Contains(k.MaKey)).ToList();
        }

        // ── Dữ liệu chi tiết ───────────────────────────────────────────────────

        public async Task<(List<KdlDong> Rows, int Total, Dictionary<string, decimal?> Tong)?> GetDuLieuAsync(
            string maForm, KdlLoc loc, int page, int pageSize)
        {
            var f = await NapFormAsync(maForm);
            if (f == null) return null;
            loc.MaModule = f.CauTruc.MaModule;

            var keys = KeyCanDoc(f);
            var (doiTuongs, total) = await _repo.SearchDoiTuongAsync(loc, page, pageSize);
            var rows = await DungDongAsync(doiTuongs, keys, loc.TuNgay, loc.DenNgay);

            var tong = (await TongHopTheoKeyAsync(loc, keys, [])).FirstOrDefault()?.GiaTri ?? [];
            return (rows, total, tong);
        }

        private async Task<List<KdlDong>> DungDongAsync(List<KDL_DoiTuong> doiTuongs, List<KDL_Key> keys, DateOnly tuNgay, DateOnly denNgay)
        {
            var keyTheoId = keys.ToDictionary(k => k.IdKey);
            var giaTris = await _repo.GetGiaTriAsync(doiTuongs.Select(d => d.IdDoiTuong).ToList(), tuNgay, denNgay, idKeys: keyTheoId.Keys);
            var theoDoiTuong = giaTris
                .GroupBy(g => g.IdDoiTuong)
                .ToDictionary(g => g.Key, g => g.GroupBy(x => x.IdKey).Select(x => x.OrderBy(UuTien).First()).ToList());

            var rows = new List<KdlDong>(doiTuongs.Count);
            foreach (var d in doiTuongs)
            {
                var dong = new KdlDong
                {
                    IdDoiTuong = d.IdDoiTuong, Me = d.Me, NgaySX = d.NgaySX, CaSX = d.CaSX, Kip = d.Kip,
                    MayDuc = d.MayDuc, Mac = d.Mac, KichThuoc = d.KichThuoc, LenhSanXuat = d.LenhSanXuat,
                };
                var so = new Dictionary<string, decimal?>(StringComparer.OrdinalIgnoreCase);
                foreach (var g in theoDoiTuong.GetValueOrDefault(d.IdDoiTuong) ?? [])
                {
                    var k = keyTheoId[g.IdKey];
                    if (k.KieuDL == KdlConst.KieuDL.Chuoi) dong.GiaTri[k.MaKey] = g.GiaTriText;
                    else { dong.GiaTri[k.MaKey] = g.GiaTri; so[k.MaKey] = g.GiaTri; }
                    if (g.Nguon is KdlConst.Nguon.NhapTay or KdlConst.Nguon.DieuChinh) dong.ThuCong.Add(k.MaKey);
                }
                _tinhToan.TinhKhiXem(keys, so);
                foreach (var k in keys.Where(k => k.CachLay == KdlConst.CachLay.TinhKhiXem && so.ContainsKey(k.MaKey)))
                    dong.GiaTri[k.MaKey] = so[k.MaKey];
                rows.Add(dong);
            }
            return rows;
        }

        // Điều chỉnh (4) > nhập tay (3) > tự động / tính
        private static int UuTien(KDL_GiaTri g) => g.Nguon switch
        {
            KdlConst.Nguon.DieuChinh => 0,
            KdlConst.Nguon.NhapTay => 1,
            _ => 2,
        };

        // ── Tổng hợp ───────────────────────────────────────────────────────────

        public async Task<List<KdlNhom>?> GetTongHopAsync(string maForm, KdlLoc loc, IReadOnlyList<string> cotNhom)
        {
            var f = await NapFormAsync(maForm);
            if (f == null) return null;
            loc.MaModule = f.CauTruc.MaModule;
            return await TongHopTheoKeyAsync(loc, KeyCanDoc(f), cotNhom);
        }

        private async Task<List<KdlNhom>> TongHopTheoKeyAsync(KdlLoc loc, List<KDL_Key> keys, IReadOnlyList<string> cotNhom)
        {
            // Chỉ cộng key số kiểu SUM; tỷ lệ (TINH_KHI_XEM) tính lại trên tổng
            var keySum = keys.Where(k => k.KieuDL == KdlConst.KieuDL.So && k.CachLay != KdlConst.CachLay.TinhKhiXem
                                         && k.KieuTongHop == KdlConst.KieuTongHop.Sum).ToList();
            var keyTheoId = keySum.ToDictionary(k => k.IdKey);

            var nhoms = await _repo.TongHopAsync(loc, cotNhom, keyTheoId.Keys);
            return nhoms
                .Select(n =>
                {
                    var gt = n.GiaTri.ToDictionary(x => keyTheoId[x.Key].MaKey, x => x.Value, StringComparer.OrdinalIgnoreCase);
                    _tinhToan.TinhKhiXem(keys, gt);
                    return new KdlNhom { Nhom = n.Nhom, SoDoiTuong = n.SoDoiTuong, GiaTri = gt };
                })
                .OrderBy(n => string.Join("|", cotNhom.Select(c => Convert.ToString(n.Nhom.GetValueOrDefault(c), CultureInfo.InvariantCulture))))
                .ToList();
        }

        // ── Nhập tay ───────────────────────────────────────────────────────────

        /// <summary>
        /// Ghi giá trị nhập tay (Nguon = 3) cho các cột editable của form; giá trị rỗng = xóa nhập tay (quay về số tự động).
        /// Sau đó tính lại key công thức / xử lý riêng của đối tượng.
        /// </summary>
        public async Task<KdlDong?> NhapTayAsync(string maForm, int idDoiTuong, Dictionary<string, JsonElement> giaTri, string? nguoi, CancellationToken ct)
        {
            var f = await NapFormAsync(maForm);
            if (f == null) return null;
            var d = await _repo.GetDoiTuongByIdAsync(idDoiTuong);
            if (d == null || !string.Equals(d.MaModule, f.CauTruc.MaModule, StringComparison.OrdinalIgnoreCase)) return null;

            var choPhep = f.La.Where(c => c.Key != null && c.Editable == true).Select(c => c.Key!).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var theoMa = f.KeysModule.ToDictionary(k => k.MaKey, StringComparer.OrdinalIgnoreCase);

            var ghi = new List<KDL_GiaTri>();
            var keySua = new List<int>();
            foreach (var (maKey, el) in giaTri)
            {
                if (!choPhep.Contains(maKey) || !theoMa.TryGetValue(maKey, out var k))
                    throw new InvalidOperationException($"Cột {maKey} không cho phép nhập tay trên form này.");
                if (k.CachLay == KdlConst.CachLay.TinhKhiXem)
                    throw new InvalidOperationException($"Cột {maKey} được tính khi xem, không nhập được.");
                keySua.Add(k.IdKey);

                var rong = el.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined
                           || (el.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(el.GetString()));
                if (rong) continue;

                var row = new KDL_GiaTri { NgaySX = d.NgaySX, CaSX = d.CaSX, IdDoiTuong = d.IdDoiTuong, IdKey = k.IdKey, Nguon = KdlConst.Nguon.NhapTay, NguoiCapNhat = nguoi };
                if (k.KieuDL == KdlConst.KieuDL.Chuoi)
                    row.GiaTriText = el.ValueKind == JsonValueKind.String ? el.GetString()!.Trim() : el.GetRawText();
                else if (el.ValueKind == JsonValueKind.Number) row.GiaTri = el.GetDecimal();
                else if (el.ValueKind == JsonValueKind.String && decimal.TryParse(el.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var so)) row.GiaTri = so;
                else throw new InvalidOperationException($"Cột {maKey}: giá trị phải là số.");
                ghi.Add(row);
            }
            if (keySua.Count == 0) throw new InvalidOperationException("Không có giá trị nào để lưu.");

            await using var tx = await _repo.BeginTransactionAsync(ct);
            await _repo.ThayGiaTriAsync(d.MaModule, d.NgaySX, d.NgaySX, [KdlConst.Nguon.NhapTay], [d.IdDoiTuong], keySua, ghi);
            await TinhLaiDoiTuongAsync(d, f.KeysModule, ct);
            await tx.CommitAsync(ct);

            return (await DungDongAsync([d], KeyCanDoc(f), d.NgaySX, d.NgaySX)).First();
        }

        private async Task TinhLaiDoiTuongAsync(KDL_DoiTuong d, List<KDL_Key> keys, CancellationToken ct)
        {
            var keyTheoId = keys.ToDictionary(k => k.IdKey);
            var t = new KdlDoiTuongTinh { DoiTuong = d };
            var giaTris = await _repo.GetGiaTriAsync([d.IdDoiTuong], d.NgaySX, d.NgaySX,
                [KdlConst.Nguon.TuDong, KdlConst.Nguon.NhapTay, KdlConst.Nguon.DieuChinh]);
            foreach (var g in giaTris.OrderBy(g => g.Nguon))   // nguồn ưu tiên hơn ghi sau
                if (keyTheoId.TryGetValue(g.IdKey, out var k))
                    KdlSyncService.GanGiaTri(t, k, g.GiaTri, g.GiaTriText);

            var kq = await _tinhToan.TinhAsync(keys, [t], ct);
            var rows = kq.Select(x => new KDL_GiaTri { NgaySX = d.NgaySX, CaSX = d.CaSX, IdDoiTuong = d.IdDoiTuong, IdKey = x.IdKey, Nguon = KdlConst.Nguon.Tinh, GiaTri = x.GiaTri }).ToList();
            await _repo.ThayGiaTriAsync(d.MaModule, d.NgaySX, d.NgaySX, [KdlConst.Nguon.Tinh], [d.IdDoiTuong], idKeys: null, rows);
        }

        // ── Xuất Excel (header nhiều tầng theo CotJson) ─────────────────────────

        public async Task<(byte[] Bytes, string TenForm)?> ExportExcelAsync(string maForm, KdlLoc loc)
        {
            var f = await NapFormAsync(maForm);
            if (f == null) return null;
            loc.MaModule = f.CauTruc.MaModule;

            var keys = KeyCanDoc(f);
            var doiTuongs = await _repo.GetDoiTuongAsync(loc, ToiDaXuatExcel);
            var rows = await DungDongAsync(doiTuongs, keys, loc.TuNgay, loc.DenNgay);
            var tong = (await TongHopTheoKeyAsync(loc, keys, [])).FirstOrDefault()?.GiaTri ?? [];

            var cot = LocCotExcel(f.CauTruc.Cot);
            var la = LayLa(cot).ToList();
            var doSau = DoSau(cot);

            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("DuLieu");

            ws.Cell(1, 1).Value = f.CauTruc.TenForm;
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Cell(2, 1).Value = $"Từ ngày {loc.TuNgay:dd/MM/yyyy} đến ngày {loc.DenNgay:dd/MM/yyyy}";

            const int dongHeader = 4;
            var cotIdx = 1;
            VeHeader(ws, cot, dongHeader, ref cotIdx, doSau, f.CauTruc.Keys);
            var vungHeader = ws.Range(dongHeader, 1, dongHeader + doSau - 1, la.Count);
            vungHeader.Style.Font.Bold = true;
            vungHeader.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9E1F2");
            vungHeader.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            vungHeader.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            vungHeader.Style.Alignment.WrapText = true;

            var dong = dongHeader + doSau;
            for (int i = 0; i < rows.Count; i++, dong++)
                for (int c = 0; c < la.Count; c++)
                    GhiO(ws.Cell(dong, c + 1), la[c], rows[i], i + 1, f.CauTruc.Keys);

            // Dòng tổng
            ws.Cell(dong, 1).Value = "Tổng";
            for (int c = 0; c < la.Count; c++)
                if (la[c].Key != null && tong.TryGetValue(la[c].Key!, out var v) && v.HasValue)
                {
                    ws.Cell(dong, c + 1).Value = (double)v.Value;
                    ws.Cell(dong, c + 1).Style.NumberFormat.Format = DinhDangSo(la[c], f.CauTruc.Keys);
                }
            ws.Range(dong, 1, dong, la.Count).Style.Font.Bold = true;
            ws.Range(dong, 1, dong, la.Count).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF2CC");

            var vung = ws.Range(dongHeader, 1, dong, la.Count);
            vung.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            vung.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            for (int c = 0; c < la.Count; c++)
                ws.Column(c + 1).Width = Math.Max(8, (la[c].Width ?? 100) / 7.0);
            var soCotCoDinh = la.TakeWhile(c => string.Equals(c.Fixed, "left", StringComparison.OrdinalIgnoreCase)).Count();
            ws.SheetView.Freeze(dongHeader + doSau - 1, soCotCoDinh);

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return (ms.ToArray(), f.CauTruc.TenForm);
        }

        private static List<KdlCot> LocCotExcel(List<KdlCot> cot)
            => cot.Where(c => c.Hidden != true && c.Excel != false)
                  .Select(c => c.Children is { Count: > 0 }
                      ? new KdlCot { Label = c.Label, Children = LocCotExcel(c.Children) }
                      : c)
                  .Where(c => c.Children == null || c.Children.Count > 0)
                  .ToList();

        private static int DoSau(List<KdlCot> cot)
            => cot.Count == 0 ? 0 : cot.Max(c => c.Children is { Count: > 0 } ? 1 + DoSau(c.Children) : 1);

        private static void VeHeader(IXLWorksheet ws, List<KdlCot> cot, int dong, ref int cotIdx, int doSauConLai, Dictionary<string, KdlKeyDto> keys)
        {
            foreach (var c in cot)
            {
                var tieuDe = c.Label ?? (c.Key != null && keys.TryGetValue(c.Key, out var k) ? k.TenKey ?? k.MaKey : c.Field ?? c.Key ?? "");
                if (c.Children is { Count: > 0 })
                {
                    var batDau = cotIdx;
                    VeHeader(ws, c.Children, dong + 1, ref cotIdx, doSauConLai - 1, keys);
                    ws.Cell(dong, batDau).Value = tieuDe;
                    if (cotIdx - 1 > batDau) ws.Range(dong, batDau, dong, cotIdx - 1).Merge();
                }
                else
                {
                    ws.Cell(dong, cotIdx).Value = tieuDe;
                    if (doSauConLai > 1) ws.Range(dong, cotIdx, dong + doSauConLai - 1, cotIdx).Merge();
                    cotIdx++;
                }
            }
        }

        private static void GhiO(IXLCell o, KdlCot c, KdlDong r, int stt, Dictionary<string, KdlKeyDto> keys)
        {
            if (c.Field != null)
            {
                switch (c.Field.ToUpperInvariant())
                {
                    case "STT": o.Value = stt; break;
                    case "ME": o.Value = r.Me; break;
                    case "NGAYSX": o.Value = r.NgaySX.ToDateTime(TimeOnly.MinValue); o.Style.DateFormat.Format = "dd/MM/yyyy"; break;
                    case "CASX": if (r.CaSX.HasValue) o.Value = r.CaSX.Value; break;
                    case "KIP": o.Value = r.Kip; break;
                    case "MAYDUC": o.Value = r.MayDuc; break;
                    case "MAC": o.Value = r.Mac; break;
                    case "KICHTHUOC": o.Value = r.KichThuoc; break;
                    case "LENHSANXUAT": o.Value = r.LenhSanXuat; break;
                }
                return;
            }
            if (c.Key == null || !r.GiaTri.TryGetValue(c.Key, out var v) || v == null) return;
            if (v is decimal d)
            {
                o.Value = (double)d;
                o.Style.NumberFormat.Format = DinhDangSo(c, keys);
            }
            else o.Value = v.ToString();
        }

        private static string DinhDangSo(KdlCot c, Dictionary<string, KdlKeyDto> keys)
        {
            var soLe = c.Decimals ?? (c.Key != null && keys.TryGetValue(c.Key, out var k) ? k.SoLe : 0);
            return soLe > 0 ? "#,##0." + new string('0', soLe) : "#,##0";
        }

        private static KdlKeyDto ToDto(KDL_Key k) => new()
        {
            MaKey = k.MaKey, TenKey = k.TenKey, DonVi = k.DonVi, KieuDL = k.KieuDL, CachLay = k.CachLay,
            KieuTongHop = k.KieuTongHop, SoLe = k.SoLe, CongThuc = k.CongThuc, MaNguon = k.MaNguon,
        };
    }
}

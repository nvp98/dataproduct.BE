using ClosedXML.Excel;
using dataproduct.api.Repositories;
using dataproduct.api.Services.Kdl;
using dataproduct.api.Services.Kdl.HRC1DPV;

namespace dataproduct.api.Services
{
    /// <summary>1 cột của bảng tổng hợp theo nhóm: chiều (Giao / Nhận / "" = chưa map) + xưởng đối tác viết tắt + Nhóm 1.</summary>
    public record HRC1_GNCotNhom(string Ma, string Chieu, string Xuong, string Nhom1);

    public class HRC1_GNDongNhom
    {
        public DateOnly NgaySX { get; set; }
        public byte? CaSX { get; set; }
        public string? Kip { get; set; }
        public int SoDong { get; set; }
        /// <summary>Σ Số liệu sau điều chỉnh (kg) theo HRC1_GNCotNhom.Ma.</summary>
        public Dictionary<string, decimal> GiaTri { get; set; } = [];
    }

    public class HRC1_GNTongHopNhom
    {
        public List<HRC1_GNCotNhom> Cot { get; set; } = [];
        public List<HRC1_GNDongNhom> Dong { get; set; } = [];
        public Dictionary<string, decimal> Tong { get; set; } = [];
        public decimal TongCong { get; set; }
    }

    /// <summary>
    /// "Bảng tổng hợp dữ liệu theo nhóm" của form Giao nhận Xưởng ĐPV (KDL HRC1_DPV_GN): cộng Số liệu sau điều chỉnh (kg)
    /// đang hiệu lực (đã gồm nhập tay) theo (Ngày, Ca, Kíp) × (Chiều, BP đối tác, Nội dung 1). Không gọi nguồn ngoài —
    /// chỉ đọc dữ liệu đã đồng bộ của form.
    /// Cột Giao = mọi cặp (Xưởng viết tắt, Nhóm 1) đang dùng trong HRC1_BC_NVLChuanHoa (thứ tự bảng) + cặp có số ngoài bảng;
    /// cột Nhận = cặp có số; cột "Chưa map" = dòng Nội dung 1 / BP đối tác N/A.
    /// </summary>
    public class HRC1_GNTongHopNhomService
    {
        public const string MaForm = "HRC1_DPV_GN";
        private const string MaChuaMap = "NA";

        private readonly KdlXemService _xem;
        private readonly IHRC1_BCChuanHoaRepository _repo;

        public HRC1_GNTongHopNhomService(KdlXemService xem, IHRC1_BCChuanHoaRepository repo)
        {
            _xem = xem;
            _repo = repo;
        }

        public async Task<HRC1_GNTongHopNhom> GetAsync(DateOnly tuNgay, DateOnly denNgay)
        {
            var na = HRC1DPVGN_BBGNNguon.KhongTraDuoc;
            var dongs = await _xem.GetTatCaDongAsync(MaForm, new KdlLoc { TuNgay = tuNgay, DenNgay = denNgay }) ?? [];
            var nvl = (await _repo.GetNvlAsync()).Where(x => x.IsActive).ToList();

            static string Chu(KdlDong d, string k) => d.GiaTri.GetValueOrDefault(k) as string ?? "";
            static string Ma(string chieu, string xuong, string nhom1) => $"{chieu}|{xuong}|{nhom1}";

            // Gắn từng dòng chi tiết vào 1 cột
            var gan = dongs.Select(d =>
            {
                var chieu = Chu(d, "ChieuGN");
                if (chieu.Length == 0) chieu = HRC1DPVGN_BBGNNguon.ChieuGiao;
                var xuong = Chu(d, "BP_DoiTac");
                if (xuong.Length == 0) xuong = d.MayDuc ?? "";
                var nhom1 = Chu(d, "NoiDung1");
                var chuaMap = xuong.Length == 0 || nhom1.Length == 0 || xuong == na || nhom1 == na;
                var giaTri = d.GiaTri.GetValueOrDefault("SoLieuSauDieuChinh") is decimal v ? v : 0m;
                return (Dong: d, Chieu: chieu, Xuong: xuong, Nhom1: nhom1, Ma: chuaMap ? MaChuaMap : Ma(chieu, xuong, nhom1), GiaTri: giaTri);
            }).ToList();

            // Cột: Giao theo bảng mapping (kể cả chưa có số) → Giao có số ngoài bảng → Nhận có số → Chưa map
            var cot = new List<HRC1_GNCotNhom>();
            var daCo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            void Them(string chieu, string xuong, string nhom1)
            {
                var ma = Ma(chieu, xuong, nhom1);
                if (daCo.Add(ma)) cot.Add(new HRC1_GNCotNhom(ma, chieu, xuong, nhom1));
            }
            foreach (var x in nvl) Them(HRC1DPVGN_BBGNNguon.ChieuGiao, x.XuongVietTat, x.Nhom1);
            foreach (var chieu in new[] { HRC1DPVGN_BBGNNguon.ChieuGiao, HRC1DPVGN_BBGNNguon.ChieuNhan })
                foreach (var g in gan.Where(g => g.Ma != MaChuaMap && g.Chieu == chieu)
                             .GroupBy(g => (g.Xuong, g.Nhom1)).OrderBy(g => g.Key.Xuong).ThenBy(g => g.Key.Nhom1))
                    Them(chieu, g.Key.Xuong, g.Key.Nhom1);
            // Mã cột dữ liệu so khớp không phân biệt hoa thường với cột từ bảng mapping
            var maCot = cot.ToDictionary(c => c.Ma, c => c.Ma, StringComparer.OrdinalIgnoreCase);
            if (gan.Any(g => g.Ma == MaChuaMap)) cot.Add(new HRC1_GNCotNhom(MaChuaMap, "", "Chưa map", "N/A"));

            var kq = new HRC1_GNTongHopNhom { Cot = cot };
            foreach (var g in gan.GroupBy(g => (g.Dong.NgaySX, g.Dong.CaSX, g.Dong.Kip)).OrderBy(g => g.Key.NgaySX).ThenBy(g => g.Key.CaSX).ThenBy(g => g.Key.Kip))
            {
                var dong = new HRC1_GNDongNhom { NgaySX = g.Key.NgaySX, CaSX = g.Key.CaSX, Kip = g.Key.Kip, SoDong = g.Count() };
                foreach (var x in g)
                {
                    var ma = x.Ma == MaChuaMap ? MaChuaMap : maCot[x.Ma];
                    dong.GiaTri[ma] = dong.GiaTri.GetValueOrDefault(ma) + x.GiaTri;
                    kq.Tong[ma] = kq.Tong.GetValueOrDefault(ma) + x.GiaTri;
                    kq.TongCong += x.GiaTri;
                }
                kq.Dong.Add(dong);
            }
            return kq;
        }

        public async Task<byte[]> ExportExcelAsync(DateOnly tuNgay, DateOnly denNgay)
        {
            var th = await GetAsync(tuNgay, denNgay);
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("TongHopNhom");
            const int cotDau = 4;                       // A Ngày, B Ca, C Kíp
            var cotCuoi = cotDau + th.Cot.Count - 1;
            var vang = XLColor.FromHtml("#FFFF00");
            var nenSo = XLColor.FromHtml("#FFF2CC");

            ws.Cell(1, 1).Value = "BẢNG TỔNG HỢP DỮ LIỆU THEO NHÓM";
            ws.Range(1, 1, 1, Math.Max(cotCuoi, 3)).Merge().Style
                .Font.SetBold().Font.SetFontSize(14).Fill.SetBackgroundColor(vang).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            ws.Cell(2, 1).Value = $"Giao nhận Xưởng Đúc phôi vuông — từ {tuNgay:dd/MM/yyyy} đến {denNgay:dd/MM/yyyy} — Số liệu sau điều chỉnh (kg)";
            ws.Range(2, 1, 2, Math.Max(cotCuoi, 3)).Merge().Style.Font.SetItalic();

            // Header 3 tầng: Chiều / Xưởng / Nhóm 1 (Ngày, Ca, Kíp gộp 3 dòng)
            const int h1 = 4, h2 = 5, h3 = 6, dongTong = 7;
            string[] dau = ["Ngày", "Ca", "Kíp"];
            for (var i = 0; i < 3; i++)
            {
                ws.Cell(h1, i + 1).Value = dau[i];
                ws.Range(h1, i + 1, h3, i + 1).Merge();
            }
            for (var i = 0; i < th.Cot.Count; i++)
            {
                var c = th.Cot[i];
                ws.Cell(h1, cotDau + i).Value = c.Chieu.Length == 0 ? "Chưa map" : c.Chieu;
                ws.Cell(h2, cotDau + i).Value = c.Xuong;
                ws.Cell(h3, cotDau + i).Value = c.Nhom1;
            }
            // Gộp ô chiều / xưởng liền kề giống nhau
            GopLienKe(ws, h1, cotDau, cotCuoi, i => th.Cot[i].Chieu);
            GopLienKe(ws, h2, cotDau, cotCuoi, i => th.Cot[i].Chieu + "|" + th.Cot[i].Xuong);

            ws.Cell(dongTong, 1).Value = "Tổng";
            ws.Range(dongTong, 1, dongTong, 3).Merge();
            for (var i = 0; i < th.Cot.Count; i++)
                GhiSo(ws.Cell(dongTong, cotDau + i), th.Tong.GetValueOrDefault(th.Cot[i].Ma));
            ws.Range(dongTong, 1, dongTong, Math.Max(cotCuoi, 3)).Style.Font.SetBold().Fill.SetBackgroundColor(nenSo);

            var r = dongTong + 1;
            foreach (var d in th.Dong)
            {
                ws.Cell(r, 1).Value = d.NgaySX.ToDateTime(TimeOnly.MinValue);
                ws.Cell(r, 1).Style.DateFormat.Format = "dd/MM/yyyy";
                ws.Cell(r, 2).Value = d.CaSX switch { 1 => "N", 2 => "Đ", _ => "" };
                ws.Cell(r, 3).Value = d.Kip ?? "";
                for (var i = 0; i < th.Cot.Count; i++)
                    GhiSo(ws.Cell(r, cotDau + i), d.GiaTri.GetValueOrDefault(th.Cot[i].Ma));
                r++;
            }

            var bang = ws.Range(h1, 1, Math.Max(r - 1, dongTong), Math.Max(cotCuoi, 3));
            bang.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);
            var hdr = ws.Range(h1, 1, h3, Math.Max(cotCuoi, 3));
            hdr.Style.Font.SetBold().Alignment.SetWrapText().Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center).Fill.SetBackgroundColor(XLColor.FromHtml("#DDEBF7"));
            ws.Range(h3, cotDau, h3, Math.Max(cotCuoi, cotDau)).Style.Font.SetFontColor(XLColor.FromHtml("#C00000"));
            ws.Range(dongTong + 1, 2, Math.Max(r - 1, dongTong + 1), 3).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            ws.Column(1).Width = 12;
            ws.Column(2).Width = 5;
            ws.Column(3).Width = 5;
            for (var c = cotDau; c <= cotCuoi; c++) ws.Column(c).Width = 15;
            ws.Row(h3).Height = 32;
            ws.SheetView.FreezeRows(dongTong);
            ws.SheetView.FreezeColumns(3);

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        private static void GhiSo(IXLCell cell, decimal v)
        {
            if (v == 0) { cell.Value = "-"; cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center); return; }
            cell.Value = v;
            cell.Style.NumberFormat.Format = "#,##0";
        }

        private static void GopLienKe(IXLWorksheet ws, int row, int cotDau, int cotCuoi, Func<int, string> khoa)
        {
            var batDau = cotDau;
            for (var c = cotDau + 1; c <= cotCuoi + 1; c++)
            {
                if (c <= cotCuoi && khoa(c - cotDau) == khoa(batDau - cotDau)) continue;
                if (c - 1 > batDau) ws.Range(row, batDau, row, c - 1).Merge();
                batDau = c;
            }
        }
    }
}

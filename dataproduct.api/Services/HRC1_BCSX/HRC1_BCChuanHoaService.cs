using dataproduct.api.Models;
using dataproduct.api.Models.MasterData;
using dataproduct.api.Repositories;
using dataproduct.api.Services.Kdl.HRC1DPV;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Services
{
    public class HRC1_XuongChuanHoaPayload
    {
        public int? IdXuong { get; set; }
        public string? Xuong { get; set; }
        public int? IdPhongBan { get; set; }
        public string? BoPhan { get; set; }
        public string? VietTat { get; set; }
        public int? ThuTu { get; set; }
        public bool? IsActive { get; set; }
    }

    public class HRC1_NVLChuanHoaPayload
    {
        public int? IdVatTu { get; set; }
        public string? TenVatTu { get; set; }
        public string? XuongVietTat { get; set; }
        public string? Nhom1 { get; set; }
        public bool? CauHinhPhanBo { get; set; }
        public int? ThuTu { get; set; }
        public bool? IsActive { get; set; }
    }

    public record HRC1_XuongChuanHoaDto(int Id, int? IdXuong, string Xuong, int? IdPhongBan, string BoPhan, string VietTat,
        int ThuTu, bool IsActive, string? NguoiCapNhat, DateTime? NgayCapNhat);

    public record HRC1_NVLChuanHoaDto(int Id, int? IdVatTu, string TenVatTu, string XuongVietTat, string Nhom1, bool CauHinhPhanBo,
        int ThuTu, bool IsActive, string? NguoiCapNhat, DateTime? NgayCapNhat);

    /// <summary>Xưởng đang ra N/A (bên nhận hoặc bên giao) — Id tra từ Tbl_Xuong/Tbl_PhongBan để điền sẵn khi thêm.</summary>
    public record HRC1_XuongChuaMapDto(string Xuong, string BoPhan, int? IdXuong, int? IdPhongBan, string Ben, int SoDong);

    /// <summary>Cặp (vật tư, BP bên nhận) đang ra N/A Nội dung 1.</summary>
    public record HRC1_NVLChuaMapDto(string TenVatTu, string XuongVietTat, int SoDong);

    public record HRC1_ChuaMapDto(List<HRC1_XuongChuaMapDto> Xuong, List<HRC1_NVLChuaMapDto> Nvl);

    /// <summary>
    /// Quản lý 2 bảng tra cứu của form Giao nhận Xưởng ĐPV (module KDL HRC1_DPV_GN):
    /// HRC1_BC_XuongChuanHoa (xưởng + BP → viết tắt), HRC1_BC_NVLChuanHoa (vật tư + xưởng viết tắt → Nhóm 1).
    /// Không xóa cứng (Ngưng dùng = IsActive false). Sửa xong cần đồng bộ lại form để áp dụng.
    /// Nghiệp vụ: docs/BCSX_HRC1_DucVuong/03-GiaoNhan-Form-Dong.md §5
    /// </summary>
    public class HRC1_BCChuanHoaService
    {
        private const int VietTatToiDa = 30;   // = KDL_DoiTuong.MayDuc

        private readonly IHRC1_BCChuanHoaRepository _repo;
        private readonly ProductDataMasterDbContext _master;

        public HRC1_BCChuanHoaService(IHRC1_BCChuanHoaRepository repo, ProductDataMasterDbContext master)
        {
            _repo = repo;
            _master = master;
        }

        // ─── Xưởng ──────────────────────────────────────────────────────────────

        public async Task<List<HRC1_XuongChuanHoaDto>> GetXuongAsync()
            => (await _repo.GetXuongAsync()).Select(ToDto).ToList();

        public async Task<HRC1_XuongChuanHoaDto> CreateXuongAsync(HRC1_XuongChuanHoaPayload p, string? nguoi)
        {
            var all = await _repo.GetXuongAsync();
            var e = new HRC1_BC_XuongChuanHoa { NguoiTao = nguoi, NgayTao = DateTime.Now };
            GanXuong(e, p, all, idDangSua: null);
            e.ThuTu = p.ThuTu ?? (all.Count == 0 ? 10 : all.Max(x => x.ThuTu) + 10);
            e.IsActive = p.IsActive ?? true;
            await _repo.AddAsync(e);
            await _repo.SaveAsync();
            return ToDto(e);
        }

        public async Task<HRC1_XuongChuanHoaDto?> UpdateXuongAsync(int id, HRC1_XuongChuanHoaPayload p, string? nguoi)
        {
            var e = await _repo.GetXuongByIdAsync(id);
            if (e == null) return null;
            GanXuong(e, p, await _repo.GetXuongAsync(), idDangSua: id);
            if (p.ThuTu.HasValue) e.ThuTu = p.ThuTu.Value;
            if (p.IsActive.HasValue) e.IsActive = p.IsActive.Value;
            e.NguoiCapNhat = nguoi;
            e.NgayCapNhat = DateTime.Now;
            await _repo.SaveAsync();
            return ToDto(e);
        }

        private static void GanXuong(HRC1_BC_XuongChuanHoa e, HRC1_XuongChuanHoaPayload p, List<HRC1_BC_XuongChuanHoa> all, int? idDangSua)
        {
            var xuong = BatBuoc(p.Xuong, "Xưởng", 200);
            var boPhan = BatBuoc(p.BoPhan, "Bộ phận", 200);
            var vietTat = BatBuoc(p.VietTat, "Viết tắt", VietTatToiDa);

            var khac = all.Where(x => x.ID != idDangSua).ToList();
            if (khac.Any(x => K(x.Xuong) == K(xuong) && K(x.BoPhan) == K(boPhan)))
                throw new InvalidOperationException($"Đã có mapping cho xưởng \"{xuong}\" / \"{boPhan}\".");
            if (p.IdXuong.HasValue && khac.Any(x => x.ID_Xuong == p.IdXuong && x.ID_PhongBan == p.IdPhongBan))
                throw new InvalidOperationException($"Đã có mapping cho ID_Xuong {p.IdXuong} / ID_PhongBan {p.IdPhongBan}.");

            e.ID_Xuong = p.IdXuong;
            e.Xuong = xuong;
            e.ID_PhongBan = p.IdPhongBan;
            e.BoPhan = boPhan;
            e.VietTat = vietTat;
        }

        // ─── NVL ────────────────────────────────────────────────────────────────

        public async Task<List<HRC1_NVLChuanHoaDto>> GetNvlAsync()
            => (await _repo.GetNvlAsync()).Select(ToDto).ToList();

        public async Task<HRC1_NVLChuanHoaDto> CreateNvlAsync(HRC1_NVLChuanHoaPayload p, string? nguoi)
        {
            var all = await _repo.GetNvlAsync();
            var e = new HRC1_BC_NVLChuanHoa { NguoiTao = nguoi, NgayTao = DateTime.Now };
            GanNvl(e, p, all, idDangSua: null);
            e.CauHinhPhanBo = p.CauHinhPhanBo ?? false;
            e.ThuTu = p.ThuTu ?? (all.Count == 0 ? 10 : all.Max(x => x.ThuTu) + 10);
            e.IsActive = p.IsActive ?? true;
            await _repo.AddAsync(e);
            await _repo.SaveAsync();
            return ToDto(e);
        }

        public async Task<HRC1_NVLChuanHoaDto?> UpdateNvlAsync(int id, HRC1_NVLChuanHoaPayload p, string? nguoi)
        {
            var e = await _repo.GetNvlByIdAsync(id);
            if (e == null) return null;
            GanNvl(e, p, await _repo.GetNvlAsync(), idDangSua: id);
            if (p.CauHinhPhanBo.HasValue) e.CauHinhPhanBo = p.CauHinhPhanBo.Value;
            if (p.ThuTu.HasValue) e.ThuTu = p.ThuTu.Value;
            if (p.IsActive.HasValue) e.IsActive = p.IsActive.Value;
            e.NguoiCapNhat = nguoi;
            e.NgayCapNhat = DateTime.Now;
            await _repo.SaveAsync();
            return ToDto(e);
        }

        private static void GanNvl(HRC1_BC_NVLChuanHoa e, HRC1_NVLChuanHoaPayload p, List<HRC1_BC_NVLChuanHoa> all, int? idDangSua)
        {
            var tenVatTu = BatBuoc(p.TenVatTu, "Tên vật tư", 200);
            var xuongVietTat = BatBuoc(p.XuongVietTat, "Xưởng viết tắt", VietTatToiDa);
            var nhom1 = BatBuoc(p.Nhom1, "Nhóm 1", 100);

            if (all.Any(x => x.ID != idDangSua && K(x.TenVatTu) == K(tenVatTu) && K(x.XuongVietTat) == K(xuongVietTat) && K(x.Nhom1) == K(nhom1)))
                throw new InvalidOperationException($"Đã có mapping \"{tenVatTu}\" / {xuongVietTat} → \"{nhom1}\".");

            e.ID_VatTu = p.IdVatTu;
            e.TenVatTu = tenVatTu;
            e.XuongVietTat = xuongVietTat;
            e.Nhom1 = nhom1;
        }

        // ─── Mapping còn thiếu (đang N/A trên form Giao nhận) ────────────────────

        public async Task<HRC1_ChuaMapDto> GetChuaMapAsync(DateOnly tuNgay, DateOnly denNgay)
        {
            var na = HRC1DPVGN_BBGNNguon.KhongTraDuoc;
            var dong = await _repo.GetDongTraCuuAsync(HRC1DPVGN_BBGNNguon.Module, tuNgay, denNgay);

            var xuongNa = dong.Where(d => d.BpNhan == na).Select(d => (Xuong: d.XuongNhan, BoPhan: d.BoPhanNhan, Ben: "Bên nhận"))
                .Concat(dong.Where(d => d.BpGiao == na).Select(d => (Xuong: d.XuongGiao, BoPhan: d.BoPhanGiao, Ben: "Bên giao")))
                .Where(x => !string.IsNullOrWhiteSpace(x.Xuong))
                .GroupBy(x => (K(x.Xuong), K(x.BoPhan)))
                .Select(g => (Xuong: HRC1DPVGN_BBGNNguon.Gon(g.First().Xuong), BoPhan: HRC1DPVGN_BBGNNguon.Gon(g.First().BoPhan),
                              Ben: string.Join(", ", g.Select(x => x.Ben).Distinct()), SoDong: g.Count()))
                .ToList();

            var ids = xuongNa.Count == 0
                ? new Dictionary<(string, string), (int? IdXuong, int? IdPhongBan)>()
                : await TraIdXuongAsync();
            var xuong = xuongNa
                .Select(x =>
                {
                    var id = ids.GetValueOrDefault((K(x.Xuong), K(x.BoPhan)));
                    return new HRC1_XuongChuaMapDto(x.Xuong, x.BoPhan, id.IdXuong, id.IdPhongBan, x.Ben, x.SoDong);
                })
                .OrderByDescending(x => x.SoDong).ToList();

            // Nội dung 1 tra theo (vật tư, BP đối tác) — chỉ tra được khi đã có BP đối tác
            var nvl = dong.Where(d => d.NoiDung1 == na && d.BpDoiTac != na && !string.IsNullOrWhiteSpace(d.VatTu) && !string.IsNullOrWhiteSpace(d.BpDoiTac))
                .GroupBy(d => (K(d.VatTu), K(d.BpDoiTac)))
                .Select(g => new HRC1_NVLChuaMapDto(HRC1DPVGN_BBGNNguon.Gon(g.First().VatTu), g.First().BpDoiTac!, g.Count()))
                .OrderByDescending(x => x.SoDong).ToList();

            return new HRC1_ChuaMapDto(xuong, nvl);
        }

        /// <summary>(tên xưởng, tên phòng ban) đã chuẩn hóa → ID trong master; trùng tên ưu tiên xưởng đang hoạt động.</summary>
        private async Task<Dictionary<(string, string), (int? IdXuong, int? IdPhongBan)>> TraIdXuongAsync()
        {
            var xuongs = await _master.Tbl_Xuong.AsNoTracking()
                .Select(x => new { x.ID_Xuong, x.TenXuong, x.ID_PhongBan, x.ID_TrangThai }).ToListAsync();
            var phongBans = await _master.Tbl_PhongBan.AsNoTracking()
                .ToDictionaryAsync(p => p.ID_PhongBan, p => p.TenPhongBan);
            return xuongs
                .Where(x => x.ID_PhongBan.HasValue && phongBans.ContainsKey(x.ID_PhongBan.Value))
                .GroupBy(x => (K(x.TenXuong), K(phongBans[x.ID_PhongBan!.Value])))
                .ToDictionary(g => g.Key, g =>
                {
                    var x = g.OrderByDescending(v => v.ID_TrangThai == 1).ThenByDescending(v => v.ID_Xuong).First();
                    return ((int?)x.ID_Xuong, x.ID_PhongBan);
                });
        }

        // ─── Tiện ích ───────────────────────────────────────────────────────────

        private static string K(string? s) => HRC1DPVGN_BBGNNguon.ChuanHoa(s);

        private static string BatBuoc(string? s, string ten, int max)
        {
            var v = HRC1DPVGN_BBGNNguon.Gon(s);
            if (v.Length == 0) throw new InvalidOperationException($"{ten} không được để trống.");
            if (v.Length > max) throw new InvalidOperationException($"{ten} tối đa {max} ký tự.");
            return v;
        }

        private static HRC1_XuongChuanHoaDto ToDto(HRC1_BC_XuongChuanHoa x)
            => new(x.ID, x.ID_Xuong, x.Xuong, x.ID_PhongBan, x.BoPhan, x.VietTat, x.ThuTu, x.IsActive, x.NguoiCapNhat ?? x.NguoiTao, x.NgayCapNhat ?? x.NgayTao);

        private static HRC1_NVLChuanHoaDto ToDto(HRC1_BC_NVLChuanHoa x)
            => new(x.ID, x.ID_VatTu, x.TenVatTu, x.XuongVietTat, x.Nhom1, x.CauHinhPhanBo, x.ThuTu, x.IsActive, x.NguoiCapNhat ?? x.NguoiTao, x.NgayCapNhat ?? x.NgayTao);
    }
}

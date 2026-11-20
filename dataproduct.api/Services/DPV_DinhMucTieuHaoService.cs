using dataproduct.api.Models;
using dataproduct.api.Repositories;

namespace dataproduct.api.Services
{
    public class DinhMucTieuHaoDto
    {
        public int Id { get; set; }
        public int? IdPB { get; set; }
        public int? IdXuong { get; set; }
        public string? DayChuyen { get; set; }
        public string? BaoCao { get; set; }
        public string ChiTieu { get; set; } = null!;
        public string? DonViTinh { get; set; }
        public decimal? MucSanLuong { get; set; }
        public decimal? DieuKienSanLuong { get; set; }
        public decimal? MucTieuHaoLDG { get; set; }
        public decimal? DieuKienTieuHaoLDG { get; set; }
        public decimal? MucTyLePhe { get; set; }
        public decimal? DieuKienTyLePhe { get; set; }
        public DateOnly? TuNgay { get; set; }
        public DateOnly? DenNgay { get; set; }
        public string? GhiChu { get; set; }
        public string? NguoiTao { get; set; }
        public DateTime NgayTao { get; set; }
        public string? NguoiCapNhat { get; set; }
        public DateTime? NgayCapNhat { get; set; }
    }

    public class DinhMucTieuHaoPayload
    {
        public int? IdPB { get; set; }
        public int? IdXuong { get; set; }
        public string? DayChuyen { get; set; }
        public string? BaoCao { get; set; }
        public string ChiTieu { get; set; } = null!;
        public string? DonViTinh { get; set; }
        public decimal? MucSanLuong { get; set; }
        public decimal? DieuKienSanLuong { get; set; }
        public decimal? MucTieuHaoLDG { get; set; }
        public decimal? DieuKienTieuHaoLDG { get; set; }
        public decimal? MucTyLePhe { get; set; }
        public decimal? DieuKienTyLePhe { get; set; }
        public DateOnly? TuNgay { get; set; }
        public DateOnly? DenNgay { get; set; }
        public string? GhiChu { get; set; }
    }

    public class DinhMucTieuHaoService
    {
        private readonly IDinhMucTieuHaoRepository _repo;

        public DinhMucTieuHaoService(IDinhMucTieuHaoRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<DinhMucTieuHaoDto>> SearchAsync(int? idPB, int? idXuong, string? baoCao, string? chiTieu, DateOnly? ngayApDung)
        {
            var rows = await _repo.SearchAsync(idPB, idXuong, baoCao, chiTieu, ngayApDung);
            return rows.Select(ToDto).ToList();
        }

        public async Task<DinhMucTieuHao> CreateAsync(DinhMucTieuHaoPayload payload, string? nguoi)
        {
            var entity = new DinhMucTieuHao { NguoiTao = nguoi, NgayTao = DateTime.Now };
            Apply(entity, payload);
            await _repo.AddAsync(entity);
            return entity;
        }

        public async Task<bool> UpdateAsync(int id, DinhMucTieuHaoPayload payload, string? nguoi)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null) return false;

            Apply(entity, payload);
            entity.NguoiCapNhat = nguoi;
            entity.NgayCapNhat = DateTime.Now;
            await _repo.UpdateAsync(entity);
            return true;
        }

        public async Task<bool> DeleteAsync(int id, string? nguoi)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null) return false;

            entity.IsDelete = true;
            entity.NguoiCapNhat = nguoi;
            entity.NgayCapNhat = DateTime.Now;
            await _repo.UpdateAsync(entity);
            return true;
        }

        private static void Apply(DinhMucTieuHao entity, DinhMucTieuHaoPayload payload)
        {
            var chiTieu = payload.ChiTieu?.Trim();
            if (string.IsNullOrEmpty(chiTieu))
                throw new InvalidOperationException("Chỉ tiêu không được để trống.");
            if (payload.DenNgay.HasValue && payload.TuNgay.HasValue && payload.DenNgay < payload.TuNgay)
                throw new InvalidOperationException("Đến ngày phải sau Từ ngày.");

            entity.IdPB = payload.IdPB;
            entity.IdXuong = payload.IdXuong;
            entity.DayChuyen = string.IsNullOrWhiteSpace(payload.DayChuyen) ? null : payload.DayChuyen.Trim();
            entity.BaoCao = string.IsNullOrWhiteSpace(payload.BaoCao) ? null : payload.BaoCao.Trim();
            entity.ChiTieu = chiTieu;
            entity.DonViTinh = string.IsNullOrWhiteSpace(payload.DonViTinh) ? null : payload.DonViTinh.Trim();
            entity.MucSanLuong = payload.MucSanLuong;
            entity.DieuKienSanLuong = payload.DieuKienSanLuong;
            entity.MucTieuHaoLDG = payload.MucTieuHaoLDG;
            entity.DieuKienTieuHaoLDG = payload.DieuKienTieuHaoLDG;
            entity.MucTyLePhe = payload.MucTyLePhe;
            entity.DieuKienTyLePhe = payload.DieuKienTyLePhe;
            entity.TuNgay = payload.TuNgay;
            entity.DenNgay = payload.DenNgay;
            entity.GhiChu = string.IsNullOrWhiteSpace(payload.GhiChu) ? null : payload.GhiChu.Trim();
        }

        private static DinhMucTieuHaoDto ToDto(DinhMucTieuHao x) => new()
        {
            Id = x.Id,
            IdPB = x.IdPB,
            IdXuong = x.IdXuong,
            DayChuyen = x.DayChuyen,
            BaoCao = x.BaoCao,
            ChiTieu = x.ChiTieu,
            DonViTinh = x.DonViTinh,
            MucSanLuong = x.MucSanLuong,
            DieuKienSanLuong = x.DieuKienSanLuong,
            MucTieuHaoLDG = x.MucTieuHaoLDG,
            DieuKienTieuHaoLDG = x.DieuKienTieuHaoLDG,
            MucTyLePhe = x.MucTyLePhe,
            DieuKienTyLePhe = x.DieuKienTyLePhe,
            TuNgay = x.TuNgay,
            DenNgay = x.DenNgay,
            GhiChu = x.GhiChu,
            NguoiTao = x.NguoiTao,
            NgayTao = x.NgayTao,
            NguoiCapNhat = x.NguoiCapNhat,
            NgayCapNhat = x.NgayCapNhat,
        };
    }
}

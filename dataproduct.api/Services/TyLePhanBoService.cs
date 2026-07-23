using dataproduct.api.DTOs.NMLG_Dto;
using dataproduct.api.Models;
using dataproduct.api.Repositories;

namespace dataproduct.api.Services
{
    public class TyLePhanBoService
    {
        private readonly ITyLePhanBoRepository _repo;
        private readonly IKetQuaPhanBoRepository _ketQuaRepo;

        public TyLePhanBoService(ITyLePhanBoRepository repo, IKetQuaPhanBoRepository ketQuaRepo)
        {
            _repo = repo;
            _ketQuaRepo = ketQuaRepo;
        }

        public async Task<List<TyLePhanBoDto>> GetHistoryAsync(int idNvl, DateTime? tuNgay, DateTime? denNgay)
        {
            var list = await _repo.GetHistoryAsync(idNvl, tuNgay, denNgay);
            return list.Select(x => new TyLePhanBoDto
            {
                Id = x.ID,
                IdNvl = x.IDNVL,
                Ngay = x.Ngay,
                Ca = x.Ca,
                TyLe = x.TyLe,
                GhiChu = x.GhiChu,
                IdNguoiNhap = x.IDNguoiNhap,
                NgayNhap = x.NgayNhap
            }).ToList();
        }

        private static readonly byte[] TatCaLoaiPhanBo = { 1, 2, 3 };

        public async Task<TyLePhanBoDto> CreateAsync(CreateTyLePhanBoDto dto)
        {
            if (dto.TyLe < 0 || dto.TyLe > 1)
                throw new InvalidOperationException("Tỷ lệ phải nằm trong khoảng 0 đến 1.");

            // Cho phép sửa % nhiều lần (ghi đè) miễn là ngày chưa chốt — Chốt khóa cả 3 loại phân bổ cùng lúc
            foreach (var loai in TatCaLoaiPhanBo)
            {
                if (await _ketQuaRepo.IsNgayDaChotAsync(dto.Ngay.Date, loai))
                    throw new InvalidOperationException($"Ngày {dto.Ngay:dd/MM/yyyy} đã chốt, không thể sửa tỷ lệ.");
            }

            var entity = new LG_TyLePhanBo
            {
                IDNVL = dto.IdNvl,
                Ngay = dto.Ngay.Date,
                Ca = dto.Ca,
                TyLe = dto.TyLe,
                GhiChu = dto.GhiChu,
                IDNguoiNhap = dto.IdNguoiNhap
            };
            var saved = await _repo.UpsertAsync(entity);

            return new TyLePhanBoDto
            {
                Id = saved.ID,
                IdNvl = saved.IDNVL,
                Ngay = saved.Ngay,
                Ca = saved.Ca,
                TyLe = saved.TyLe,
                GhiChu = saved.GhiChu,
                IdNguoiNhap = saved.IDNguoiNhap,
                NgayNhap = saved.NgayNhap
            };
        }
    }
}

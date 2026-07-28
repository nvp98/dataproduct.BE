using dataproduct.api.DTOs.NMLG_Dto;
using dataproduct.api.Models;
using dataproduct.api.Repositories;
using dataproduct.api.Utils.Enums;

namespace dataproduct.api.Services
{
    public class TyLePhanBoService
    {
        private readonly ITyLePhanBoRepository _repo;
        private readonly IKetQuaPhanBoRepository _ketQuaRepo;
        private readonly INhomPhanBoRepository _nhomRepo;

        public TyLePhanBoService(ITyLePhanBoRepository repo, IKetQuaPhanBoRepository ketQuaRepo, INhomPhanBoRepository nhomRepo)
        {
            _repo = repo;
            _ketQuaRepo = ketQuaRepo;
            _nhomRepo = nhomRepo;
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

            var entity = new LG_PB_TyLePhanBo
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

        // ─── % theo nhóm (chỉ nhóm PP2) — nhập 1 lần, cascade xuống mọi NVL thành viên ─

        public async Task<decimal?> GetTyLeNhomAsync(int idNhomPhanBo, DateTime ngay, byte ca, int idLoCao)
        {
            var entity = await _repo.GetTyLeNhomAsync(idNhomPhanBo, ngay.Date, ca, idLoCao);
            return entity?.TyLe;
        }

        public async Task<int> CreateForNhomAsync(CreateTyLeNhomDto dto)
        {
            if (dto.TyLe < 0 || dto.TyLe > 1)
                throw new InvalidOperationException("Tỷ lệ phải nằm trong khoảng 0 đến 1.");

            foreach (var loai in TatCaLoaiPhanBo)
            {
                if (await _ketQuaRepo.IsNgayDaChotAsync(dto.Ngay.Date, loai))
                    throw new InvalidOperationException($"Ngày {dto.Ngay:dd/MM/yyyy} đã chốt, không thể sửa tỷ lệ.");
            }

            var nhom = await _nhomRepo.GetByIdAsync(dto.IdNhomPhanBo)
                ?? throw new InvalidOperationException("Không tìm thấy nhóm phân bổ.");
            if (nhom.PhuongThucPhanBo != (byte)PhuongThucPhanBoEnum.TyLeNhapTay)
                throw new InvalidOperationException("Chỉ áp dụng % theo nhóm cho nhóm dùng phương thức Tỷ lệ nhập tay.");

            await _repo.UpsertTyLeNhomAsync(new LG_PB_TyLeNhom
            {
                IDNhomPhanBo = dto.IdNhomPhanBo,
                Ngay = dto.Ngay.Date,
                Ca = dto.Ca,
                IDLoCao = dto.IdLoCao,
                TyLe = dto.TyLe,
                GhiChu = dto.GhiChu,
                IDNguoiNhap = dto.IdNguoiNhap
            });

            // Cascade % vừa nhập xuống toàn bộ NVL đang thuộc nhóm tại đúng (Ngày, Ca) này
            var thanhVien = await _nhomRepo.GetNvlByNhomAsync(dto.IdNhomPhanBo, dto.Ngay.Date, dto.Ca);
            foreach (var tv in thanhVien)
            {
                await _repo.UpsertAsync(new LG_PB_TyLePhanBo
                {
                    IDNVL = tv.IdNvl,
                    Ngay = dto.Ngay.Date,
                    Ca = dto.Ca,
                    TyLe = dto.TyLe,
                    IDNguoiNhap = dto.IdNguoiNhap
                });
            }
            return thanhVien.Count;
        }
    }
}

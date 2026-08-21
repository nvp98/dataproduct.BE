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

        // LG_PB_TyLePhanBo/LG_PB_TyLeNhom không lưu lò cao — tỷ lệ dùng chung cho NVL đó ở MỌI lò cao
        // của (Ngày, Ca) — nên "đã chốt" ở đây phải xét bất kỳ lò cao nào của (Ngày, Ca) đã chốt (ở bất
        // kỳ loại phân bổ nào, vì Chốt khóa cả 3 loại cùng lúc), không phải riêng 1 lò cao. Nếu Ca không
        // xác định thì kiểm tra bất kỳ ca nào của ngày đã chốt để an toàn.
        // Dùng chung cho gate sửa tỷ lệ (CreateAsync/CreateForNhomAsync) VÀ cho FE hỏi trước khi hiện UI sửa.
        public async Task<bool> IsCaDaChotAsync(DateTime ngay, byte? ca)
        {
            foreach (var loai in TatCaLoaiPhanBo)
            {
                var daChot = ca.HasValue
                    ? await _ketQuaRepo.IsCaDaChotAsync(ngay.Date, loai, ca.Value)
                    : (await _ketQuaRepo.GetChotSetAsync(ngay.Date, loai)).Count > 0;
                if (daChot) return true;
            }
            return false;
        }

        public async Task<TyLePhanBoDto> CreateAsync(CreateTyLePhanBoDto dto)
        {
            if (dto.TyLe < 0 || dto.TyLe > 1)
                throw new InvalidOperationException("Tỷ lệ phải nằm trong khoảng 0 đến 1.");

            // Cho phép sửa % nhiều lần (ghi đè) miễn là ca đó chưa chốt — Chốt khóa cả 3 loại phân bổ cùng lúc
            if (await IsCaDaChotAsync(dto.Ngay.Date, dto.Ca))
                throw new InvalidOperationException(
                    $"Ngày {dto.Ngay:dd/MM/yyyy}{(dto.Ca.HasValue ? $", Ca {dto.Ca}" : "")} đã chốt, không thể sửa tỷ lệ.");

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

        public async Task<(int SoCapNhat, int SoGiuNguyen)> CreateForNhomAsync(CreateTyLeNhomDto dto)
        {
            if (dto.TyLe < 0 || dto.TyLe > 1)
                throw new InvalidOperationException("Tỷ lệ phải nằm trong khoảng 0 đến 1.");

            if (await IsCaDaChotAsync(dto.Ngay.Date, dto.Ca))
                throw new InvalidOperationException($"Ngày {dto.Ngay:dd/MM/yyyy}, Ca {dto.Ca} đã chốt, không thể sửa tỷ lệ.");

            var nhom = await _nhomRepo.GetByIdAsync(dto.IdNhomPhanBo)
                ?? throw new InvalidOperationException("Không tìm thấy nhóm phân bổ.");
            if (nhom.PhuongThucPhanBo != (byte)PhuongThucPhanBoEnum.TyLeNhapTay)
                throw new InvalidOperationException("Chỉ áp dụng % theo nhóm cho nhóm dùng phương thức Tỷ lệ nhập tay.");

            // % nhóm CŨ (trước khi ghi giá trị mới) — dùng để nhận diện NVL nào đang "theo % nhóm"
            // (chưa từng sửa riêng) so với NVL đã có % riêng khác đi.
            var tyLeNhomCu = await _repo.GetTyLeNhomAsync(dto.IdNhomPhanBo, dto.Ngay.Date, dto.Ca, dto.IdLoCao);

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

            // Cascade % vừa nhập xuống NVL đang thuộc nhóm — CHỈ với NVL chưa có % nào (mới) hoặc đang
            // giữ đúng % nhóm cũ (chưa sửa riêng). NVL đã có % riêng khác % nhóm cũ thì GIỮ NGUYÊN, không
            // ghi đè — coi đó là giá trị người dùng đã cố tình sửa riêng cho đúng NVL đó.
            var thanhVien = await _nhomRepo.GetNvlByNhomAsync(dto.IdNhomPhanBo, dto.Ngay.Date, dto.Ca);
            var hienTaiMap = thanhVien.Count > 0
                ? await _repo.GetExactMapAsync(thanhVien.Select(x => x.IdNvl), dto.Ngay.Date, dto.Ca)
                : new Dictionary<int, decimal>();

            var soCapNhat = 0;
            var soGiuNguyen = 0;
            foreach (var tv in thanhVien)
            {
                var dangTheoNhom = !hienTaiMap.TryGetValue(tv.IdNvl, out var hienTai)
                    || (tyLeNhomCu != null && hienTai == tyLeNhomCu.TyLe);

                if (!dangTheoNhom)
                {
                    soGiuNguyen++;
                    continue;
                }

                await _repo.UpsertAsync(new LG_PB_TyLePhanBo
                {
                    IDNVL = tv.IdNvl,
                    Ngay = dto.Ngay.Date,
                    Ca = dto.Ca,
                    TyLe = dto.TyLe,
                    IDNguoiNhap = dto.IdNguoiNhap
                });
                soCapNhat++;
            }
            return (soCapNhat, soGiuNguyen);
        }
    }
}

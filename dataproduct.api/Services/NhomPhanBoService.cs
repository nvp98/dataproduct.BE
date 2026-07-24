using dataproduct.api.DTOs.NMLG_Dto;
using dataproduct.api.Models;
using dataproduct.api.Repositories;
using dataproduct.api.Utils.Enums;

namespace dataproduct.api.Services
{
    public class NhomPhanBoService
    {
        private readonly INhomPhanBoRepository _repo;

        public NhomPhanBoService(INhomPhanBoRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<NhomPhanBoDto>> GetListAsync(byte? loaiPhanBo)
        {
            var list = await _repo.GetListAsync(loaiPhanBo);
            return list.Select(MapNhom).ToList();
        }

        public async Task<NhomPhanBoDto> CreateAsync(CreateNhomPhanBoDto dto)
        {
            ValidateLoaiPhanBo(dto.LoaiPhanBo);

            var entity = new LG_PB_NhomPhanBo
            {
                TenNhom = dto.TenNhom,
                LoaiPhanBo = dto.LoaiPhanBo,
                PhuongThucPhanBo = dto.PhuongThucPhanBo,
                ThuTu = dto.ThuTu
            };
            var created = await _repo.AddAsync(entity);
            return MapNhom(created);
        }

        // Than cốc <10mm dùng chung cấu hình nhóm/NVL với CVH (LoaiPhanBo=2) — không tạo nhóm riêng cho 3
        private static void ValidateLoaiPhanBo(byte loaiPhanBo)
        {
            if (loaiPhanBo == (byte)LoaiPhanBoEnum.ThanCoc10)
                throw new InvalidOperationException(
                    "Không tạo nhóm riêng cho Than cốc <10mm — nhóm này dùng chung cấu hình với CVH (chọn Loại phân bổ = CVH).");
        }

        public async Task<NhomPhanBoDto> UpdateAsync(int id, UpdateNhomPhanBoDto dto)
        {
            ValidateLoaiPhanBo(dto.LoaiPhanBo);

            var entity = new LG_PB_NhomPhanBo
            {
                TenNhom = dto.TenNhom,
                LoaiPhanBo = dto.LoaiPhanBo,
                PhuongThucPhanBo = dto.PhuongThucPhanBo,
                ThuTu = dto.ThuTu
            };
            var updated = await _repo.UpdateAsync(id, entity);
            if (updated == null) throw new InvalidOperationException("Không tìm thấy nhóm phân bổ.");
            return MapNhom(updated);
        }

        public async Task<bool> DeleteAsync(int id) => await _repo.DeleteAsync(id);

        // NVL thành viên của nhóm — cấu hình RIÊNG cho từng (Ngày, Ca), không kế thừa từ ngày/ca trước
        public async Task<List<NvlNhomPhanBoDto>> GetNvlByNhomAsync(int idNhomPhanBo, DateTime ngay, byte ca)
            => await _repo.GetNvlByNhomAsync(idNhomPhanBo, ngay, ca);

        // Với nhóm PP1 (tỷ trọng + dòng dư), "dòng dư" (NVL nhận phần bù trừ do làm tròn) được
        // PhanBoService TỰ ĐỘNG chọn theo khối lượng nạp liệu (E) lớn nhất tại thời điểm tính —
        // không cần cấu hình tay ở đây, kể cả khi nhóm chỉ có 1 NVL.
        public async Task<NvlNhomPhanBoDto> AddNvlAsync(int idNhomPhanBo, AddNvlNhomPhanBoDto dto)
        {
            _ = await _repo.GetByIdAsync(idNhomPhanBo)
                ?? throw new InvalidOperationException("Không tìm thấy nhóm phân bổ.");

            var entity = new LG_PB_NVL_NhomPhanBo
            {
                IDNVL = dto.IdNvl,
                IDNhomPhanBo = idNhomPhanBo,
                Ngay = dto.Ngay.Date,
                Ca = dto.Ca,
                IDLoCao = dto.IdLoCao,
                ThuTuUuTien = 0
            };
            await _repo.AddNvlAsync(entity);

            var list = await _repo.GetNvlByNhomAsync(idNhomPhanBo, dto.Ngay, dto.Ca);
            return list.First(x => x.IdNvl == dto.IdNvl);
        }

        public async Task<bool> RemoveNvlAsync(int idNhomPhanBo, int idNvl, DateTime ngay, byte ca)
            => await _repo.RemoveNvlAsync(idNhomPhanBo, idNvl, ngay, ca);

        private static NhomPhanBoDto MapNhom(LG_PB_NhomPhanBo x) => new()
        {
            Id = x.ID,
            TenNhom = x.TenNhom,
            LoaiPhanBo = x.LoaiPhanBo,
            PhuongThucPhanBo = x.PhuongThucPhanBo,
            ThuTu = x.ThuTu
        };
    }
}

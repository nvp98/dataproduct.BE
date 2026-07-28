using dataproduct.api.Models;
using dataproduct.api.Repositories;

namespace dataproduct.api.Services
{
    public class Hrc1PhuLieuNmUpsertDto
    {
        public string TenPhuLieu { get; set; } = null!;
        public int? ThuTu { get; set; }
    }

    public class Hrc1PhuLieuNmService
    {
        private readonly IHrc1PhuLieuNmRepository _repo;

        public Hrc1PhuLieuNmService(IHrc1PhuLieuNmRepository repo)
        {
            _repo = repo;
        }

        public Task<IEnumerable<Hrc1PhuLieuNm>> GetAllAsync(bool? dangSuDung, string? searchKey)
            => _repo.GetAllAsync(dangSuDung, searchKey);

        public Task<Hrc1PhuLieuNm?> GetByIdAsync(int id)
            => _repo.GetByIdAsync(id);

        public async Task<Hrc1PhuLieuNm> CreateAsync(Hrc1PhuLieuNmUpsertDto dto)
        {
            if (await _repo.ExistsByTenPhuLieuAsync(dto.TenPhuLieu))
                throw new InvalidOperationException("Tên phụ liệu đã tồn tại.");

            var entity = new Hrc1PhuLieuNm
            {
                TenPhuLieu = dto.TenPhuLieu,
                ThuTu = dto.ThuTu,
                // Phụ liệu thêm qua trang quản lý luôn là thêm tay, không gắn với cột view nguồn (TenPhuLieuNM),
                // và không đánh dấu IsNM — chỉ SP_Sync_HRC1_PhuLieu mới ghi nhận là dữ liệu tự động.
                TenPhuLieuNM = null,
                IsNM = false,
                DangSuDung = true,
                NgayTao = DateTime.Now,
            };
            await _repo.AddAsync(entity);
            return entity;
        }

        public async Task<bool> UpdateAsync(int id, Hrc1PhuLieuNmUpsertDto dto)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return false;

            if (await _repo.ExistsByTenPhuLieuAsync(dto.TenPhuLieu, id))
                throw new InvalidOperationException("Tên phụ liệu đã tồn tại.");

            existing.TenPhuLieu = dto.TenPhuLieu;
            existing.ThuTu = dto.ThuTu;
            await _repo.UpdateAsync(existing);
            return true;
        }

        public async Task<bool?> ToggleDangSuDungAsync(int id)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return null;

            existing.DangSuDung = !existing.DangSuDung;
            await _repo.UpdateAsync(existing);
            return existing.DangSuDung;
        }

        /// <summary>Bật/tắt danh sách mặc định cho Sổ Xuất-Nhập-Tồn HRC1 (dùng khi khởi tạo phiếu
        /// HRC1_STD_NXT đầu tiên — chưa có ca trước để kế thừa). Xem .claude/hrc1_xnt.md mục 2.2.</summary>
        public async Task<bool?> ToggleIsUsedNXTAsync(int id)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return null;

            existing.IsUsedNXT = !(existing.IsUsedNXT ?? false);
            await _repo.UpdateAsync(existing);
            return existing.IsUsedNXT;
        }

        /// <summary>Xóa phụ liệu — chặn nếu đã được dùng ở phiếu tiêu hao BOF/LF hoặc Sổ Xuất-Nhập-Tồn
        /// (chi tiết hoặc tổng hợp). Xem Hrc1PhuLieuNmRepository.GetInUseReasonAsync để biết đầy đủ các
        /// nơi được kiểm tra.</summary>
        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return false;

            var reason = await _repo.GetInUseReasonAsync(id);
            if (reason != null)
                throw new InvalidOperationException($"Không thể xóa phụ liệu vì {reason}.");

            await _repo.DeleteAsync(existing);
            return true;
        }
    }
}

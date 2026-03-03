using dataproduct.api.DTOs;
using dataproduct.api.Models;
using dataproduct.api.Repositories;
using dataproduct.api.Utils.Enums;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Services
{
    public class BmQuyenXlService
    {
        private readonly IBmQuyenXlRepository _repo;
        private readonly ProductFormContext _context;

        public BmQuyenXlService(IBmQuyenXlRepository repo, ProductFormContext context)
        {
            _repo = repo;
            _context = context;
        }

        public async Task<IEnumerable<BmQuyenXlDto>> GetAllAsync(int? idTaiKhoan, string? maBm, string? maKhuVuc)
        {
            var data = await _repo.GetAllAsync(idTaiKhoan, maBm, maKhuVuc);
            return data.Select(x => new BmQuyenXlDto
            {
                Id = x.Id,
                IdTaiKhoan = x.IdTaiKhoan,
                MaBm = x.MaBm,
                MaKhuVuc = x.MaKhuVuc,
                QuyenChucNang = x.QuyenChucNang
            });
        }

        public async Task<BmQuyenXlDto?> GetByIdAsync(int id)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null) return null;

            return new BmQuyenXlDto
            {
                Id = entity.Id,
                IdTaiKhoan = entity.IdTaiKhoan,
                MaBm = entity.MaBm,
                MaKhuVuc = entity.MaKhuVuc,
                QuyenChucNang = entity.QuyenChucNang
            };
        }

        public async Task<IEnumerable<BmQuyenXlDto>> GetByTaiKhoanIdAsync(int idTaiKhoan)
        {
            var data = await _repo.GetByTaiKhoanIdAsync(idTaiKhoan);
            return data.Select(x => new BmQuyenXlDto
            {
                Id = x.Id,
                IdTaiKhoan = x.IdTaiKhoan,
                MaBm = x.MaBm,
                MaKhuVuc = x.MaKhuVuc,
                QuyenChucNang = x.QuyenChucNang
            });
        }

        /// <summary>
        /// Lấy danh sách MaBM cho menu: Việc tôi bắt đầu (XULY, CHOT) và Việc đến tôi (PHEDUYET).
        /// QuyenChucNang null coi như XULY (1) để tương thích dữ liệu cũ.
        /// </summary>
        public async Task<MenuPermissionsDto> GetMenuPermissionsAsync(int idTaiKhoan)
        {
            var data = (await _repo.GetByTaiKhoanIdAsync(idTaiKhoan)).ToList();
            var processing = data
                .Where(x =>
                {
                    var q = x.QuyenChucNang;
                    return q == null
                        || q == (byte)QuyenChucNangEnum.XULY
                        || q == (byte)QuyenChucNangEnum.CHOT
                        || q == (byte)QuyenChucNangEnum.XULY_VA_PHEDUYET;
                })
                .Select(x => x.MaBm)
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct()
                .ToList();

            // Việc đến tôi: chỉ cần user nằm trong list phê duyệt của phiếu (BM_PheDuyet) và CapDuyet != 0
            // Không phụ thuộc vào BM_QuyenXL.QuyenChucNang
            var approving = await _context.BmPheDuyets
                .Where(pd =>
                    pd.NguoiDuyetId == idTaiKhoan
                    && (pd.CapDuyet ?? 0) != 0
                    && pd.PhieuId != null)
                .Join(
                    _context.BmPhieus.Where(p => p.IsDelete != 1),
                    pd => pd.PhieuId,
                    p => (Guid?)p.Idphieu,
                    (pd, p) => p.MaBm ?? "")
                .Where(maBm => !string.IsNullOrEmpty(maBm))
                .Distinct()
                .ToListAsync();
            return new MenuPermissionsDto
            {
                ProcessingForms = processing,
                ApprovingForms = approving
            };
        }

        public async Task<BmQuyenXl> CreateAsync(BmQuyenXlCreateDto dto)
        {
            // Kiểm tra trùng lặp
            var isDuplicate = await _repo.CheckDuplicateAsync(dto.IdTaiKhoan, dto.MaBm, dto.MaKhuVuc, dto.QuyenChucNang);
            if (isDuplicate)
            {
                throw new InvalidOperationException(
                    $"Quyền xử lý đã tồn tại cho Tài khoản ID: {dto.IdTaiKhoan ?? 0}, Mã BM: '{dto.MaBm}', Khu vực: '{dto.MaKhuVuc}'"
                );
            }

            var entity = new BmQuyenXl
            {
                IdTaiKhoan = dto.IdTaiKhoan,
                MaBm = dto.MaBm,
                MaKhuVuc = dto.MaKhuVuc,
                QuyenChucNang = dto.QuyenChucNang
            };

            await _repo.AddAsync(entity);
            return entity;
        }

        public async Task<bool> UpdateAsync(int id, BmQuyenXlUpdateDto dto)
        {
            if (!await _repo.ExistsAsync(id)) return false;

            // Kiểm tra trùng lặp (loại trừ bản ghi hiện tại)
            var isDuplicate = await _repo.CheckDuplicateAsync(dto.IdTaiKhoan, dto.MaBm, dto.MaKhuVuc, dto.QuyenChucNang, id);
            if (isDuplicate)
            {
                throw new InvalidOperationException(
                    $"Quyền xử lý đã tồn tại cho Tài khoản ID: {dto.IdTaiKhoan ?? 0}, Mã BM: '{dto.MaBm}', Khu vực: '{dto.MaKhuVuc}'"
                );
            }

            var entity = new BmQuyenXl
            {
                Id = id,
                IdTaiKhoan = dto.IdTaiKhoan,
                MaBm = dto.MaBm,
                MaKhuVuc = dto.MaKhuVuc,
                QuyenChucNang = dto.QuyenChucNang
            };

            await _repo.UpdateAsync(entity);
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            if (!await _repo.ExistsAsync(id)) return false;
            await _repo.DeleteAsync(id);
            return true;
        }
    }
}

using dataproduct.api.DTOs;
using dataproduct.api.Models;
using dataproduct.api.Repositories;

namespace dataproduct.api.Services
{
    public class BmQuyenXlService
    {
        private readonly IBmQuyenXlRepository _repo;

        public BmQuyenXlService(IBmQuyenXlRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<BmQuyenXlDto>> GetAllAsync(int? idTaiKhoan, string? maBm, string? maKhuVuc)
        {
            var data = await _repo.GetAllAsync(idTaiKhoan, maBm, maKhuVuc);
            return data.Select(x => new BmQuyenXlDto
            {
                Id = x.Id,
                IdTaiKhoan = x.IdTaiKhoan,
                MaBm = x.MaBm,
                MaKhuVuc = x.MaKhuVuc
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
                MaKhuVuc = entity.MaKhuVuc
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
                MaKhuVuc = x.MaKhuVuc
            });
        }

        public async Task<BmQuyenXl> CreateAsync(BmQuyenXlCreateDto dto)
        {
            // Kiểm tra trùng lặp
            var isDuplicate = await _repo.CheckDuplicateAsync(dto.IdTaiKhoan, dto.MaBm, dto.MaKhuVuc);
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
                MaKhuVuc = dto.MaKhuVuc
            };

            await _repo.AddAsync(entity);
            return entity;
        }

        public async Task<bool> UpdateAsync(int id, BmQuyenXlUpdateDto dto)
        {
            if (!await _repo.ExistsAsync(id)) return false;

            // Kiểm tra trùng lặp (loại trừ bản ghi hiện tại)
            var isDuplicate = await _repo.CheckDuplicateAsync(dto.IdTaiKhoan, dto.MaBm, dto.MaKhuVuc, id);
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
                MaKhuVuc = dto.MaKhuVuc
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

using dataproduct.api.DTOs;
using dataproduct.api.Models;
using dataproduct.api.Repositories;

namespace dataproduct.api.Services
{
    public class BmPheDuyetService
    {
        private readonly IBMPheDuyetRepository _repo;

        public BmPheDuyetService(IBMPheDuyetRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<BM_PheDuyetDto>> GetAllAsync()
        {
            var data = await _repo.GetAllAsync();
            return data.Select(x => new BM_PheDuyetDto
            {
                Id = x.Id,
                PhieuId = x.PhieuId,
                CapDuyet = x.CapDuyet,
                NguoiDuyetId = x.NguoiDuyetId,
                NgayDuyet = x.NgayDuyet,
                GhiChu = x.GhiChu,
                TinhTrang = x.TinhTrang
            });
        }

        public async Task<BM_PheDuyetDto?> GetByIdAsync(int id)
        {
            var x = await _repo.GetByIdAsync(id);
            if (x == null) return null;
            return new BM_PheDuyetDto
            {
                Id = x.Id,
                PhieuId = x.PhieuId,
                CapDuyet = x.CapDuyet,
                NguoiDuyetId = x.NguoiDuyetId,
                NgayDuyet = x.NgayDuyet,
                GhiChu = x.GhiChu,
                TinhTrang = x.TinhTrang
            };
        }

        public async Task<BmPheDuyet> CreateAsync(BmPheDuyet model)
        {
            await _repo.AddAsync(model);
            return model;
        }

        public async Task<bool> UpdateAsync(int id, BmPheDuyet model)
        {
            var item = await _repo.GetByIdAsync(id);
            if (item == null) return false;
            model.Id = id;
            await _repo.UpdateAsync(model);
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var item = await _repo.GetByIdAsync(id);
            if (item == null) return false;
            await _repo.DeleteAsync(id);
            return true;
        }
    }
}

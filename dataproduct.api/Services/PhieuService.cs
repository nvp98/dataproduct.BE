using dataproduct.api.DTOs;
using dataproduct.api.Models;
using dataproduct.api.Repositories;

namespace dataproduct.api.Business
{
    public class PhieuService
    {
        private readonly IPhieuRepository _repo;

        public PhieuService(IPhieuRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<PhieuDto>> GetAllAsync()
        {
            var data = await _repo.GetAllAsync();
            return data.Select(p => new PhieuDto
            {
                Idphieu = p.Idphieu,
                MaBm = p.MaBm,
                SoPhieu = p.SoPhieu,
                XuongId = p.XuongId,
                IdphongBan = p.IdphongBan,
                Idkip = p.Idkip,
                Ca = p.Ca,
                Kip = p.Kip,
                NgayTao = p.NgayTao,
                MayDuc = p.MayDuc,
                NguoiTaoId = p.NguoiTaoId,
                TinhTrang = p.TinhTrang,
                DataJson = p.DataJson,
                IsDelete = p.IsDelete,
                IsLock = p.IsLock

            });
        }

        public async Task<PhieuDto?> GetByIdAsync(Guid id)
        {
            var item = await _repo.GetByIdAsync(id);
            if (item == null) return null;

            return new PhieuDto
            {
                Idphieu = item.Idphieu,
                MaBm = item.MaBm,
                SoPhieu = item.SoPhieu,
                XuongId = item.XuongId,
                IdphongBan = item.IdphongBan,
                Idkip = item.Idkip,
                Ca = item.Ca,
                Kip = item.Kip,
                NgayTao = item.NgayTao,
                MayDuc = item.MayDuc,
                NguoiTaoId = item.NguoiTaoId,
                TinhTrang = item.TinhTrang,
                DataJson = item.DataJson,
                IsDelete = item.IsDelete,
                IsLock = item.IsLock
            };
        }

        public async Task<BmPhieu> CreateAsync(BmPhieu bm)
        {
            bm.Idphieu = Guid.NewGuid();
            await _repo.AddAsync(bm);
            return bm;
        }

        public async Task<bool> UpdateAsync(Guid id, BmPhieu bm)
        {
            var exists = await _repo.ExistsAsync(id);
            if (!exists) return false;
            bm.Idphieu = id;
            await _repo.UpdateAsync(bm);
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var exists = await _repo.ExistsAsync(id);
            if (!exists) return false;
            await _repo.DeleteAsync(id);
            return true;
        }
    }
}

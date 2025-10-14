using dataproduct.api.DTOs;
using dataproduct.api.Models;
using dataproduct.api.Repositories;

namespace dataproduct.api.Services
{
    public class BKPhoiThepService
    {
        private readonly IBKPhoiThepRepository _repo;

        public BKPhoiThepService(IBKPhoiThepRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<BK_PhoiThepDto>> GetAllAsync(DateOnly? NgaySX, int? Ca, string? Kip)
        {
            var data = await _repo.GetAllAsync(NgaySX,Ca,Kip);
            return data.Select(x => new BK_PhoiThepDto
            {
                Id = x.Id,
                Ca = x.Ca,
                Kip = x.Kip,
                NgaySx = x.NgaySx,
                KichThuoc = x.KichThuoc,
                ChieuDai = x.ChieuDai,
                Me = x.Me,
                Mac = x.Mac,
                MauThu = x.MauThu,
                MayDuc = x.MayDuc,
                SoThanh = x.SoThanh,
                TongKhoiLuog = x.TongKhoiLuog,
                LoaiId = x.LoaiId,
                LoaiPhoi = x.LoaiPhoi,
                TenLoai = x.TenLoai,
                NgayTaoBk = x.NgayTaoBk
            });
        }

        public async Task<BK_PhoiThepDto?> GetByIdAsync(int id)
        {
            var x = await _repo.GetByIdAsync(id);
            if (x == null) return null;

            return new BK_PhoiThepDto
            {
                Id = x.Id,
                Ca = x.Ca,
                Kip = x.Kip,
                NgaySx = x.NgaySx,
                KichThuoc = x.KichThuoc,
                ChieuDai = x.ChieuDai,
                Me = x.Me,
                Mac = x.Mac,
                MauThu = x.MauThu,
                MayDuc = x.MayDuc,
                SoThanh = x.SoThanh,
                TongKhoiLuog = x.TongKhoiLuog,
                LoaiId = x.LoaiId,
                LoaiPhoi = x.LoaiPhoi,
                TenLoai = x.TenLoai,
                NgayTaoBk = x.NgayTaoBk
            };
        }

        public async Task<BkPhoiThep> CreateAsync(BkPhoiThep entity)
        {
            await _repo.AddAsync(entity);
            return entity;
        }

        public async Task<bool> UpdateAsync(int id, BkPhoiThep entity)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return false;
            entity.Id = id;
            await _repo.UpdateAsync(entity);
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return false;
            await _repo.DeleteAsync(id);
            return true;
        }
    }
}

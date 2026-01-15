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

        public async Task<IEnumerable<BK_PhoiThepDto>> GetAllAsync(DateOnly? NgaySX, int? Ca, string? Kip, int? LoaiPhoi, int? MayDuc)
        {
            var data = await _repo.GetAllAsync(NgaySX, Ca, Kip, LoaiPhoi, MayDuc);
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
                NgayTaoBk = x.NgayTaoBk,
                TenPhanLoai = x.TenPhanLoai,
                VanChuyen = x.VanChuyen,
                GhiChu = x.GhiChu,
                IsChuyen = x.IsChuyen,
                IsNm = x.IsNm,
                StDaChuyen = x.StDaChuyen,
                PhanLoaiPhoi = x.PhanLoaiPhoi,
                LoaiChatLuong = x.LoaiChatLuong,
                DonTrongPhoi = x.DonTrongPhoi
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
                NgayTaoBk = x.NgayTaoBk,
                TenPhanLoai = x.TenPhanLoai,
                VanChuyen = x.VanChuyen,
                GhiChu = x.GhiChu,
                IsChuyen = x.IsChuyen,
                IsNm = x.IsNm,
                StDaChuyen = x.StDaChuyen,
                PhanLoaiPhoi = x.PhanLoaiPhoi,
                LoaiChatLuong = x.LoaiChatLuong,
                DonTrongPhoi = x.DonTrongPhoi
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

        public Task<bool> UpdateStDaChuyenAsync(int id, int stDaChuyen)
        {
            return _repo.UpdateStDaChuyenAsync(id, stDaChuyen);
        }

        public Task<int> UpdateStDaChuyenRangeAsync(List<BKPhoiThepStUpdate> items)
        {
            return _repo.UpdateStDaChuyenRangeAsync(items);
        }

        public Task<int?> RevokeStDaChuyenAsync(int id, int soThuHoi)
        {
            return _repo.RevokeStDaChuyenAsync(id, soThuHoi);
        }

        public Task<int> RevokeStDaChuyenRangeAsync(List<BKPhoiThepStRevoke> items)
        {
            return _repo.RevokeStDaChuyenRangeAsync(items);
        }
    }
}

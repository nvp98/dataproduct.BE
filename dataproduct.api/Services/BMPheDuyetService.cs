using dataproduct.api.DTOs;
using dataproduct.api.Models;
using dataproduct.api.Models.MasterData;
using dataproduct.api.Repositories;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace dataproduct.api.Services
{
    public class BmPheDuyetService
    {
        private readonly IBMPheDuyetRepository _repo;
        private readonly ProductDataMasterDbContext _contextMaster;

        public BmPheDuyetService(IBMPheDuyetRepository repo, ProductDataMasterDbContext Mastercontext)
        {
            _repo = repo;
            _contextMaster = Mastercontext;
        }

        public async Task<IEnumerable<BM_PheDuyetDto>> GetAllAsync(int? NguoiDuyetID)
        {
            var data = await _repo.GetAllAsync(NguoiDuyetID);
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

        public async Task<IEnumerable<BM_PheDuyetDto>?> GetByIdAsync(Guid id)
        {
            var x = await _repo.GetByIdAsync(id);
            if (x == null) return null;

            var result =(from pd in x
                                join tk in _contextMaster.Tbl_TaiKhoan
                                    on pd.NguoiDuyetId equals tk.ID_TaiKhoan into joined
                                from tk in joined.DefaultIfEmpty()
                                //where pd.NguoiDuyetId == id
                                select new BM_PheDuyetDto
                                {
                                    Id = pd.Id,
                                    PhieuId = pd.PhieuId,
                                    CapDuyet = pd.CapDuyet,
                                    NguoiDuyetId = pd.NguoiDuyetId,
                                    TenNguoiDuyet = tk != null ? tk.HoVaTen : "",
                                    NgayDuyet = pd.NgayDuyet,
                                    GhiChu = pd.GhiChu,
                                    TinhTrang = pd.TinhTrang
                                });
            return result;
            //return x.Select(x => new BM_PheDuyetDto
            //{
            //    Id = x.Id,
            //    PhieuId = x.PhieuId,
            //    CapDuyet = x.CapDuyet,
            //    NguoiDuyetId = x.NguoiDuyetId,
            //    TenNguoiDuyet = x.,
            //    NgayDuyet = x.NgayDuyet,
            //    GhiChu = x.GhiChu,
            //    TinhTrang = x.TinhTrang,

            //});        
        }

        public async Task<BmPheDuyet> CreateAsync(BmPheDuyet model)
        {
            await _repo.AddAsync(model);
            return model;
        }

        public async Task<bool> UpdateAsync(Guid id, BmPheDuyet model)
        {
            var item = await _repo.GetByIdAsync(id);
            if (item == null) return false;
            //model.Id = id;
            await _repo.UpdateAsync(model);
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var item = await _repo.GetByIdAsync(id);
            if (item == null) return false;
            //await _repo.DeleteAsync(id);
            return true;
        }
    }
}

using dataproduct.api.DTOs;
using dataproduct.api.Repositories;

namespace dataproduct.api.Services
{
    public class Hrc2SlabWorkflowService
    {
        private readonly IHrc2SlabWorkflowRepository _repo;

        public Hrc2SlabWorkflowService(IHrc2SlabWorkflowRepository repo)
        {
            _repo = repo;
        }

        public Task<(IEnumerable<BkHrc2SlabItem> Data, int TotalCount)> SearchAsync(HrcSlabSearchRequest request)
            => _repo.SearchWithTrangThaiAsync(request);

        public Task<IEnumerable<SlabTongHopItem>> GetTongHopAsync(
            DateOnly? tuNgay, DateOnly? denNgay, string? ca, string? kip)
            => _repo.GetTongHopAsync(tuNgay, denNgay, ca, kip);

        public Task<IEnumerable<PhieuBBSLItem>> GetPhieuBBSLAsync(string? kip, int? ca)
            => _repo.GetPhieuBBSLAsync(kip, ca);

        public Task<IEnumerable<SlabTongHopItem>> GetRuotPhieuAsync(Guid idPhieu)
            => _repo.GetRuotPhieuAsync(idPhieu);

        public Task<IEnumerable<BkHrc2SlabItem>> GetSlabsByPhieuAsync(Guid idPhieu)
            => _repo.GetSlabsByPhieuAsync(idPhieu);

        public Task ChuyenBBSLAsync(ChuyenBBSLRequest req)
            => _repo.ChuyenBBSLAsync(req.IdSlabs, req.IdPhieu, req.NguoiThucHien);

        public Task ThuHoiAsync(ThuHoiRequest req)
            => _repo.ThuHoiAsync(req.IdSlabs, req.NguoiThucHien);

        public Task XacNhanAsync(XacNhanRequest req)
            => _repo.XacNhanAsync(req.IdSlabs, req.LoaiXacNhan, req.NguoiThucHien);

        public Task HuyXacNhanAsync(XacNhanRequest req)
            => _repo.HuyXacNhanAsync(req.IdSlabs, req.LoaiXacNhan, req.NguoiThucHien);

        public Task ChotPhieuAsync(ChotPhieuRequest req)
            => _repo.ChotPhieuAsync(req.IdPhieu, req.NguoiThucHien);

        public Task HuyChotPhieuAsync(ChotPhieuRequest req)
            => _repo.HuyChotPhieuAsync(req.IdPhieu, req.NguoiThucHien);

        public Task<SyncStatusItem> SyncAsync(DateOnly? ngayBatDau, DateOnly? ngayKetThuc)
            => _repo.SyncAsync(ngayBatDau, ngayKetThuc);

        public Task<SyncStatusItem?> GetSyncStatusAsync()
            => _repo.GetSyncStatusAsync();
    }
}

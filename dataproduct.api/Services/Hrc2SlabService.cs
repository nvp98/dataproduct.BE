using dataproduct.api.DTOs;
using dataproduct.api.Models;
using dataproduct.api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Services
{
    public class Hrc2SlabService
    {
        private readonly IHrc2SlabRepository _repo;
        private readonly ProductFormContext _context;

        public Hrc2SlabService(IHrc2SlabRepository repo, ProductFormContext context)
        {
            _repo = repo;
            _context = context;
        }

        public Task<(IEnumerable<Hrc2SlabItem> Data, int TotalCount)> SearchAsync(Hrc2SlabSearchRequest req)
            => _repo.SearchAsync(req);

        public Task<IEnumerable<Hrc2SlabTongHopItem>> GetTongHopAsync(
            string? tuNgay, string? denNgay, string? ca, string? kip)
            => _repo.GetTongHopAsync(tuNgay, denNgay, ca, kip);

        public Task<IEnumerable<Hrc2PhieuBBSLItem>> GetPhieuBBSLAsync(string? kip, int? ca)
            => _repo.GetPhieuBBSLAsync(kip, ca);

        public Task<IEnumerable<Hrc2SlabTongHopItem>> GetRuotPhieuAsync(Guid idPhieu)
            => _repo.GetRuotPhieuAsync(idPhieu);

        public Task<IEnumerable<Hrc2SlabItem>> GetSlabsByPhieuAsync(Guid idPhieu)
            => _repo.GetSlabsByPhieuAsync(idPhieu);

        public Task XacNhanAsync(Hrc2XacNhanRequest req)
            => _repo.XacNhanAsync(req.IdSlabs, req.LoaiXacNhan, req.NguoiThucHien);

        public Task HuyXacNhanAsync(Hrc2XacNhanRequest req)
            => _repo.HuyXacNhanAsync(req.IdSlabs, req.LoaiXacNhan, req.NguoiThucHien);

        public Task ChotPhieuAsync(Hrc2ChotPhieuRequest req)
            => _repo.ChotPhieuAsync(req.IdPhieu, req.NguoiThucHien);

        public Task HuyChotPhieuAsync(Hrc2ChotPhieuRequest req)
            => _repo.HuyChotPhieuAsync(req.IdPhieu, req.NguoiThucHien);

        public async Task<int> ChuyenBbslAsync(Hrc2ChuyenBbslRequest req)
            => await _repo.ChuyenBbslAsync(req.IdSlabs, req.IdPhieu, req.NguoiThucHien);

        public async Task<int> ThuHoiAsync(Hrc2ChuyenBbslRequest req)
            => await _repo.ThuHoiAsync(req.IdSlabs, req.NguoiThucHien);

        // Sync status từ BK_SyncHRC2SlabControl
        public async Task<object?> GetSyncStatusAsync()
        {
            var latest = await _context.BkSyncHrc2SlabControls
                .AsNoTracking()
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync();

            if (latest == null) return null;

            return new
            {
                id          = latest.Id,
                trangThai   = latest.TrangThai,
                batDauLuc   = latest.BatDauLuc,
                ketThucLuc  = latest.KetThucLuc,
                ghiChu      = latest.GhiChu,
            };
        }
        public Task<SyncStatusItem> SyncAsync(DateOnly? ngayBatDau, DateOnly? ngayKetThuc)
            => _repo.SyncAsync(ngayBatDau, ngayKetThuc);
    }
}

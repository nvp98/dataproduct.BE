using dataproduct.api.Models;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public class HRC1_BCSXRepository : IHRC1_BCSXRepository
    {
        private readonly ProductFormContext _context;

        public HRC1_BCSXRepository(ProductFormContext context)
        {
            _context = context;
        }

        public async Task<BmPhieu?> GetPhieuByIdAsync(Guid idPhieu)
            => await _context.BmPhieus.FindAsync(idPhieu);

        public async Task<Dictionary<byte, decimal>> GetDongLucTheoCaAsync(DateOnly ngay, string maHeader)
            => (await _context.DuLieu_DongLucs.AsNoTracking()
                    .Where(x => x.Ngay == ngay && x.MaHeader == maHeader && x.GiaTri != null)
                    .GroupBy(x => x.CaSX)
                    .Select(g => new { Ca = g.Key, Tong = g.Sum(x => x.GiaTri!.Value) })
                    .ToListAsync())
               .ToDictionary(x => x.Ca, x => x.Tong);

        // Thứ tự hiển thị theo biên bản: kíp ca 1 trước ca 2, trong kíp theo ID (= thứ tự nhập)
        public async Task<HRC1_BCSXChiTiet> GetByPhieuAsync(Guid idPhieu) => new()
        {
            ThepLong = await _context.HRC1_BC_ThepLongs.AsNoTracking()
                .Where(x => x.PhieuID == idPhieu && !x.IsDelete)
                .OrderBy(x => x.CaSX).ThenBy(x => x.ID).ToListAsync(),
            Dien = await _context.HRC1_BC_Diens.AsNoTracking()
                .Where(x => x.PhieuID == idPhieu && !x.IsDelete)
                .OrderBy(x => x.CaSX).ThenBy(x => x.ID).ToListAsync(),
            NguyenVatLieu = await _context.HRC1_BC_NguyenVatLieus.AsNoTracking()
                .Where(x => x.PhieuID == idPhieu && !x.IsDelete)
                .OrderBy(x => x.CaSX).ThenBy(x => x.ID).ToListAsync(),
            ThanhPham = await _context.HRC1_BC_ThanhPhams.AsNoTracking()
                .Where(x => x.PhieuID == idPhieu && !x.IsDelete)
                .OrderBy(x => x.CaSX).ThenBy(x => x.ID).ToListAsync(),
        };

        public async Task<(HRC1_BCSXChiTiet Data, Dictionary<Guid, string?> SoPhieu)> GetTheoKhoangNgayAsync(
            string maBM, DateOnly tuNgay, DateOnly denNgay)
        {
            var phieus = await _context.BmPhieus.AsNoTracking()
                .Where(p => p.MaBm == maBM && (p.IsDelete == null || p.IsDelete == 0)
                            && p.NgaySX >= tuNgay && p.NgaySX <= denNgay)
                .Select(p => new { p.Idphieu, p.SoPhieu })
                .ToListAsync();
            var ids = phieus.Select(p => p.Idphieu).ToList();

            var data = new HRC1_BCSXChiTiet
            {
                ThepLong = await _context.HRC1_BC_ThepLongs.AsNoTracking()
                    .Where(x => ids.Contains(x.PhieuID) && !x.IsDelete)
                    .OrderBy(x => x.NgaySX).ThenBy(x => x.CaSX).ThenBy(x => x.ID).ToListAsync(),
                Dien = await _context.HRC1_BC_Diens.AsNoTracking()
                    .Where(x => ids.Contains(x.PhieuID) && !x.IsDelete)
                    .OrderBy(x => x.NgaySX).ThenBy(x => x.CaSX).ThenBy(x => x.ID).ToListAsync(),
                NguyenVatLieu = await _context.HRC1_BC_NguyenVatLieus.AsNoTracking()
                    .Where(x => ids.Contains(x.PhieuID) && !x.IsDelete)
                    .OrderBy(x => x.NgaySX).ThenBy(x => x.CaSX).ThenBy(x => x.ID).ToListAsync(),
                ThanhPham = await _context.HRC1_BC_ThanhPhams.AsNoTracking()
                    .Where(x => ids.Contains(x.PhieuID) && !x.IsDelete)
                    .OrderBy(x => x.NgaySX).ThenBy(x => x.CaSX).ThenBy(x => x.ID).ToListAsync(),
            };
            return (data, phieus.ToDictionary(p => p.Idphieu, p => p.SoPhieu));
        }

        public async Task ReplaceByPhieuAsync(Guid idPhieu, HRC1_BCSXChiTiet data)
        {
            // PhieuService có thể đã mở transaction → chỉ tự mở khi chưa có
            var ownsTransaction = _context.Database.CurrentTransaction == null;
            var transaction = ownsTransaction ? await _context.Database.BeginTransactionAsync() : null;
            try
            {
                await _context.HRC1_BC_ThepLongs.Where(x => x.PhieuID == idPhieu).ExecuteDeleteAsync();
                await _context.HRC1_BC_Diens.Where(x => x.PhieuID == idPhieu).ExecuteDeleteAsync();
                await _context.HRC1_BC_NguyenVatLieus.Where(x => x.PhieuID == idPhieu).ExecuteDeleteAsync();
                await _context.HRC1_BC_ThanhPhams.Where(x => x.PhieuID == idPhieu).ExecuteDeleteAsync();

                foreach (var x in data.ThepLong) { x.ID = 0; x.PhieuID = idPhieu; }
                foreach (var x in data.Dien) { x.ID = 0; x.PhieuID = idPhieu; }
                foreach (var x in data.NguyenVatLieu) { x.ID = 0; x.PhieuID = idPhieu; }
                foreach (var x in data.ThanhPham) { x.ID = 0; x.PhieuID = idPhieu; }

                await _context.HRC1_BC_ThepLongs.AddRangeAsync(data.ThepLong);
                await _context.HRC1_BC_Diens.AddRangeAsync(data.Dien);
                await _context.HRC1_BC_NguyenVatLieus.AddRangeAsync(data.NguyenVatLieu);
                await _context.HRC1_BC_ThanhPhams.AddRangeAsync(data.ThanhPham);
                await _context.SaveChangesAsync();

                // Chỉ tách các dòng vừa ghi (không đụng entity khác PhieuService đang theo dõi)
                foreach (var e in data.ThepLong.Cast<object>().Concat(data.Dien).Concat(data.NguyenVatLieu).Concat(data.ThanhPham))
                    _context.Entry(e).State = EntityState.Detached;

                if (transaction != null) await transaction.CommitAsync();
            }
            catch
            {
                if (transaction != null) await transaction.RollbackAsync();
                throw;
            }
            finally
            {
                if (transaction != null) await transaction.DisposeAsync();
            }
        }
    }
}

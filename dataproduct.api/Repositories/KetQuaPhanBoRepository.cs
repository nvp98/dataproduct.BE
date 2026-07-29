using dataproduct.api.Models;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public class KetQuaPhanBoRepository : IKetQuaPhanBoRepository
    {
        private readonly ProductFormContext _context;

        public KetQuaPhanBoRepository(ProductFormContext context)
        {
            _context = context;
        }

        public async Task<bool> IsNgayDaChotAsync(DateTime ngay, byte loaiPhanBo)
        {
            return await _context.LG_PB_KetQuaPhanBo
                .AnyAsync(x => x.Ngay == ngay.Date && x.LoaiPhanBo == loaiPhanBo && x.TrangThai == 1);
        }

        public async Task ReplaceNhapAsync(DateTime ngay, byte loaiPhanBo, List<LG_PB_KetQuaPhanBo> entities)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.LG_PB_KetQuaPhanBo
                    .Where(x => x.Ngay == ngay.Date && x.LoaiPhanBo == loaiPhanBo && x.TrangThai == 0)
                    .ExecuteDeleteAsync();

                if (entities.Count > 0)
                {
                    await _context.LG_PB_KetQuaPhanBo.AddRangeAsync(entities);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<LG_PB_KetQuaPhanBo>> GetByNgayAsync(DateTime ngay, byte? loaiPhanBo, int? idLoCao, byte? ca = null)
        {
            return await _context.LG_PB_KetQuaPhanBo
                .Where(x => x.Ngay == ngay.Date
                    && (loaiPhanBo == null || x.LoaiPhanBo == loaiPhanBo)
                    && (idLoCao == null || x.IDLoCao == idLoCao)
                    && (ca == null || x.Ca == ca))
                .OrderBy(x => x.IDLoCao).ThenBy(x => x.IDNhomPhanBo).ThenBy(x => x.IDNVL)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<int> ChotAsync(DateTime ngay, byte loaiPhanBo, int idNguoiXacNhan)
        {
            var rows = await _context.LG_PB_KetQuaPhanBo
                .Where(x => x.Ngay == ngay.Date && x.LoaiPhanBo == loaiPhanBo && x.TrangThai == 0)
                .ToListAsync();

            foreach (var row in rows)
            {
                row.TrangThai = 1;
                row.IDNguoiXacNhan = idNguoiXacNhan;
                row.NgayXacNhan = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return rows.Count;
        }

        public async Task<List<LG_PB_KetQuaPhanBo>> GetBaoCaoAsync(DateTime tuNgay, DateTime denNgay, int? idLoCao, byte? loaiPhanBo)
        {
            return await _context.LG_PB_KetQuaPhanBo
                .Where(x => x.Ngay >= tuNgay.Date && x.Ngay <= denNgay.Date && x.TrangThai == 1
                    && (idLoCao == null || x.IDLoCao == idLoCao)
                    && (loaiPhanBo == null || x.LoaiPhanBo == loaiPhanBo))
                .OrderBy(x => x.Ngay).ThenBy(x => x.IDLoCao).ThenBy(x => x.IDNVL)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<int> UpdateMaCongDoanChiPhiAsync(DateTime ngay, int idNvl, string? maCongDoanChiPhi)
        {
            return await _context.LG_PB_KetQuaPhanBo
                .Where(x => x.Ngay == ngay.Date && x.IDNVL == idNvl && x.TrangThai == 0)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.MaCongDoanChiPhi, maCongDoanChiPhi));
        }

    }
}

using dataproduct.api.Models;
using Microsoft.EntityFrameworkCore;
using System.Runtime.ConstrainedExecution;

namespace dataproduct.api.Repositories
{
    public class BMPheDuyetRepository : IBMPheDuyetRepository
    {
        private readonly ProductFormContext _context;

        public BMPheDuyetRepository(ProductFormContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<BmPheDuyet>> GetAllAsync(int? NguoiDuyetID, int? isCheckDuyet)
        {
            var query = _context.BmPheDuyets.Where(x => x.NguoiDuyetId == NguoiDuyetID).AsQueryable(); // danh sách duyệt --> tkhoan
            if (isCheckDuyet == 1)
            {
                query = query.Where(x => !_context.BmPheDuyets.Any(prev =>
                                prev.PhieuId == x.PhieuId &&
                                prev.CapDuyet < x.CapDuyet &&
                                prev.TinhTrang != 1));
            }
            //var res = await _context.BmPheDuyets.Where(x => x.NguoiDuyetId == NguoiDuyetID).ToListAsync();
            return await query.ToListAsync();
        }

        public async Task<BmPheDuyet> GetByIdAsync(int? id)
        {
            return await _context.BmPheDuyets.Where(x => x.Id == id).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<BmPheDuyet>> GetByIdPhieuAsync(Guid id)
        {
            return await _context.BmPheDuyets.Where(x => x.PhieuId == id).ToListAsync();
        }

        public async Task AddAsync(BmPheDuyet entity)
        {
            _context.BmPheDuyets.Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task AddListAsync(List<BmPheDuyet> entity, Guid id)
        {
            // xóa dữ liệu phê duyệt cũ
            var listDuyet = _context.BmPheDuyets.Where(p => p.PhieuId == id).ToList();
            _context.BmPheDuyets.RemoveRange(listDuyet);
            // Lưu danh sách phê duyệt mới
            foreach (var item in entity)
            {
                _context.BmPheDuyets.Add(item);
            }
            _context.SaveChanges();
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(BmPheDuyet entity)
        {
            // 1️ Lấy entity thật từ DB
            var existing = await _context.BmPheDuyets.FindAsync(entity.Id);
            if (existing == null)
                throw new Exception($"Không tìm thấy bản ghi có ID = {entity.Id}");

            // 2️ Cập nhật giá trị
            _context.Entry(existing).CurrentValues.SetValues(entity);

            // 3️ Lưu
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var item = await _context.BmPheDuyets.FindAsync(id);
            if (item != null)
            {
                _context.BmPheDuyets.Remove(item);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.BmPheDuyets.AnyAsync(e => e.Id == id);
        }

    }
}

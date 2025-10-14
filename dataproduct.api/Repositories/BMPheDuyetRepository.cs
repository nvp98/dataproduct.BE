using dataproduct.api.Models;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public class BMPheDuyetRepository : IBMPheDuyetRepository
    {
        private readonly ProductFormContext _context;

        public BMPheDuyetRepository(ProductFormContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<BmPheDuyet>> GetAllAsync(int? NguoiDuyetID)
        {
            return await _context.BmPheDuyets.Where(x=>x.NguoiDuyetId == NguoiDuyetID).ToListAsync();
        }

        public async Task<IEnumerable<BmPheDuyet>> GetByIdAsync(Guid id)
        {
            return await _context.BmPheDuyets.Where(x => x.PhieuId == id).ToListAsync();
        }

        public async Task AddAsync(BmPheDuyet entity)
        {
            _context.BmPheDuyets.Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task AddListAsync(List<BmPheDuyet> entity,Guid id)
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
            _context.BmPheDuyets.Update(entity);
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

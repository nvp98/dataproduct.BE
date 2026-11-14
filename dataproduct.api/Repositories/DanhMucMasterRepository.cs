using dataproduct.api.Models.MasterData;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    /// <summary>Danh mục dùng chung từ DB master PRODUCTDATA (phòng ban / nhà máy, xưởng) — chỉ đọc.</summary>
    public class DanhMucMasterRepository : IDanhMucMasterRepository
    {
        private readonly ProductDataMasterDbContext _dbcontext;

        public DanhMucMasterRepository(ProductDataMasterDbContext dbcontext)
        {
            _dbcontext = dbcontext;
        }

        public Task<List<PhongBan>> GetPhongBanAsync()
            => _dbcontext.Tbl_PhongBan.AsNoTracking().OrderBy(x => x.TenNgan).ToListAsync();

        public Task<List<Tbl_Xuong>> GetXuongAsync()
            => _dbcontext.Tbl_Xuong.AsNoTracking().OrderBy(x => x.TenXuong).ToListAsync();
    }
}

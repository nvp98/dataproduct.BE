using dataproduct.api.DTOs;
using dataproduct.api.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Services
{
    public class HRC1_NMSyncService
    {
        private readonly ProductFormContext _context;

        public HRC1_NMSyncService(ProductFormContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gọi SP_HRC1_BOF_Sync_Full để đồng bộ mẻ thổi + phụ liệu từ NM cho đúng 1 tổ hợp Ngày+Ca+Lò.
        /// Khác HRC2 (sync toàn bộ rồi lọc), SP này đã nhận tham số nên chỉ sync đúng phạm vi cần.
        /// </summary>
        public async Task SyncHRC1FromNMAsync(SyncFromNM_HRC1_Request request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.SP_HRC1_BOF_Sync_Full @NgaySanXuat={0}, @Ca={1}, @LoThoi={2}",
                DateOnly.FromDateTime(request.NgaySX), request.Ca, request.Scope);
        }
    }
}

using dataproduct.api.Models;
using dataproduct.api.Services;
using System.Text.Json;

namespace dataproduct.api.Services.Initializers
{
    public class HRC2BBGNThepLongInitializer : IPhieuJsonInitializer
    {
        private readonly BBGN_ThepLongService _bbgnThepLongService;

        public HRC2BBGNThepLongInitializer(BBGN_ThepLongService bbgnThepLongService)
        {
            _bbgnThepLongService = bbgnThepLongService;
        }

        public bool CanHandle(string? maBm)
        {
            if (string.IsNullOrWhiteSpace(maBm))
                return false;

            // FE đang gửi maBm theo config: HRC1_BBGN_ThepLong / HRC2_BBGN_ThepLong
            return maBm.Equals("HRC1_BBGN_ThepLong", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("HRC2_BBGN_ThepLong", StringComparison.OrdinalIgnoreCase)
                // giữ backward compatibility nếu dữ liệu cũ dùng mã rút gọn
                || maBm.Equals("BBGN_ThepLong", StringComparison.OrdinalIgnoreCase);
        }

        public async Task InitializeAsync(BmPhieu phieu)
        {
            if (string.IsNullOrWhiteSpace(phieu.DataJson))
                return;

            using var doc = JsonDocument.Parse(phieu.DataJson);
            await _bbgnThepLongService.SaveHRC2BBGNThepLongAsync(doc.RootElement, phieu.Idphieu);
        }
    }
}


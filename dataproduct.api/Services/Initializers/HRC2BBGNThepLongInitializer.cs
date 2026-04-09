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

            return maBm.Equals("BBGN_ThepLong", StringComparison.OrdinalIgnoreCase);
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


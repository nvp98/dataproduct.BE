using dataproduct.api.Models;

namespace dataproduct.api.Services.Initializers
{
    public class LGNLChiTietInitializer : IPhieuJsonInitializer
    {
        private readonly LGNLService _service;

        public LGNLChiTietInitializer(LGNLService service)
        {
            _service = service;
        }

        public bool CanHandle(string? maBm)
        {
            if (string.IsNullOrWhiteSpace(maBm))
                return false;

            return maBm.Equals("NMLG_BM_NapLieuLoCao", StringComparison.OrdinalIgnoreCase);
        }

        public async Task InitializeAsync(BmPhieu phieu)
        {
            await _service.InsertFromPhieuJsonAsync(phieu);
        }
    }
}

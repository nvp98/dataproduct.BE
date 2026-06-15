using dataproduct.api.Models;

namespace dataproduct.api.Services.Initializers
{
    public class LGPTLCInitializer : IPhieuJsonInitializer
    {
        private readonly LGPTLCService _service;

        public LGPTLCInitializer(LGPTLCService service)
        {
            _service = service;
        }

        public bool CanHandle(string? maBm)
        {
            if (string.IsNullOrWhiteSpace(maBm)) return false;
            return maBm.Equals("NMLG_NK_VHPTLC", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<List<string>> InitializeAsync(BmPhieu phieu)
        {
            await _service.InsertFromPhieuJsonAsync(phieu);
            return new List<string>();
        }
    }
}

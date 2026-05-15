using dataproduct.api.Models;

namespace dataproduct.api.Services.Initializers
{
    public class NLBTDBenPheJsonInitializer : IPhieuJsonInitializer
    {
        private readonly NLBTDBenPheService _service;

        public NLBTDBenPheJsonInitializer(NLBTDBenPheService service)
        {
            _service = service;
        }

        public bool CanHandle(string? maBm)
        {
            if (string.IsNullOrWhiteSpace(maBm))
                return false;

            return maBm.Equals("NL_BB_TheoDoiBenPhe", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("NL.BB.TheoDoiBenPhe", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BB_BenPhe", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<List<string>> InitializeAsync(BmPhieu phieu)
        {
            await _service.InsertNLBTDBenPheFromPhieuJsonAsync(phieu);
            return new List<string>();
        }
    }
}

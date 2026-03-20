using dataproduct.api.Models;
using dataproduct.api.Repositories;

namespace dataproduct.api.Services.Initializers
{
    public class HRC2StdNxtJsonInitializer : IPhieuJsonInitializer
    {
        private readonly ISTD_NXT_HRC2Repository _stdNxtHrc2Repo;

        public HRC2StdNxtJsonInitializer(ISTD_NXT_HRC2Repository stdNxtHrc2Repo)
        {
            _stdNxtHrc2Repo = stdNxtHrc2Repo;
        }

        public bool CanHandle(string? maBm)
        {
            if (string.IsNullOrWhiteSpace(maBm))
                return false;

            return maBm.Equals("HRC2_STD_NXT", StringComparison.OrdinalIgnoreCase);
        }

        public async Task InitializeAsync(BmPhieu phieu)
        {
            await _stdNxtHrc2Repo.InitializeHRC2_STD_NXTAsync(phieu);
        }
    }
}


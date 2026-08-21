using dataproduct.api.Models;
using dataproduct.api.Repositories;

namespace dataproduct.api.Services.Initializers
{
    public class HRC1StdNxtJsonInitializer : IPhieuJsonInitializer
    {
        private readonly ISTD_NXT_HRC1Repository _stdNxtHrc1Repo;

        public HRC1StdNxtJsonInitializer(ISTD_NXT_HRC1Repository stdNxtHrc1Repo)
        {
            _stdNxtHrc1Repo = stdNxtHrc1Repo;
        }

        public bool CanHandle(string? maBm)
        {
            if (string.IsNullOrWhiteSpace(maBm))
                return false;

            return maBm.Equals("HRC1_STD_NXT", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<List<string>> InitializeAsync(BmPhieu phieu)
        {
            await _stdNxtHrc1Repo.InitializeHRC1_STD_NXTAsync(phieu);
            return new List<string>();
        }
    }
}

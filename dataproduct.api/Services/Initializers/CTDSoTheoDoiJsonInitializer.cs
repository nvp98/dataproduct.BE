using dataproduct.api.Models;

namespace dataproduct.api.Services.Initializers
{
    public class CTDSoTheoDoiJsonInitializer : IPhieuJsonInitializer
    {
        private readonly CTDSoTheoDoiService _service;

        public CTDSoTheoDoiJsonInitializer(CTDSoTheoDoiService service)
        {
            _service = service;
        }

        public bool CanHandle(string? maBm)
        {
            if (string.IsNullOrWhiteSpace(maBm))
                return false;

            return maBm.Equals("CTD_STD_Sanxuat", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("CTD_SoTheoDoi", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM.09-QT.05.13", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM.09/QT.05.13", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM09-QT.05.13", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM09/QT.05.13", StringComparison.OrdinalIgnoreCase);
        }

        public async Task InitializeAsync(BmPhieu phieu)
        {
            await _service.InsertFromPhieuJsonAsync(phieu);
        }
    }
}

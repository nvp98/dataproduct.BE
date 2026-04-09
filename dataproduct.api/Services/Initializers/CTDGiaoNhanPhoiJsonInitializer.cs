using dataproduct.api.Models;

namespace dataproduct.api.Services.Initializers
{
    public class CTDGiaoNhanPhoiJsonInitializer : IPhieuJsonInitializer
    {
        private readonly CTDGiaoNhanPhoiService _service;

        public CTDGiaoNhanPhoiJsonInitializer(CTDGiaoNhanPhoiService service)
        {
            _service = service;
        }

        public bool CanHandle(string? maBm)
        {
            if (string.IsNullOrWhiteSpace(maBm))
                return false;

            return maBm.Equals("CTD_BB_GNP", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("CTD_BB_GiaoNhanPhoi", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM.05/QT.05.13", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM.05-QT.05.13", StringComparison.OrdinalIgnoreCase);
        }

        public async Task InitializeAsync(BmPhieu phieu)
        {
            await _service.InsertFromPhieuJsonAsync(phieu);
        }
    }
}

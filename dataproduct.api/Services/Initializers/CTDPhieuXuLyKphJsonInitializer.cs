using dataproduct.api.Models;

namespace dataproduct.api.Services.Initializers
{
    public class CTDPhieuXuLyKphJsonInitializer : IPhieuJsonInitializer
    {
        private readonly CTDPhieuXuLyKphService _service;

        public CTDPhieuXuLyKphJsonInitializer(CTDPhieuXuLyKphService service)
        {
            _service = service;
        }

        public bool CanHandle(string? maBm)
        {
            if (string.IsNullOrWhiteSpace(maBm))
                return false;

            return maBm.Equals("CTD_KPH_Sanxuat", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("CTD_XuLyKPH", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("KPH", StringComparison.OrdinalIgnoreCase)
                || maBm.Contains("KPH", StringComparison.OrdinalIgnoreCase);
        }

        public async Task InitializeAsync(BmPhieu phieu)
        {
            await _service.InsertFromPhieuJsonAsync(phieu);
        }
    }
}

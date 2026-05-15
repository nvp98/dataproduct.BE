using dataproduct.api.Models;

namespace dataproduct.api.Services.Initializers
{
    public class CTDPhoiNguoiJsonInitializer : IPhieuJsonInitializer
    {
        private readonly CTDPhoiNapNguoiService _ctdPhoiNapNguoiService;

        public CTDPhoiNguoiJsonInitializer(CTDPhoiNapNguoiService ctdPhoiNapNguoiService)
        {
            _ctdPhoiNapNguoiService = ctdPhoiNapNguoiService;
        }

        public bool CanHandle(string? maBm)
        {
            if (string.IsNullOrWhiteSpace(maBm))
                return false;

            return maBm.Equals("CTD_BB_Phoinapnguoi", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("CTD_BB_PhoiNguoi", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM.02-QT.05.13", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM.02/QT.05.13", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<List<string>> InitializeAsync(BmPhieu phieu)
        {
            await _ctdPhoiNapNguoiService.InsertFromPhieuJsonAsync(phieu);
            return new List<string>();
        }
    }
}

using dataproduct.api.Models;

namespace dataproduct.api.Services.Initializers
{
    public class BMSanLuongPhoiJsonInitializer : IPhieuJsonInitializer
    {
        private readonly BMDucCTDService _service;

        public BMSanLuongPhoiJsonInitializer(BMDucCTDService service)
        {
            _service = service;
        }

        public bool CanHandle(string? maBm)
        {
            if (string.IsNullOrWhiteSpace(maBm))
                return false;

            return maBm.Equals("HRC1_BB_Sanluongphoi", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("HRC1_BB_Sanluongphoi", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM.11-QT.05.11", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM.11/QT.05.11", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM11-QT.05.11", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM11/QT.05.11", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<List<string>> InitializeAsync(BmPhieu phieu)
        {
            await _service.InsertSanLuongPhoiFromPhieuJsonAsync(phieu);
            return new List<string>();
        }
    }
}

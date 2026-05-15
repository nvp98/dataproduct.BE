using dataproduct.api.Models;

namespace dataproduct.api.Services.Initializers
{
    public class BMPhoiNhapKhoJsonInitializer : IPhieuJsonInitializer
    {
        private readonly BMDucCTDService _service;

        public BMPhoiNhapKhoJsonInitializer(BMDucCTDService service)
        {
            _service = service;
        }

        public bool CanHandle(string? maBm)
        {
            if (string.IsNullOrWhiteSpace(maBm))
                return false;

            return maBm.Equals("HRC1_BB_GiaoNhanPhoiNhapKho", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("HRC1_BB_GiaoNhanPhoiNhapKho", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM12-QT.05.11", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM.12-QT.05.11", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM12/QT.05.11", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM.12/QT.05.11", StringComparison.OrdinalIgnoreCase);
        }

        public Task<List<string>> InitializeAsync(BmPhieu phieu)
        {
            // await _service.InsertPhoiNhapKhoFromPhieuJsonAsync(phieu);
            return Task.FromResult(new List<string>());
        }
    }
}

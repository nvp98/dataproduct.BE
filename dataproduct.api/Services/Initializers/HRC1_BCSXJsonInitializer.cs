using dataproduct.api.Models;

namespace dataproduct.api.Services.Initializers
{
    /// <summary>Sau khi lưu phiếu HRC1_BCSX_DPV: ghi 4 bảng HRC1_BC_* từ DataJson.</summary>
    public class HRC1_BCSXJsonInitializer : IPhieuJsonInitializer
    {
        private readonly HRC1_BCSXService _service;

        public HRC1_BCSXJsonInitializer(HRC1_BCSXService service)
        {
            _service = service;
        }

        public bool CanHandle(string? maBm)
            => string.Equals(maBm, HRC1_BCSXService.MaBM, StringComparison.OrdinalIgnoreCase);

        public Task<List<string>> InitializeAsync(BmPhieu phieu) => _service.InsertFromPhieuJsonAsync(phieu);
    }
}

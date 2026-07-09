using dataproduct.api.Models;
using dataproduct.api.Services;
using System.Text.Json;

namespace dataproduct.api.Services.Initializers
{
    /// <summary>Song song với HRC1ManualFromPhieuFormJsonInitializer (BOF) nhưng cho LF — LF không có
    /// nguồn NM, mọi mẻ/phụ liệu đều nhập tay, xem DLNMHRC1Service vùng "LF manual save pipeline".</summary>
    public class HRC1LFManualFromPhieuFormJsonInitializer : IPhieuJsonInitializer
    {
        private readonly DLNMHRC1Service _dlnmHrc1Service;

        public HRC1LFManualFromPhieuFormJsonInitializer(DLNMHRC1Service dlnmHrc1Service)
        {
            _dlnmHrc1Service = dlnmHrc1Service;
        }

        public bool CanHandle(string? maBm)
        {
            if (string.IsNullOrWhiteSpace(maBm))
                return false;

            return maBm.Equals("HRC1_BB_TieuHao_LF", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<List<string>> InitializeAsync(BmPhieu phieu)
        {
            if (string.IsNullOrWhiteSpace(phieu.DataJson))
                return new List<string>();

            using var doc = JsonDocument.Parse(phieu.DataJson);
            await _dlnmHrc1Service.SaveHRC1LFManualFromPhieuFormAsync(doc.RootElement);
            return new List<string>();
        }
    }
}

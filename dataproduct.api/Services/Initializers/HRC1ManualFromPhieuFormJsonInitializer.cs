using dataproduct.api.Models;
using dataproduct.api.Services;
using System.Text.Json;

namespace dataproduct.api.Services.Initializers
{
    public class HRC1ManualFromPhieuFormJsonInitializer : IPhieuJsonInitializer
    {
        private readonly DLNMHRC1Service _dlnmHrc1Service;

        public HRC1ManualFromPhieuFormJsonInitializer(DLNMHRC1Service dlnmHrc1Service)
        {
            _dlnmHrc1Service = dlnmHrc1Service;
        }

        public bool CanHandle(string? maBm)
        {
            if (string.IsNullOrWhiteSpace(maBm))
                return false;

            return maBm.Equals("HRC1_BB_TieuHao_BOF", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<List<string>> InitializeAsync(BmPhieu phieu)
        {
            if (string.IsNullOrWhiteSpace(phieu.DataJson))
                return new List<string>();

            using var doc = JsonDocument.Parse(phieu.DataJson);
            await _dlnmHrc1Service.SaveHRC1ManualFromPhieuFormAsync(doc.RootElement, phieu.Idphieu);
            return new List<string>();
        }
    }
}

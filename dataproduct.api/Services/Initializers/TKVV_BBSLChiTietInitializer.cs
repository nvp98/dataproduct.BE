using dataproduct.api.Models;

namespace dataproduct.api.Services.Initializers
{
    public class TKVV_BBSLChiTietInitializer : IPhieuJsonInitializer
    {
        private readonly TKVV_BBSLService _service;

        public TKVV_BBSLChiTietInitializer(TKVV_BBSLService service)
        {
            _service = service;
        }

        public bool CanHandle(string? maBm)
        {
            if (string.IsNullOrWhiteSpace(maBm))
                return false;

            return maBm.Equals("TKVV_BB_SanLuong", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<List<string>> InitializeAsync(BmPhieu phieu)
        {
            var (itemCount, skippedRows) = await _service.InsertFromPhieuJsonAsync(phieu);

            var warnings = new List<string>();
            if (skippedRows > 0)
                warnings.Add(
                    $"{skippedRows} dòng trong bảng sản lượng chưa được lưu vì thiếu Sản lượng " +
                    "(chưa cấu hình danh mục sản phẩm cho xưởng này). Vào lại phiếu để bổ sung.");
            else if (itemCount == 0)
                warnings.Add("Bảng chi tiết sản lượng đang trống — chưa có dòng nào có số liệu để lưu.");

            return warnings;
        }
    }
}

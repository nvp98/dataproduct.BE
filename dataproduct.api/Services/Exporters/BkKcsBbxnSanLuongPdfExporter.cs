using dataproduct.api.DTOs.Export;

namespace dataproduct.api.Services.Exporters
{
    /// <summary>
    /// Exporter for BBXN (Biên Bản Xác Nhận Sản Lượng) PDF export
    /// </summary>
    public class BkKcsBbxnSanLuongPdfExporter : IPhieuPdfExporter
    {
        private readonly BkKcsBbxnSanLuongService _service;

        public BkKcsBbxnSanLuongPdfExporter(BkKcsBbxnSanLuongService service)
        {
            _service = service;
        }

        public bool CanHandle(string? maBm)
        {
            if (string.IsNullOrWhiteSpace(maBm))
                return false;

            // Handle various BBXN form codes
            return maBm.IndexOf("CTD_BienBan_SanLuong", StringComparison.OrdinalIgnoreCase) >= 0
                || maBm.IndexOf("BM.08-QT.05.13", StringComparison.OrdinalIgnoreCase) >= 0
                || maBm.IndexOf("BM.08/QT.05.13", StringComparison.OrdinalIgnoreCase) >= 0
                || maBm.IndexOf("SanLuong", StringComparison.OrdinalIgnoreCase) >= 0
                || maBm.IndexOf("XacNhan", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public Task<ExportFileResult> ExportPdfAsync(Guid phieuId)
        {
            return _service.ExportPdfBienBanAsync(phieuId);
        }
    }
}

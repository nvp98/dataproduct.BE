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

            // Handle various BBXN form codes - exact match, giống BkKcsBbxnSanLuongExcelExporter.
            // Không dùng substring "SanLuong"/"XacNhan" nữa vì khớp nhầm cả các maBm khác cùng
            // chứa các chữ này (VD: TKVV_BB_SanLuong của module TKVV_BBSL).
            return maBm.Equals("CTD_BienBan_SanLuong", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM.08-QT.05.13", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM.08/QT.05.13", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM08-QT.05.13", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM08/QT.05.13", StringComparison.OrdinalIgnoreCase);
        }

        public Task<ExportFileResult> ExportPdfAsync(Guid phieuId)
        {
            return _service.ExportPdfBienBanAsync(phieuId);
        }
    }
}

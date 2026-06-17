using dataproduct.api.DTOs.Export;

namespace dataproduct.api.Services.Exporters
{
    /// <summary>
    /// Exporter for BBXN (Biên Bản Xác Nhận Sản Lượng) - supports per-phiếu export only
    /// </summary>
    public class BkKcsBbxnSanLuongExcelExporter : IPhieuExcelExporter
    {
        private readonly BkKcsBbxnSanLuongService _service;

        public BkKcsBbxnSanLuongExcelExporter(BkKcsBbxnSanLuongService service)
        {
            _service = service;
        }

        /// <summary>
        /// Kiểm tra xem exporter này có thể xử lý mã BM không
        /// </summary>
        public bool CanHandle(string? maBm)
        {
            if (string.IsNullOrWhiteSpace(maBm))
                return false;

            // Handle various BBXN form codes - exact match like KPH exporter
            return maBm.Equals("CTD_BienBan_SanLuong", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM.08-QT.05.13", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM.08/QT.05.13", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM08-QT.05.13", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM08/QT.05.13", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Xuất Excel Biên bản xác nhận sản lượng cho một phiếu
        /// </summary>
        // public Task<ExportFileResult> ExportExcelAsync(Guid phieuId)
        // {
        //     return _service.ExportExcelBienBanAsync(phieuId);
        // }

        public Task<ExportFileResult> ExportTongHopExcelAsync(DateOnly? fromDate, DateOnly? toDate)
        {
            return _service.ExportExcelTongHopAsync(fromDate, toDate);
        }
        public Task<ExportFileResult> ExportDetailExcelAsync(Guid phieuId)
        {
            return _service.ExportExcelChiTietAsync(phieuId);
        }
    }
}

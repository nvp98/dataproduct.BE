using dataproduct.api.DTOs.Export;

namespace dataproduct.api.Services.Exporters
{
    /// <summary>
    /// Excel exporter cho phiếu xử lý không phù hợp sản phẩm KPH
    /// </summary>
    public class CTDPhieuXuLyKphExcelExporter : IPhieuExcelExporter
    {
        private readonly CTDPhieuXuLyKphService _service;

        public CTDPhieuXuLyKphExcelExporter(CTDPhieuXuLyKphService service)
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

            return maBm.Equals("CTD_KPH_Sanxuat", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("CTD_BB_XuLyBanThanhPhamKPH", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM01C-QT.11", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM01C/QT.11", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("BM.01C/QT.11", StringComparison.OrdinalIgnoreCase)
                || maBm.Equals("CTD_BB_XuLy_SanPham_KPH", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Xuất Excel tổng hợp phiếu xử lý KPH
        /// </summary>
        public Task<ExportFileResult> ExportTongHopExcelAsync(DateOnly? fromDate, DateOnly? toDate)
        {
            return _service.ExportExcelTongHopKphAsync(fromDate, toDate);
        }
        public Task<ExportFileResult> ExportDetailExcelAsync(Guid phieuId)
        {
            return _service.ExportExcelChiTietKphAsync(phieuId);
        }
    }
}

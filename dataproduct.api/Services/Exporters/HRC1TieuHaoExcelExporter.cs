using dataproduct.api.DTOs.Export;

namespace dataproduct.api.Services.Exporters
{
    // Route Excel cho HRC1_BB_TieuHao_BOF / HRC1_BB_TieuHao_LF (NM.HRC1 - TaoTieuHaoLoThoi.tsx /
    // TaoTieuHaoTinhLuyenLF.tsx) vào api/Phieus/{id}/export-excel. Toàn bộ logic dựng file nằm ở
    // DLNMHRC1Service (region "EXPORT EXCEL/PDF CHI TIẾT PHIẾU") — lớp này chỉ khai báo maBm nào được
    // xử lý (mirror HRC1_BBGNPdfExporter.cs).
    public class HRC1TieuHaoExcelExporter : IPhieuExcelExporter
    {
        private readonly DLNMHRC1Service _svc;

        private static readonly string[] _supported =
        {
            "HRC1_BB_TieuHao_BOF",
            "HRC1_BB_TieuHao_LF",
        };

        public HRC1TieuHaoExcelExporter(DLNMHRC1Service svc)
        {
            _svc = svc;
        }

        public bool CanHandle(string? maBm) =>
            !string.IsNullOrWhiteSpace(maBm) &&
            _supported.Any(s => s.Equals(maBm, StringComparison.OrdinalIgnoreCase));

        public Task<ExportFileResult> ExportTongHopExcelAsync(DateOnly? fromDate, DateOnly? toDate) =>
            throw new NotSupportedException("Chưa hỗ trợ export Excel tổng hợp cho biểu mẫu Tiêu hao HRC1.");

        public Task<ExportFileResult> ExportExcelPhieuAsync(Guid phieuId) =>
            _svc.ExportTieuHaoExcelAsync(phieuId);
    }
}

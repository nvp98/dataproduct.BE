using dataproduct.api.Models;
using dataproduct.api.Repositories;
using dataproduct.api.Utils;
using ClosedXML.Excel;
using DinkToPdf;
using DinkToPdf.Contracts;
using System.Text;
using System.Text.Json;

namespace dataproduct.api.Services
{
    public class BkKcsBbxnSanLuongService
    {
        private readonly IBkKcsBbxnSanLuongRepository _repo;
        private readonly IPhieuRepository _repoPhieu;
        private readonly IConverter _pdfConverter;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly PheDuyetService _pheDuyetService;

        public BkKcsBbxnSanLuongService(
            IBkKcsBbxnSanLuongRepository repo,
            IPhieuRepository repoPhieu,
            IConverter pdfConverter,
            IWebHostEnvironment env,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            PheDuyetService pheDuyetService)
        {
            _repo = repo;
            _repoPhieu = repoPhieu;
            _pdfConverter = pdfConverter;
            _env = env;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _pheDuyetService = pheDuyetService;
        }

        public async Task<IEnumerable<BkKcsBbxnSanLuong>> GetAllAsync(DateOnly? ngaySX, string? ca, string? sanPham, string? macThep, string? idXuongCan)
        {
            return await _repo.GetAllAsync(ngaySX, ca, sanPham, macThep, idXuongCan);
        }

        public async Task<BkKcsBbxnSanLuong?> GetByIdAsync(long id)
        {
            return await _repo.GetByIdAsync(id);
        }

        /// <summary>
        /// Lấy dữ liệu theo phiếu
        /// </summary>
        /// <param name="idPhieu">ID của phiếu</param>
        /// <returns>Danh sách records của phiếu</returns>
        public async Task<IEnumerable<BkKcsBbxnSanLuong>> GetByIdPhieuAsync(Guid idPhieu)
        {
            return await _repo.GetByIdPhieuAsync(idPhieu);
        }

        /// <summary>
        /// Cập nhật IDPhieu và TinhTrang cho một record
        /// </summary>
        /// <param name="id">ID của record BkKcsBbxnSanLuong</param>
        /// <param name="idPhieu">ID của phiếu</param>
        /// <param name="tinhTrang">Trạng thái (0=Đang lưu, 1=Đã gửi, 2=Hoàn thành, 3=Đã thu hồi, 4=Không xác nhận, 5=Đã chốt, 6=Đang phê duyệt)</param>
        /// <returns></returns>
        public async Task UpdatePhieuInfoAsync(long id, Guid idPhieu, int tinhTrang)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null)
                throw new Exception($"Không tìm thấy record với ID: {id}");

            entity.IDPhieu = idPhieu;
            entity.TinhTrang = tinhTrang;

            await _repo.UpdateAsync(entity);
        }

        /// <summary>
        /// Cập nhật IDPhieu và TinhTrang cho nhiều records
        /// </summary>
        /// <param name="ids">Danh sách ID của các records cần cập nhật</param>
        /// <param name="idPhieu">ID của phiếu</param>
        /// <param name="tinhTrang">Trạng thái</param>
        /// <returns></returns>
        public async Task UpdatePhieuInfoBatchAsync(IEnumerable<long> ids, Guid idPhieu, int tinhTrang)
        {
            if (!ids.Any())
                throw new ArgumentException("Danh sách IDs không được rỗng");

            await _repo.UpdatePhieuInfoAsync(ids, idPhieu, tinhTrang);
        }

        /// <summary>
        /// Cập nhật toàn bộ thông tin cho một record
        /// </summary>
        /// <param name="entity">Entity cần cập nhật</param>
        /// <returns></returns>
        public async Task UpdateAsync(BkKcsBbxnSanLuong entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            await _repo.UpdateAsync(entity);
        }

        /// <summary>
        /// Cập nhật nhiều records cùng lúc
        /// </summary>
        /// <param name="entities">Danh sách entities cần cập nhật</param>
        /// <returns></returns>
        public async Task UpdateRangeAsync(IEnumerable<BkKcsBbxnSanLuong> entities)
        {
            if (entities == null || !entities.Any())
                throw new ArgumentException("Danh sách entities không được rỗng");

            await _repo.UpdateRangeAsync(entities);
        }

        /// <summary>
        /// Đổi trạng thái các records của một phiếu
        /// </summary>
        /// <param name="idPhieu">ID của phiếu</param>
        /// <param name="tinhTrang">Trạng thái mới</param>
        /// <returns></returns>
        public async Task UpdateTinhTrangByPhieuAsync(Guid idPhieu, int tinhTrang)
        {
            var entities = (await _repo.GetByIdPhieuAsync(idPhieu)).ToList();

            if (!entities.Any())
                throw new Exception($"Không tìm thấy records cho phiếu ID: {idPhieu}");

            foreach (var entity in entities)
            {
                entity.TinhTrang = tinhTrang;
            }

            await _repo.UpdateRangeAsync(entities);
        }

        /// <summary>
        /// Export PDF Biên bản xác nhận sản lượng theo form ISO
        /// </summary>
        /// <param name="phieuId">ID của phiếu</param>
        /// <returns>PDF file result</returns>
        public async Task<DTOs.Export.ExportFileResult> ExportPdfBienBanAsync(Guid phieuId)
        {
            // Lấy phiếu
            var phieu = await _repoPhieu.GetByIdAsync(phieuId);
            if (phieu == null)
                throw new Exception("Không tìm thấy phiếu");

            // Lấy dữ liệu BBXN theo phiếu
            var data = await _repo.GetByIdPhieuAsync(phieuId);
            if (!data.Any())
                throw new Exception("Không có dữ liệu BBXN để xuất PDF");

            // Load template HTML
            var templatePath = Path.Combine(
                _env.WebRootPath,
                "template_html",
                "BM.08-QT.05.13_Bien_ban_xac_nhan_san_luong_kcs.html"
            );

            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"Không tìm thấy template: {templatePath}");

            var html = await File.ReadAllTextAsync(templatePath);

            // Xây dựng dòng dữ liệu nhóm theo loại
            var rows = new StringBuilder();
            var dataList = data.ToList();

            // Group dữ liệu theo TenPhanLoai (loại sản phẩm)
            var groupedData = dataList
                .GroupBy(x => x.TenPhanLoai ?? "Không xác định")
                .OrderBy(g => g.Key);

            int globalStt = 1;
            decimal totalBo = groupedData.Sum(g => g.Sum(x => x.SoBo ?? 0));
            double totalKl = groupedData.Sum(g => g.Sum(x => x.KhoiLuong ?? 0));
            decimal totalThanh = groupedData.Sum(g => g.Sum(x => x.SoThanh ?? 0));
            foreach (var group in groupedData)
            {
                // Dòng header loại
                rows.Append($@"
            <tr style='font-weight: bold;'>
                <td colspan='8' style ='text-align: left;'> {group.Key}</td>
            </tr>");

                // Dòng dữ liệu của loại
                decimal groupTotalBo = 0;
                double groupTotalKL = 0;
                decimal groupTotalThanh = 0;

                foreach (var item in group)
                {
                    groupTotalBo += item.SoBo ?? 0;
                    groupTotalKL += item.KhoiLuong ?? 0;
                    groupTotalThanh += item.SoThanh ?? 0;

                    rows.Append($@"
            <tr>
                <td>{globalStt++}</td>
                <td class='text-left'>{item.SanPham}</td>
                <td>{item.MacThep}</td>
                <td class='text-right'>{item.ChieuDai:N2}</td>
                <td class='text-right'>{item.SoBo}</td>
                <td class='text-right'>{item.KhoiLuong:N0}</td>
                <td class='text-right'>{item.SoThanh}</td>
                <td></td>
            </tr>");
                }

                // Dòng tổng loại
                rows.Append($@"
            <tr style='font-weight: bold; '>
                <td colspan='4' class='text-left'>Tổng {group.Key}</td>
                <td class='text-right'>{groupTotalBo:N0}</td>
                <td class='text-right'>{groupTotalKL:N0}</td>
                <td class='text-right'>{groupTotalThanh:N0}</td>
                <td></td>
            </tr>");
            }
            // them tong cong san luong
            rows.Append($@"
            <tr style='font-weight: bold; '>
                <td colspan='4' class='text-left'>Tổng sản lượng</td>
                <td class='text-right'>{totalBo:N0}</td>
                <td class='text-right'>{totalKl:N0}</td>
                <td class='text-right'>{totalThanh:N0}</td>
                <td></td>
            </tr>");

            // Lấy logo URL
            var logoUrl = _configuration.GetValue<string>("AppSettings:LogoUrl") ?? "https://report.hoaphatdungquat.vn/img/logoHP.png";

            // Lấy thông tin phê duyệt từ PheDuyetService
            var pheDuyetList = await _pheDuyetService.GetPheDuyetPhieuAsync(phieuId);
            string signTruongKip = "";
            string nameTruongKip = "";
            string chucVuTruongKip = "";
            string bpTruongKip = "";
            string signTruongKCS = "";
            string nameTruongKCS = "";
            string chucVuQLCL = "";
            string bpQLCL = "";
            string signThukho = "";
            string nameThukho = "";
            string chucVuKho = "";
            string bpKho = "";

            if (pheDuyetList.Any())
            {
                // CapDuyet: 1=Trưởng kíp, 2=Trưởng KCS, 3=Thủ kho
                var duyet0 = pheDuyetList.FirstOrDefault(x => x.CapDuyet == 1);
                if (duyet0 != null)
                {
                    signTruongKip = _pheDuyetService.FormatChuKy(duyet0.ChuKy) ?? "";
                    nameTruongKip = duyet0.HoVaTen ?? "";
                    chucVuTruongKip = duyet0.TenViTri ?? "";
                    bpTruongKip = duyet0.TenPhongBan ?? "";
                }

                var duyet1 = pheDuyetList.FirstOrDefault(x => x.CapDuyet == 0);
                if (duyet1 != null)
                {
                    signTruongKCS = _pheDuyetService.FormatChuKy(duyet1.ChuKy) ?? "";
                    nameTruongKCS = duyet1.HoVaTen ?? "";
                    chucVuQLCL = duyet1.TenViTri ?? "";
                    bpQLCL = duyet1.TenPhongBan ?? "";
                }

                var duyet2 = pheDuyetList.FirstOrDefault(x => x.CapDuyet == 2);
                if (duyet2 != null)
                {
                    signThukho = _pheDuyetService.FormatChuKy(duyet2.ChuKy) ?? "";
                    nameThukho = duyet2.HoVaTen ?? "";
                    chucVuKho = duyet2.TenViTri ?? "";
                    bpKho = duyet2.TenPhongBan ?? "";

                }
            }

            // Tính toán thời gian ca kíp
            var (tuGio, denGio, tuNgay, denNgay) = ThoiGianHelper.CalculateCaKipTime(phieu.NgaySX, phieu.Ca);

            // Thay thế placeholders
            html = html
                .Replace("{{LogoUrl}}", logoUrl)
                .Replace("{{xuong}}", phieu.MayDuc?.ToString() ?? "")
                .Replace("{{Ca}}", phieu.Ca?.ToString("D") ?? "")
                .Replace("{{Kip}}", phieu.Kip ?? "")
                .Replace("{{TuGio}}", tuGio)
                .Replace("{{TuNgay}}", tuNgay)
                .Replace("{{DenGio}}", denGio)
                .Replace("{{DenNgay}}", denNgay)
                .Replace("{{ChucVuNhanTruongKip}}", chucVuTruongKip)
                .Replace("{{BPNhanTruongKip}}", bpTruongKip)
                .Replace("{{ChucVuNhanQLCL}}", chucVuQLCL)
                .Replace("{{BPQLCL}}", bpQLCL)
                .Replace("{{ChucVuNhanKho}}", chucVuKho)
                .Replace("{{BPKho}}", bpKho)
                .Replace("{{Rows}}", rows.ToString())
                .Replace("{{Sign_TruongKip}}", signTruongKip)
                .Replace("{{Sign_TruongKCS}}", signTruongKCS)
                .Replace("{{Sign_Thukho}}", signThukho)
                .Replace("{{Name_TruongKip}}", nameTruongKip)
                .Replace("{{Name_TruongKCS}}", nameTruongKCS)
                .Replace("{{Name_Thukho}}", nameThukho);

            // Convert HTML to PDF
            var doc = new HtmlToPdfDocument
            {
                GlobalSettings =
                {
                    PaperSize = PaperKind.A4,
                    Orientation = Orientation.Portrait
                },
                Objects =
                {
                    new ObjectSettings
                    {
                        HtmlContent = html,
                        WebSettings =
                        {
                            DefaultEncoding = "utf-8",
                            LoadImages = true,
                            EnableJavascript = false,
                            PrintMediaType = true,
                            UserStyleSheet = ""
                        },
                        LoadSettings =
                        {
                            BlockLocalFileAccess = false,
                            DebugJavascript = false,
                            LoadErrorHandling = ContentErrorHandling.Ignore
                        }
                    }
                }
            };

            var pdfBytes = _pdfConverter.Convert(doc);

            return new DTOs.Export.ExportFileResult
            {
                Content = pdfBytes,
                FileName = $"BienBanXacNhanSanLuong_{phieu.NgaySX:yyyyMMdd}_{DateTime.Now:HHmmss}.pdf",
                ContentType = "application/pdf"
            };
        }

        /// <summary>
        /// Export Excel Biên bản xác nhận sản lượng từ template
        /// </summary>
        /// <param name="phieuId">ID của phiếu</param>
        /// <returns>Excel file result</returns>
        public async Task<DTOs.Export.ExportFileResult> ExportExcelBienBanAsync(Guid phieuId)
        {
            // Lấy phiếu
            var phieu = await _repoPhieu.GetByIdAsync(phieuId);
            if (phieu == null)
                throw new Exception("Không tìm thấy phiếu");

            // Lấy dữ liệu BBXN theo phiếu
            var data = await _repo.GetByIdPhieuAsync(phieuId);
            if (!data.Any())
                throw new Exception("Không có dữ liệu BBXN để xuất Excel");

            // Load template Excel
            var templatePath = Path.Combine(
                _env.WebRootPath,
                "templates",
                "BM_TongHopSanLuong - KCS.xlsx"
            );

            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"Không tìm thấy template: {templatePath}");

            using var workbook = new XLWorkbook(templatePath);
            var ws = workbook.Worksheet(1);

            // Tính toán thời gian ca kíp
            var (tuGio, denGio, tuNgay, denNgay) = ThoiGianHelper.CalculateCaKipTime(phieu.NgaySX, phieu.Ca);

            // Lấy thông tin phê duyệt
            var pheDuyetList = await _pheDuyetService.GetPheDuyetPhieuAsync(phieuId);
            string nameTruongKip = "";
            string chucVuTruongKip = "";
            string bpTruongKip = "";
            string nameTruongKCS = "";
            string chucVuQLCL = "";
            string bpQLCL = "";
            string nameThukho = "";
            string chucVuKho = "";
            string bpKho = "";

            if (pheDuyetList.Any())
            {
                var duyet0 = pheDuyetList.FirstOrDefault(x => x.CapDuyet == 0);
                if (duyet0 != null)
                {
                    nameTruongKip = duyet0.HoVaTen ?? "";
                    chucVuTruongKip = duyet0.TenViTri ?? "";
                    bpTruongKip = duyet0.TenPhongBan ?? "";
                }

                var duyet1 = pheDuyetList.FirstOrDefault(x => x.CapDuyet == 1);
                if (duyet1 != null)
                {
                    nameTruongKCS = duyet1.HoVaTen ?? "";
                    chucVuQLCL = duyet1.TenViTri ?? "";
                    bpQLCL = duyet1.TenPhongBan ?? "";
                }

                var duyet2 = pheDuyetList.FirstOrDefault(x => x.CapDuyet == 2);
                if (duyet2 != null)
                {
                    nameThukho = duyet2.HoVaTen ?? "";
                    chucVuKho = duyet2.TenViTri ?? "";
                    bpKho = duyet2.TenPhongBan ?? "";
                }
            }

            // Điền thông tin tiêu đề
            ws.Cell("B2").Value = phieu.NgaySX?.ToString("dd/MM/yyyy") ?? "";
            ws.Cell("B3").Value = phieu.Ca;
            ws.Cell("B4").Value = phieu.Kip ?? "";
            ws.Cell("D2").Value = $"{tuGio}h - {denGio}h";
            ws.Cell("D3").Value = tuNgay;
            ws.Cell("D4").Value = denNgay;
            ws.Cell("F2").Value = phieu.MayDuc?.ToString() ?? "";

            // Điền thông tin nhân viên
            ws.Cell("B10").Value = nameTruongKip;
            ws.Cell("D10").Value = chucVuTruongKip;
            ws.Cell("F10").Value = bpTruongKip;

            ws.Cell("B11").Value = nameTruongKCS;
            ws.Cell("D11").Value = chucVuQLCL;
            ws.Cell("F11").Value = bpQLCL;

            ws.Cell("B12").Value = nameThukho;
            ws.Cell("D12").Value = chucVuKho;
            ws.Cell("F12").Value = bpKho;

            // Ghi dữ liệu nhóm theo loại
            var dataList = data.ToList();
            var groupedData = dataList
                .GroupBy(x => x.TenPhanLoai ?? "Không xác định")
                .OrderBy(g => g.Key);

            int currentRow = 15; // Dòng bắt đầu dữ liệu
            int globalStt = 1;

            foreach (var group in groupedData)
            {
                // Dòng header loại sản phẩm
                ws.Cell(currentRow, 1).Value = group.Key;
                ws.Cell(currentRow, 1).Style.Font.Bold = true;
                currentRow++;

                // Dòng dữ liệu của loại
                decimal groupTotalBo = 0;
                double groupTotalKL = 0;
                decimal groupTotalThanh = 0;

                foreach (var item in group)
                {
                    groupTotalBo += item.SoBo ?? 0;
                    groupTotalKL += item.KhoiLuong ?? 0;
                    groupTotalThanh += item.SoThanh ?? 0;

                    ws.Cell(currentRow, 1).Value = globalStt++;
                    ws.Cell(currentRow, 2).Value = item.SanPham;
                    ws.Cell(currentRow, 3).Value = item.MacThep;
                    ws.Cell(currentRow, 4).Value = item.ChieuDai;
                    ws.Cell(currentRow, 5).Value = item.SoBo;
                    ws.Cell(currentRow, 6).Value = item.KhoiLuong;
                    ws.Cell(currentRow, 7).Value = item.SoThanh;
                    ws.Cell(currentRow, 8).Value = "";

                    // Căn phải cho số
                    ws.Cell(currentRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    ws.Cell(currentRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    ws.Cell(currentRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    ws.Cell(currentRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                    currentRow++;
                }

                // Dòng tổng loại
                ws.Cell(currentRow, 1).Value = $"Tổng {group.Key}";
                ws.Cell(currentRow, 1).Style.Font.Bold = true;
                ws.Cell(currentRow, 5).Value = groupTotalBo;
                ws.Cell(currentRow, 6).Value = groupTotalKL;
                ws.Cell(currentRow, 7).Value = groupTotalThanh;
                ws.Cell(currentRow, 5).Style.Font.Bold = true;
                ws.Cell(currentRow, 6).Style.Font.Bold = true;
                ws.Cell(currentRow, 7).Style.Font.Bold = true;
                ws.Cell(currentRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                ws.Cell(currentRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                ws.Cell(currentRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                currentRow++;
            }

            // Lưu vào memory stream
            var ms = new MemoryStream();
            workbook.SaveAs(ms);
            ms.Position = 0;

            return new DTOs.Export.ExportFileResult
            {
                Content = ms.ToArray(),
                FileName = $"BienBanXacNhanSanLuong_{phieu.NgaySX:yyyyMMdd}_{DateTime.Now:HHmmss}.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            };
        }

        /// <summary>
        /// Lấy dữ liệu biên bản xác nhận sản lượng để xuất Excel tổng hợp
        /// </summary>
        public async Task<List<DTOs.Export.BmTongHopBbxnSanLuongRow>> GetDataExportTongHopBbxnAsync(DateOnly? fromDate, DateOnly? toDate)
        {
            var allData = await GetAllAsync(null, null, null, null, null);
            var data = allData.AsEnumerable();

            if (fromDate.HasValue)
                data = data.Where(x => x.NgaySX >= fromDate.Value);

            if (toDate.HasValue)
                data = data.Where(x => x.NgaySX <= toDate.Value);

            data = data.ToList();

            var result = new List<DTOs.Export.BmTongHopBbxnSanLuongRow>();

            foreach (var item in data)
            {
                var row = new DTOs.Export.BmTongHopBbxnSanLuongRow
                {
                    IdPhieu = item.IDPhieu ?? Guid.Empty,
                    NgaySX = item.NgaySX,
                    TenCa = item.TenCa,
                    Ca = !string.IsNullOrWhiteSpace(item.Ca) ? int.TryParse(item.Ca, out var caVal) ? caVal : null : null,
                    IDXuongCan = item.TenXuongCan,
                    TinhTrang = item.TinhTrang,

                    SanPham = item.SanPham,
                    MacThep = item.MacThep,
                    ChieuDai = item.ChieuDai?.ToString(),
                    SoBo = item.SoBo,
                    KhoiLuong = item.KhoiLuong,
                    SoThanh = item.SoThanh,
                    TenPhanLoai = item.TenPhanLoai,

                    CreatedAt = item.NgayTao
                };

                // Lấy thông tin người lập và người phê duyệt từ Phiếu
                if (item.IDPhieu.HasValue)
                {
                    var pheDuyets = await _pheDuyetService.GetPheDuyetPhieuAsync(item.IDPhieu.Value);
                    var nguoiLap = pheDuyets.FirstOrDefault(x => x.CapDuyet == 0);
                    var nguoiPheDuyet = pheDuyets.FirstOrDefault(x => x.CapDuyet >= 1);

                    row.NguoiLapPhieu = nguoiLap?.HoVaTen;
                    row.NguoiPheDuyet = nguoiPheDuyet?.HoVaTen;

                    // Lấy số phiếu từ phiếu
                    var phieu = await _repoPhieu.GetByIdAsync(item.IDPhieu.Value);
                    row.SoPhieu = phieu?.SoPhieu;
                }

                result.Add(row);
            }

            return result;
        }

        /// <summary>
        /// Export Excel tổng hợp Biên bản xác nhận sản lượng theo date range
        /// </summary>
        public async Task<DTOs.Export.ExportFileResult> ExportExcelTongHopAsync(DateOnly? fromDate, DateOnly? toDate)
        {
            var data = await GetDataExportTongHopBbxnAsync(fromDate, toDate);

            if (!data.Any())
                throw new Exception("Không có dữ liệu để xuất Excel");

            // Load template Excel
            var templatePath = Path.Combine(
                _env.WebRootPath,
                "templates",
                "BM_TongHopSanLuong - KCS.xlsx"
            );

            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"Không tìm thấy template: {templatePath}");

            using var workbook = new XLWorkbook(templatePath);
            var ws = workbook.Worksheet(1);

            try
            {
                // Ghi tiêu đề ngày/thời gian lọc vào dòng 5
                var dateStr = "";
                if (fromDate.HasValue && toDate.HasValue)
                    dateStr = $"Từ ngày: {fromDate:dd/MM/yyyy} đến ngày: {toDate:dd/MM/yyyy}";
                else if (fromDate.HasValue)
                    dateStr = $"Từ ngày: {fromDate:dd/MM/yyyy}";
                else if (toDate.HasValue)
                    dateStr = $"Đến ngày: {toDate:dd/MM/yyyy}";
                else
                    dateStr = "Toàn bộ dữ liệu";

                ws.Cell("A5").Value = dateStr;

                // Dữ liệu bắt đầu từ dòng 10 (dòng 7-8 là header)
                int row = 9;
                int stt = 1;

                foreach (var item in data.Where(x => x.SoPhieu != null))
                {
                    // STT
                    ws.Cell(row, 1).Value = stt++;

                    // Ngày
                    ws.Cell(row, 2).Value = item.NgaySX?.ToString("dd/MM/yyyy");

                    // Kíp
                    ws.Cell(row, 3).Value = item.TenCa;

                    // Ca
                    ws.Cell(row, 4).Value = item.Ca;

                    // Xưởng cần
                    ws.Cell(row, 5).Value = item.IDXuongCan;

                    // Sản phẩm
                    ws.Cell(row, 6).Value = item.SanPham;

                    // Mác thép
                    ws.Cell(row, 7).Value = item.MacThep;

                    // Chiều dài
                    ws.Cell(row, 8).Value = item.ChieuDai;

                    // Số bó
                    ws.Cell(row, 9).Value = item.SoBo;

                    // KL Cân
                    ws.Cell(row, 10).Value = item.KhoiLuong;

                    // Số thanh
                    ws.Cell(row, 11).Value = item.SoThanh;

                    // Ghi chú (để trống)
                    ws.Cell(row, 12).Value = "";

                    // Mã phiếu
                    ws.Cell(row, 13).Value = item.SoPhieu;

                    // Tính trạng phiếu
                    var statusCell = ws.Cell(row, 14);
                    statusCell.Value = GetTinhTrangText(item.TinhTrang);
                    statusCell.Style.Fill.BackgroundColor = GetTinhTrangColor(item.TinhTrang);

                    // Căn phải cho số
                    ws.Cell(row, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    ws.Cell(row, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    ws.Cell(row, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    ws.Cell(row, 11).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                    // Set borders cho toàn bộ cells trong dòng
                    for (int col = 1; col <= 14; col++)
                    {
                        ws.Cell(row, col).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                        ws.Cell(row, col).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                        ws.Cell(row, col).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                        ws.Cell(row, col).Style.Border.RightBorder = XLBorderStyleValues.Thin;
                    }

                    row++;
                }

                // Lưu vào memory stream
                var ms = new MemoryStream();
                workbook.SaveAs(ms);
                ms.Position = 0;

                return new DTOs.Export.ExportFileResult
                {
                    Content = ms.ToArray(),
                    FileName = $"TongHopBienBanSanLuong_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                    ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                };
            }
            finally
            {
                workbook?.Dispose();
            }
        }

        /// <summary>
        /// Lấy text tính trạng phiếu
        /// </summary>
        private string GetTinhTrangText(int? tinhTrang)
        {
            return tinhTrang switch
            {
                0 => "Đang lưu",
                1 => "Đã gửi",
                2 => "Hoàn thành",
                3 => "Đã thu hồi",
                4 => "Không xác nhận",
                5 => "Đã chốt",
                6 => "Đang phê duyệt",
                _ => "Không xác định"
            };
        }

        /// <summary>
        /// Lấy màu tính trạng phiếu
        /// </summary>
        private XLColor GetTinhTrangColor(int? tinhTrang)
        {
            return tinhTrang switch
            {
                0 => XLColor.LightGray,
                1 => XLColor.LightBlue,
                2 => XLColor.LightGreen,
                3 => XLColor.Orange,
                4 => XLColor.Red,
                5 => XLColor.DarkGreen,
                6 => XLColor.Yellow,
                _ => XLColor.White
            };
        }
    }
}

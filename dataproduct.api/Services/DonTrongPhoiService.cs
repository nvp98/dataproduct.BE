using ClosedXML.Excel;
using dataproduct.api.Models;
using dataproduct.api.Repositories;

namespace dataproduct.api.Services
{
    public class ImportDonTrongPhoiResult
    {
        public int Created { get; set; }
        public int Updated { get; set; }
        public List<string> Errors { get; set; } = [];
    }

    public class DonTrongPhoiService
    {
        private readonly IDonTrongPhoiRepository _repo;

        public DonTrongPhoiService(IDonTrongPhoiRepository repo)
        {
            _repo = repo;
        }

        public Task<IEnumerable<DonTrongPhoi>> GetAllAsync(string? macPhoi, string? mac, string? kichThuoc, int? isXacNhan = null, bool excludeLocked = false, string? maVatTu = null, int? idXuong = null)
            => _repo.GetAllAsync(macPhoi, mac, kichThuoc, isXacNhan, excludeLocked, maVatTu, idXuong);

        public Task<DonTrongPhoi?> GetByIdAsync(int id)
            => _repo.GetByIdAsync(id);

        /// <summary>
        /// Tra đơn trọng theo thứ tự ưu tiên: (Mác + KT, riêng xưởng) → (Mác + KT, dùng chung)
        /// → (KT, riêng xưởng) → (KT, dùng chung). Không tìm thấy → null (bên gọi phải chặn tính, không ghi 0).
        /// </summary>
        public async Task<(DonTrongPhoi Row, string NguonTra)?> TraCuuAsync(string? mac, string kichThuoc, int? idXuong)
        {
            var rows = await _repo.GetByKichThuocAsync(kichThuoc.Trim());
            var macNorm = string.IsNullOrWhiteSpace(mac) ? null : mac.Trim();

            bool KhopMac(DonTrongPhoi x) => macNorm != null && string.Equals(x.Mac?.Trim(), macNorm, StringComparison.OrdinalIgnoreCase);
            bool MacTrong(DonTrongPhoi x) => string.IsNullOrWhiteSpace(x.Mac);

            var thuTu = new (Func<DonTrongPhoi, bool> Match, string Nguon)[]
            {
                (x => KhopMac(x) && idXuong.HasValue && x.IdXuong == idXuong, "Mác + KT (riêng xưởng)"),
                (x => KhopMac(x) && x.IdXuong == null,                         "Mác + KT (dùng chung)"),
                (x => MacTrong(x) && idXuong.HasValue && x.IdXuong == idXuong, "KT (riêng xưởng)"),
                (x => MacTrong(x) && x.IdXuong == null,                        "KT (dùng chung)"),
            };

            foreach (var (match, nguon) in thuTu)
            {
                var row = rows.FirstOrDefault(match);
                if (row != null) return (row, nguon);
            }
            return null;
        }

        public async Task<DonTrongPhoi> CreateAsync(DonTrongPhoi entity)
        {
            if (await _repo.ExistsAsync(entity.MacPhoi, entity.Mac, entity.KichThuoc, idXuong: entity.IdXuong))
                throw new InvalidOperationException("Đã tồn tại bản ghi với Mác phôi, Mác thép và Kích thước này.");

            await _repo.AddAsync(entity);
            return entity;
        }

        public async Task<bool> UpdateAsync(int id, DonTrongPhoi entity)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return false;

            if (await _repo.ExistsAsync(entity.MacPhoi, entity.Mac, entity.KichThuoc, id, entity.IdXuong))
                throw new InvalidOperationException("Đã tồn tại bản ghi với Mác phôi, Mác thép và Kích thước này.");

            existing.MacPhoi    = entity.MacPhoi;
            existing.DonTrong   = entity.DonTrong;
            existing.Mac        = entity.Mac;
            existing.KichThuoc  = entity.KichThuoc;
            existing.IsXacNhan  = entity.IsXacNhan;
            existing.IdPB       = entity.IdPB;
            existing.IdXuong    = entity.IdXuong;
            await _repo.UpdateAsync(existing);
            return true;
        }

        public async Task<bool> DeleteAsync(int id, int? replacementId = null)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return false;
            await _repo.ReplaceInSoTheoDoiAsync(id, replacementId);
            await _repo.DeleteAsync(id);
            return true;
        }

        public Task SyncMaVatTuAsync() => _repo.SyncMaVatTuAsync();

        public Task<bool> ToggleLockAsync(int id) => _repo.ToggleLockAsync(id);

        public async Task<byte[]> ExportExcelAsync()
        {
            var all = await _repo.GetAllAsync(null, null, null);
            var rows = all.OrderBy(x => x.MacPhoi).ThenBy(x => x.Mac).ThenBy(x => x.KichThuoc).ToList();

            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("DonTrongPhoi");

            // Header row
            ws.Cell(1, 1).Value = "Mác phôi";
            ws.Cell(1, 2).Value = "Mác";
            ws.Cell(1, 3).Value = "Kích thước";
            ws.Cell(1, 4).Value = "Đơn trọng (kg)";

            var headerRow = ws.Range(1, 1, 1, 4);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9E1F2");
            headerRow.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            headerRow.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // Data rows
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                int rowIdx = i + 2;
                ws.Cell(rowIdx, 1).Value = r.MacPhoi;
                ws.Cell(rowIdx, 2).Value = r.Mac ?? "";
                ws.Cell(rowIdx, 3).Value = r.KichThuoc ?? "";
                ws.Cell(rowIdx, 4).Value = (double)r.DonTrong;
                ws.Cell(rowIdx, 4).Style.NumberFormat.Format = "#,##0.000";
            }

            if (rows.Count > 0)
            {
                var dataRange = ws.Range(2, 1, rows.Count + 1, 4);
                dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            }

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        public async Task<ImportDonTrongPhoiResult> ImportExcelAsync(IFormFile file)
        {
            var result = new ImportDonTrongPhoiResult();

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            ms.Position = 0;

            using var wb = new XLWorkbook(ms);
            var ws = wb.Worksheet(1);
            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

            for (int rowIdx = 2; rowIdx <= lastRow; rowIdx++)
            {
                var macPhoi = ws.Cell(rowIdx, 1).GetString().Trim();
                var mac = ws.Cell(rowIdx, 2).GetString().Trim();
                var kichThuoc = ws.Cell(rowIdx, 3).GetString().Trim();
                var donTrongRaw = ws.Cell(rowIdx, 4).GetString().Trim();

                if (string.IsNullOrEmpty(macPhoi) && string.IsNullOrEmpty(donTrongRaw))
                    continue;

                if (string.IsNullOrEmpty(macPhoi))
                {
                    result.Errors.Add($"Dòng {rowIdx}: Mác phôi không được để trống.");
                    continue;
                }

                if (!decimal.TryParse(donTrongRaw, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out var donTrong) || donTrong < 0)
                {
                    // Try reading as double from cell directly
                    try
                    {
                        donTrong = (decimal)ws.Cell(rowIdx, 4).GetDouble();
                    }
                    catch
                    {
                        result.Errors.Add($"Dòng {rowIdx}: Đơn trọng không hợp lệ ('{donTrongRaw}').");
                        continue;
                    }
                }

                var macNorm     = string.IsNullOrEmpty(mac)      ? null : mac;
                var kichThuocNorm = string.IsNullOrEmpty(kichThuoc) ? null : kichThuoc;

                var existing = await _repo.FindByKeyAsync(macPhoi, macNorm, kichThuocNorm);
                if (existing != null)
                {
                    existing.DonTrong = donTrong;
                    await _repo.UpdateAsync(existing);
                    result.Updated++;
                }
                else
                {
                    await _repo.AddAsync(new DonTrongPhoi
                    {
                        MacPhoi   = macPhoi,
                        Mac       = macNorm,
                        KichThuoc = kichThuocNorm,
                        DonTrong  = donTrong,
                    });
                    result.Created++;
                }
            }

            return result;
        }
    }
}

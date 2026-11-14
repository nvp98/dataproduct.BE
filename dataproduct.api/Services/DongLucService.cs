using ClosedXML.Excel;
using dataproduct.api.Models;
using dataproduct.api.Repositories;

namespace dataproduct.api.Services
{
    public class TagDongLucPayload
    {
        public string TagIDEMS { get; set; } = null!;
        public string MaHeader { get; set; } = null!;
        public string? YNghia { get; set; }
        public string MaXuong { get; set; } = null!;
        public byte TinhTrang { get; set; } = 1;
        public int ThuTu { get; set; }
    }

    /// <summary>Danh mục tag + tra cứu / xuất Excel dữ liệu Động lực (dữ liệu chỉ đọc).</summary>
    public class DongLucService
    {
        private readonly IDongLucRepository _repo;

        public DongLucService(IDongLucRepository repo)
        {
            _repo = repo;
        }

        // ── Tag ────────────────────────────────────────────────────────────────

        public Task<List<TagID_DongLuc>> GetTagsAsync(string? maXuong, byte? tinhTrang, string? searchKey)
            => _repo.GetTagsAsync(maXuong, tinhTrang, searchKey);

        public async Task<TagID_DongLuc> CreateTagAsync(TagDongLucPayload payload, string? nguoi)
        {
            var entity = new TagID_DongLuc { NguoiTao = nguoi, NgayTao = DateTime.Now };
            await ApplyAsync(entity, payload, excludeId: null);
            await _repo.AddTagAsync(entity);
            return entity;
        }

        public async Task<bool> UpdateTagAsync(int id, TagDongLucPayload payload, string? nguoi)
        {
            var entity = await _repo.GetTagByIdAsync(id);
            if (entity == null) return false;

            await ApplyAsync(entity, payload, excludeId: id);
            entity.NguoiCapNhat = nguoi;
            entity.NgayCapNhat = DateTime.Now;
            await _repo.UpdateTagAsync(entity);
            return true;
        }

        // Xóa mềm — dữ liệu đã kéo về của tag vẫn giữ nguyên
        public async Task<bool> DeleteTagAsync(int id, string? nguoi)
        {
            var entity = await _repo.GetTagByIdAsync(id);
            if (entity == null) return false;

            entity.IsDelete = true;
            entity.NguoiCapNhat = nguoi;
            entity.NgayCapNhat = DateTime.Now;
            await _repo.UpdateTagAsync(entity);
            return true;
        }

        private async Task ApplyAsync(TagID_DongLuc entity, TagDongLucPayload payload, int? excludeId)
        {
            var tagId = payload.TagIDEMS?.Trim();
            var maHeader = payload.MaHeader?.Trim();
            var maXuong = payload.MaXuong?.Trim().ToUpperInvariant();

            if (string.IsNullOrEmpty(tagId)) throw new InvalidOperationException("TagID EMS không được để trống.");
            if (string.IsNullOrEmpty(maHeader)) throw new InvalidOperationException("Mã header không được để trống.");
            if (string.IsNullOrEmpty(maXuong)) throw new InvalidOperationException("Mã xưởng không được để trống.");
            if (payload.TinhTrang is not (0 or 1)) throw new InvalidOperationException("Tình trạng không hợp lệ.");
            if (await _repo.ExistsTagAsync(tagId, excludeId))
                throw new InvalidOperationException($"TagID EMS {tagId} đã có trong danh mục.");

            entity.TagIDEMS = tagId;
            entity.MaHeader = maHeader;
            entity.YNghia = string.IsNullOrWhiteSpace(payload.YNghia) ? null : payload.YNghia.Trim();
            entity.MaXuong = maXuong;
            entity.TinhTrang = payload.TinhTrang;
            entity.ThuTu = payload.ThuTu;
        }

        // ── Dữ liệu ────────────────────────────────────────────────────────────

        public Task<(List<DuLieu_DongLuc> Rows, int Total)> SearchDuLieuAsync(
            DateOnly tuNgay, DateOnly denNgay, byte? caSX, string? maXuong, string? maHeader, int page, int pageSize)
            => _repo.SearchDuLieuAsync(tuNgay, denNgay, caSX, maXuong, maHeader, page, pageSize);

        public Task<DongLuc_SyncLog?> GetSyncLogGanNhatAsync() => _repo.GetSyncLogGanNhatAsync();

        public async Task<byte[]> ExportDuLieuExcelAsync(DateOnly tuNgay, DateOnly denNgay, byte? caSX, string? maXuong, string? maHeader)
        {
            var rows = await _repo.GetDuLieuAsync(tuNgay, denNgay, caSX, maXuong, maHeader);

            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("DuLieu_DongLuc");

            string[] headers = ["Ngày", "Ca", "Mã header", "TagID", "Giá trị", "Ngày cập nhật", "Mã xưởng"];
            for (int c = 0; c < headers.Length; c++)
                ws.Cell(1, c + 1).Value = headers[c];

            var headerRow = ws.Range(1, 1, 1, headers.Length);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9E1F2");
            headerRow.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            headerRow.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                int rowIdx = i + 2;
                ws.Cell(rowIdx, 1).Value = r.Ngay.ToDateTime(TimeOnly.MinValue);
                ws.Cell(rowIdx, 1).Style.DateFormat.Format = "dd/MM/yyyy";
                ws.Cell(rowIdx, 2).Value = r.CaSX;
                ws.Cell(rowIdx, 3).Value = r.MaHeader;
                ws.Cell(rowIdx, 4).Value = r.TagID;
                if (r.GiaTri.HasValue)
                {
                    ws.Cell(rowIdx, 5).Value = (double)r.GiaTri.Value;
                    ws.Cell(rowIdx, 5).Style.NumberFormat.Format = "#,##0.###";
                }
                ws.Cell(rowIdx, 6).Value = r.NgayCapNhat;
                ws.Cell(rowIdx, 6).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
                ws.Cell(rowIdx, 7).Value = r.MaXuong;
            }

            if (rows.Count > 0)
            {
                var dataRange = ws.Range(2, 1, rows.Count + 1, headers.Length);
                dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            }

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }
    }
}

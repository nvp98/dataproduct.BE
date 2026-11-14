using dataproduct.api.Models;
using dataproduct.api.Repositories;

namespace dataproduct.api.Services
{
    /// <summary>
    /// Kéo giá trị động lực theo ca từ EMS về DuLieu_DongLuc cho các tag đang bật (TinhTrang = 1).
    /// Dùng chung cho nút "Đồng bộ" (người dùng) và DongLucSyncWorker (tự động).
    /// </summary>
    public class DongLucSyncService
    {
        public const string NguonTuDong = "auto";
        private const int SoNgayToiDa = 31;

        // Mọi lần đồng bộ (tay + tự động) dùng chung 1 khóa — không chạy chồng
        private static readonly SemaphoreSlim _khoaDongBo = new(1, 1);

        private readonly IDongLucRepository _repo;
        private readonly EmsApiService _ems;
        private readonly ILogger<DongLucSyncService> _logger;

        public DongLucSyncService(IDongLucRepository repo, EmsApiService ems, ILogger<DongLucSyncService> logger)
        {
            _repo = repo;
            _ems = ems;
            _logger = logger;
        }

        public async Task<DongLuc_SyncLog> SyncAsync(DateOnly tuNgay, DateOnly denNgay, string? maXuong, string? nguonChay, CancellationToken ct = default)
        {
            if (denNgay < tuNgay) throw new InvalidOperationException("Đến ngày phải sau Từ ngày.");
            if (denNgay.DayNumber - tuNgay.DayNumber + 1 > SoNgayToiDa)
                throw new InvalidOperationException($"Mỗi lần đồng bộ tối đa {SoNgayToiDa} ngày.");
            if (!await _khoaDongBo.WaitAsync(0, ct))
                throw new InvalidOperationException("Đang có một lần đồng bộ khác chạy, vui lòng thử lại sau.");

            var log = new DongLuc_SyncLog
            {
                ThoiGianBatDau = DateTime.Now,
                TuNgay = tuNgay,
                DenNgay = denNgay,
                MaXuong = string.IsNullOrWhiteSpace(maXuong) ? null : maXuong.Trim(),
                NguonChay = nguonChay,
            };

            try
            {
                await _repo.AddSyncLogAsync(log);

                var tags = await _repo.GetTagsAsync(log.MaXuong, tinhTrang: 1, searchKey: null);
                var tagMap = tags.ToDictionary(x => x.TagIDEMS, StringComparer.OrdinalIgnoreCase);

                var giaTris = await _ems.LayGiaTriTheoCaAsync(tuNgay, denNgay, tagMap.Keys.ToList(), ct);

                // MaHeader / MaXuong lấy theo danh mục tag tại thời điểm kéo; bỏ tag EMS trả về nhưng không có trong danh mục
                var rows = giaTris
                    .Where(x => tagMap.ContainsKey(x.TagID))
                    .GroupBy(x => (x.Ngay, x.CaSX, Tag: tagMap[x.TagID].TagIDEMS))
                    .Select(g =>
                    {
                        var tag = tagMap[g.Key.Tag];
                        return new DuLieu_DongLuc
                        {
                            Ngay = g.Key.Ngay,
                            CaSX = g.Key.CaSX,
                            TagID = tag.TagIDEMS,
                            MaHeader = tag.MaHeader,
                            MaXuong = tag.MaXuong,
                            GiaTri = g.Last().GiaTri,
                        };
                    })
                    .ToList();

                log.SoDong = await _repo.UpsertDuLieuAsync(rows);
                log.ThanhCong = true;
            }
            catch (Exception ex)
            {
                log.ThanhCong = false;
                log.LoiChiTiet = ex.Message;
                _logger.LogError(ex, "Đồng bộ Động lực {TuNgay}→{DenNgay} lỗi", tuNgay, denNgay);
            }
            finally
            {
                log.ThoiGianKetThuc = DateTime.Now;
                try { await _repo.UpdateSyncLogAsync(log); }
                catch (Exception ex) { _logger.LogError(ex, "Không ghi được nhật ký đồng bộ Động lực"); }
                _khoaDongBo.Release();
            }

            return log;
        }
    }
}

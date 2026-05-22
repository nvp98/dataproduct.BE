using dataproduct.api.Models;
using System.Text.Json;

namespace dataproduct.api.Services.Initializers
{
    /// <summary>
    /// Initializer để cập nhật IDPhieu và TinhTrang cho BkKcsBbxnSanLuong khi tạo phiếu BBXN
    /// </summary>
    public class BkKcsBbxnSanLuongJsonInitializer : IPhieuJsonInitializer
    {
        private readonly BkKcsBbxnSanLuongService _service;

        public BkKcsBbxnSanLuongJsonInitializer(BkKcsBbxnSanLuongService service)
        {
            _service = service;
        }

        public bool CanHandle(string? maBm)
        {
            if (string.IsNullOrWhiteSpace(maBm))
                return false;

            // Xử lý các form code liên quan đến BBXN (Biên Bản Xác Nhận Sản Lượng)
            return maBm.IndexOf("CTD_BienBan_SanLuong", StringComparison.OrdinalIgnoreCase) >= 0
                || maBm.Equals("BK_KCS_BBXNSanLuong", StringComparison.OrdinalIgnoreCase)
                || maBm.Contains("SanLuong", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<List<string>> InitializeAsync(BmPhieu phieu)
        {
            if (phieu == null)
                return new List<string>();

            if (string.IsNullOrWhiteSpace(phieu.DataJson))
                return new List<string>();

            try
            {
                using var jsonDoc = JsonDocument.Parse(phieu.DataJson);
                var root = jsonDoc.RootElement;

                // Lấy danh sách các record IDs từ DataJson
                var recordIds = ExtractRecordIds(root);

                if (recordIds.Any())
                {
                    // Cập nhật IDPhieu và TinhTrang cho các records
                    // TinhTrang = 1 (Đã xử lý) - hoặc có thể set giá trị khác tùy logic
                    await _service.UpdatePhieuInfoBatchAsync(recordIds, phieu.Idphieu, 1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi initialize BkKcsBbxnSanLuong: {ex.Message}");
                // Không throw exception, cho phép phiếu được tạo, nhưng log lỗi
            }

            return new List<string>();
        }

        /// <summary>
        /// Trích xuất danh sách các record ID từ JSON payload
        /// </summary>
        private IEnumerable<long> ExtractRecordIds(JsonElement element)
        {
            var ids = new List<long>();

            try
            {
                // Thử lấy từ các key có thể chứa IDs
                // if (element.TryGetProperty("recordIds", out var recordIds) && recordIds.ValueKind == JsonValueKind.Array)
                // {
                //     foreach (var id in recordIds.EnumerateArray())
                //     {
                //         if (id.ValueKind == JsonValueKind.Number && id.TryGetInt64(out var longId))
                //             ids.Add(longId);
                //         else if (id.ValueKind == JsonValueKind.String && long.TryParse(id.GetString(), out longId))
                //             ids.Add(longId);
                //     }
                // }

                // Thử từ "selectedRecords"
                // if (element.TryGetProperty("selectedRecords", out var selectedRecords) && selectedRecords.ValueKind == JsonValueKind.Array)
                // {
                //     foreach (var id in selectedRecords.EnumerateArray())
                //     {
                //         if (id.ValueKind == JsonValueKind.Number && id.TryGetInt64(out var longId))
                //             ids.Add(longId);
                //         else if (id.ValueKind == JsonValueKind.String && long.TryParse(id.GetString(), out longId))
                //             ids.Add(longId);
                //     }
                // }

                // Thử từ "rows" - có thể là mảng các objects có id
                // if (element.TryGetProperty("rows", out var rows) && rows.ValueKind == JsonValueKind.Array)
                // {
                //     foreach (var row in rows.EnumerateArray())
                //     {
                //         if (row.ValueKind == JsonValueKind.Object)
                //         {
                //             // Tìm ID trong row
                //             if (row.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.Number && id.TryGetInt64(out var longId))
                //                 ids.Add(longId);
                //             else if (row.TryGetProperty("Id", out var ID) && ID.ValueKind == JsonValueKind.Number && ID.TryGetInt64(out longId))
                //                 ids.Add(longId);
                //             else if (row.TryGetProperty("recordId", out var recordId) && recordId.ValueKind == JsonValueKind.Number && recordId.TryGetInt64(out longId))
                //                 ids.Add(longId);
                //         }
                //     }
                // }

                // Thử từ "table1" - pattern từ khác
                if (element.TryGetProperty("table1", out var table1) && table1.ValueKind == JsonValueKind.Array)
                {
                    foreach (var row in table1.EnumerateArray())
                    {
                        if (row.ValueKind == JsonValueKind.Object)
                        {
                            if (row.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.Number && id.TryGetInt64(out var longId))
                                ids.Add(longId);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi trích xuất record IDs: {ex.Message}");
            }

            return ids.Distinct();
        }
    }
}

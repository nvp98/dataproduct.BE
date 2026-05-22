namespace dataproduct.api.Utils;

/// <summary>
/// Cấu hình hiển thị trạng thái phiếu (text + color) dùng chung cho BE và có thể trả về cho FE.
/// </summary>
public static class PhieuStatusDisplay
{
    public static readonly IReadOnlyDictionary<int, (string Text, string Color)> Config = new Dictionary<int, (string, string)>
    {
        { 0, ("Đang lưu", "purple") },
        { 1, ("Đã gửi", "pink") },
        { 2, ("Hoàn thành", "blue") },
        { 3, ("Đã thu hồi", "tomato") },
        { 4, ("Không xác nhận", "yellow") },
        { 5, ("Chốt", "green") },
        { 6, ("Đang phê duyệt", "gray") },
        { 7, ("Hiệu chỉnh", "orange") },
    };

    public static string GetText(int? tinhTrang)
    {
        if (tinhTrang == null || !Config.TryGetValue(tinhTrang.Value, out var pair))
            return tinhTrang?.ToString() ?? "";
        return pair.Text;
    }

    public static string GetColor(int? tinhTrang)
    {
        if (tinhTrang == null || !Config.TryGetValue(tinhTrang.Value, out var pair))
            return "default";
        return pair.Color;
    }
}

using System.Text.RegularExpressions;

namespace dataproduct.api.Utils
{
    public static class HtmlTemplateHelper
    {
        // Caching để tránh đọc file nhiều lần
        private static readonly Dictionary<string, (string, DateTime)> _cache = new();

        /// <summary>
        /// Đọc BM code, ngày hiệu lực, lần sửa đổi từ phần header-right của HTML template.
        /// Trả về chuỗi đã format sẵn để gán vào cell Excel:
        /// "BM.08/QT.05.13\nNgày hiệu lực: 15/04/2023\nLần sửa đổi: 01"
        /// </summary>
        public static async Task<string> GetBmHeaderTextAsync(string htmlFilePath)
        {
            var (maBm, ngayHieuLuc, lanSuaDoi) = await ParseBmHeaderAsync(htmlFilePath);
            return $"{maBm}\nNgày hiệu lực: {ngayHieuLuc}\nLần sửa đổi: {lanSuaDoi}";
        }

        public static async Task<(string MaBm, string NgayHieuLuc, string LanSuaDoi)> ParseBmHeaderAsync(string htmlFilePath)
        {
            if (!File.Exists(htmlFilePath))
                return ("", "", "");

            // Cache theo (path, last-write-time) để tự động refresh khi template thay đổi
            var lastWrite = File.GetLastWriteTime(htmlFilePath);
            var key = htmlFilePath;
            if (_cache.TryGetValue(key, out var cached) && cached.Item2 == lastWrite)
            {
                var parts = cached.Item1.Split('\n');
                return parts.Length >= 3 ? (parts[0], parts[1], parts[2]) : ("", "", "");
            }

            var html = await File.ReadAllTextAsync(htmlFilePath);

            // BM code: <b>BM.XX/QT.XX.XX</b>  (dòng đầu tiên trong header-right)
            var maBm = Regex.Match(html, @"<b>(BM\.[^<\s]+)</b>", RegexOptions.IgnoreCase)
                            .Groups[1].Value.Trim();

            // Ngày hiệu lực: Ngày hiệu lực:</i>\s*DD/MM/YYYY
            var ngayHieuLuc = Regex.Match(html, @"Ngày hiệu lực:\s*</i>\s*([\d/]+)", RegexOptions.IgnoreCase)
                                   .Groups[1].Value.Trim();
            if (string.IsNullOrEmpty(ngayHieuLuc))
                ngayHieuLuc = Regex.Match(html, @"Ngày hiệu lực:[^>]*>\s*([\d/]+)")
                                   .Groups[1].Value.Trim();

            // Lần sửa đổi: Lần sửa đổi:</i>\s*NN
            var lanSuaDoi = Regex.Match(html, @"Lần sửa đổi:\s*</i>\s*(\w+)", RegexOptions.IgnoreCase)
                                 .Groups[1].Value.Trim();
            if (string.IsNullOrEmpty(lanSuaDoi))
                lanSuaDoi = Regex.Match(html, @"Lần sửa đổi:[^>]*>\s*(\w+)")
                                 .Groups[1].Value.Trim();

            _cache[key] = ($"{maBm}\n{ngayHieuLuc}\n{lanSuaDoi}", lastWrite);
            return (maBm, ngayHieuLuc, lanSuaDoi);
        }
    }
}

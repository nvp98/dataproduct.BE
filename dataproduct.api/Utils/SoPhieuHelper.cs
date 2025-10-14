using dataproduct.api.Models;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Utils
{
    public class SoPhieuHelper
    {
        public static async Task<string> GenerateAutoSoPhieu(ProductFormContext context, string prefix = "BBGN")
        {
            var today = DateTime.Now.ToString("yyyyMMdd");

            var lastPhieu = await context.BmPhieus
                .Where(x => x.SoPhieu.StartsWith($"{prefix}-{today}"))
                .OrderByDescending(x => x.SoPhieu)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastPhieu != null)
            {
                var parts = lastPhieu.SoPhieu.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int current))
                    nextNumber = current + 1;
            }

            return $"{prefix}-{today}-{nextNumber:D3}";
        }
    }
}

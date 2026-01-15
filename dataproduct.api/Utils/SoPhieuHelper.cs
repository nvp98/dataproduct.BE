using dataproduct.api.Models;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Utils
{
    public class SoPhieuHelper
    {
        public static async Task<string> GenerateAutoSoPhieu(ProductFormContext context, string prefix, int scope, int ca, DateOnly? ngaySX)
        {
            string today = ngaySX.Value.ToString("yyyyMMdd");
            string caStr = ca == 1 ? "N" : "D";
            if(scope > 0){
                return $"{prefix}-{today}-{caStr}-{scope}";
            }else{
                return $"{prefix}-{today}-{caStr}";
            }
        }
    }
}

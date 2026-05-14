namespace dataproduct.api.Utils
{
    /// <summary>
    /// Helper class for time/date calculations
    /// </summary>
    public static class ThoiGianHelper
    {
        /// <summary>
        /// Tính toán thời gian ca kíp
        /// </summary>
        /// <param name="ngaySX">Ngày sản xuất</param>
        /// <param name="ca">Số ca (1=08h-20h, 2=20h-08h (next day))</param>
        /// <returns>Tuple (TuGio, DenGio, TuNgay, DenNgay)</returns>
        public static (string TuGio, string DenGio, string TuNgay, string DenNgay) CalculateCaKipTime(DateOnly? ngaySX, int? ca)
        {
            string tuGio = "", denGio = "", tuNgay = "", denNgay = "";

            if (ngaySX.HasValue && ca.HasValue)
            {
                DateOnly ngayBatDau = ngaySX.Value;
                DateOnly ngayKetThuc = ngaySX.Value;

                switch (ca.Value)
                {
                    case 1: // Ca 1: 08h - 20h
                        tuGio = "08";
                        denGio = "20";
                        break;
                    case 2: // Ca 2: 20h - 08h (hôm sau)
                        tuGio = "20";
                        denGio = "08";
                        ngayKetThuc = ngaySX.Value.AddDays(1);
                        break;
                    default:
                        tuGio = "";
                        denGio = "";
                        break;
                }

                tuNgay = ngayBatDau.ToString("dd/MM/yyyy");
                denNgay = ngayKetThuc.ToString("dd/MM/yyyy");
            }

            return (tuGio, denGio, tuNgay, denNgay);
        }
    }
}

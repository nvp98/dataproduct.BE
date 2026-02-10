using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.DTOs.CTD_Dto
{
    [Keyless]
    public class SanLuongPhoiDto
    {
        public string KipNgay { get; set; }

        public string MacThep { get; set; }
        public string KichThuoc { get; set; }

        public int StLoai1 { get; set; }
        public double KlLoai1 { get; set; }

        public int StPhoiNgan { get; set; }
        public double KlPhoiNgan { get; set; }

        public int StLoai2 { get; set; }
        public double KlLoai2 { get; set; }

        public int StLoai3 { get; set; }
        public double KlLoai3 { get; set; }

        public int TongSoThanh { get; set; }
        public double TongKhoiLuong { get; set; }
    }
    public class SaveSanLuongPhoiDto
    {
        public Guid IdPhieu { get; set; }
        public string SoPhieu { get; set; }

        public DateTime NgaySX { get; set; }
        public string Kip { get; set; }
        public int Ca { get; set; }
        public int MayDuc { get; set; }

        public List<InsertSanLuongPhoiDto> Table1 { get; set; }
    }
    public class InsertSanLuongPhoiDto
    {
        public string? KipNgay { get; set; }

        public string? MacThep { get; set; }
        public string? KichThuoc { get; set; }

        public int? StLoai1 { get; set; }
        public decimal? KlLoai1 { get; set; }

        public int? StPhoiNgan { get; set; }
        public decimal? KlPhoiNgan { get; set; }

        public int? StLoai2 { get; set; }
        public decimal? KlLoai2 { get; set; }

        public int? StLoai3 { get; set; }
        public decimal? KlLoai3 { get; set; }

        public int? TongSoThanh { get; set; }
        public decimal? TongKhoiLuong { get; set; }
    }
}

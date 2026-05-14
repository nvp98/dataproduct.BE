using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

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
        public bool? TTHD { get; set; }
    }

    public class BmSanLuongPhoiRow
    {
        public string? SoPhieu { get; set; }
        public DateOnly? NgaySX { get; set; }

        public string? Kip { get; set; }
        public int? Ca { get; set; }

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

        public int? TinhTrang { get; set; }
    }

    public class BmPhieuSLPJson
    {
        public int? ca { get; set; }
        public string? kip { get; set; }
        public DateOnly? NgaySX { get; set; }
        public int? mayduc { get; set; }
        public int? TinhTrang { get; set; }
        public List<Table1SLPRow>? table1 { get; set; }
    }
    public class Table1SLPRow
    {
        public string? kipNgay { get; set; }

        [JsonPropertyName("macThep")]
        public string? macThep { get; set; }

        public string? kichThuoc { get; set; }

        public int? stLoai1 { get; set; }
        public decimal? klLoai1 { get; set; }

        public int? stPhoiNgan { get; set; }
        public decimal? klPhoiNgan { get; set; }

        public int? stLoai2 { get; set; }
        public decimal? klLoai2 { get; set; }

        public int? stLoai3 { get; set; }
        public decimal? klLoai3 { get; set; }

        public int? tongSoThanh { get; set; }
        public decimal? tongKhoiLuong { get; set; }
    }
}

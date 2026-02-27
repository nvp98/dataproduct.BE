using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace dataproduct.api.DTOs.CTD_Dto
{
    [Keyless]
    public class PhoinhapkhoDto
    {
        public string Me { get; set; }

        public string Mac { get; set; }
        public string KichThuoc { get; set; }
        
        public int StLoai1 { get; set; }
        public double KlLoai1 { get; set; }

        public int StPhoiNgan { get; set; }
        public double KlPhoiNgan { get; set; }
        public double CdPhoiNgan { get; set; }

        public int StLoai2 { get; set; }
        public double KlLoai2 { get; set; }

        public int stLoai2tp { get; set; }
        public double klLoai2tp { get; set; }

        public int StLoai3 { get; set; }
        public double KlLoai3 { get; set; }

        public int TongSoThanh { get; set; }
        public double TongKhoiLuong { get; set; }

    }
    public class SavePhoiNhapKhoDto
    {
        public Guid IdPhieu { get; set; }
        public string SoPhieu { get; set; }

        public DateTime NgaySX { get; set; }
        public string Kip { get; set; }
        public int Ca { get; set; }
        public int MayDuc { get; set; }

        public List<InsertPhoiNhapKhoDto> Table1 { get; set; }
    }
    public class InsertPhoiNhapKhoDto
    {
        public string? Me { get; set; } = null!;
        public string? Mac { get; set; } = null!;
        public string? KichThuoc { get; set; } = null!;

        public int? StLoai1 { get; set; }
        public decimal? KlLoai1 { get; set; }

        public int? StPhoiNgan { get; set; }
        public decimal? KlPhoiNgan { get; set; }
        public decimal? CdPhoiNgan { get; set; }

        public int? StLoai2 { get; set; }
        public decimal? KlLoai2 { get; set; }

        public int? StLoai2TP { get; set; }
        public decimal? KlLoai2TP { get; set; }

        public int? StLoai3 { get; set; }
        public decimal? KlLoai3 { get; set; }

        public int? TongSoThanh { get; set; }
        public decimal? TongKhoiLuong { get; set; }
    }

    public class PhoiNhapKhoPdfDTOReq
    {
        public Guid IdPhieu { get; set; }
        public string SoPhieu { get; set; } = string.Empty;

        public DateTime NgaySX { get; set; }
        public int Ca { get; set; }
        public string Kip { get; set; } = string.Empty;
        public int MayDuc { get; set; }
        public List<InsertPhoiNhapKhoDto>? Rows { get; set; }
        public List<PheDuyetDto>? listNguoiPheDuyet { get; set; }
        public PheDuyetDto? XuongDuc { get; set; }
        public PheDuyetDto? QLCL { get; set; }
        public PheDuyetDto? KhoPhoi { get; set; }

    }
}

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
    public class InsertPhoiNhapKhoDto
    {
        public string? SoPhieu { get; set; } = null!;
        public DateTime NgaySX { get; set; }
        public int? Ca { get; set; }
        public string? Kip { get; set; } = null!;
        public int? MayDuc { get; set; }
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

    public class InsertPhoiNhapKhoRequest
    {
        public Guid IdPhieu { get; set; }
        public string SoPhieu { get; set; } = string.Empty;
        public DateTime NgaySX { get; set; }
        public int Ca { get; set; }
        public string Kip { get; set; } = string.Empty;
        public int MayDuc { get; set; }
        public int? NguoiTaoId { get; set; }
        public List<InsertPhoiNhapKhoDto> Table1 { get; set; } = new();
    }

    public class PhoiNhapKhoListItemDto
    {
        public int Id { get; set; }
        public Guid IdPhieu { get; set; }
        public string SoPhieu { get; set; } = string.Empty;
        public DateTime NgaySX { get; set; }
        public string Kip { get; set; } = string.Empty;
        public int Ca { get; set; }
        public int MayDuc { get; set; }
        public string Me { get; set; } = string.Empty;
        public string Mac { get; set; } = string.Empty;
        public string KichThuoc { get; set; } = string.Empty;
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
        public bool? TTHD { get; set; }
        public DateTime ThoiGianTao { get; set; }
        public DateTime? NgayGiao { get; set; }
        public int? TinhTrangCap0 { get; set; }
        public int? ID_NguoiCap0 { get; set; }
        public int? TinhTrangCap1 { get; set; }
        public int? ID_NguoiCap1 { get; set; }
        public int? TinhTrangCap2 { get; set; }
        public int? ID_NguoiCap2 { get; set; }
        public int? TinhTrang { get; set; }
        public int? ID_Chot { get; set; }
    }

    public class ThuHoiPhoiNhapKhoRequest
    {
        public List<int> Ids { get; set; } = new();
    }

    public class ChotPhoiNhapKhoRequest
    {
        public Guid IdPhieu { get; set; }
        public int? TinhTrangChot { get; set; } = 1; // 1 = chốt, 0 = hủy chốt
    }

    public class XacNhanPhoiNhapKhoRequest
    {
        public List<int> Ids { get; set; } = new();
        public int NguoiXacNhanId { get; set; }
        public int CapXacNhan { get; set; }
        public int? TinhTrangCap { get; set; }
        public Guid PhieuId { get; set; }
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
    public class BmPhieuExportRow
    {
        public string? SoPhieu { get; set; }
        public DateOnly? NgaySX { get; set; }
        public int? MayDuc { get; set; }
        public string? Kip { get; set; }
        public int? Ca { get; set; }

        public string? Me { get; set; }
        public string? Mac { get; set; }
        public string? KichThuoc { get; set; }

        public int? StLoai1 { get; set; }
        public decimal? KlLoai1 { get; set; }

        public int? StLoai1_BK { get; set; }
        public decimal? KlLoai1_BK { get; set; }

        public int? StLoai1_Lan2 { get; set; }
        public decimal? KlLoai1_Lan2 { get; set; }

        public int? StLoai2 { get; set; }
        public decimal? KlLoai2 { get; set; }

        public int? StLoai2_Lan2 { get; set; }
        public decimal? KlLoai2_Lan2 { get; set; }

        public int? StLoai2_BK { get; set; }
        public decimal? KlLoai2_BK { get; set; }

        public int? StLoai2tp { get; set; }
        public decimal? KlLoai2tp { get; set; }

        public int? StLoai2tp_Lan2 { get; set; }
        public decimal? KlLoai2tp_Lan2 { get; set; }

        public int? StLoai2tp_BK { get; set; }
        public decimal? KlLoai2tp_BK { get; set; }


        public int? StPhoiNgan { get; set; }
        public decimal? CdPhoiNgan { get; set; }
        public decimal? KlPhoiNgan { get; set; }

        public int? StPhoiNgan_Lan2 { get; set; }
        public decimal? CdPhoiNgan_Lan2 { get; set; }
        public decimal? KlPhoiNgan_Lan2 { get; set; }

        public int? StPhoiNgan_BK { get; set; }
        public decimal? CdPhoiNgan_BK { get; set; }
        public decimal? KlPhoiNgan_BK { get; set; }

        public int? StLoai3 { get; set; }
        public decimal? KlLoai3 { get; set; }

        public int? StLoai3_Lan2 { get; set; }
        public decimal? KlLoai3_Lan2 { get; set; }
        public int? StLoai3_BK { get; set; }
        public decimal? KlLoai3_BK { get; set; }

        public int? TongSoThanh { get; set; }
        public decimal? TongKhoiLuong { get; set; }

        public int? TongSoThanh_Lan2 { get; set; }
        public decimal? TongKhoiLuong_Lan2 { get; set; }
        public int? TongSoThanh_BK { get; set; }
        public decimal? TongKhoiLuong_BK { get; set; }
        public int? TinhTrang { get; set; }
        public int? TinhTrang_HRC { get; set; }
        public int? TinhTrang_QLCL { get; set; }
        public int? TinhTrang_Chot { get; set; }

        public int? TinhTrang_HRC2 { get; set; }
        public int? TinhTrang_QLCL2 { get; set; }
        public int? TinhTrang_Chot2 { get; set; }

    }
    public class BmPhieuJson
    {
        public int? ca { get; set; }
        public string? kip { get; set; }
        public DateOnly? NgaySX { get; set; }
        public int? mayduc { get; set; }
        public int? TinhTrang { get; set; }
        public List<Table1Row>? table1 { get; set; }
    }

    public class Table1Row
    {
        public string? me { get; set; }
        public string? mac { get; set; }
        public string? kichThuoc { get; set; }

        public int? stLoai1 { get; set; }
        public decimal? klLoai1 { get; set; }

        public int? stLoai2 { get; set; }
        public decimal? klLoai2 { get; set; }

        public int? stLoai2tp { get; set; }
        public decimal? klLoai2tp { get; set; }

        public int? stPhoiNgan { get; set; }
        public decimal? cdPhoiNgan { get; set; }
        public decimal? klPhoiNgan { get; set; }

        public int? stLoai3 { get; set; }
        public decimal? klLoai3 { get; set; }

        public int? tongSoThanh { get; set; }
        public decimal? tongKhoiLuong { get; set; }
    }

    public class PhoinhapkhoNhanPhoiDto
    {
        public string Me { get; set; }

        public string Mac { get; set; }
        public string KichThuoc { get; set; }

        public DateOnly NgaySX { get; set; }
        public int Ca { get; set; }
        public int StLoai1 { get; set; }
        public double KlLoai1 { get; set; }
        public int? StDachuyenLoai1 { get; set; }

        public int StPhoiNgan { get; set; }
        public double KlPhoiNgan { get; set; }
        public double CdPhoiNgan { get; set; }
        public int? StDachuyenPhoiNgan { get; set; }

        public int StLoai2 { get; set; }
        public double KlLoai2 { get; set; }
        public int? StDachuyenLoai2 { get; set; }

        public int stLoai2tp { get; set; }
        public double klLoai2tp { get; set; }
        public int? StDachuyenLoai2tp { get; set; }

        public int StLoai3 { get; set; }
        public double KlLoai3 { get; set; }
        public int? StDachuyenLoai3 { get; set; }

        public int TongSoThanh { get; set; }
        public int? TongST_DaChuyen { get; set; }
        public double TongKhoiLuong { get; set; }
        [NotMapped]
        public bool? isCaTruoc { get; set; } = false;
        [NotMapped]
        public int? ST_CaTruocChuyen { get; set; }
        [NotMapped]
        public int? ST_NhapTrongCa { get; set; }
        [NotMapped]
        public int? ST_CaSauChuyen { get; set; }

        [NotMapped]
        public int? TinhTrang_Chuyen { get; set; }

    }


}

using System.ComponentModel.DataAnnotations.Schema;

namespace dataproduct.api.DTOs
{
    public class DLNM_HRC2SearchDto
    {
        public DateTime? NgaySX { get; set; }
        public int? Ca { get; set; }
        public string? LoaiBM { get; set; }
        public int? Scope { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class DLNM_HRC2GroupedDto
    {
        public int Id { get; set; }
        public int REPORT_NO { get; set; }
        public DateTime? NgaySx { get; set; }
        public DateTime? Ngay { get; set; }
        public int Ca { get; set; }
        public string? BieuMau { get; set; }
        public int? Scope { get; set; }
        public string? MeThoi { get; set; }
        public string? MacThep { get; set; }
        public double? O2 { get; set; }
        public double? AR_RH { get; set; }
        public double? N2 { get; set; }
        public double? AR_BOF { get; set; }
        public double? AR_LF { get; set; }
        public double? KLGangLong { get; set; }
        public double? KLThepPhe { get; set; }
        public double? KlThepLong { get; set; }
    }

    public class ChuyenMeThoiRequest
    {
        public string MaBM { get; set; }
        public DateOnly NgaySX { get; set; }
        public int Ca { get; set; }
        public int Scope { get; set; }
        public int ChuyenToiCa { get; set; }
        public string MeThoi { get; set; }
        public string BieuMau { get; set; }
    }

    public class HRC2InsertModel
    {
        public int? Id { get; set; }
        public DateTime Ngay { get; set; }
        public int Ca { get; set; }
        public string BieuMau { get; set; }
        public int Scope { get; set; }

        public string MeThoi { get; set; }
        public string MacThep { get; set; }

        public double? O2 { get; set; }
        public double? AR_RH { get; set; }
        public double? AR_LF { get; set; }
        public double? AR_BOF { get; set; }
        public double? N2 { get; set; }

        public double? KLGangLong { get; set; }
        public double? KLThepPhe { get; set; }
        public double? KlThepLong { get; set; }

        public bool IsNM { get; set; }
        public bool IsChuyenCa { get; set; }
        [NotMapped]
        public Guid RowKey { get; set; } = Guid.NewGuid();

        public List<HRC2_PhuLieuInSertModel> hRC2_PhuLieus { get; set; } = new List<HRC2_PhuLieuInSertModel>();
    }
    public class HRC2_PhuLieuInSertModel
    {
        public string MeThoi { get; set; }
        public string BieuMau { get; set; }
        public int? ID_PhuLieu { get; set; }
        public string TenPhuLieu { get; set; }
        public double? KLPhuGia { get; set; }

        public int? ID_HeaderKey { get; set; }
        public string TenHienThi { get; set; }
        public bool? IsPhanBo { get; set; } = false; // ⭐ Đánh dấu là phân bổ
    }

    public class FilterSTD_NXTRequest
    {
        public DateTime NgaySX { get; set; }
        public int Ca { get; set; }
    }
}


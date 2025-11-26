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
    }

    public class ChuyenMeThoiRequest
    {
        public string MeThoi { get; set; }
    }
}


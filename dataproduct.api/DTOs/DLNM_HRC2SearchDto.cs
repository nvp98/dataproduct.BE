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
}


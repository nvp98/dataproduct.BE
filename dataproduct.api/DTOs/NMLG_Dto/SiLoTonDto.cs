namespace dataproduct.api.DTOs.NMLG_Dto
{
    // DTO cho Lò Cao 1-4 (sp_GetSiLoTonLG1)

    public class SiLoTonRequest
    {
        public int? IDLoCao { get; set; }
        public int? IDCa { get; set; }
        public DateTime? Ngay { get; set; }
    }
    public class SiLoTonDto
    {
        public int? ID { get; set; }
        public int? IdLoCao { get; set; }
        public int? IDSiLo { get; set; }
        public decimal? Ton { get; set; }
        public DateTime? Ngay { get; set; }
        public int? IDNguon { get; set; }

        // Từ bảng SiLo_LG
        public int? ID_LoCao { get; set; }
        public string? TenSiLo { get; set; }
        public  int? ThuTu { get; set; }
        public string? TenNL { get; set; }
        public string? TenNL_DieuChinh { get; set; }
    }

}

using dataproduct.api.Models;

namespace dataproduct.api.ResponseModels
{
    public class STD_NXT_HRC1_UpsertResponse
    {
        public Guid Id_Phieu { get; set; }
    }

    public class STD_NXT_HRC1_GetDetailResponse
    {
        public Guid Id_Phieu { get; set; }
        public string? BieuMau { get; set; }
        public DateTime NgaySX { get; set; }
        public int Ca { get; set; }
        public List<NXTDetailResponseModel_HRC1> Details { get; set; } = new();
        public List<NXTSummaryResponseModel_HRC1> Summary { get; set; } = new();
    }

    public class NXTDetailResponseModel_HRC1
    {
        public int Scope { get; set; }
        public string? BieuMau { get; set; }
        public int PhuLieuID { get; set; }
        public string? TenNguyenLieu { get; set; }
        public int? ViTri { get; set; }
        public int? IDSilo { get; set; }
        public string? TenSilo { get; set; }
        public decimal? TonDauCa { get; set; }
        public string? TuongQuanDauCa { get; set; }
        public decimal? NhapVaoTrongCa { get; set; }
        public decimal? MucLieu { get; set; }
        public decimal? TheTich { get; set; }
        public decimal? TyTrong { get; set; }
        public decimal? TonCuoiCa { get; set; }
        public string? TuongQuanCuoiCa { get; set; }
        public decimal? TongThucTe { get; set; }
        public decimal? LuongSuDungKiemKe { get; set; }
    }

    public class NXTSummaryResponseModel_HRC1
    {
        public int PhuLieuID { get; set; }
        public string? TenNguyenLieu { get; set; }
        public decimal? TongTonDauCa { get; set; }
        public decimal? TongTonNhapTrongCa { get; set; }
        public decimal? TongTonCuoiCa { get; set; }
        public decimal? TongSuDung { get; set; }
        public decimal? TongSDTrenSoSach { get; set; }
        public decimal? ChenhLech { get; set; }
        public bool? HasPhanBo { get; set; }
        public decimal? TyLeBOF { get; set; }
        public decimal? TyLeLF { get; set; }
        public decimal? KLPB_BOF { get; set; }
        public decimal? KLPB_LF { get; set; }
    }
}

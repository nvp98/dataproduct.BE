namespace dataproduct.api.DTOs
{
    public class Hrc2SlabSearchRequest
    {
        public string? TuNgay { get; set; }
        public string? DenNgay { get; set; }
        public string? CaSanXuat { get; set; }
        public string? Kip { get; set; }
        public int? MayDuc { get; set; }
        public string? MeThep { get; set; }
        public List<string>? IdSlabs { get; set; }
        public string? MacThep { get; set; }
        public bool? IsChot { get; set; }
        public bool? IsTrungIDSlab { get; set; }
        public bool? IsDiffMacThep { get; set; }
        public bool? IsSaiLotName { get; set; }
        public int? TrangThaiKCS { get; set; }
        public int? TrangThaiDuc { get; set; }
        public int? TrangThaiKho { get; set; }
        public int? TrangThaiPKH { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
    public class Hrc2SlabItem
    {
        public int Id { get; set; }
        public int? BkmisId { get; set; }
        public string? NgaySanXuat { get; set; }
        public string? ShiftName { get; set; }
        public string? CaSanXuat { get; set; }
        public string? KipSanXuat { get; set; }
        public string? MeThep { get; set; }
        public string? IdSlab { get; set; }
        public string? MacThep { get; set; }
        public string? ChatLuong { get; set; }
        public decimal? ChieuDay { get; set; }
        public decimal? ChieuRong { get; set; }
        public decimal? ChieuDai { get; set; }
        public decimal? KhoiLuong { get; set; }
        public decimal? KhoiLuongTinhToan { get; set; }
        public string? ChatLuongTPHH { get; set; }
        public string? ThongTinPhoi { get; set; }
        public string? TpKhongDatGangLong { get; set; }
        public string? GhiChu { get; set; }
        public string? LoaiPhoi { get; set; }
        public string? SapCode { get; set; }
        public string? SapDescription { get; set; }
        public string? SoLo { get; set; }
        public string? OrderId { get; set; }
        public int? MayDuc { get; set; }
        public bool? IsTrungIDSlab { get; set; }
        public bool? IsDiffMacThep { get; set; }
        public bool IsSaiLotName { get; set; }
        public int? Line { get; set; }
        public DateOnly? SapLastTime { get; set; }
        public bool IsChot { get; set; }
        public DateTime? NgayTao { get; set; }
        public string? PhanLoai { get; set; }
        // Thông tin phiếu
        public string? NgayXuLy { get; set; }
        public int? CaBBSL { get; set; }
        public string? KipBBSL { get; set; }
        public string? IdPhieuBBSL { get; set; }
        public string? SoPhieuBBSL { get; set; }
        // Workflow
        public int TrangThaiKCS { get; set; }
        public int TrangThaiDuc { get; set; }
        public int TrangThaiKho { get; set; }
        public int TrangThaiPKH { get; set; }
        // Người xử lý từng bước (HoVaTen, resolve từ NguoiChuyenKCS/NguoiXacNhanDuc/NguoiXacNhanKho/NguoiChotPKH)
        public string? NguoiChuyenBBSL { get; set; }
        public string? NguoiXacNhanDuc { get; set; }
        public string? NguoiXacNhanKho { get; set; }
        public string? NguoiXacNhanPKH { get; set; }
    }

    public class Hrc2SlabTongHopItem
    {
        public string? MeThep { get; set; }
        public string? MacThep { get; set; }
        public decimal? ChieuDay { get; set; }
        public decimal? ChieuRong { get; set; }
        public decimal? ChieuDai { get; set; }
        public string? LoaiPhoi { get; set; }
        public string? ChatLuongTPHH { get; set; }
        public string? PhanLoai { get; set; }
        public int SoLuong { get; set; }
        public decimal? TongKhoiLuong { get; set; }
    }

    public class Hrc2PhieuBBSLItem
    {
        public Guid IdPhieu { get; set; }
        public string? SoPhieu { get; set; }
        public DateOnly? NgaySX { get; set; }
        public int? Ca { get; set; }
        public string? Kip { get; set; }
        public int? TinhTrang { get; set; }
        public int SoSlabDaChot { get; set; }
        public int SoSlabKCS { get; set; }
        public int SoSlabDuc { get; set; }
        public int SoSlabKho { get; set; }
        public int SoSlabPKH { get; set; }
    }

    public class Hrc2XacNhanRequest
    {
        public List<int> IdSlabs { get; set; } = [];
        public string LoaiXacNhan { get; set; } = "";
        public int NguoiThucHien { get; set; }
    }

    public class Hrc2ChotPhieuRequest
    {
        public Guid IdPhieu { get; set; }
        public int NguoiThucHien { get; set; }
    }

    public class Hrc2ChuyenBbslRequest
    {
        public List<int> IdSlabs { get; set; } = [];
        public Guid IdPhieu { get; set; }
        public int NguoiThucHien { get; set; }
    }

}

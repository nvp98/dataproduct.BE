namespace dataproduct.api.DTOs.Export
{
    /// <summary>Thông tin kíp / lò để render header dòng 4-5 biên bản BOF.</summary>
    public class ExportBOFRequest
    {
        /// <summary>Số lò, ví dụ "1", "2".</summary>
        public string SoLo { get; set; } = "";
        public DateOnly NgaySX { get; set; }
        public int Ca { get; set; }
        /// <summary>Giờ bắt đầu kíp, ví dụ "06:00".</summary>
        public string GioBatDau { get; set; } = "";
        /// <summary>Giờ kết thúc kíp, ví dụ "18:00".</summary>
        public string GioKetThuc { get; set; } = "";
    }

    /// <summary>Cấu hình một cột HeaderKey động trong file Excel BOF.</summary>
    public class HeaderKeyConfig
    {
        public int Id { get; set; }
        public string TenHienThi { get; set; } = "";
        public int? ThuTu_Excel_BOF { get; set; }
    }

    /// <summary>Dữ liệu một mẻ luyện để render dòng data.</summary>
    public class MeLuyenModel
    {
        public string? MeNauSo { get; set; }
        public string? MacThep { get; set; }
        public double? KLGangLongCCT { get; set; }
        public double? KLThepPhe { get; set; }
        /// <summary>Khối lượng từng phụ gia, key = HeaderKey.Id.</summary>
        public Dictionary<int, double?> PhuGia { get; set; } = new();
        public double? O2 { get; set; }
        public double? N2 { get; set; }
        public string? GhiChu { get; set; }
    }

    /// <summary>Một dòng lượng tồn (Vôi, Dolomite, Quặng, FeSi...) render sau Tổng cộng.</summary>
    public class LuongTonModel
    {
        public string TenLuongTon { get; set; } = "";
        public double? GiaTri { get; set; }
    }
}

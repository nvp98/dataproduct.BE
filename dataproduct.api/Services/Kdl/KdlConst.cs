namespace dataproduct.api.Services.Kdl
{
    /// <summary>Mã enum của Kho dữ liệu động — khớp CHECK constraint trong migrations/kdl_dong.sql.</summary>
    public static class KdlConst
    {
        public static class KieuDL
        {
            public const byte So = 1;
            public const byte Chuoi = 2;
        }

        public static class CachLay
        {
            /// <summary>Mặc định: đọc giá trị theo MaKey do nguồn đẩy về.</summary>
            public const byte DuLieu = 1;
            /// <summary>Tính từ key khác lúc đồng bộ / lưu, rồi lưu lại (Nguon = Tinh).</summary>
            public const byte CongThuc = 2;
            /// <summary>Tính lúc xem, sau khi tổng hợp (tỷ lệ) — không lưu.</summary>
            public const byte TinhKhiXem = 3;
            public const byte NhapTay = 4;
            /// <summary>Handler C# (IKdlXuLyRieng) theo TenXuLy.</summary>
            public const byte XuLyRieng = 5;
        }

        public static class KieuTongHop
        {
            public const byte Sum = 1;
            public const byte Ratio = 2;
            public const byte Last = 3;
            public const byte Avg = 4;
        }

        public static class Nguon
        {
            public const byte TuDong = 1;
            public const byte Tinh = 2;
            public const byte NhapTay = 3;
            public const byte DieuChinh = 4;
        }

        public const string NguonChayTuDong = "auto";

        /// <summary>Trường header (KDL_DoiTuong) mà cột form được tham chiếu bằng "field".</summary>
        public static readonly string[] TruongDoiTuong =
            ["STT", "Me", "NgaySX", "CaSX", "Kip", "MayDuc", "Mac", "KichThuoc", "LenhSanXuat"];
    }
}

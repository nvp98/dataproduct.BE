using Microsoft.EntityFrameworkCore;

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

        public int StLoai2 { get; set; }
        public double KlLoai2 { get; set; }

        public int stLoai2tp { get; set; }
        public double klLoai2tp { get; set; }

        public int StLoai3 { get; set; }
        public double KlLoai3 { get; set; }

        public int TongSoThanh { get; set; }
        public double TongKhoiLuong { get; set; }
    }
}

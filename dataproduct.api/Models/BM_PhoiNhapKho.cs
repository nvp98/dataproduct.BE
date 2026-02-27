using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace dataproduct.api.Models
{
    public class BM_PhoiNhapKho
    {
        [Key]
        public int Id { get; set; }

        public Guid IdPhieu { get; set; }
        public string SoPhieu { get; set; }

        public DateTime NgaySX { get; set; }
        public string Kip { get; set; }
        public int Ca { get; set; }
        public int MayDuc { get; set; }
        public string Me { get; set; }
        public string Mac{ get; set; }
        public string KichThuoc { get; set; }

        public int? StLoai1 { get; set; }
        [Column(TypeName = "decimal(10,3)")]
        public decimal? KlLoai1 { get; set; }

        public int? StPhoiNgan { get; set; }
        [Column(TypeName = "decimal(10,3)")]
        public decimal? KlPhoiNgan { get; set; }
        [Column(TypeName = "decimal(6,2)")]
        public decimal? CdPhoiNgan { get; set; }

        public int? StLoai2 { get; set; }
        [Column(TypeName = "decimal(10,3)")]
        public decimal? KlLoai2 { get; set; }

        public int? StLoai2TP { get; set; }

        [Column(TypeName = "decimal(10,3)")]
        public decimal? KlLoai2TP { get; set; }

        public int? StLoai3 { get; set; }
        [Column(TypeName = "decimal(10,3)")]
        public decimal? KlLoai3 { get; set; }

        public int? TongSoThanh { get; set; }
        [Column(TypeName = "decimal(10,3)")]
        public decimal? TongKhoiLuong { get; set; }

        public int? NguoiTaoId { get; set; }
        public DateTime ThoiGianTao { get; set; }
    }
}

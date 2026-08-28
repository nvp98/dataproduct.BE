using System.ComponentModel.DataAnnotations;

namespace dataproduct.api.Models
{
    public class TKVV_TonSilo
    {
        [Key]
        public long ID { get; set; }
        public Guid? PhieuID { get; set; }
        public DateOnly NgaySX { get; set; }
        public byte Ca { get; set; }              // 1 = ngày, 2 = đêm
        public string? Kip { get; set; }          // text hiển thị, vd "1B"
        public int? Scope { get; set; }           // 1-6, cùng convention TKVV_BaoCaoSanLuongChiPhi
        public int? ThuTu { get; set; }
        public int SiloID { get; set; }
        public int? NguyenVatLieuID { get; set; } // snapshot NVL hiện đang mapping vào Silo
        public decimal? DoAm { get; set; }
        public string? DoAmText { get; set; }  // nối tất cả DoAm từ BBGN: "12.5, 13.0, 11.8"
        public decimal? TonDau { get; set; }
        public decimal? Nhap { get; set; }
        public decimal? NhapAuto { get; set; } // giá trị tự động từ BBGN (sp_TKVV_Get_NVL_BBGN)
        public decimal? Xuat { get; set; }
        public decimal? XuatAuto { get; set; }
        public decimal? TonCuoi { get; set; }     // = TonDau + Nhap - Xuat, backend tự tính khi save
        public decimal? TonCuoiAuto { get; set; } // giá trị tự động từ nguồn EMS/cảm biến
        public string? GhiChu { get; set; }
        public bool IsAdjusted { get; set; }      // true khi TonCuoi != TonCuoiAuto
        public int? AdjustedBy { get; set; }
        public DateTime? AdjustedDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public bool IsDelete { get; set; }
    }
}

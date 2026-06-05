using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace dataproduct.api.Models;

public class HRC1_MeThep
{
    public int Id { get; set; }

    // Từ DB ngoài
    [MaxLength(30)]
    public string? MaMe { get; set; }

    [MaxLength(20)]
    public string? ThungSo { get; set; }

    public int? LoSo { get; set; }             // lò thổi số 1–5

    [MaxLength(5)]
    public string? ThoiGian { get; set; }      // TL nhập, định dạng HH:mm
    public decimal? KLLFSauThep { get; set; }  // LT nhập
    public decimal? KlLan1 { get; set; }       // TL nhập
    public decimal? KlLan2 { get; set; }       // TL nhập
    public decimal? KlLan3 { get; set; }       // LT nhập
    public decimal? KlThepLong { get; set; }   // Auto: len_thang=(KLLF-KlLan2); tinh_luyen=(KlLan1-KlLan2)

    // Lò thổi chọn đích
    [MaxLength(20)]
    public string? DichChuyen { get; set; }    // tinh_luyen | len_thang
    public int? TLDichSo { get; set; }         // TL được chỉ định (1–5), null nếu lên thẳng
    public int? IdMayDucDich { get; set; }     // FK→MayDuc (dùng cho lên thẳng và TL→đúc)

    public bool? IsThuNghiem { get; set; }
    public bool? IsTrungMeThoi { get; set; }
    public bool? IsGhost { get; set; }
    public bool? IsChot { get; set; }             // kế hoạch chốt mẻ → khóa toàn bộ chỉnh sửa
    public bool? IsManualTL { get; set; }         // true = mẻ do Tinh luyện tạo tay (độc lập, có thể xóa)

    [MaxLength(500)]
    public string? GhiChuLo { get; set; }

    [MaxLength(50)]
    public string? PhanLoai { get; set; }

    [MaxLength(50)]
    public string? MacThep { get; set; }

    [MaxLength(50)]
    public string? MacThepBKMIS { get; set; }

    public int? IdMacThep { get; set; }

    [MaxLength(500)]
    public string? GhiChuTL { get; set; }

    // LT/Đúc: 0=chờ xử lý, 1=đã xác nhận, 2=đã chốt
    // TL:     0=chờ xử lý, 1=đã nhận, 2=đã xác nhận, 3=đã chốt
    public int? TrangThaiLo { get; set; }
    public int? TrangThaiTL { get; set; }
    public int? TrangThaiDuc { get; set; }

    public int? CapNhatBoi { get; set; }       // ID_TaiKhoan (lò thổi cuối)
    public DateTime? CapNhatLuc { get; set; }
    public int? CapNhatBoiTL { get; set; }     // ID_TaiKhoan (tinh luyện cuối)
    public int? CapNhatBoiDuc { get; set; }    // ID_TaiKhoan (máy đúc cuối)

    public DateTime NgayTao { get; set; }      // set by SQL DEFAULT GETDATE() on insert; không thay đổi sau khi tạo
    public DateTime? NgayNhanTL { get; set; }  // ngày TL nhận mẻ (date-only; dùng để lọc ngày cho phiếu máy đúc)
    public int? CaTinhLuyen { get; set; }      // ca khi TL nhận mẻ (1=ngày, 2=đêm); dùng để lọc ca cho phiếu máy đúc

    public int? Ca { get; set; }               // ca của phiếu lò thổi (1=ngày, 2=đêm)

    [MaxLength(1)]
    public string? Kip { get; set; }           // kíp của phiếu lò thổi

    public decimal? KLThepLongPhanBo { get; set; }
}

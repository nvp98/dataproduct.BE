namespace dataproduct.api.Models;

/// <summary>Đánh dấu "đã check" độc lập theo từng user cho 1 slab — không phải trạng thái
/// xác nhận nghiệp vụ (BkHrc2SlabTrangThai), chỉ là marker cá nhân: user A check thì chỉ A thấy,
/// user B check cùng slab thì chỉ B thấy. Không FK cứng tới BkHrc2Slab/Tbl_TaiKhoan — liên kết
/// theo quy ước tên cột, đảm bảo toàn vẹn ở tầng ứng dụng.</summary>
public class BkHrc2Slab_UserCheck
{
    public int IdUser { get; set; }
    public int IdSlab { get; set; }
    public DateTime NgayCheck { get; set; }
}

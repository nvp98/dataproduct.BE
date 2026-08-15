namespace dataproduct.api.Models
{
    // Kết quả trả về từ dbo.EMS_GetMappingTag — đọc EMS_MAPPING_TAG qua linked server
    // SQL_OT.DATA_SANXUAT. Không có bảng vật lý tương ứng trong PRODUCT_FORM, chỉ dùng
    // để map kết quả FromSqlRaw (xem TKVV_BBSLRepository.GetEmsTagListAsync).
    public class EMS_MappingTag
    {
        public int ID { get; set; }
        public string? Xuong { get; set; }
        public string? Loai { get; set; }
        public string? TenNVL { get; set; }
        public string? TenCan { get; set; }
        public string? MaCan { get; set; }
        public string? AddressPLC { get; set; }
        public string? IPPLC { get; set; }
        // "0" = đang tích lũy ca hiện tại (không cố định ngày/đêm — không dùng để Mapping),
        // "1" = tích lũy ca ngày, "2" = tích lũy ca đêm.
        public string? LoaiDuLieu { get; set; }
        public long? TagIDEMS { get; set; }
        public string? TagName { get; set; }
        public string? GhiChu { get; set; }
        public DateTime NgayCapNhat { get; set; }
        public bool IsDelete { get; set; }
    }
}

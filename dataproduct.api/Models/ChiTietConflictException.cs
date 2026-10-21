namespace dataproduct.api.Models
{
    // Dòng chi tiết Phiếu điều chỉnh đã bị người khác sửa/xóa kể từ lúc người dùng tải phiếu —
    // controller trả 409 để FE báo và tải lại thay vì ghi đè mất số liệu của người kia.
    public class ChiTietConflictException : Exception
    {
        public ChiTietConflictException(string message) : base(message) { }
    }
}

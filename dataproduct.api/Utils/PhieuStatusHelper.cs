namespace dataproduct.api.Utils
{
    /// <summary>
    /// Helper class để kiểm tra quy tắc chuyển đổi trạng thái phiếu
    /// </summary>
    public static class PhieuStatusHelper
    {
        /// <summary>
        /// Kiểm tra xem có được phép chuyển từ trạng thái hiện tại sang trạng thái mới không
        /// </summary>
        /// <param name="currentStatus">Trạng thái hiện tại</param>
        /// <param name="newStatus">Trạng thái mới muốn chuyển</param>
        /// <exception cref="Exception">Throw exception nếu không được phép chuyển</exception>
        public static void CheckAllowStatusChange(int currentStatus, int newStatus)
        {
            if (newStatus == 2 && currentStatus == 3) 
                throw new Exception("Phiếu đã bị thu hồi, không thể xác nhận");
            else if (newStatus == 3 && currentStatus == 5) 
                throw new Exception("Phiếu đã được chốt, không thể thu hồi lại");
            else if (newStatus == 1 && currentStatus == 1) 
                throw new Exception("Phiếu đã được gửi đi, không thể gửi lại");
            else if (newStatus == 4 && currentStatus == 3) 
                throw new Exception("Phiếu bị thu hồi, không thể không xác nhận");
            else if (newStatus == 5 && currentStatus != 2) 
                throw new Exception("Phiếu chưa được duyệt, không thể chốt");
            else if (newStatus == 7 && currentStatus == 5) 
                throw new Exception("Phiếu đã được chốt, không thể đề nghị hiệu chỉnh");
            else if (newStatus == 7 && currentStatus != 2 && currentStatus != 6) 
                throw new Exception("Chỉ được đề nghị hiệu chỉnh khi phiếu ở trạng thái Hoàn thành hoặc Đang phê duyệt");
            else if (newStatus == 3 && currentStatus == 2 )
                throw new Exception("Phiếu đã được duyệt, vui lòng load lại và đề nghị hiệu chỉnh");
            // Cho phép: DaGui (1) → DaThuHoi (3) thu hồi để sửa trực tiếp
            // Clone từ 2/6: phiếu gốc chỉ IsLock=1; phiếu clone tạo với TinhTrang=7. Cho phép 7→1 khi Lưu và Gửi.
            // Trạng thái 3 = Đã thu hồi (thu hồi từ Đã gửi, sửa trực tiếp). Trạng thái 7 = Hiệu chỉnh (phiếu clone, Lưu giữ 7, Gửi→1).
        }
    }
}


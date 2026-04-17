Tôi đang xây dựng chức năng đồng bộ dữ liệu mẻ thổi (MaMeThoi) cho hệ thống sản xuất thép với hàm XuLyDuLieuMeThoiGangLongAsync trong BBGN_ThepLongRepository.cs.

Ngữ cảnh hệ thống:

1. Có bảng BBGN_ThepLong:
- Id (PK)
- IdPhieu (int) → liên kết phiếu
- MaMeThoi (string)
- IsGhost (bool) → đánh dấu mẻ không còn trong dữ liệu mới nhưng không được xóa

2. Có bảng Phieu:
- Id
- TinhTrang (int)
    + 5 = đã chốt (LOCK dữ liệu, không được xóa)
    + khác 5 = chưa chốt

3. Input:
- idPhieu (int)
- listNew (List<string>) → danh sách MaMeThoi mới nhất từ nhà máy (API)

=====================================================
YÊU CẦU NGHIỆP VỤ
=====================================================

Viết hàm C# async dùng Entity Framework để đồng bộ dữ liệu giữa:

- listNew (nguồn mới)
- dữ liệu hiện tại trong DB (BBGN_ThepLong theo IdPhieu)

=====================================================
LOGIC CHI TIẾT
=====================================================

Bước 1: Lấy toàn bộ dữ liệu hiện tại trong BBGN_ThepLong theo IdPhieu

Bước 2: Nếu DB chưa có dữ liệu:
→ Insert toàn bộ listNew
→ IsGhost = false

-----------------------------------------------------

Bước 3: Nếu đã có dữ liệu:

Chuyển dữ liệu thành 2 tập:
- currentSet = tập MaMeThoi trong DB
- newSet = tập MaMeThoi từ listNew

-----------------------------------------------------

Bước 4: So sánh dữ liệu:

4.1. Mẻ cần thêm:
toInsert = newSet - currentSet

→ Insert các mẻ này vào DB
→ IsGhost = false

-----------------------------------------------------

4.2. Mẻ bị thiếu:
toRemove = currentSet - newSet

→ Với từng mẻ trong toRemove:

    Nếu Phieu.TinhTrang == 5 (đã chốt):
        - Không xóa
        - Update IsGhost = true

    Nếu chưa chốt:
        - Xóa record khỏi DB

-----------------------------------------------------

4.3. Mẻ vẫn tồn tại (currentSet ∩ newSet):
→ Đảm bảo:
    - Nếu trước đó IsGhost = true thì set lại IsGhost = false

-----------------------------------------------------

=====================================================
YÊU CẦU KỸ THUẬT
=====================================================

1. Sử dụng Entity Framework (async/await)

2. Tối ưu performance:
- Dùng HashSet để so sánh (O(1))
- Không query DB trong loop
- Không SaveChanges nhiều lần

3. Code phải:
- Rõ ràng
- Tách logic dễ hiểu
- Không duplicate query

4. Tránh:
- O(n^2)
- Query DB nhiều lần
- Update không cần thiết

5. Xử lý null / empty list:
- Nếu listNew null hoặc rỗng:
    → coi như toRemove = toàn bộ currentSet

6. Đảm bảo:
- Không insert trùng (IdPhieu + MaMeThoi unique)

=====================================================
OUTPUT
=====================================================

Viết:
- Hàm async hoàn chỉnh
- Có comment rõ từng bước
- Clean code
- Dễ maintain

=====================================================
OPTIONAL (nếu có thể)
=====================================================

- Thêm transaction
- Batch insert/update
- Logging số lượng insert/update/delete
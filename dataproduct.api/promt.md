# NHIỆM VỤ
Tạo file HTML template chuẩn ISO dùng để nhập liệu và xuất PDF qua trình duyệt.
Tên form: "BIÊN BẢN GIAO NHẬN THÉP LỒNG"
Mã biểu mẫu: BM.16/QT.05.10 | Ngày hiệu lực: 01/09/2023
Đơn vị: Công ty Cổ phần Thép Hòa Phát Dung Quất

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
## 1. THÔNG SỐ TRANG
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
- Khổ giấy: A4 đứng (210mm × 297mm)
- Font chữ: Times New Roman, 11pt
- Lề trang: trên 12mm, phải 12mm, dưới 10mm, trái 12mm
- Màu chủ đạo thương hiệu: đỏ #c8001e

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
## 2. LAYOUT TỔNG THỂ (từ trên xuống dưới)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[SECTION A] HEADER — 2 cột ngang
  CỘT TRÁI:
    - Logo SVG Hòa Phát (hình thang đỏ + chữ)
    - Dòng 1 (đỏ, bold): CÔNG TY CỔ PHẦN THÉP
    - Dòng 2 (đen, bold): HÒA PHÁT DUNG QUẤT

  CỘT PHẢI (căn phải):
    - BM.16/QT.05.10  (bold)
    - Ngày hiệu lực: 01/09/2023

[SECTION B] TIÊU ĐỀ — căn giữa
  - Chữ IN HOA, bold, 14pt: BIÊN BẢN GIAO NHẬN THÉP LỒNG
  - Dòng phụ 9.5pt:
    "Kíp [input 32px] Từ [input giờ] giờ ngày [input ngày/tháng/năm]
     đến [input giờ] giờ ngày [input ngày/tháng/năm]"

[SECTION C] THÔNG TIN HAI BÊN — danh sách dọc
  - "Chúng tôi gồm:"
  - "1. Bên giao: Ông/bà: [input họ tên, flex-grow]   Chức vụ: [input 130px]"
  - "2. Bên nhận: Ông/bà: [input họ tên, flex-grow]   Chức vụ: [input 130px]"
  - Dòng nghiêng: "Cùng nhau thống nhất lập "Biên bản giao nhận thép lồng" chi tiết như sau:"

[SECTION D] BẢNG DỮ LIỆU CHÍNH
  → Xem chi tiết ở mục 3

[SECTION E] GHI CHÚ PHÁP LÝ
  - In nghiêng 8.5pt:
    "Ghi chú: Biên bản này được lập thành 2 bản có giá trị như nhau:
     bên giao và bên nhận mỗi bên giữ 1 bản."

[SECTION F] CHỮ KÝ — 2 cột cách đều
  CỘT TRÁI:                     CỘT PHẢI:
    Bên giao (bold)               Bên nhận (bold)
    (Ký, ghi rõ họ tên)           (Ký, ghi rõ họ tên)
    [khoảng trống 36px]           [khoảng trống 36px]
    ─────────────────             ─────────────────
    [input họ tên]                [input họ tên]

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
## 3. CẤU TRÚC BẢNG CHÍNH (SECTION D)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

BẢNG: border-collapse, 100% width, font 9pt, border #333

HEADER BẢNG — 2 dòng header (sử dụng rowspan và colspan):

  ┌──────┬──────────┬──────────┬──────────┬──────────┬──────────────────────────────────────────────────────────────┬───────────┬─────────┬───────────────┐
  │      │          │          │          │          │ KL. thùng & thép lồng vào bệ xoay (tấn) - lần 1              │           │         │               │
  │  TT  │ Máy đúc  │ Mác thép │ Thùng số │ Thời gian├──────────────────────┬───────────────────┬──────────────────┤ KL. thép  │ Ghi chú │ Tinh Luyện,   │
  │      │          │          │          │          │ a                    │ b                 │ c                │ lồng (tấn)│         │ Lên Thăng     │
  └──────┴──────────┴──────────┴──────────┴──────────┴──────────────────────┴───────────────────┴──────────────────┴───────────┴─────────┴───────────────┘

  Rowspan=2: TT, Máy đúc, Mác thép, Thùng số, Thời gian, KL.thép lồng, Ghi chú, Tinh Luyện Lên Thăng
  Rowspan=1 (chỉ dòng 1): "KL. thùng & thép lồng vào bệ xoay (tấn) - lần 1" — colspan=3
  Dòng 2: a | b | c (3 cột con của cột KL)

  Giải thích từng cột:
  ┌──────────┬──────┬──────────────────────────────────────────────────────────────────────┐
  │ Tên cột  │ px   │ Nội dung                                                             │
  ├──────────┼──────┼──────────────────────────────────────────────────────────────────────┤
  │ TT       │ 22   │ Số thứ tự (text, hiển thị cố định 1→22)                             │
  │ Máy đúc  │ 34   │ Tên/mã máy đúc (input text)                                         │
  │ Mác thép │ 48   │ Mác thép, ví dụ Q235B, SD390 (input text)                           │
  │ Thùng số │ 30   │ Số thùng (input text)                                                │
  │ Thời gian│ 44   │ Giờ vào lò, định dạng hh:mm (input text)                            │
  │ a        │ 52   │ KL. thùng + thép lồng vào bệ xoay (tấn) – lần 1 (input number)     │
  │ b        │ 44   │ KL. thùng (tấn) – lần 2 (input number)                              │
  │ c        │ 52   │ KL. thùng thép lồng lần 3 (nếu có) (input number)                  │
  │ KL thép  │ 40   │ KL. thép lồng (tấn) — tự động tính tổng (input number)             │
  │ Ghi chú  │ 42   │ Ghi chú tự do (input text)                                          │
  │ Tinh Luyện│ 52  │ Tinh Luyện, Lên Thăng (input text)                                  │
  └──────────┴──────┴──────────────────────────────────────────────────────────────────────┘

BODY BẢNG:
  - 22 hàng dữ liệu (rows 1→22), mỗi hàng là <tr> với <input> trong mỗi ô
  - Tất cả input: border:none, text-align:center, background:transparent, width:100%

FOOTER BẢNG (hàng Tổng):
  - Ô đầu: colspan=8, text "Tổng" căn giữa, font-weight:bold
  - Ô KL thép lồng: input number id="total_kl", TỰ TÍNH bằng JS khi người dùng nhập
  - Ô Ghi chú và Tinh Luyện: để trống

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
## 4. LOGIC JAVASCRIPT
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
- Sinh 22 hàng bằng JS (không hardcode HTML)
- Lắng nghe input trên cột "KL thép lồng" → cộng tổng → đổ vào #total_kl
- Nút "Xóa trắng": reset toàn bộ input về ''
- Nút "Dữ liệu mẫu": điền sẵn 5 dòng đầu để test in thử
- Nút "In / Xuất PDF": gọi window.print()

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
## 5. PRINT / PDF
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
@media print {
  - Ẩn hoàn toàn .toolbar
  - @page { size: A4 portrait; margin: 0; }
  - Trang không có shadow, padding theo lề đã định
  - Input hiển thị như text thường (không border khi in)
}

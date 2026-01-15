# 📊 DATAPRODUCT API - Hệ thống Quản lý Phiếu Sản xuất & Dữ liệu Công nghệ

## 📖 Tổng quan dự án

Hệ thống quản lý phiếu sản xuất và dữ liệu công nghệ cho nhà máy thép Hòa Phát. Dự án cung cấp API backend phục vụ cho việc quản lý các phiếu sản xuất, biên kiểm nguyên liệu, phôi thép, và đồng bộ dữ liệu từ hệ thống Nucor Manufacturing (NM).

### ✨ Tính năng chính

- **Quản lý Phiếu Sản xuất** - Tạo, sửa, xóa, phê duyệt phiếu theo ca/kíp/máy đúc
- **Biên kiểm Nguyên liệu** - Theo dõi nguyên liệu đầu vào (độ ẩm, Silo, kíp...)
- **Biên kiểm Phôi thép** - Quản lý phôi thép đầu ra (loại, kích thước, mác...)
- **Đồng bộ HRC2** - Tích hợp dữ liệu từ hệ thống NM (Nucor Manufacturing)
- **Phê duyệt đa cấp** - Workflow phê duyệt theo phòng ban/người dùng
- **Cửa thép Phôi nóng** - Quản lý phôi nóng và phân loại chất lượng
- **Export PDF/Excel** - Xuất báo cáo với template HTML

---

## 🏗️ Kiến trúc hệ thống

### Technology Stack

```
Backend:    .NET 8.0 Web API
Frontend:   React SPA (trong wwwroot)
Database:   SQL Server 2019+
ORM:        Entity Framework Core 8.0
DI:         Scrutor (Auto-registration)
PDF:        DinkToPdf
Excel:      ClosedXML
```

### Kiến trúc 3-Layer

```
┌─────────────────────────────────────────────────────┐
│                   PRESENTATION                       │
│              Controllers (API Endpoints)             │
│  PhieusController, BKPhoiThepController, etc.       │
└────────────────────┬────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────┐
│                  BUSINESS LOGIC                      │
│               Services (Business Rules)              │
│  PhieuService, BKPhoiThepService, etc.              │
└────────────────────┬────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────┐
│                  DATA ACCESS                         │
│            Repositories (Data Operations)            │
│  PhieuRepository, BKPhoiThepRepository, etc.        │
└────────────────────┬────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────┐
│                    DATABASE                          │
│  ProductFormContext (PRODUCT_FORM)                  │
│  ProductDataMasterDbContext (PRODUCTDATA1)          │
└─────────────────────────────────────────────────────┘
```

### Dependency Injection Strategy

```csharp
// Auto-registration với Scrutor
- Helpers:     Singleton  (SoPhieuHelper, SecurityHelper, etc.)
- Repositories: Scoped    (IPhieuRepository, IBKPhoiThepRepository, etc.)
- Services:     Scoped    (PhieuService, BKPhoiThepService, etc.)
```

---

## 📁 Cấu trúc thư mục

```
dataproduct.api/
│
├── Controllers/                    # API Controllers
│   ├── PhieusController.cs        # Quản lý phiếu sản xuất
│   ├── BKNguyenLieuController.cs  # Biên kiểm nguyên liệu
│   ├── BKPhoiThepController.cs    # Biên kiểm phôi thép
│   ├── DLNMHRC2Controller.cs      # Dữ liệu HRC2 từ NM
│   ├── BmPheDuyetController.cs    # Phê duyệt phiếu
│   ├── CtdPhoiNongController.cs   # Cửa thép phôi nóng
│   ├── HeaderKeyController.cs     # Header key mapping
│   └── TaiKhoanController.cs      # Quản lý tài khoản
│
├── Services/                       # Business Logic Layer
│   ├── PhieuService.cs            # Logic nghiệp vụ phiếu
│   ├── BKPhoiThepService.cs       # Logic nghiệp vụ phôi thép
│   ├── HRC2_NMSyncService.cs      # Đồng bộ dữ liệu NM
│   └── ...
│
├── Repositories/                   # Data Access Layer
│   ├── IRepository.cs             # Interface definitions
│   ├── PhieuRepository.cs         # Repository cho phiếu
│   ├── BKPhoiThepRepository.cs    # Repository cho phôi thép
│   └── ...
│
├── Models/                         # Database Entities
│   ├── ProductFormContext.cs      # DbContext chính
│   ├── BmPhieu.cs                 # Entity phiếu
│   ├── BkPhoiThep.cs              # Entity phôi thép
│   ├── BkNguyenLieu.cs            # Entity nguyên liệu
│   ├── DLNM_HRC2.cs               # Entity dữ liệu HRC2
│   ├── CtdPhoiNong.cs             # Entity phôi nóng
│   └── MasterData/                # Master data entities
│       ├── ProductDataMasterDbContext.cs
│       ├── TaiKhoan.cs            # Tài khoản người dùng
│       └── PhongBan.cs            # Phòng ban
│
├── DTOs/                           # Data Transfer Objects
│   ├── PhieuDto.cs                # DTO cho phiếu
│   ├── BK_PhoiThepDto.cs          # DTO cho phôi thép
│   ├── DLNM_HRC2Dtos.cs           # DTO cho HRC2
│   ├── PagedResult.cs             # Paging response
│   └── Export/                    # Export DTOs
│       └── ExportFileResultDto.cs
│
├── ResponseModels/                 # API Response Models
│   ├── Phieu_ResponseModels.cs    # Response models cho phiếu
│   ├── DLNMHRC2_ResponseModels.cs # Response models cho HRC2
│   └── Header_ResponeModels.cs    # Response models cho header
│
├── Utils/                          # Utilities & Helpers
│   ├── SoPhieuHelper.cs           # Generate số phiếu
│   ├── PhieuStatusHelper.cs       # Xử lý trạng thái phiếu
│   └── SecurityHelper.cs          # Security utilities
│
├── wwwroot/                        # Static files (React build)
│   ├── templates/                 # Excel templates
│   └── template_html/             # HTML templates for PDF
│
├── Properties/
│   └── launchSettings.json        # Launch configuration
│
├── Program.cs                      # Application entry point
├── appsettings.json                # Configuration
└── dataproduct.api.csproj          # Project file
```

---

## 🗄️ Database Schema

### Database: PRODUCT_FORM (Main)

#### 1. BM_Phieu - Phiếu Sản xuất

```sql
- IDPhieu (GUID, PK)          - ID phiếu
- MaBM (string)               - Mã biểu mẫu
- SoPhieu (string)            - Số phiếu (auto-generate)
- NgaySX (DateOnly)           - Ngày sản xuất
- Ca (int)                    - Ca làm việc
- Kip (char)                  - Kíp (A, B, C)
- Scope (int)                 - Phạm vi (1, 2, 3)
- MayDuc (int)                - Máy đúc
- IDPhongBan (int)            - ID phòng ban
- NguoiTaoID (int)            - ID người tạo
- TinhTrang (int)             - Trạng thái (0-4)
- DataJSON (nvarchar(max))    - Dữ liệu động dạng JSON
- IsDelete (int)              - Đã xóa (0/1)
- IsLock (int)                - Đã khóa (0/1)
- LoaiPhieu (int)             - Loại phiếu
- IsClone (bit)               - Phiếu clone
- VersionClone (int)          - Version clone
- ID_PhieuGoc (GUID)          - ID phiếu gốc
```

#### 2. BM_PheDuyet - Phê duyệt

```sql
- ID (int, PK)                - ID phê duyệt
- PhieuID (GUID, FK)          - ID phiếu
- NguoiDuyetID (int)          - ID người duyệt
- TinhTrang (int)             - Trạng thái (1: chờ, 2: duyệt, 3: từ chối)
- NgayDuyet (datetime)        - Ngày duyệt
- GhiChu (string)             - Ghi chú
- STT (int)                   - Số thứ tự duyệt
```

#### 3. BK_PhoiThep - Biên kiểm Phôi thép

```sql
- ID (int, PK)                - ID
- NgaySX (DateOnly)           - Ngày sản xuất
- Ca (int)                    - Ca
- Kip (char)                  - Kíp
- LoaiPhoi (string)           - Loại phôi
- LoaiID (int)                - ID loại
- TenLoai (string)            - Tên loại
- TenPhanLoai (string)        - Tên phân loại
- MayDuc (int)                - Máy đúc
- Me (string)                 - Mẻ
- Mac (string)                - Mác thép
- KichThuoc (string)          - Kích thước
- SoThanh (int)               - Số thanh
- KhoiLuong (decimal)         - Khối lượng
- MauThu (string)             - Mẫu thử
- VanChuyen (string)          - Vận chuyển
- ST_DaChuyen (int)           - Số thanh đã chuyển
- IsNM (bit)                  - Từ NM
- NgayTaoBK (datetime)        - Ngày tạo
- GhiChu (string)             - Ghi chú
```

#### 4. BK_NguyenLieu - Biên kiểm Nguyên liệu

```sql
- ID (int, PK)                - ID
- NgaySX (DateOnly)           - Ngày sản xuất
- Ca (int)                    - Ca
- Kip (char)                  - Kíp
- Tron_ID (int)               - ID trộn
- TenNVL (string)             - Tên nguyên vật liệu
- Silo (string)               - Silo
- DoAm (decimal)              - Độ ẩm
- GioLayMau (string)          - Giờ lấy mẫu
- GioNhap_BK (string)         - Giờ nhập BK
- GhiChu (string)             - Ghi chú
```

#### 5. DLNM_HRC2 - Dữ liệu HRC2 từ NM

```sql
- ID (int, PK)                - ID
- REPORT_NO (int)             - Số báo cáo NM
- NgaySX (DateOnly)           - Ngày sản xuất
- Ngay (DateTime)             - Ngày
- Ca (int)                    - Ca
- BieuMau (string)            - Biểu mẫu
- Scope (int)                 - Phạm vi
- MeThoi (string)             - Mẻ thổi
- MacThep (string)            - Mác thép
- O2 (decimal)                - O2
- AR_RH (decimal)             - AR RH
- N2 (decimal)                - N2
- AR_BOF (decimal)            - AR BOF
- AR_LF (decimal)             - AR LF
- KLGangLong (decimal)        - KL Gang lỏng
- KLThepPhe (decimal)         - KL Thép phế
```

#### 6. CTD_PhoiNong - Cửa thép Phôi nóng

```sql
- ID (int, PK)                - ID
- IDPhieu (GUID)              - ID phiếu
- NgaySX (DateOnly)           - Ngày sản xuất
- CaKip (string)              - Ca kíp
- Kip (string)                - Kíp
- Me (string)                 - Mẻ
- Mac (string)                - Mác
- KichThuoc (string)          - Kích thước
- TongST (int)                - Tổng số thanh
- TongKL (decimal)            - Tổng khối lượng
- SoThanh_Loai1/2/3 (int)     - Số thanh theo loại
- KhoiLuong_Loai1/2/3 (dec)   - Khối lượng theo loại
- TinhTrangCTD (int)          - Tình trạng CTD
- TinhTrangQLCL (int)         - Tình trạng QLCL
- ID_BK_PhoiThep (int, FK)    - ID BK Phôi thép
```

#### 7. Header_Key - Header mapping keys

```sql
- ID (int, PK)                - ID
- KeyGuid (string)            - Key GUID
- TenHienThi (string)         - Tên hiển thị
- Mota (string)               - Mô tả
- LoaiPhieu (int)             - Loại phiếu
- IsActive (bit)              - Kích hoạt
- ThuTu (int)                 - Thứ tự
```

#### 8. Header_Mapping - Mapping nguồn dữ liệu

```sql
- ID (int, PK)                - ID
- TenNguonDuLieu (string)     - Tên nguồn dữ liệu
- ID_PhuLieu (int)            - ID phụ liệu
- ID_HeaderKey (int, FK)      - ID header key
- IsActive (bit)              - Kích hoạt
```

### Database: PRODUCTDATA1 (Master Data)

#### 1. Tbl_TaiKhoan - Tài khoản

```sql
- ID (int, PK)                - ID
- TenDangNhap (string)        - Tên đăng nhập
- MatKhau (string)            - Mật khẩu
- HoVaTen (string)            - Họ và tên
- Email (string)              - Email
- ID_PhongBan (int, FK)       - ID phòng ban
- ChucVu (string)             - Chức vụ
- ChuKy (nvarchar(max))       - Chữ ký (Base64)
- PhongBan_Them (string)      - Phòng ban thêm (JSON)
- IsActive (bit)              - Kích hoạt
- NgayTao (date)              - Ngày tạo
```

#### 2. Tbl_PhongBan - Phòng ban

```sql
- ID (int, PK)                - ID
- MaPhongBan (string)         - Mã phòng ban
- TenPhongBan (string)        - Tên phòng ban
- MoTa (string)               - Mô tả
- IsActive (bit)              - Kích hoạt
```

---

## 🔌 API Endpoints

### Base URL

```
Development: https://localhost:7xxx
Production:  http://192.168.240.3:xxxx
```

### 1. Phiếu Sản xuất (PhieusController)

| Method | Endpoint                           | Description                     |
| ------ | ---------------------------------- | ------------------------------- |
| GET    | `/api/Phieus`                      | Lấy danh sách phiếu (có filter) |
| GET    | `/api/Phieus/{id}`                 | Lấy chi tiết phiếu theo ID      |
| POST   | `/api/Phieus`                      | Tạo phiếu mới                   |
| PUT    | `/api/Phieus/{id}`                 | Cập nhật phiếu                  |
| DELETE | `/api/Phieus/{id}`                 | Xóa phiếu                       |
| POST   | `/api/Phieus/{id}/clone`           | Clone phiếu                     |
| PUT    | `/api/Phieus/{id}/status`          | Thay đổi trạng thái             |
| GET    | `/api/Phieus/exist`                | Kiểm tra phiếu tồn tại          |
| POST   | `/api/Phieus/search`               | Tìm kiếm phiếu (có phân trang)  |
| PUT    | `/api/Phieus/{id}/status-extended` | Cập nhật trạng thái mở rộng     |
| POST   | `/api/Phieus/{id}/export-pdf`      | Export phiếu ra PDF             |
| GET    | `/api/Phieus/{id}/download`        | Download file phiếu             |
| GET    | `/api/Phieus/so-phieu`             | Generate số phiếu               |

**Query Parameters (GET /api/Phieus):**

- `MaBM` - Mã biểu mẫu
- `NguoiTaoID` - ID người tạo
- `NguoiDuyetID` - ID người duyệt
- `isCheckDuyet` - Lọc theo phê duyệt (0/1)

**Search Request Body (POST /api/Phieus/search):**

```json
{
  "tuNgay": "2026-01-01",
  "denNgay": "2026-01-31",
  "maBm": "BM_HRC2",
  "nguoiTaoId": 1,
  "tinhTrang": 2,
  "page": 1,
  "pageSize": 20
}
```

### 2. Biên kiểm Phôi thép (BKPhoiThepController)

| Method | Endpoint                           | Description                 |
| ------ | ---------------------------------- | --------------------------- |
| GET    | `/api/BKPhoiThep`                  | Lấy danh sách phôi thép     |
| GET    | `/api/BKPhoiThep/{id}`             | Lấy chi tiết phôi thép      |
| POST   | `/api/BKPhoiThep`                  | Tạo mới phôi thép           |
| PUT    | `/api/BKPhoiThep/{id}`             | Cập nhật phôi thép          |
| DELETE | `/api/BKPhoiThep/{id}`             | Xóa phôi thép               |
| PUT    | `/api/BKPhoiThep/{id}/st-dachuyen` | Cập nhật số thanh đã chuyển |
| PUT    | `/api/BKPhoiThep/st-dachuyen-bulk` | Cập nhật bulk số thanh      |
| PUT    | `/api/BKPhoiThep/{id}/st-thuhoi`   | Thu hồi số thanh            |
| PUT    | `/api/BKPhoiThep/st-thuhoi-bulk`   | Thu hồi bulk số thanh       |

**Query Parameters:**

- `NgaySX` - Ngày sản xuất
- `Ca` - Ca (1, 2, 3)
- `Kip` - Kíp (A, B, C)
- `LoaiPhoi` - Loại phôi
- `MayDuc` - Máy đúc (1, 2, 3...)

### 3. Biên kiểm Nguyên liệu (BKNguyenLieuController)

| Method | Endpoint                 | Description               |
| ------ | ------------------------ | ------------------------- |
| GET    | `/api/BKNguyenLieu`      | Lấy danh sách nguyên liệu |
| GET    | `/api/BKNguyenLieu/{id}` | Lấy chi tiết nguyên liệu  |
| POST   | `/api/BKNguyenLieu`      | Tạo mới nguyên liệu       |
| PUT    | `/api/BKNguyenLieu/{id}` | Cập nhật nguyên liệu      |
| DELETE | `/api/BKNguyenLieu/{id}` | Xóa nguyên liệu           |

### 4. Dữ liệu HRC2 (DLNMHRC2Controller)

| Method | Endpoint                          | Description         |
| ------ | --------------------------------- | ------------------- |
| GET    | `/api/DLNMHRC2`                   | Lấy danh sách HRC2  |
| GET    | `/api/DLNMHRC2/{id}`              | Lấy chi tiết HRC2   |
| GET    | `/api/DLNMHRC2/report/{reportNo}` | Lấy theo số báo cáo |
| POST   | `/api/DLNMHRC2`                   | Tạo mới HRC2        |
| PUT    | `/api/DLNMHRC2/{id}`              | Cập nhật HRC2       |
| DELETE | `/api/DLNMHRC2/{id}`              | Xóa HRC2            |
| POST   | `/api/DLNMHRC2/sync-from-nm`      | Đồng bộ từ NM       |
| POST   | `/api/DLNMHRC2/export-excel`      | Export Excel        |

**Sync Request Body:**

```json
{
  "plant": "HPDQ",
  "plantNo": 2,
  "workDate": "2026-01-13",
  "shift": 1,
  "bieuMau": "BM_HRC2",
  "scope": 1
}
```

### 5. Phê duyệt (BmPheDuyetController)

| Method | Endpoint                           | Description              |
| ------ | ---------------------------------- | ------------------------ |
| GET    | `/api/BmPheDuyet`                  | Lấy danh sách phê duyệt  |
| GET    | `/api/BmPheDuyet/{id}`             | Lấy chi tiết phê duyệt   |
| GET    | `/api/BmPheDuyet/phieu/{phieuId}`  | Lấy phê duyệt theo phiếu |
| POST   | `/api/BmPheDuyet`                  | Tạo phê duyệt            |
| PUT    | `/api/BmPheDuyet/{id}`             | Cập nhật phê duyệt       |
| PUT    | `/api/BmPheDuyet/update-tinhtrang` | Cập nhật tình trạng      |

### 6. Cửa thép Phôi nóng (CtdPhoiNongController)

| Method | Endpoint                       | Description             |
| ------ | ------------------------------ | ----------------------- |
| GET    | `/api/CtdPhoiNong`             | Lấy danh sách phôi nóng |
| GET    | `/api/CtdPhoiNong/{id}`        | Lấy chi tiết            |
| POST   | `/api/CtdPhoiNong`             | Tạo mới                 |
| PUT    | `/api/CtdPhoiNong/{id}`        | Cập nhật                |
| PUT    | `/api/CtdPhoiNong/{id}/status` | Cập nhật trạng thái     |

### 7. Tài khoản (TaiKhoanController)

| Method | Endpoint              | Description             |
| ------ | --------------------- | ----------------------- |
| GET    | `/api/TaiKhoan`       | Lấy danh sách tài khoản |
| GET    | `/api/TaiKhoan/{id}`  | Lấy chi tiết tài khoản  |
| POST   | `/api/TaiKhoan/login` | Đăng nhập               |

---

## 🔧 Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DbConnectionString": "Server=192.168.240.3,1433;Database=PRODUCT_FORM;User Id=sa;Password=***;TrustServerCertificate=True;",
    "MasterDbConnection": "Server=192.168.240.3,1433;Database=PRODUCTDATA1;User Id=sa;Password=***;TrustServerCertificate=True;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### CORS Policy

```csharp
Policy: "AllowAllOrigins"
- AllowAnyOrigin()
- AllowAnyMethod()
- AllowAnyHeader()
```

---

## 🚀 Cài đặt và chạy dự án

### Yêu cầu hệ thống

- .NET 8.0 SDK
- SQL Server 2019+
- Visual Studio 2022 / VS Code
- Node.js (cho React frontend)

### Các bước cài đặt

1. **Clone repository**

```bash
git clone <repository-url>
cd dataproduct.api
```

2. **Restore packages**

```bash
dotnet restore
```

3. **Cấu hình Database**

- Cập nhật connection strings trong `appsettings.json`
- Chạy migrations (nếu có):

```bash
dotnet ef database update --context ProductFormContext
dotnet ef database update --context ProductDataMasterDbContext
```

4. **Build project**

```bash
dotnet build
```

5. **Run project**

```bash
dotnet run
```

6. **Truy cập ứng dụng**

- API: `https://localhost:7xxx`
- Swagger: `https://localhost:7xxx/swagger`
- React App: `https://localhost:7xxx/index.html`

---

## 📦 Dependencies

### NuGet Packages

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.20" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.20" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.9.0" />
<PackageReference Include="Scrutor" Version="6.1.0" />
<PackageReference Include="DinkToPdf" Version="1.0.8" />
<PackageReference Include="libwkhtmltox-64" Version="1.0.0" />
<PackageReference Include="ClosedXML" Version="0.105.0" />
```

---

## 📝 Business Rules

### Trạng thái Phiếu (TinhTrang)

```
0 - Mới tạo
1 - Chờ duyệt
2 - Đã duyệt
3 - Từ chối
4 - Hoàn thành
```

### Trạng thái Phê duyệt

```
1 - Chờ duyệt
2 - Đã duyệt
3 - Từ chối
```

### Quy tắc Số phiếu (SoPhieu)

**Format:** `{MaBM}-{Scope}-{YYYYMMDD}-{Ca}-{Kip}{MayDuc}`

Ví dụ:

- `BM_HRC2-1-20260113-1-A1` - Phiếu HRC2, Scope 1, ngày 13/01/2026, ca 1, kíp A, máy 1
- `BM_HRC2-2-20260113-2-B2` - Phiếu HRC2, Scope 2, ngày 13/01/2026, ca 2, kíp B, máy 2

### Workflow Phê duyệt

1. Người tạo phiếu → Tạo phiếu (TinhTrang = 0)
2. Gửi phê duyệt → Tạo danh sách người duyệt (BM_PheDuyet)
3. Người duyệt cấp 1 → Duyệt/Từ chối
4. Người duyệt cấp 2 → Duyệt/Từ chối
5. Tất cả duyệt → Phiếu hoàn thành (TinhTrang = 4)

---

## 🔐 Security Notes

### ⚠️ Cần cải thiện

- [ ] Thêm Authentication (JWT)
- [ ] Thêm Authorization (Role-based)
- [ ] Hash passwords (BCrypt)
- [ ] Validate input data
- [ ] Rate limiting
- [ ] CORS policy cụ thể (thay vì AllowAnyOrigin)
- [ ] HTTPS enforcement
- [ ] Audit logging

---

## 🧪 Testing

### Unit Tests (Chưa có)

Đề xuất tạo:

- `Services.Tests/` - Test business logic
- `Repositories.Tests/` - Test data access
- `Controllers.Tests/` - Test API endpoints

### Integration Tests (Chưa có)

Đề xuất:

- API endpoint tests
- Database integration tests

---

## 📊 Performance Considerations

### Tối ưu hóa hiện tại

- ✅ Scrutor auto-registration (giảm boilerplate)
- ✅ DbContext scoped lifetime
- ✅ Async/await cho tất cả operations

### Đề xuất cải thiện

- [ ] Thêm caching (Redis/Memory Cache) cho master data
- [ ] Pagination cho tất cả list endpoints
- [ ] Database indexing optimization
- [ ] Query optimization (Select specific columns)
- [ ] Connection pooling configuration
- [ ] Response compression

---

## 🐛 Known Issues

1. **CORS Policy** - AllowAnyOrigin không an toàn cho production
2. **No Authentication** - API public, không có xác thực
3. **Error Handling** - Chưa có global exception handler
4. **Logging** - Chưa có structured logging
5. **Validation** - Input validation chưa đầy đủ

---

## 📈 Roadmap

### Version 1.1 (Q1 2026)

- [ ] JWT Authentication
- [ ] Role-based Authorization
- [ ] Structured Logging (Serilog)
- [ ] Global Error Handler
- [ ] Input Validation (FluentValidation)

### Version 1.2 (Q2 2026)

- [ ] SignalR Real-time notifications
- [ ] Redis Caching
- [ ] Advanced Reporting
- [ ] Mobile API optimization

### Version 2.0 (Q3 2026)

- [ ] Microservices architecture
- [ ] Event-driven with RabbitMQ
- [ ] Docker containerization
- [ ] CI/CD pipeline

---

## 👥 Team & Contact

- **Developer Team**: Phòng CNTT - Hòa Phát
- **Project Manager**: [Tên PM]
- **Technical Lead**: [Tên TL]

---

## 📄 License

Internal use only - Hòa Phát Group

---

## 📚 Documentation

- [Swagger UI](https://localhost:7xxx/swagger) - API documentation
- [Database Diagram](#) - ER diagram (TODO)
- [Postman Collection](#) - API testing (TODO)

---

## 🔄 Changelog

### Version 1.0.0 (2026-01-13)

- ✅ Initial release
- ✅ Core CRUD operations for all entities
- ✅ HRC2 sync from NM system
- ✅ Multi-level approval workflow
- ✅ PDF export with DinkToPdf
- ✅ Excel export with ClosedXML
- ✅ React SPA integration

---

**Last Updated**: 2026-01-13  
**Documentation Version**: 1.0

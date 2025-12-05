# Hướng dẫn quản lý Nhập hàng và Nhà cung cấp

## Tổng quan

Module này bao gồm các API quản lý phiếu nhập thuốc và thông tin nhà cung cấp.

## 1. PhieuNhap API - Quản lý phiếu nhập

### Các endpoint chính

#### 1.1 Lấy danh sách phiếu nhập theo khoảng thời gian

**GET** `/api/PhieuNhap/GetByDateRange`

**Query Parameters:**
- `startDate` (bắt buộc): Ngày bắt đầu
- `endDate` (bắt buộc): Ngày kết thúc
- `maNV` (tùy chọn): Mã nhân viên để lọc
- `maNCC` (tùy chọn): Mã nhà cung cấp để lọc

**Ví dụ:** `GET /api/PhieuNhap/GetByDateRange?startDate=2025-01-01&endDate=2025-12-31`

#### 1.2 Tạo phiếu nhập mới

**POST** `/api/PhieuNhap/AddPhieuNhap`

**Request Body:**
```json
{
  "maNV": "NV001",
  "maNCC": "NCC001",
  "ghiChu": "Nhập thuốc bổ sung",
  "chiTietPhieuNhaps": [
    {
      "maThuoc": "THUOC001",
      "soLuong": 100,
      "donGia": 15000,
      "hanSuDung": "2026-11-17",
      "maLD": "LD001"
    }
  ],
  "loThuocHSDs": [
    {
      "maThuoc": "THUOC001",
      "soLuong": 50,
      "hanSuDung": "2026-11-17",
      "maLD": "LD001"
    }
  ]
}
```

#### 1.3 Lấy chi tiết phiếu nhập

**GET** `/api/PhieuNhap/GetChiTietPhieuNhapByMaPN`

**Query Parameter:** `maPN` (bắt buộc)

**Ví dụ:** `GET /api/PhieuNhap/GetChiTietPhieuNhapByMaPN?maPN=PN20251117000001`

## 2. NhaCungCap API - Quản lý nhà cung cấp

### Các endpoint chính

#### 2.1 Lấy danh sách tất cả nhà cung cấp

**GET** `/api/NhaCungCap`

#### 2.2 Lấy thông tin nhà cung cấp theo mã

**GET** `/api/NhaCungCap/{id}`

#### 2.3 Tạo nhà cung cấp mới

**POST** `/api/NhaCungCap`

**Request Body:**
```json
{
  "tenNCC": "Công ty Dược phẩm XYZ",
  "diaChi": "456 Đường GHI, Quận 2, TP.HCM",
  "soDT": "0987654321",
  "email": "contact@xyz.com"
}
```

#### 2.4 Cập nhật thông tin nhà cung cấp

**PUT** `/api/NhaCungCap/{id}`

## Luồng nhập hàng

### Tạo phiếu nhập:
1. Chuẩn bị thông tin nhà cung cấp (hoặc tạo mới nếu chưa có)
2. Tạo phiếu nhập với danh sách thuốc và chi tiết
3. Hệ thống tự động tạo lô thuốc và cập nhật tồn kho

### Xem lịch sử nhập:
1. Lấy danh sách phiếu nhập theo khoảng thời gian
2. Xem chi tiết từng phiếu nhập

## Flow Diagrams

### 1. 📦 Luồng Nhập hàng (Đầy đủ)

```mermaid
flowchart TD
    A[👨‍💼 Nhân viên kho] --> B[📋 Kiểm tra hàng cần nhập]
    B --> C{🏢 NCC có sẵn?}
    C -->|❌ Chưa có| D[➕ Tạo NCC mới]
    C -->|✅ Có rồi| E[📝 Chuẩn bị phiếu nhập]
    D --> F[💾 Lưu thông tin NCC]
    F --> E
    E --> G[📦 Chọn thuốc nhập]
    G --> H[🔢 Nhập số lượng & giá]
    H --> I[📅 Đặt hạn sử dụng]
    I --> J[📤 Gửi tạo phiếu nhập]
    J --> K[🔍 Validate dữ liệu]
    K --> L{✅ Hợp lệ?}
    L -->|❌ Không| M[⚠️ Báo lỗi]
    L -->|✅ Có| N[🏷️ Tạo mã PN tự động]
    N --> O[💾 Lưu phiếu nhập]
    O --> P[📦 Tạo lô thuốc]
    P --> Q[📊 Cập nhật tồn kho]
    Q --> R[✅ Nhập hàng thành công]
    R --> S[🧾 In phiếu nhập]
```

### 2. 🏢 Luồng Quản lý Nhà cung cấp

```mermaid
flowchart TD
    A[👨‍💼 Quản lý] --> B{💡 Muốn làm gì?}
    B -->|👀 Xem danh sách| C[📋 GET /api/NhaCungCap]
    B -->|🔍 Xem chi tiết| D["🏢 GET /api/NhaCungCap/{id}"]
    B -->|➕ Thêm mới| E[📝 Nhập thông tin NCC]
    B -->|✏️ Cập nhật| F[🏢 Chọn NCC cần sửa]
    C --> G[🗄️ Database]
    D --> G
    E --> H[📤 POST /api/NhaCungCap]
    F --> I["📤 PUT /api/NhaCungCap/{id}"]
    H --> J[🔍 Validate dữ liệu]
    I --> J
    J --> K{✅ Hợp lệ?}
    K -->|❌ Không| L[⚠️ Báo lỗi]
    K -->|✅ Có| M[🏷️ Generate mã NCC]
    M --> N[💾 Lưu database]
    N --> O[✅ Thành công]
    G --> P[📊 Trả dữ liệu]
    P --> Q[💻 Hiển thị]
    O --> Q
    L --> R[💻 Hiển thị lỗi]
```

### 3. 📊 Luồng Tra cứu Phiếu nhập

```mermaid
flowchart TD
    A[👨‍💼 Nhân viên] --> B[📅 Chọn khoảng thời gian]
    B --> C[🏢 Chọn NCC - tùy chọn]
    C --> D[👨‍💼 Chọn NV - tùy chọn]
    D --> E[🔍 Tìm phiếu nhập]
    E --> F[📤 GET /api/PhieuNhap/GetByDateRange]
    F --> G[🗄️ Query database]
    G --> H{📋 Có dữ liệu?}
    H -->|❌ Không| I[📭 Không tìm thấy]
    H -->|✅ Có| J[📊 Hiển thị danh sách]
    J --> K[👆 Click phiếu cần xem]
    K --> L[📤 GET /api/PhieuNhap/GetChiTietPhieuNhapByMaPN]
    L --> M[🗄️ Query chi tiết]
    M --> N[📋 Hiển thị chi tiết]
    N --> O[🧾 Xuất báo cáo]
    I --> P[💻 Hiển thị thông báo]
```

### 4. 🔄 Luồng Xử lý Tồn kho khi Nhập hàng

```mermaid
flowchart TD
    A[📦 Phiếu nhập được tạo] --> B[📋 Duyệt chi tiết thuốc]
    B --> C[🔍 Kiểm tra thuốc tồn tại]
    C --> D{🏷️ Thuốc có sẵn?}
    D -->|❌ Chưa có| E[⚠️ Báo lỗi - thuốc không tồn tại]
    D -->|✅ Có| F[📦 Tạo lô thuốc mới]
    F --> G[🏷️ Generate mã lô]
    G --> H[📅 Set hạn sử dụng]
    H --> I[🔢 Set số lượng ban đầu]
    I --> J[💾 Lưu lô thuốc]
    J --> K[📊 Cộng vào tồn kho]
    K --> L{📋 Còn thuốc khác?}
    L -->|✅ Có| B
    L -->|❌ Hết| M[✅ Cập nhật tồn kho hoàn tất]
    M --> N[📈 Thống kê tồn kho mới]
```

### 5. 📈 Tổng quan Quy trình Nhập hàng

```mermaid
graph TB
    subgraph "📋 Chuẩn bị"
        A[🏢 Chọn NCC]
        B[📦 Chọn thuốc]
        C[🔢 Nhập số lượng]
        D[💰 Nhập đơn giá]
    end

    subgraph "⚙️ Xử lý"
        E[🔍 Validate]
        F[🏷️ Tạo phiếu nhập]
        G[📦 Tạo lô thuốc]
        H[📊 Cập nhật tồn kho]
    end

    subgraph "✅ Hoàn thành"
        I[🧾 Phiếu nhập]
        J[📈 Báo cáo tồn kho]
        K[💰 Cập nhật chi phí]
    end

    A --> E
    B --> E
    C --> E
    D --> E
    E --> F
    F --> G
    G --> H
    H --> I
    H --> J
    H --> K

    style A fill:#e8f5e8
    style E fill:#fff3e0
    style I fill:#e3f2fd
```

## Ví dụ sử dụng từ frontend

### JavaScript / Fetch API

```javascript
// Lấy phiếu nhập theo khoảng thời gian
const phieuNhap = await fetch('/api/PhieuNhap/GetByDateRange?startDate=2025-01-01&endDate=2025-12-31');
const phieuNhapData = await phieuNhap.json();

// Tạo phiếu nhập mới
const newPhieuNhap = {
  maNV: "NV001",
  maNCC: "NCC001",
  ghiChu: "Nhập thuốc bổ sung",
  chiTietPhieuNhaps: [
    {
      maThuoc: "THUOC001",
      soLuong: 100,
      donGia: 15000,
      hanSuDung: "2026-11-17",
      maLD: "LD001"
    }
  ]
};

const createResponse = await fetch('/api/PhieuNhap/AddPhieuNhap', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify(newPhieuNhap)
});

// Lấy danh sách nhà cung cấp
const nccList = await fetch('/api/NhaCungCap');
const nccData = await nccList.json();

// Tạo nhà cung cấp mới
const newNCC = {
  tenNCC: "Công ty Dược phẩm NEW",
  diaChi: "789 Đường JKL, Quận 3, TP.HCM",
  soDT: "0912345678",
  email: "contact@new.com"
};

const nccResponse = await fetch('/api/NhaCungCap', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify(newNCC)
});
```

## Lưu ý kỹ thuật

### PhieuNhap API:
- Mã phiếu nhập: Tự động generate theo format PN + timestamp
- Tổng tiền: Tự động tính từ chiTietPhieuNhaps
- Lô thuốc: Tự động tạo nếu không cung cấp loThuocHSDs
- Validation: Kiểm tra maNV, maNCC, maThuoc tồn tại

### NhaCungCap API:
- Mã nhà cung cấp (maNCC): Tự động generate khi tạo
- Tên nhà cung cấp: Bắt buộc, không được null
- Địa chỉ, số điện thoại, email: Có thể null
- Validation: ModelState validation được áp dụng

## Test API

### Sử dụng Swagger UI
1. Chạy: `dotnet run --launch-profile "https"`
2. Mở: `https://localhost:port/swagger`
3. Tìm endpoints trong `PhieuNhap` và `NhaCungCap`

### Sử dụng PowerShell

```powershell
# Lấy phiếu nhập theo khoảng thời gian
Invoke-WebRequest -Uri "https://localhost:5001/api/PhieuNhap/GetByDateRange?startDate=2025-01-01&endDate=2025-12-31" -Method GET -SkipCertificateCheck

# Tạo phiếu nhập
$phieuNhapBody = @{
    maNV = "NV001"
    maNCC = "NCC001"
    ghiChu = "Test nhập"
    chiTietPhieuNhaps = @(
        @{
            maThuoc = "THUOC001"
            soLuong = 10
            donGia = 15000
            hanSuDung = "2026-12-31"
            maLD = "LD001"
        }
    )
} | ConvertTo-Json

Invoke-WebRequest -Uri "https://localhost:5001/api/PhieuNhap/AddPhieuNhap" -Method POST -Body $phieuNhapBody -ContentType "application/json" -SkipCertificateCheck

# Lấy danh sách nhà cung cấp
Invoke-WebRequest -Uri "https://localhost:5001/api/NhaCungCap" -Method GET -SkipCertificateCheck
```

## Hỗ trợ

Kiểm tra logs server nếu gặp lỗi. Đảm bảo:
- maNV, maNCC, maThuoc tồn tại trong hệ thống
- Ngày tháng đúng format
- Số lượng và đơn giá > 0
- HanSuDung là future date
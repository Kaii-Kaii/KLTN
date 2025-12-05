# Hướng dẫn quản lý Hóa đơn và Thanh toán

## Tổng quan

Module này bao gồm các API quản lý hóa đơn bán hàng và tích hợp thanh toán PayOS.

## 1. HoaDon API - Quản lý hóa đơn

### Các endpoint chính

#### 1.1 Tạo hóa đơn trực tiếp

**POST** `/api/HoaDon/Create`

**Request Body:**
```json
{
  "maKH": "KH001",
  "maNV": "NV001",
  "ghiChu": "Bán lẻ tại quầy",
  "tongTien": 150000,
  "items": [
    {
      "maThuoc": "THUOC001",
      "soLuong": 10,
      "donGia": 15000
    }
  ]
}
```

#### 1.2 Tạo hóa đơn online

**POST** `/api/HoaDon/CreateOnline`

**Request Body:**
```json
{
  "maKH": "KH001",
  "ghiChu": "Đặt hàng online",
  "tongTien": 150000,
  "items": [
    {
      "maThuoc": "THUOC001",
      "soLuong": 10,
      "donGia": 15000
    }
  ]
}
```

#### 1.3 Xác nhận hóa đơn online

**POST** `/api/HoaDon/ConfirmOnline`

**Request Body:**
```json
{
  "maHD": "HDOL20251117120000",
  "maNV": "NV001"
}
```

#### 1.4 Cập nhật trạng thái hóa đơn

**PUT** `/api/HoaDon/UpdateStatus`

**Request Body:**
```json
{
  "maHD": "HDOL20251117120000",
  "trangThaiGiaoHang": 1
}
```

## 2. PayOS API - Tích hợp thanh toán

### Các endpoint chính

#### 2.1 Tạo thanh toán

**POST** `/api/SimplePayment/Create`

**Request Body:**
```json
{
  "amount": 150000,
  "description": "Thanh toán hóa đơn HDOL20251117120000",
  "returnUrl": "https://yourapp.com/payment/success",
  "cancelUrl": "https://yourapp.com/payment/cancel"
}
```

#### 2.2 Kiểm tra trạng thái thanh toán

**GET** `/api/SimplePayment/Status/{orderCode}`

## Luồng hóa đơn online với thanh toán

### Quy trình đầy đủ:
1. **Thêm giỏ hàng** (xử lý trên frontend)
2. **Tạo hóa đơn online**: POST `/api/HoaDon/CreateOnline`
3. **Tạo thanh toán**: POST `/api/SimplePayment/Create`
4. **Thanh toán** (redirect đến PayOS)
5. **Xử lý kết quả**:
   - Thành công: returnUrl được gọi
   - Thất bại: cancelUrl được gọi
6. **Nhân viên xác nhận**: POST `/api/HoaDon/ConfirmOnline`
7. **Cập nhật trạng thái giao hàng**: PUT `/api/HoaDon/UpdateStatus`

### Các trạng thái giao hàng:
- 0: Chưa xử lý
- 1: Đang chuẩn bị
- 2: Đang giao
- 3: Đã nhận
- 4: Đã hủy

## Flow Diagrams

### 1. 🛒 Luồng Bán hàng tại Quầy

```mermaid
flowchart TD
    A[👨‍💼 Nhân viên] --> B[🛒 Khách chọn thuốc]
    B --> C[🔢 Nhập số lượng]
    C --> D[💰 Tính tiền]
    D --> E[👤 Chọn khách hàng]
    E --> F[📝 Tạo hóa đơn trực tiếp]
    F --> G[📤 POST /api/HoaDon/Create]
    G --> H[🔍 Kiểm tra tồn kho]
    H --> I{📦 Còn đủ?}
    I -->|❌ Không| J[⚠️ Báo hết hàng]
    I -->|✅ Có| K[💾 Lưu hóa đơn]
    K --> L[📦 Xuất kho ngay]
    L --> M[🧾 In hóa đơn]
    M --> N[💰 Thu tiền]
    N --> O[✅ Hoàn thành]
```

### 2. 🌐 Luồng Đặt hàng Online (Đầy đủ)

```mermaid
flowchart TD
    A[👤 Khách hàng online] --> B[🛒 Thêm vào giỏ hàng]
    B --> C[📱 Đặt hàng]
    C --> D[📝 Tạo hóa đơn online]
    D --> E[📤 POST /api/HoaDon/CreateOnline]
    E --> F[🔍 Validate tồn kho]
    F --> G{✅ Đủ hàng?}
    G -->|❌ Không| H[⚠️ Báo hết hàng]
    G -->|✅ Có| I[💾 Lưu HD online]
    I --> J[💳 Tạo thanh toán]
    J --> K[📤 POST /api/SimplePayment/Create]
    K --> L[🔗 Redirect PayOS]
    L --> M{💳 Thanh toán}
    M -->|✅ Thành công| N[🔄 Callback returnUrl]
    M -->|❌ Thất bại| O[🔄 Callback cancelUrl]
    N --> P[✅ Cập nhật trạng thái]
    O --> Q[❌ Hủy đơn hàng]
    P --> R[👨‍💼 NV xác nhận]
    R --> S[📤 POST /api/HoaDon/ConfirmOnline]
    S --> T[📦 Xuất kho]
    T --> U[🚚 Giao hàng]
    U --> V[📤 PUT /api/HoaDon/UpdateStatus]
    V --> W[✅ Hoàn thành]
```

### 3. 💳 Luồng Thanh toán PayOS

```mermaid
sequenceDiagram
    participant KH as Khách hàng
    participant FE as Frontend
    participant BE as Backend
    participant PayOS as PayOS Gateway
    participant Bank as Ngân hàng

    KH->>FE: Nhấn thanh toán
    FE->>BE: POST /api/SimplePayment/Create
    BE->>BE: Tạo orderCode
    BE->>BE: Tạo signature HMAC-SHA256
    BE->>PayOS: Gửi payment request
    PayOS->>BE: Return payment URL
    BE->>FE: Return checkout URL
    FE->>KH: Redirect to PayOS
    KH->>PayOS: Nhập thông tin thẻ
    PayOS->>Bank: Xử lý thanh toán
    Bank->>PayOS: Kết quả thanh toán
    PayOS->>KH: Hiển thị kết quả
    PayOS->>BE: Callback returnUrl/cancelUrl
    BE->>BE: Verify signature
    BE->>BE: Cập nhật trạng thái
    BE->>FE: Redirect về website
```

### 4. 📦 Luồng Xử lý Đơn hàng bởi Nhân viên

```mermaid
flowchart TD
    A[👨‍💼 Nhân viên] --> B[📋 Xem đơn hàng mới]
    B --> C[👀 Kiểm tra chi tiết]
    C --> D{📦 Có thể chuẩn bị?}
    D -->|❌ Thiếu hàng| E[📞 Liên hệ khách]
    D -->|✅ OK| F[📦 Chuẩn bị hàng]
    F --> G[📤 POST /api/HoaDon/ConfirmOnline]
    G --> H[💾 Xuất kho]
    H --> I[📦 Đóng gói]
    I --> J[🚚 Giao hàng]
    J --> K[📱 Cập nhật trạng thái]
    K --> L[📤 PUT /api/HoaDon/UpdateStatus]
    L --> M{🚚 Trạng thái giao}
    M -->|📍 Đang giao| N[🚚 Giao hàng]
    M -->|✅ Đã nhận| O[✅ Hoàn thành]
    M -->|❌ Trả hàng| P[🔄 Xử lý trả hàng]
    E --> Q[💬 Tư vấn khách]
    P --> R[💰 Hoàn tiền]
```

### 5. 📊 Luồng Tra cứu và Báo cáo

```mermaid
flowchart TD
    A[👨‍💼 Quản lý] --> B{💡 Muốn xem gì?}
    B -->|🧾 Danh sách HD| C[📅 Chọn khoảng thời gian]
    B -->|💳 Thanh toán| D[🔍 Tra cứu theo orderCode]
    B -->|📈 Báo cáo| E[📊 Chọn loại báo cáo]
    C --> F[📤 GET /api/HoaDon theo filter]
    D --> G["📤 GET /api/SimplePayment/Status/{orderCode}"]
    E --> H[📊 Query database]
    F --> I[📋 Hiển thị danh sách]
    G --> J[💳 Hiển thị trạng thái]
    H --> K[📊 Xuất báo cáo]
    I --> L[👆 Click xem chi tiết]
    L --> M[📋 Chi tiết hóa đơn]
    M --> N[🖨️ In hóa đơn]
    J --> O[💰 Xử lý khiếu nại]
    K --> P[📊 Phân tích doanh thu]
```

### 6. 🔄 Tổng quan Quy trình Bán hàng

```mermaid
graph TB
    subgraph "🛒 Bán hàng"
        A[Trực tiếp tại quầy]
        B[Đặt hàng online]
    end

    subgraph "💳 Thanh toán"
        C[Thu tiền mặt]
        D[Tích hợp PayOS]
    end

    subgraph "📦 Xử lý"
        E[Xuất kho ngay]
        F[NV xác nhận]
        G[Giao hàng]
    end

    subgraph "✅ Hoàn thành"
        H[In hóa đơn]
        I[Cập nhật trạng thái]
        J[Báo cáo]
    end

    A --> C
    B --> D
    C --> E
    D --> F
    E --> H
    F --> G
    G --> I
    I --> J

    style A fill:#e8f5e8
    style D fill:#fff3e0
    style G fill:#e3f2fd
```

## Ví dụ sử dụng từ frontend

### JavaScript / Fetch API

```javascript
// Tạo hóa đơn trực tiếp
const hoaDonTrucTiep = {
  maKH: "KH001",
  maNV: "NV001",
  ghiChu: "Bán lẻ tại quầy",
  tongTien: 150000,
  items: [
    {
      maThuoc: "THUOC001",
      soLuong: 10,
      donGia: 15000
    }
  ]
};

const hdResponse = await fetch('/api/HoaDon/Create', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify(hoaDonTrucTiep)
});

// Tạo hóa đơn online
const hoaDonOnline = {
  maKH: "KH001",
  ghiChu: "Đặt hàng online",
  tongTien: 150000,
  items: [
    {
      maThuoc: "THUOC001",
      soLuong: 10,
      donGia: 15000
    }
  ]
};

const hdolResponse = await fetch('/api/HoaDon/CreateOnline', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify(hoaDonOnline)
});

// Tạo thanh toán PayOS
const paymentData = {
  amount: 150000,
  description: "Thanh toán hóa đơn",
  returnUrl: "https://yourapp.com/success",
  cancelUrl: "https://yourapp.com/cancel"
};

const paymentResponse = await fetch('/api/SimplePayment/Create', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify(paymentData)
});

// Xác nhận hóa đơn online
const confirmData = {
  maHD: "HDOL20251117120000",
  maNV: "NV001"
};

const confirmResponse = await fetch('/api/HoaDon/ConfirmOnline', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify(confirmData)
});

// Cập nhật trạng thái
const statusData = {
  maHD: "HDOL20251117120000",
  trangThaiGiaoHang: 2
};

const statusResponse = await fetch('/api/HoaDon/UpdateStatus', {
  method: 'PUT',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify(statusData)
});
```

## Lưu ý kỹ thuật

### HoaDon API:
- Mã hóa đơn: HD (trực tiếp) / HDOL (online) + timestamp
- Xuất kho: Chỉ thực hiện khi ConfirmOnline hoặc Create trực tiếp
- Validation: Kiểm tra tồn kho trước khi tạo
- Tổng tiền: Tự động tính từ items

### PayOS API:
- HMAC-SHA256 signature validation
- ReturnUrl/CancelUrl: Cần HTTPS trong production
- OrderCode: Unique identifier cho mỗi thanh toán
- Amount: Đơn vị VND, không có dấu chấm

## Test API

### Sử dụng Swagger UI
1. Chạy: `dotnet run --launch-profile "https"`
2. Mở: `https://localhost:port/swagger`
3. Tìm endpoints trong `HoaDon` và `SimplePayment`

### Sử dụng PowerShell

```powershell
# Tạo hóa đơn trực tiếp
$hoaDonBody = @{
    maKH = "KH001"
    maNV = "NV001"
    ghiChu = "Test bán"
    tongTien = 150000
    items = @(
        @{
            maThuoc = "THUOC001"
            soLuong = 10
            donGia = 15000
        }
    )
} | ConvertTo-Json

Invoke-WebRequest -Uri "https://localhost:5001/api/HoaDon/Create" -Method POST -Body $hoaDonBody -ContentType "application/json" -SkipCertificateCheck

# Tạo thanh toán
$paymentBody = @{
    amount = 150000
    description = "Test payment"
    returnUrl = "https://example.com/success"
    cancelUrl = "https://example.com/cancel"
} | ConvertTo-Json

Invoke-WebRequest -Uri "https://localhost:5001/api/SimplePayment/Create" -Method POST -Body $paymentBody -ContentType "application/json" -SkipCertificateCheck
```

## Hỗ trợ

Kiểm tra logs server nếu gặp lỗi. Đảm bảo:
- Tồn kho đủ trước khi tạo hóa đơn
- maKH, maNV, maThuoc tồn tại
- Tổng tiền khớp với items
- PayOS credentials đúng
- URLs hợp lệ cho returnUrl/cancelUrl
# Hướng dẫn tạo hoá đơn (Trực tiếp và Online)

## Tổng quan

Module hoá đơn cho phép tạo và quản lý hoá đơn bán thuốc. Có 2 loại hoá đơn:
- **Hoá đơn trực tiếp (HD):** Tạo tại quầy, xuất kho ngay lập tức
- **Hoá đơn online (HDOL):** Đặt hàng online, chưa xuất kho, cần xác nhận sau

## Các endpoint chính

### 1. Tạo hoá đơn trực tiếp

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

**Response (thành công):**
```json
{
  "status": 0,
  "message": "Success",
  "data": {
    "maHD": "HD20251117120000",
    "ngayLap": "2025-11-17T12:00:00",
    "maKH": "KH001",
    "tenKH": "Nguyễn Văn A",
    "diaChiKH": "123 Đường ABC",
    "dienThoaiKH": "0123456789",
    "maNV": "NV001",
    "tenNV": "Trần Thị B",
    "tongTien": 150000,
    "ghiChu": "Bán lẻ tại quầy",
    "trangThaiGiaoHang": 3
  }
}
```

**Lưu ý:**
- Xuất kho ngay lập tức
- TrangThaiGiaoHang = 3 (Đã nhận)
- MaNV bắt buộc

### 2. Tạo hoá đơn online

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

**Response (thành công):**
```json
{
  "status": 0,
  "message": "Success",
  "data": {
    "maHD": "HDOL20251117120000",
    "ngayLap": "2025-11-17T12:00:00",
    "maKH": "KH001",
    "tenKH": "Nguyễn Văn A",
    "diaChiKH": "123 Đường ABC",
    "dienThoaiKH": "0123456789",
    "maNV": null,
    "tenNV": null,
    "tongTien": 150000,
    "ghiChu": "Đặt hàng online",
    "trangThaiGiaoHang": 0
  }
}
```

**Lưu ý:**
- Không xuất kho ngay (chỉ đặt hàng)
- TrangThaiGiaoHang = 0 (Đã đặt)
- MaNV = null

### 3. Xác nhận hoá đơn online

**POST** `/api/HoaDon/ConfirmOnline`

**Request Body:**
```json
{
  "maHD": "HDOL20251117120000",
  "maNV": "NV001",
  "items": [
    {
      "maThuoc": "THUOC001",
      "donVi": "Viên",
      "soLuong": 10,
      "donGia": 15000,
      "maLD": "LD001",
      "hanSuDung": "2026-11-17"
    }
  ]
}
```

**Response (thành công):**
```json
{
  "status": 0,
  "message": "Hoá đơn online đã được xác nhận và xuất kho thành công",
  "data": {
    "maHD": "HDOL20251117120000",
    "trangThaiGiaoHang": 1
  }
}
```

**Lưu ý:**
- Xuất kho và cập nhật lô thuốc
- TrangThaiGiaoHang = 1 (Đã xác nhận)

### 4. Cập nhật trạng thái giao hàng

**PATCH** `/api/HoaDon/UpdateStatus`

**Request Body:**
```json
{
  "maHD": "HDOL20251117120000",
  "trangThaiGiaoHang": 2
}
```

**Response (thành công):**
```json
{
  "status": 0,
  "message": "Success",
  "data": {
    "maHD": "HDOL20251117120000",
    "trangThaiGiaoHang": 2
  }
}
```

## Luồng nghiệp vụ hoá đơn online

### Luồng hoàn chỉnh theo yêu cầu

```
[Frontend: Giỏ hàng]
    |
    v
Khách hàng thêm sản phẩm --> Xử lý giỏ hàng trên FE
    |
    v
[Thanh toán online] --> Gọi PayOS API
    |
    v
Thanh toán thành công --> Lưu hoá đơn online (CreateOnline)
    |                    TrangThaiGiaoHang = 0 (Đã đặt)
    v
[Nhân viên xác nhận] --> Gọi ConfirmOnline
    |                    Xuất kho, TrangThaiGiaoHang = 1 (Đã xác nhận)
    v
[Cập nhật trạng thái] --> UpdateStatus (2: Đã giao, 3: Đã nhận)
```

### Chi tiết từng bước

#### Bước 1: Thêm giỏ hàng (Frontend)
- Khách hàng thêm sản phẩm vào giỏ
- Lưu trữ trên FE (localStorage, state, etc.)
- Tính tổng tiền, validate số lượng

#### Bước 2: Thanh toán online
```javascript
// Tạo payment link
const paymentRes = await fetch('/api/SimplePayment/Create', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    amount: totalAmount,
    description: 'Thanh toán đơn hàng',
    returnUrl: '/payment-success',
    cancelUrl: '/payment-cancel'
  })
});

// Redirect đến PayOS
window.location.href = paymentRes.data.paymentUrl;
```

#### Bước 3: Lưu hoá đơn sau thanh toán
```javascript
// Tại trang success, sau khi check thanh toán thành công
const invoiceRes = await fetch('/api/HoaDon/CreateOnline', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    maKH: customer.maKH,
    ghiChu: 'Đặt hàng online',
    tongTien: totalAmount,
    items: cartItems
  })
});
```

#### Bước 4: Nhân viên xác nhận
```javascript
// Nhân viên xem đơn hàng pending, xác nhận và xuất kho
const confirmRes = await fetch('/api/HoaDon/ConfirmOnline', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    maHD: 'HDOL20251117120000',
    maNV: 'NV001',
    items: detailedItemsWithLots // Chi tiết với lô thuốc
  })
});
```

#### Bước 5: Cập nhật trạng thái giao hàng
```javascript
// Khi bắt đầu giao hàng
await fetch('/api/HoaDon/UpdateStatus', {
  method: 'PATCH',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    maHD: 'HDOL20251117120000',
    trangThaiGiaoHang: 2 // Đã giao
  })
});

// Khi khách nhận hàng
await fetch('/api/HoaDon/UpdateStatus', {
  method: 'PATCH',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    maHD: 'HDOL20251117120000',
    trangThaiGiaoHang: 3 // Đã nhận
  })
});
```

### Hoá đơn trực tiếp (đơn giản hơn)

```
Khách hàng đến quầy --> Nhân viên nhập thông tin
    |
    v
Kiểm tra tồn kho --> Tạo hoá đơn (Create)
    |
    v
Xuất kho ngay --> In hoá đơn --> Hoàn thành
                  TrangThaiGiaoHang = 3 (Đã nhận)
```

## Trạng thái giao hàng

- **0:** Đã đặt (online, chưa xác nhận)
- **1:** Đã xác nhận (online, đã xuất kho)
- **2:** Đã giao (đang giao)
- **3:** Đã nhận (hoàn thành)

## Xử lý lỗi

Tất cả API trả về response thống nhất:

```json
{
  "status": -1,
  "message": "Mô tả lỗi",
  "data": null
}
```

**Lỗi thường gặp:**
- Khách hàng không tồn tại
- Thuốc không đủ tồn kho (trực tiếp)
- Số lượng không hợp lệ
- Tổng tiền không khớp

## Ví dụ sử dụng từ frontend

### JavaScript / Fetch API

```javascript
// Tạo hoá đơn trực tiếp
const directInvoice = await fetch('/api/HoaDon/Create', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    maKH: "KH001",
    maNV: "NV001",
    ghiChu: "Bán lẻ",
    tongTien: 150000,
    items: [
      {
        maThuoc: "THUOC001",
        soLuong: 10,
        donGia: 15000
      }
    ]
  })
});

// Tạo hoá đơn online
const onlineInvoice = await fetch('/api/HoaDon/CreateOnline', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
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
  })
});

// Xác nhận online
const confirm = await fetch('/api/HoaDon/ConfirmOnline', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    maHD: "HDOL20251117120000",
    maNV: "NV001",
    items: [
      {
        maThuoc: "THUOC001",
        donVi: "Viên",
        soLuong: 10,
        donGia: 15000,
        maLD: "LD001",
        hanSuDung: "2026-11-17"
      }
    ]
  })
});
```

## Lưu ý kỹ thuật

- Mã hoá đơn trực tiếp: `HD{yyyyMMddHHmmss}{random}`
- Mã hoá đơn online: `HDOL{yyyyMMddHHmmss}{random}`
- Mã chi tiết: `CTHD{yyyyMMddHHmmss}{random}`
- Tất cả mã đều duy nhất
- Trực tiếp: xuất kho ngay, không cần lô thuốc
- Online: cần ConfirmOnline để xuất kho và chỉ định lô

## Test API

### Sử dụng Swagger UI
1. Chạy: `dotnet run --launch-profile "https"`
2. Mở: `https://localhost:port/swagger`
3. Tìm endpoints trong `HoaDon`

### Sử dụng PowerShell

```powershell
# Tạo hoá đơn trực tiếp
$body = @{
    maKH = "KH001"
    maNV = "NV001"
    ghiChu = "Test trực tiếp"
    tongTien = 150000
    items = @(
        @{
            maThuoc = "THUOC001"
            soLuong = 10
            donGia = 15000
        }
    )
} | ConvertTo-Json -Depth 10

Invoke-WebRequest -Uri "https://localhost:5001/api/HoaDon/Create" `
    -Method POST `
    -Headers @{ "Content-Type" = "application/json" } `
    -Body $body `
    -SkipCertificateCheck
```

## Hỗ trợ

Kiểm tra logs server nếu gặp lỗi. Đảm bảo:
- MaKH tồn tại
- MaNV tồn tại (trực tiếp)
- Thuốc có đủ tồn kho (trực tiếp)
- Dữ liệu items hợp lệ
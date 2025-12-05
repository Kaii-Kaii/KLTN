# Hướng dẫn tích hợp PayOS

## Tổng quan

Dự án này đã tích hợp PayOS để xử lý thanh toán trực tuyến. Các endpoint cho phép:
- Tạo link thanh toán từ số tiền, mô tả, và URL trả về/cancel
- Kiểm tra trạng thái giao dịch thanh toán
- Verify chữ ký khi nhận webhook từ PayOS

## Cấu hình môi trường

### Tệp `.env` (khuyến nghị)

Lưu biến môi trường trong file `BE_QLTiemThuoc/.env` (file này đã được thêm vào `.gitignore`):

```
ConnectionStrings__DefaultConnection=Data Source=DESKTOP-A4AOROR;Initial Catalog=QuanLyTiemThuoc;Integrated Security=True;TrustServerCertificate=True

Cloudinary__CloudName=your-cloud-name
Cloudinary__ApiKey=your-cloud-api-key
Cloudinary__ApiSecret=your-cloud-api-secret
Cloudinary__Folder=assets

PayOS__ClientId=your-payos-client-id
PayOS__ApiKey=your-payos-api-key
PayOS__ChecksumKey=your-payos-checksum-key

ASPNETCORE_ENVIRONMENT=Development
```

### Ưu tiên cấu hình

`Program.cs` gọi `Env.Load()` sớm để nạp file `.env`. Quy tắc ưu tiên:
1. **Biến môi trường** (từ `.env` hoặc hệ thống) — có độ ưu tiên cao nhất
2. **`appsettings.json`** — fallback nếu biến môi trường không tìm thấy

Ví dụ trong controller:
```csharp
string clientId = Environment.GetEnvironmentVariable("PayOS__ClientId") 
                  ?? _configuration["PayOS:ClientId"] 
                  ?? "";
```

## Các endpoint chính

### 1. Tạo link thanh toán

**POST** `/api/SimplePayment/Create`

**Request Body:**
```json
{
  "amount": 150000,
  "description": "Thanh toán thuốc Panadol",
  "returnUrl": "https://your-domain.com/payment-success",
  "cancelUrl": "https://your-domain.com/payment-cancel"
}
```

**Response (thành công):**
```json
{
  "success": true,
  "data": {
    "paymentUrl": "https://pay.payos.vn/web/...",
    "orderCode": "1731645234",
    "amount": 150000,
    "message": "Tạo giao dịch thành công"
  }
}
```

**Lưu ý:**
- Số tiền phải từ 2,000 đến 50,000,000 VND
- `returnUrl` và `cancelUrl` là bắt buộc và phải được cung cấp trong request body
- Endpoint tự động tạo `orderCode` từ timestamp hiện tại

### 2. Kiểm tra trạng thái thanh toán

**GET** `/api/SimplePayment/Status/{orderCode}`

**Ví dụ:**
```
GET /api/SimplePayment/Status/1731645234
```

**Response:**
```json
{
  "success": true,
  "data": {
    "orderCode": "1731645234",
    "status": "PAID",
    "isPaid": true,
    "amount": 150000,
    "message": "Thanh toán thành công"
  }
}
```

**Trạng thái có thể:**
- `PAID` → Thanh toán thành công
- `PENDING` → Đang chờ thanh toán
- `CANCELLED` → Đã hủy
- `UNKNOWN` → Không xác định

## Cách sử dụng từ frontend

### JavaScript / Fetch API

```javascript
// 1. Tạo payment link
const response = await fetch('/api/SimplePayment/Create', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    amount: 150000,
    description: "Thanh toán đơn hàng",
    returnUrl: "https://your-domain.com/payment-success",
    cancelUrl: "https://your-domain.com/payment-cancel"
  })
});

const result = await response.json();

if (result.success) {
  // Chuyển hướng khách hàng tới PayOS
  window.location.href = result.data.paymentUrl;
}

// 2. Sau khi thanh toán, check trạng thái
const statusResponse = await fetch(`/api/SimplePayment/Status/${orderCode}`);
const status = await statusResponse.json();

if (status.data.isPaid) {
  console.log('Thanh toán thành công!');
}
```

## Chi tiết kỹ thuật

### Chữ ký (Signature)

Controller sử dụng HMAC-SHA256 để tạo chữ ký:

```csharp
string signatureData = $"amount={amount}&cancelUrl={cancelUrl}&description={description}&orderCode={orderCode}&returnUrl={returnUrl}";
string signature = ComputeHmacSha256(signatureData, checksumKey);
```

Các tham số được sắp xếp theo thứ tự bảng chữ cái và được kết nối bằng `&`.

### Headers HTTP

Khi gọi API PayOS, controller gửi:
```
x-client-id: {PayOS__ClientId}
x-api-key: {PayOS__ApiKey}
```

## Webhook (tùy chọn)

Để nhận thông báo từ PayOS khi trạng thái giao dịch thay đổi:

1. Cấu hình URL webhook trong PayOS dashboard: `https://your-domain.com/api/SimplePayment/Webhook`
2. PayOS sẽ gửi POST request với payload chứa thông tin giao dịch
3. Implement endpoint webhook để verify chữ ký và cập nhật trạng thái trong database

## Kiểm thử cục bộ

### Xây dựng và chạy

```powershell
cd i:\Ky_06_2025_2026\KhoaLuan\DoAn\QLTiemThuoc\BE_QLTiemThuoc
dotnet build
dotnet run --launch-profile "https"
```

### Test bằng Swagger UI

1. Mở browser: `https://localhost:port/swagger`
2. Tìm endpoint `POST /api/SimplePayment/Create`
3. Nhập request body:
   ```json
   {
     "amount": 150000,
     "description": "Test payment",
     "returnUrl": "https://your-domain.com/success",
     "cancelUrl": "https://your-domain.com/cancel"
   }
   ```
4. Bấm "Execute"
5. Xem console log để kiểm tra request/response từ PayOS

### Test bằng PowerShell

```powershell
$headers = @{
    'Content-Type' = 'application/json'
}

$body = @{
    amount = 150000
    description = "Test payment"
    returnUrl = "https://your-domain.com/success"
    cancelUrl = "https://your-domain.com/cancel"
} | ConvertTo-Json

$response = Invoke-WebRequest -Uri "https://localhost:5001/api/SimplePayment/Create" `
    -Method POST `
    -Headers $headers `
    -Body $body `
    -SkipCertificateCheck

$response.Content | ConvertFrom-Json | ConvertTo-Json -Depth 10
```

## Xử lý lỗi

Controller trả về JSON error response cho tất cả lỗi:

```json
{
  "success": false,
  "message": "Chi tiết lỗi ở đây"
}
```

Kiểm tra console log (controller in ra `[PayOS] Request:` và `[PayOS] Response:`) để debug:

```
[PayOS] Request: {"orderCode":1731645234,"amount":150000,...}
[PayOS] Response: {"code":"00","desc":"Success","data":{"checkoutUrl":"https://pay.payos.vn/web/...",...}}
```

## 🔄 Workflow thanh toán

1. **Khách hàng chọn thanh toán** → Gọi API tạo payment link với returnUrl và cancelUrl
2. **Nhận CheckoutUrl** → Redirect khách hàng đến PayOS
3. **Khách hàng thanh toán** → PayOS xử lý thanh toán
4. **PayOS redirect về returnUrl** → Xử lý thành công
5. **PayOS redirect về cancelUrl** → Xử lý hủy
6. **PayOS gửi webhook** → API nhận thông báo kết quả
7. **Cập nhật trạng thái** → Cập nhật database và gửi email xác nhận

## ⚠️ Lưu ý bảo mật
- Đã sử dụng HTTP Client thay vì SDK để tránh lỗi dependency
- Thông tin cấu hình được lưu an toàn trong appsettings
- Webhook có thể được mở rộng để verify signature từ PayOS

## 🧪 Test API
Để test API, bạn có thể sử dụng Swagger UI hoặc Postman với các endpoint đã tạo.

## 📞 Hỗ trợ
- PayOS Documentation: https://payos.vn/docs/
- API Base URL: https://api-merchant.payos.vn/v2/
# Hướng dẫn thao tác nhập thuốc (Phiếu Nhập)

## Tổng quan

Module phiếu nhập cho phép quản lý việc nhập thuốc vào kho. Bao gồm:
- Tạo phiếu nhập mới với chi tiết thuốc
- Lấy danh sách phiếu nhập theo khoảng thời gian
- Xem chi tiết phiếu nhập

## Các endpoint chính

### 1. Lấy danh sách phiếu nhập theo khoảng thời gian

**GET** `/api/PhieuNhap/GetByDateRange`

**Query Parameters:**
- `startDate` (bắt buộc): Ngày bắt đầu (định dạng: yyyy-MM-dd, yyyy-MM-ddTHH:mm:ss, hoặc dd/MM/yyyy)
- `endDate` (bắt buộc): Ngày kết thúc (định dạng tương tự)
- `maNV` (tùy chọn): Mã nhân viên để lọc
- `maNCC` (tùy chọn): Mã nhà cung cấp để lọc

**Ví dụ request:**
```
GET /api/PhieuNhap/GetByDateRange?startDate=2025-01-01&endDate=2025-12-31&maNV=NV001&maNCC=NCC001
```

**Response (thành công):**
```json
{
  "status": 0,
  "message": "Success",
  "data": [
    {
      "maPN": "PN20251117000001",
      "ngayNhap": "2025-11-17T10:00:00",
      "maNV": "NV001",
      "tenNV": "Nguyễn Văn A",
      "maNCC": "NCC001",
      "tenNCC": "Công ty Dược phẩm ABC",
      "tongTien": 1500000,
      "ghiChu": "Nhập thuốc bổ sung"
    }
  ]
}
```

### 2. Tạo phiếu nhập mới

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

**Lưu ý:**
- `chiTietPhieuNhaps`: Danh sách chi tiết thuốc nhập (bắt buộc)
- `loThuocHSDs`: Danh sách lô thuốc (tùy chọn, nếu không cung cấp sẽ tự động tạo từ chiTietPhieuNhaps)

**Response (thành công):**
```json
{
  "status": 0,
  "message": "Phiếu nhập đã được tạo thành công",
  "data": {
    "maPN": "PN20251117000001",
    "ngayNhap": "2025-11-17T10:00:00",
    "maNV": "NV001",
    "maNCC": "NCC001",
    "tongTien": 1500000,
    "ghiChu": "Nhập thuốc bổ sung"
  }
}
```

### 3. Lấy chi tiết phiếu nhập

**GET** `/api/PhieuNhap/GetChiTietPhieuNhapByMaPN`

**Query Parameters:**
- `maPN` (bắt buộc): Mã phiếu nhập

**Ví dụ request:**
```
GET /api/PhieuNhap/GetChiTietPhieuNhapByMaPN?maPN=PN20251117000001
```

**Response (thành công):**
```json
{
  "status": 0,
  "message": "Success",
  "data": [
    {
      "maCTPN": "CTPN20251117000001",
      "maPN": "PN20251117000001",
      "maThuoc": "THUOC001",
      "tenThuoc": "Paracetamol",
      "soLuong": 100,
      "donGia": 15000,
      "thanhTien": 1500000,
      "hanSuDung": "2026-11-17T00:00:00",
      "maLD": "LD001",
      "tenLD": "Viên nén",
      "maLo": "LO20251117000001"
    }
  ]
}
```

## Quy trình nhập thuốc

### Bước 1: Chuẩn bị dữ liệu
- Thu thập thông tin nhà cung cấp (maNCC)
- Thu thập thông tin nhân viên nhập (maNV)
- Liệt kê các loại thuốc cần nhập với số lượng, đơn giá, hạn sử dụng

### Bước 2: Tạo phiếu nhập
- Gọi API `POST /api/PhieuNhap/AddPhieuNhap` với dữ liệu chi tiết
- Hệ thống sẽ:
  - Tạo mã phiếu nhập tự động (PN + timestamp)
  - Tạo chi tiết phiếu nhập
  - Tạo lô thuốc và cập nhật tồn kho
  - Tính tổng tiền

### Bước 3: Xác nhận và kiểm tra
- Sử dụng `GET /api/PhieuNhap/GetChiTietPhieuNhapByMaPN` để kiểm tra chi tiết
- Sử dụng `GET /api/PhieuNhap/GetByDateRange` để xem danh sách phiếu nhập

## Diagram quy trình nhập thuốc

```
[Frontend/User] --> Chuẩn bị dữ liệu nhập thuốc
    |
    v
[API: POST /api/PhieuNhap/AddPhieuNhap] --> Validate dữ liệu
    |
    v
Tạo phiếu nhập (MaPN) --> Tạo chi tiết phiếu nhập
    |
    v
Tạo lô thuốc (MaLo) --> Cập nhật tồn kho (TonKho)
    |
    v
Tính tổng tiền --> Lưu vào database
    |
    v
[Response: Thành công] --> Trả về MaPN
    |
    v
[Frontend] --> Hiển thị xác nhận hoặc in phiếu
```

### Luồng xử lý chi tiết

1. **Validate input:**
   - Kiểm tra maNV, maNCC tồn tại
   - Validate chiTietPhieuNhaps (maThuoc, soLuong > 0, donGia > 0, hanSuDung hợp lệ)

2. **Tạo phiếu nhập:**
   - Generate MaPN: PN + timestamp + random
   - Insert vào PhieuNhap table

3. **Xử lý chi tiết:**
   - For each chiTietPhieuNhap:
     - Generate MaCTPN
     - Insert vào ChiTietPhieuNhap
     - Nếu loThuocHSDs không cung cấp: tự động tạo lô từ chiTiet
     - Update TonKho: tăng soLuongTon

4. **Tính toán:**
   - Tổng tiền = sum(soLuong * donGia) cho tất cả chiTiet

5. **Response:**
   - Trả về thông tin phiếu nhập đã tạo

## Xử lý lỗi

Tất cả API trả về response với format thống nhất:

```json
{
  "status": -1,
  "message": "Mô tả lỗi",
  "data": null
}
```

**Các lỗi thường gặp:**
- Ngày tháng không hợp lệ
- Thiếu thông tin bắt buộc
- Mã nhà cung cấp hoặc nhân viên không tồn tại
- Số lượng hoặc đơn giá không hợp lệ

## Ví dụ sử dụng từ frontend

### JavaScript / Fetch API

```javascript
// 1. Tạo phiếu nhập
const response = await fetch('/api/PhieuNhap/AddPhieuNhap', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
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
  })
});

const result = await response.json();
console.log('Phiếu nhập đã tạo:', result.data.maPN);

// 2. Lấy chi tiết phiếu nhập
const detailResponse = await fetch(`/api/PhieuNhap/GetChiTietPhieuNhapByMaPN?maPN=${result.data.maPN}`);
const details = await detailResponse.json();
console.log('Chi tiết phiếu nhập:', details.data);
```

## Lưu ý kỹ thuật

- Mã phiếu nhập được tạo tự động theo format: `PN{yyyyMMddHHmmss}{random}`
- Mã chi tiết phiếu nhập: `CTPN{yyyyMMddHHmmss}{random}`
- Mã lô thuốc: `LO{yyyyMMddHHmmss}{random}`
- Tất cả các mã đều đảm bảo duy nhất
- Khi tạo phiếu nhập, hệ thống sẽ tự động cập nhật tồn kho
- Hạn sử dụng phải là ngày trong tương lai

## Test API

### Sử dụng Swagger UI
1. Chạy project: `dotnet run --launch-profile "https"`
2. Mở browser: `https://localhost:port/swagger`
3. Tìm các endpoint trong `PhieuNhap`

### Sử dụng PowerShell

```powershell
# Tạo phiếu nhập
$body = @{
    maNV = "NV001"
    maNCC = "NCC001"
    ghiChu = "Test nhập thuốc"
    chiTietPhieuNhaps = @(
        @{
            maThuoc = "THUOC001"
            soLuong = 50
            donGia = 10000
            hanSuDung = "2026-12-31"
            maLD = "LD001"
        }
    )
} | ConvertTo-Json -Depth 10

Invoke-WebRequest -Uri "https://localhost:5001/api/PhieuNhap/AddPhieuNhap" `
    -Method POST `
    -Headers @{ "Content-Type" = "application/json" } `
    -Body $body `
    -SkipCertificateCheck
```

## Hỗ trợ

Nếu gặp vấn đề, kiểm tra:
- Định dạng ngày tháng
- Mã nhân viên và nhà cung cấp có tồn tại
- Dữ liệu thuốc hợp lệ
- Logs của server để debug
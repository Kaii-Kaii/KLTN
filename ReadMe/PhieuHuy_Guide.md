# Hướng dẫn sử dụng API Phiếu hủy thuốc

## Tổng quan

API Phiếu hủy thuốc là hệ thống quản lý việc hủy bỏ thuốc trong nhà thuốc với 2 loại hủy chính:

### 1. **Hủy từ kho (LoaiHuy = "KHO")**
- Thuốc hết hạn sử dụng
- Thuốc hỏng, vỡ bao bì
- Thuốc không đạt chất lượng
- Thuốc tồn kho quá lâu

### 2. **Hủy từ hóa đơn (LoaiHuy = "HOADON")**
- Xử lý khi khách hàng trả lại thuốc
- Thuốc trong hóa đơn bị hủy một phần
- Thuốc sắp hết hạn trong hóa đơn
- Lỗi giao hàng hoặc sai thuốc

## Cấu trúc Database

### Bảng PhieuHuy
```sql
CREATE TABLE PhieuHuy (
    MaPH NVARCHAR(20) PRIMARY KEY,           -- Mã phiếu hủy (PH + 10 số)
    NgayHuy DATETIME NOT NULL,               -- Ngày tạo phiếu hủy
    MaNV NVARCHAR(10) NOT NULL,              -- Mã nhân viên thực hiện
    LoaiHuy NVARCHAR(10) NOT NULL,           -- "KHO" hoặc "HOADON"
    MaHD NVARCHAR(20) NULL,                  -- Mã hóa đơn (khi LoaiHuy = "HOADON")
    TongSoLuongHuy DECIMAL(18,2) NOT NULL,   -- Tổng số lượng thuốc hủy
    GhiChu NVARCHAR(MAX) NULL                -- Ghi chú thêm
);
```

### Bảng ChiTietPhieuHuy
```sql
CREATE TABLE ChiTietPhieuHuy (
    MaCTPH NVARCHAR(20) PRIMARY KEY,         -- Mã chi tiết phiếu hủy
    MaPH NVARCHAR(20) NOT NULL,              -- Mã phiếu hủy (FK)
    MaLo NVARCHAR(20) NOT NULL,              -- Mã lô thuốc (FK đến TonKho)
    SoLuongHuy DECIMAL(18,2) NOT NULL,       -- Số lượng hủy
    LyDoHuy NVARCHAR(500) NOT NULL,          -- Lý do hủy
    GhiChu NVARCHAR(MAX) NULL,               -- Ghi chú chi tiết

    FOREIGN KEY (MaPH) REFERENCES PhieuHuy(MaPH),
    FOREIGN KEY (MaLo) REFERENCES TonKho(MaLo)
);
```

## API Endpoints

### 1. Hủy thuốc từ kho
**POST** `/api/PhieuHuy/huy-tu-kho`

### 2. Hủy thuốc từ hóa đơn
**POST** `/api/PhieuHuy/huy-tu-hoa-don`

### 3. Tra cứu phiếu hủy theo khoảng thời gian
**GET** `/api/PhieuHuy/GetByDateRange?startDate={yyyy-MM-dd}&endDate={yyyy-MM-dd}`

### 4. Lấy chi tiết phiếu hủy
**GET** `/api/PhieuHuy/GetChiTietPhieuHuy/{maPH}`

### 5. Tạo phiếu hủy (deprecated - sử dụng hủy từ kho thay thế)
**POST** `/api/PhieuHuy/CreatePhieuHuy`

### 6. Hủy hóa đơn (deprecated - sử dụng hủy từ hóa đơn thay thế)
**POST** `/api/PhieuHuy/HuyHoaDon`

## Chi tiết các API

## Chi tiết các API

### 1. Hủy thuốc từ kho (huy-tu-kho)

**Endpoint:** `POST /api/PhieuHuy/huy-tu-kho`

**Mô tả:** Hủy thuốc trực tiếp từ kho với lý do cụ thể

**Request Body:**
```json
{
  "maNhanVien": "NV001",
  "lyDoHuy": "Thuốc sắp hết hạn sử dụng",
  "chiTietPhieuHuy": [
    {
      "maLo": "LO001",
      "soLuongHuy": 20,
      "lyDoChiTiet": "Hạn sử dụng còn 2 tháng"
    },
    {
      "maLo": "LO002",
      "soLuongHuy": 15,
      "lyDoChiTiet": "Bao bì bị hỏng"
    }
  ]
}
```

**Validation Rules:**
- `maNhanVien`: Bắt buộc, phải tồn tại
- `lyDoHuy`: Bắt buộc, tối đa 1000 ký tự
- `chiTietPhieuHuy`: Ít nhất 1 item
- `maLo`: Phải tồn tại trong TonKho
- `soLuongHuy`: > 0 và ≤ SoLuongCon hiện tại
- Nếu thuốc còn hạn > 12 tháng: bắt buộc có `lyDoChiTiet`

**Response Success:**
```json
{
  "status": 0,
  "message": "Success",
  "data": {
    "maPhieuHuy": "PH20241201001",
    "ngayHuy": "2024-12-01T10:30:00",
    "tongSoLuongHuy": 35,
    "chiTietPhieuHuy": [
      {
        "maLo": "LO001",
        "tenThuoc": "Paracetamol 500mg",
        "soLuongHuy": 20,
        "donGia": 5000
      }
    ],
    "message": "Tạo phiếu hủy từ kho thành công"
  }
}
```

### 2. Hủy thuốc từ hóa đơn (huy-tu-hoa-don)

**Endpoint:** `POST /api/PhieuHuy/huy-tu-hoa-don`

**Mô tả:** Xử lý hủy từ hóa đơn với 2 lựa chọn: hủy hoặc hoàn lại kho

**Request Body:**
```json
{
  "maHoaDon": "HD20241128001",
  "maNhanVien": "NV001",
  "lyDoHuy": "Khách hàng yêu cầu hủy đơn",
  "chiTietXuLy": [
    {
      "maLo": "LO003",
      "loaiXuLy": "HUY",
      "soLuongHuy": 5,
      "lyDoChiTiet": "Bao bì bị rách"
    },
    {
      "maLo": "LO004",
      "loaiXuLy": "HOAN_LAI",
      "lyDoChiTiet": "Thuốc còn tốt, hoàn lại kho"
    }
  ]
}
```

**Validation Rules:**
- `maHoaDon`: Bắt buộc, phải tồn tại
- `maNhanVien`: Bắt buộc, phải tồn tại
- `chiTietXuLy`: Ít nhất 1 item
- `maLo`: Phải thuộc về hóa đơn được chỉ định
- `loaiXuLy`: Chỉ "HUY" hoặc "HOAN_LAI"
- Khi `loaiXuLy = "HUY"`: `soLuongHuy` bắt buộc
- Khi thuốc còn hạn > 6 tháng và `loaiXuLy = "HUY"`: bắt buộc có `lyDoChiTiet`

**Response Success:**
```json
{
  "status": 0,
  "message": "Success",
  "data": {
    "maPhieuHuy": "PH20241201002",
    "maHoaDon": "HD20241128001",
    "trangThaiGiaoHang": -2,
    "tongSoLuongHuy": 5,
    "tongSoLuongHoanLai": 10,
    "chiTietXuLy": [
      {
        "maLo": "LO003",
        "tenThuoc": "Amoxicillin 500mg",
        "loaiXuLy": "HUY",
        "soLuong": 5
      },
      {
        "maLo": "LO004",
        "tenThuoc": "Vitamin C 1000mg",
        "loaiXuLy": "HOAN_LAI",
        "soLuong": 10
      }
    ],
    "message": "Xử lý hủy hóa đơn thành công: hủy 5 sản phẩm, hoàn lại 10 sản phẩm"
  }
}
```

### 3. Tra cứu phiếu hủy theo khoảng thời gian

**Endpoint:** `GET /api/PhieuHuy/GetByDateRange?startDate=2025-11-01&endDate=2025-11-30`

**Mô tả:** Lấy danh sách phiếu hủy trong khoảng thời gian

**Parameters:**
- `startDate`: Ngày bắt đầu (yyyy-MM-dd)
- `endDate`: Ngày kết thúc (yyyy-MM-dd)

**Response:**
```json
{
  "status": 0,
  "message": "Success",
  "data": [
    {
      "maPH": "PH0000000001",
      "ngayHuy": "2025-11-18T14:30:00",
      "maNV": "NV001",
      "loaiHuy": "KHO",
      "maHD": null,
      "tongSoLuongHuy": 50.0,
      "ghiChu": "Hủy thuốc hết hạn"
    }
  ]
}
```

### 4. Lấy chi tiết phiếu hủy

**Endpoint:** `GET /api/PhieuHuy/GetChiTietPhieuHuy/{maPH}`

**Mô tả:** Lấy thông tin chi tiết của một phiếu hủy bao gồm danh sách thuốc

**Response:**
```json
{
  "status": 0,
  "message": "Success",
  "data": {
    "phieuHuy": {
      "maPH": "PH0000000001",
      "ngayHuy": "2025-11-18T14:30:00",
      "maNV": "NV001",
      "loaiHuy": "KHO",
      "maHD": null,
      "tongSoLuongHuy": 50.0,
      "ghiChu": "Hủy thuốc hết hạn"
    },
    "chiTiet": [
      {
        "maLo": "LO001",
        "soLuongHuy": 50.0,
        "lyDoHuy": "Thuốc hết hạn sử dụng",
        "ghiChu": "Paracetamol 500mg",
        "tenThuoc": "Paracetamol 500mg",
        "hanSuDung": "2025-12-31T00:00:00",
        "donViTinh": "Viên"
      }
    ]
  }
}
```

## Quy trình nghiệp vụ chi tiết

### Quy trình hủy từ kho

**Luồng xử lý theo mô tả:**
1. **Chọn lô thuốc cần hủy** từ danh sách tồn kho
2. **Tạo phiếu hủy** với lý do cụ thể cho từng lô

**Chi tiết các bước:**
1. **Kiểm tra tồn kho**: Nhân viên kho xem danh sách lô thuốc
2. **Đánh giá điều kiện hủy**:
   - Thuốc hết hạn (< 3 tháng)
   - Thuốc hỏng, vỡ bao bì
   - Thuốc tồn kho quá lâu
   - Lý do đặc biệt khác
3. **Chọn lô thuốc**: Chọn các lô cần hủy từ danh sách
4. **Nhập thông tin hủy**:
   - Mã nhân viên thực hiện
   - Lý do hủy tổng quát
   - Chi tiết từng lô (số lượng, lý do cụ thể)
5. **Validation**: Kiểm tra tồn kho, hạn sử dụng
6. **Tạo phiếu hủy**: Tạo MaPH, lưu thông tin
7. **Cập nhật tồn kho**: Giảm SoLuongCon
8. **Hoàn tất**: Trả về thông tin phiếu hủy

### Quy trình hủy từ hóa đơn

**Luồng xử lý theo mô tả:**
1. **Khách hủy hàng ở hóa đơn**
2. **Kiểm tra chi tiết hóa đơn** (đặc biệt hạn sử dụng)
3. **Chọn chi tiết cần hủy** (- các sản phẩm còn lại không chọn là chuyển vào kho)
4. **Tạo phiếu hủy** cho phần được chọn hủy

**Chi tiết các bước:**
1. **Tiếp nhận yêu cầu**: Khách hàng yêu cầu hủy/trả hàng
2. **Xác minh hóa đơn**: Kiểm tra mã HD, trạng thái
3. **Kiểm tra chi tiết**: Xem từng lô thuốc trong hóa đơn
4. **Đánh giá từng loại thuốc**:
   - **Hạn sử dụng**: Còn hạn > 6 tháng → ưu tiên hoàn lại
   - **Tình trạng bao bì**: Nguyên vẹn → hoàn lại
   - **Nhu cầu sử dụng**: Thuốc quý hiếm → hoàn lại
   - **Tình trạng thuốc**: Hỏng → hủy
5. **Phân loại xử lý**:
   - **HUY**: Thuốc cần hủy (tạo phiếu hủy, giảm tồn kho)
   - **HOAN_LAI**: Thuốc còn tốt (tăng tồn kho, không tạo phiếu hủy)
6. **Xử lý hoàn lại**: Tăng SoLuongCon trong TonKho
7. **Tạo phiếu hủy**: Chỉ cho thuốc được chọn "HUY"
8. **Cập nhật trạng thái**: Hóa đơn → -2 (hoàn tất) hoặc -3 (còn xử lý)
9. **Thông báo kết quả**: Xác nhận với khách hàng

## Flow Diagrams

### Flow Diagram - Hủy từ kho

```
┌─────────────────┐
│   Bắt đầu       │
└─────────────────┘
          │
          ▼
┌─────────────────┐
│ Kiểm tra tồn kho│
│ - Xem lô sắp HSD│
│ - Check SL thực │
└─────────────────┘
          │
          ▼
┌─────────────────┐
│   Tạo phiếu hủy │
│ - Chọn NV       │
│ - Nhập lý do    │
│ - Xác nhận SL   │
└─────────────────┘
          │
          ▼
┌─────────────────┐       ┌─────────────────┐
│Validate dữ liệu │────▶ │  Lỗi validation │
│- MaLo tồn tại   │       │  Return error   │
│- SL > 0 & <= SL │       └─────────────────┘
│  tồn kho        │
└─────────────────┘
          │
          ▼
┌─────────────────┐
│Cập nhật tồn kho │
│TonKho.SoLuongCon│
│    -= SL hủy    │
└─────────────────┘
          │
          ▼
┌─────────────────┐
│ Lưu phiếu hủy   │
│- Tạo MaPH       │
│- Lưu PhieuHuy   │
│- Lưu ChiTietPH  │
└─────────────────┘
          │
          ▼
┌─────────────────┐
│   Kết thúc      │
│ Return success  │
└─────────────────┘
```

### Flow Diagram - Hủy từ hóa đơn

```
┌─────────────────┐
│   Bắt đầu       │
└─────────────────┘
          │
          ▼
┌─────────────────┐
│Nhận yêu cầu hủy │
│từ khách hàng    │
└─────────────────┘
          │
          ▼
┌─────────────────┐     ┌─────────────────┐
│Validate hóa đơn │────▶│HĐ không tồn tại│
│- MaHD tồn tại   │     │  Return error   │
│- Trạng thái hợp │     └─────────────────┘
│  lệ             │
└─────────────────┘
          │
          ▼
┌─────────────────┐
│ Phân tích từng  │
│loại thuốc       │
│- Check HSD      │
│- Đánh giá bao bì│
│- Xác định xử lý │
└─────────────────┘
          │
          ▼
┌─────────────────┐
│ Xử lý từng loại │
│thuốc            │
│                 │
│FOR EACH thuốc:  │
│  ┌─────────────┐│
│  │Thuốc còn tốt││
│  │→ Hoàn lại   ││
│  │   kho       ││
│  └─────────────┘│
│         │       │
│  ┌─────────────┐│
│  │Thuốc cần hủy││
│  │→ Tạo phiếu  ││
│  │   hủy       ││
│  └─────────────┘│
└─────────────────┘
          │
          ▼
┌─────────────────┐
│ Cập nhật trạng  │
│thái hóa đơn     │
│-  Tất cả xử lý: │
│  TrangThai = -2 │
│- Còn lại: -3    │
└─────────────────┘
          │
          ▼
┌─────────────────┐
│Thông báo kết quả│
│- Xác nhận KH    │
│- Cung cấp biên  │
│   lai hủy       │
└─────────────────┘
          │
          ▼
┌─────────────────┐
│   Kết thúc      │
│ Return success  │
└─────────────────┘
```

### Sequence Diagram - Xử lý hủy hóa đơn

```
Client (Nhân viên)          API Controller          Service Layer          Repository          Database

        │                        │                     │                     │                     │
        │   POST /huy-tu-hoa-don │                     │                     │                     │
        ├──────────────────────▶│                     │                     │                     │
        │                        │                     │                     │                     │
        │                        │  ValidateRequest()  │                     │                     │
        │                        ├────────────────────▶│                     │                     │
        │                        │                     │                     │                     │
        │                        │                     │  GetHoaDonById()    │                     │
        │                        │                     ├────────────────────▶│                     │
        │                        │                     │                     │   SELECT * FROM HoaDon│
        │                        │                     │                     ├────────────────────▶│
        │                        │                     │                     │                     │
        │                        │                     │  GetChiTietHoaDon() │                     │
        │                        │                     ├────────────────────▶│                     │
        │                        │                     │                     │SELECT * FROM ChiTietHD│
        │                        │                     ├────────────────────▶│                     │
        │                        │                     │                     │                     │
        │                        │                     │  ValidateBusiness   │                     │
        │                        │                     │     Rules           │                     │
        │                        │                     ├────────────────────▶│                     │
        │                        │                     │                     │                     │
        │                        │                     │  BeginTransaction  │                     │
        │                        │                     ├────────────────────▶│                     │
        │                        │                     │                     │   BEGIN TRANSACTION  │
        │                        │                     │                     ├────────────────────▶│
        │                        │                     │                     │                     │
        │                        │                     │  ProcessEachItem    │                     │
        │                        │                     │  (Hoàn lại/Hủy)     │                     │
        │                        │                     ├────────────────────▶│                     │
        │                        │                     │                     │                     │
        │                        │                     │  UpdateTonKho()     │                     │
        │                        │                     ├────────────────────▶│                     │
        │                        │                     │                     │ UPDATE TonKho SET... │
        │                        │                     ├────────────────────▶│                     │
        │                        │                     │                     │                     │
        │                        │                     │  CreatePhieuHuy()   │                     │
        │                        │                     ├────────────────────▶│                     │
        │                        │                     │                     │ INSERT INTO PhieuHuy │
        │                        │                     ├────────────────────▶│                     │
        │                        │                     │                     │                     │
        │                        │                     │  UpdateHoaDonStatus │                     │
        │                        │                     ├────────────────────▶│                     │
        │                        │                     │                     │ UPDATE HoaDon SET... │
        │                        │                     ├────────────────────▶│                     │
        │                        │                     │                     │                     │
        │                        │                     │  CommitTransaction  │                     │
        │                        │                     ├────────────────────▶│                     │
        │                        │                     │                     │   COMMIT             │
        │                        │                     ├────────────────────▶│                     │
        │                        │                     │                     │                     │
        │                        │  ReturnSuccess()    │                     │                     │
        │                        ◀────────────────────┼─────────────────────┼─────────────────────┘
        │                        │                     │                     │                     │
```

### State Diagram - Trạng thái hóa đơn

```
┌─────────────────────────────────────────────────────────────────┐
│                        TRẠNG THÁI HÓA ĐƠN                       │
└─────────────────────────────────────────────────────────────────┘

┌─────────────┐
│  Đặt hàng   │
│ TrangThai = │
│     0       │
└─────────────┘
        │
        ▼
┌─────────────┐
│ Đang xử lý  │
│ TrangThai = │
│     1       │
└─────────────┘
        │
        ▼
┌─────────────┐     ┌─────────────────┐
│ Đã giao hàng│────▶│   Yêu cầu hủy   │
│ TrangThai = │     │                 │
│     2       │     └─────────────────┘
└─────────────┘             │
        │                   │
        ▼                   ▼
┌─────────────┐     ┌─────────────────┐
│ Hoàn thành  │     │  Đang xử lý hủy │
│ TrangThai = │     │ TrangThai = -1  │
│     3       │     └─────────────────┘
└─────────────┘             │
                            ▼
                    ┌─────────────────┐
                    │                 │
                    │   Xử lý từng    │
                    │   loại thuốc    │
                    │                 │
                    └─────────────────┘
                            │
                    ┌───────┴───────┐
                    │               │
            ┌───────▼───────┐ ┌─────▼──────┐
            │Tất cả đã xử lý│ │Còn chi tiết│
            │ TrangThai = -2│ │chưa xử lý  │
            │               │ │TrangThai=-3│
            └───────────────┘ └────────────┘
```

### Activity Diagram - Quy trình hủy thuốc

```
┌─────────────────────────────────────────────────────────────────┐
│                    QUY TRÌNH HỦY THUỐC                          │
└─────────────────────────────────────────────────────────────────┘

                    ┌─────────────┐
                    │Bắt đầu hủy  │
                    │thuốc        │
                    └─────────────┘
                            │
                            ▼
                    ┌─────────────┐
                    │Xác định loại│
                    │hủy          │
                    └─────────────┘
                            │
                ┌───────────┴───────────┐
                │                       │
        ┌───────▼───────┐     ┌─────────▼───────┐
        │  Hủy từ kho   │     │ Hủy từ hóa đơn  │
        │               │     │                 │
        └───────┬───────┘     └────────┬────────┘
                │                      │
                ▼                      ▼
        ┌─────────────┐        ┌─────────────┐
        │Kiểm tra tồn │        │Validate hóa │
        │kho          │        │đơn          │
        └─────────────┘        └─────────────┘
                │                      │
                ▼                      ▼
        ┌─────────────┐        ┌─────────────┐
        │Tạo phiếu    │        │Phân tích    │
        │hủy          │        │thuốc        │
        └─────────────┘        └─────────────┘
                │                      │
                ▼                      ▼
        ┌─────────────┐        ┌─────────────┐
        │Cập nhật     │        │Xử lý từng   │
        │tồn kho      │        │loại thuốc   │
        └─────────────┘        └─────────────┘
                │                      │
                ▼                      ▼
        ┌─────────────┐        ┌─────────────┐
        │Lưu phiếu    │        │Cập nhật     │
        │hủy          │        │trạng thái HĐ│
        └─────────────┘        └─────────────┘
                │                      │
                └──────────┬───────────┘
                           │
                           ▼
                    ┌─────────────┐
                    │Thông báo    │
                    │kết quả      │
                    └─────────────┘
                            │
                            ▼
                    ┌─────────────┐
                    │Kết thúc     │
                    └─────────────┘
```

### Use Case Diagram - Hệ thống phiếu hủy

```
┌─────────────────────────────────────────────────────────────────┐
│                        USE CASE DIAGRAM                         │
└─────────────────────────────────────────────────────────────────┘

                              ┌─────────────┐
                              │ Nhân viên   │
                              │ kho         │
                              └──────┬──────┘
                                     │
                                     │
                    ┌────────────────┼────────────────┐
                    │                │                │
            ┌───────▼───────┐ ┌──────▼──────┐ ┌──────▼──────┐
            │  Kiểm tra     │ │  Tạo phiếu  │ │  Duyệt phiếu│
            │  tồn kho      │ │  hủy        │ │  hủy        │
            └───────────────┘ └─────────────┘ └─────────────┘
                    │                │                │
                    └────────────────┼────────────────┘
                                     │
                                     ▼
                              ┌─────────────┐
                              │ Giám đốc    │
                              │ nhà thuốc   │
                              └──────┬──────┘
                                     │
                    ┌────────────────┼────────────────┐
                    │                │                │
            ┌───────▼───────┐ ┌──────▼──────┐ ┌──────▼──────┐
            │  Duyệt phiếu  │ │  Xem báo cáo│ │  Quản lý    │
            │  hủy lớn      │ │  hủy thuốc  │ │  chính sách │
            │               │ │             │ │  hủy        │
            └───────────────┘ └─────────────┘ └─────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                    KHÁCH HÀNG & HỆ THỐNG                        │
└─────────────────────────────────────────────────────────────────┘

                              ┌─────────────┐
                              │ Khách hàng  │
                              └──────┬──────┘
                                     │
                                     ▼
                              ┌─────────────┐
                              │ Yêu cầu     │
                              │ hủy/trả     │
                              │ hàng        │
                              └─────────────┘
                                     │
                                     ▼
                              ┌─────────────┐
                              │ Hệ thống    │
                              │ quản lý     │
                              │ phiếu hủy   │
                              └──────┬──────┘
                                     │
                    ┌────────────────┼────────────────┐
                    │                │                │
            ┌───────▼───────┐ ┌──────▼──────┐ ┌──────▼──────┐
            │  Validate     │ │  Xử lý hủy  │ │  Cập nhật   │
            │  yêu cầu      │ │  hóa đơn    │ │  tồn kho    │
            └───────────────┘ └─────────────┘ └─────────────┘
                    │                │                │
                    └────────────────┼────────────────┘
                                     │
                                     ▼
                              ┌─────────────┐
                              │ Thông báo   │
                              │ kết quả     │
                              │ cho KH      │
                              └─────────────┘
```

### Component Diagram - Kiến trúc hệ thống

```
┌─────────────────────────────────────────────────────────────────┐
│                    COMPONENT DIAGRAM                            │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                        Presentation Layer                       │
│                                                                 │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐  │
│  │   Web API       │  │   MVC Views     │  │   Mobile App    │  │
│  │  Controllers    │  │                 │  │   Interface     │  │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                                   │
                                   │ HTTP/JSON
                                   ▼
┌─────────────────────────────────────────────────────────────────┐
│                       Business Logic Layer                      │
│                                                                 │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐  │
│  │ PhieuHuyService │  │ Validation      │  │ Business Rules  │  │
│  │                 │  │ Engine          │  │ Engine          │  │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                                   │
                                   │ .NET Objects
                                   ▼
┌─────────────────────────────────────────────────────────────────┐
│                        Data Access Layer                        │
│                                                                 │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐  │
│  │ PhieuHuyRepo    │  │ TonKhoRepo      │  │ HoaDonRepo      │  │
│  │                 │  │                 │  │                 │  │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                                   │
                                   │ Entity Framework
                                   ▼
┌─────────────────────────────────────────────────────────────────┐
│                        Database Layer                           │
│                                                                 │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐  │
│  │   PhieuHuy      │  │ ChiTietPhieuHuy │  │    TonKho       │  │
│  │   Table         │  │   Table         │  │    Table        │  │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

### Deployment Diagram - Môi trường triển khai

```
┌─────────────────────────────────────────────────────────────────┐
│                    DEPLOYMENT DIAGRAM                           │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                          Client Side                            │
│                                                                 │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐  │
│  │   Web Browser   │  │   Mobile App    │  │   Desktop App   │  │
│  │  (Chrome/Firefox│  │  (iOS/Android)  │  │  (Windows)      │  │
│  │      )          │  │                 │  │                 │  │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                                   │
                                   │ HTTPS/REST API
                                   ▼
┌────────────────────────────────────────────────────────────────────┐
│                        Application Server                          │
│                                                                    │
│  ┌───────────────────────────────────────────────────────────────┐ │
│  │                    ASP.NET Core Web API                       │ │
│  │                                                               │ │
│  │  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐│ │
│  │  │   Controllers   │  │   Services      │  │   Repositories  ││ │ 
│  │  └─────────────────┘  └─────────────────┘  └─────────────────┘│ │
│  └───────────────────────────────────────────────────────────────┘ │
└────────────────────────────────────────────────────────────────────┘
                                   │
                                   │ Database Connection
                                   ▼
┌────────────────────────────────────────────────────────────────────┐
│                        Database Server                          │
│                                                                │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │                   SQL Server Database                       │ │
│  │                                                             │ │
│  │  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐│ │
│  │  │   PhieuHuy      │  │ ChiTietPhieuHuy │  │    HoaDon      │ │
│  │  │   Tables        │  │   Tables        │  │    Tables      │ │
│  │  └─────────────────┘  └─────────────────┘  └─────────────────┘│ │
│  └─────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

### ER Diagram - Mối quan hệ dữ liệu

```
┌─────────────────────────────────────────────────────────────────┐
│                        ENTITY RELATIONSHIP                      │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────┐          ┌─────────────────┐
│   NhanVien      │          │   PhieuHuy      │
│                 │          │                 │
│  MaNV (PK)      │◄─────────┤  MaNV (FK)      │
│  TenNV          │          │  MaPH (PK)      │
│  ChucVu         │          │  NgayHuy        │
│                 │          │  LoaiHuy        │
│                 │          │  MaHD (FK)      │
└─────────────────┘          │  TongSoLuongHuy │
                             │  GhiChu         │
                             └─────────────────┘
                                       │
                                       │ 1:N
                                       ▼
┌─────────────────┐          ┌─────────────────┐
│   HoaDon        │          │ChiTietPhieuHuy  │
│                 │          │                 │
│  MaHD (PK)      │◄─────────┤  MaCTPH (PK)    │
│  MaKH (FK)      │          │  MaPH (FK)      │
│  NgayDat        │          │  MaLo (FK)      │
│  TrangThaiGiaoHang│        │  SoLuongHuy     │
│                 │          │  LyDoHuy        │
└─────────────────┘          │  GhiChu         │
                             └─────────────────┘
                                       │
                                       │ N:1
                                       ▼
                             ┌─────────────────┐
                             │     TonKho      │
                             │                 │
                             │  MaLo (PK)      │
                             │  MaThuoc (FK)   │
                             │  SoLuongCon     │
                             │  HanSuDung      │
                             │  DonGia         │
                             └─────────────────┘
                                       │
                                       │ 1:N
                                       ▼
                             ┌─────────────────┐
                             │ ChiTietHoaDon   │
                             │                 │
                             │  MaCTHD (PK)    │
                             │  MaHD (FK)      │
                             │  MaLo (FK)      │
                             │  SoLuong        │
                             │  DonGia         │
                             └─────────────────┘
```

### Data Flow Diagram - Luồng dữ liệu

```
┌─────────────────────────────────────────────────────────────────┐
│                      DATA FLOW DIAGRAM                          │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────┐
│   User Input    │
│  (API Request)  │
└─────────────────┘
         │
         ▼
┌─────────────────┐     ┌─────────────────┐
│   Validation    │────▶│   Error         │
│   Layer         │     │   Response      │
└─────────────────┘     └─────────────────┘
         │
         ▼
┌─────────────────┐
│ Business Logic  │
│   Processing    │
└─────────────────┘
         │
    ┌────┴────┐
    │         │
┌───▼───┐ ┌───▼───┐
│Update │ │Create │
│Inventory││PhieuHuy│
└────────┘ └───────┘
    │         │
    └────┬────┘
         │
         ▼
┌─────────────────┐
│   Database      │
│   Operations    │
│   (Transaction) │
└─────────────────┘
         │
         ▼
┌─────────────────┐
│   Success       │
│   Response      │
└─────────────────┘
```

### Timing Diagram - Thời gian xử lý

```
┌─────────────────────────────────────────────────────────────────┐
│                      TIMING DIAGRAM                             │
└─────────────────────────────────────────────────────────────────┘

Time ─────────────────────────────────────────────────────────────▶

User Request: POST /api/PhieuHuy/huy-tu-hoa-don
      │
      │ 0ms: Request received
      ▼
API Controller
      │
      │ 5ms: Model validation
      ▼
Service Layer
      │
      │ 10ms: Business rules validation
      │ 15ms: Database queries (HoaDon, ChiTietHoaDon)
      │ 50ms: Transaction begin
      │ 55ms: Process each medicine item
      │ 100ms: Update inventory (TonKho)
      │ 120ms: Create PhieuHuy records
      │ 140ms: Update HoaDon status
      │ 150ms: Transaction commit
      ▼
Repository Layer
      │
      │ 155ms: Return results
      ▼
Response
      │
      │ 160ms: JSON serialization
      │ 165ms: Response sent to client
      ▼
Client
      │
      │ 170ms: Response received
      ▼
```

### Class Diagram - Lớp chính trong hệ thống

```
┌─────────────────────────────────────────────────────────────────┐
│                        CLASS DIAGRAM                            │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────┐          ┌─────────────────┐
│ PhieuHuyController│          │ PhieuHuyService │
│                   │          │                  │
│ + HuyTuKho()      │          │ + HuyTuKhoAsync()│
│ + HuyTuHoaDon()   │          │ + HuyTuHoaDonAsync│
│ + GetByDateRange()│          │ + GetByDateRange()│
│ + GetChiTiet()    │          │ + ValidateData() │
└─────────────────┘          └─────────────────┘
         │                              │
         │                              │
         ▼                              ▼
┌─────────────────┐          ┌─────────────────┐
│IPhieuHuyRepository│          │ PhieuHuyRepository│
│                   │          │                    │
│ + AddAsync()      │          │ + AddAsync()       │
│ + GetByIdAsync()  │          │ + GetByIdAsync()   │
│ + UpdateAsync()   │          │ + GetByDateRange() │
│ + DeleteAsync()   │          │ + GetChiTietAsync()│
└─────────────────┘          └─────────────────┘
         │                              │
         │                              │
         ▼                              ▼
┌─────────────────┐          ┌─────────────────┐
│   PhieuHuy      │          │ChiTietPhieuHuy  │
│   (Entity)      │          │   (Entity)      │
│                 │          │                  │
│ - MaPH          │          │ - MaCTPH         │
│ - NgayHuy       │          │ - MaPH           │
│ - MaNV          │          │ - MaLo           │
│ - LoaiHuy       │          │ - SoLuongHuy     │
│ - MaHD          │          │ - LyDoHuy        │
│ - TongSoLuongHuy│          │ - GhiChu         │
└─────────────────┘          └─────────────────┘
```

## Mô tả chi tiết quy trình nghiệp vụ

### 1. Quy trình hủy từ kho chi tiết

**Bước 1: Thu thập thông tin tồn kho**
- Nhân viên kho kiểm tra danh sách thuốc sắp hết hạn (≤ 3 tháng)
- Xem báo cáo tồn kho theo lô
- Đánh giá tình trạng vật lý của thuốc (bao bì, màu sắc, mùi)

**Bước 2: Phân loại thuốc cần hủy**
- **Thuốc hết hạn**: Hủy 100%
- **Thuốc sắp hết hạn**: Đánh giá nguy cơ, có thể hủy một phần
- **Thuốc hỏng**: Hủy toàn bộ lô nếu phát hiện hỏng
- **Thuốc tồn kho lâu**: Hủy nếu không có nhu cầu sử dụng

**Bước 3: Tạo phiếu hủy**
- Chọn nhân viên phụ trách (thường là quản lý kho)
- Nhập lý do hủy chi tiết cho từng lô
- Xác nhận số lượng hủy (có thể hủy một phần lô)
- Upload hình ảnh chứng minh (nếu cần)

**Bước 4: Duyệt phiếu hủy**
- Quản lý kho duyệt phiếu
- Giám đốc duyệt (cho số lượng lớn hoặc giá trị cao)
- Từ chối nếu lý do không hợp lệ

**Bước 5: Thực hiện hủy**
- Xuất thuốc ra khỏi kho
- Phá hủy thuốc theo quy định (đốt, chôn lấp, hóa chất)
- Lưu mẫu kiểm tra (nếu cần)

**Bước 6: Cập nhật hệ thống**
- Giảm số lượng tồn kho
- Lưu thông tin phiếu hủy
- Cập nhật báo cáo tồn kho

### 2. Quy trình hủy từ hóa đơn chi tiết

**Bước 1: Tiếp nhận yêu cầu hủy**
- Khách hàng liên hệ yêu cầu hủy/trả hàng
- Xác minh thông tin khách hàng và hóa đơn
- Kiểm tra thời hạn hủy (thường trong 7-30 ngày)

**Bước 2: Kiểm tra điều kiện hủy**
- Hóa đơn chưa giao hoặc giao nhưng chưa quá thời hạn
- Thuốc còn nguyên bao bì, tem mác
- Chưa sử dụng hoặc sử dụng ít (tùy chính sách)

**Bước 3: Thu hồi thuốc**
- Khách hàng mang thuốc đến nhà thuốc
- Nhân viên kiểm tra tình trạng thuốc
- Đóng gói và lưu trữ tạm thời

**Bước 4: Đánh giá từng loại thuốc**
- **Kiểm tra hạn sử dụng**: Còn hạn > 6 tháng → hoàn lại kho
- **Đánh giá bao bì**: Nguyên vẹn → hoàn lại kho
- **Kiểm tra chất lượng**: Bị hỏng/vấn đề → hủy
- **Đánh giá nhu cầu**: Thuốc hiếm → hoàn lại kho

**Bước 5: Xử lý hoàn tiền**
- Tính toán số tiền hoàn lại (đã trừ phí hủy nếu có)
- Hoàn tiền bằng tiền mặt/chuyển khoản
- Cập nhật trạng thái hóa đơn

**Bước 6: Xử lý tồn kho**
- Thuốc hoàn lại: Tăng số lượng tồn kho
- Thuốc hủy: Giảm số lượng tồn kho, tạo phiếu hủy

**Bước 7: Báo cáo và lưu trữ**
- Lưu thông tin hủy vào hệ thống
- Cập nhật báo cáo kinh doanh
- Lưu trữ hóa đơn hủy

### 3. Quy tắc xử lý tồn kho

**Cho hủy từ kho:**
```
TonKho.SoLuongCon = TonKho.SoLuongCon - ChiTietPhieuHuy.SoLuongHuy
```

**Cho hoàn lại từ hóa đơn:**
```
TonKho.SoLuongCon = TonKho.SoLuongCon + ChiTietHoaDon.SoLuong
```

**Cho hủy từ hóa đơn:**
```
TonKho.SoLuongCon = TonKho.SoLuongCon - ChiTietPhieuHuy.SoLuongHuy
```

### 4. Quy tắc cập nhật trạng thái hóa đơn

**Logic cập nhật trạng thái:**
- Nếu tất cả chi tiết hóa đơn đã được xử lý (hoàn lại hoặc hủy) → `TrangThaiGiaoHang = -2`
- Nếu còn chi tiết chưa xử lý → `TrangThaiGiaoHang = -3`
- Cho phép xử lý từng phần, cập nhật trạng thái động

**Công thức tính:**
```csharp
bool tatCaDaXuLy = hoaDon.ChiTietHoaDon.All(ct => ct.DaXuLy == true);
if (tatCaDaXuLy)
{
    hoaDon.TrangThaiGiaoHang = -2; // Hoàn tất xử lý hủy
}
else
{
    hoaDon.TrangThaiGiaoHang = -3; // Còn chi tiết chưa xử lý
}
```

### 5. Validation Rules chi tiết

**Validation cho hủy từ kho:**
- Mã lô phải tồn tại trong `TonKho`
- Số lượng hủy > 0 và ≤ `TonKho.SoLuongCon`
- Lý do hủy bắt buộc, tối thiểu 10 ký tự
- Nhân viên thực hiện phải có quyền hủy thuốc

**Validation cho hủy từ hóa đơn:**
- Mã hóa đơn phải tồn tại
- Hóa đơn chưa hoàn tất (TrangThaiGiaoHang < 3)
- Mã lô phải thuộc về hóa đơn được chỉ định
- Loại xử lý chỉ chấp nhận "HUY" hoặc "HOAN_LAI"
- Nếu "HUY" thì SoLuongHuy và LyDoHuy bắt buộc

**Business Rules:**
- Không cho phép hủy thuốc còn hạn > 6 tháng (trừ trường hợp đặc biệt)
- Thuốc quý hiếm/hiếm có ưu tiên hoàn lại kho
- Số lượng hủy không được vượt quá số lượng trong hóa đơn
- Một lô thuốc có thể được xử lý một phần (một phần hủy, một phần hoàn lại)

## Business Rules

### Validation Rules
- **Mã lô thuốc**: Phải tồn tại trong hệ thống
- **Số lượng hủy**: > 0 và ≤ số lượng tồn kho hiện tại
- **Hạn sử dụng**: Không cho phép hủy thuốc còn hạn > 6 tháng (trừ trường hợp đặc biệt)
- **Lý do hủy**: Bắt buộc, phải rõ ràng và cụ thể

### Inventory Management
- **Hủy từ kho**: `TonKho.SoLuongCon -= ChiTietPhieuHuy.SoLuongHuy`
- **Hoàn lại từ hóa đơn**: `TonKho.SoLuongCon += ChiTietHoaDon.SoLuong`
- **Hủy từ hóa đơn**: `TonKho.SoLuongCon -= ChiTietPhieuHuy.SoLuongHuy`

### Transaction Safety
- Toàn bộ quá trình hủy được thực hiện trong database transaction
- Nếu có lỗi ở bất kỳ bước nào, rollback toàn bộ thay đổi
- Đảm bảo tính nhất quán dữ liệu

## Error Handling

### Common Error Codes

| Error Code | Description | Solution |
|------------|-------------|----------|
| `INVALID_MA_LO` | Mã lô không tồn tại | Kiểm tra lại mã lô |
| `INSUFFICIENT_STOCK` | Không đủ số lượng tồn kho | Kiểm tra số lượng tồn kho hiện tại |
| `INVALID_ORDER` | Hóa đơn không tồn tại hoặc không hợp lệ | Xác nhận mã hóa đơn |
| `EXPIRED_MEDICINE` | Thuốc đã hết hạn | Chỉ cho phép hủy thuốc hết hạn |
| `INVALID_QUANTITY` | Số lượng không hợp lệ | Số lượng phải > 0 |

### Error Response Format
```json
{
  "status": 1,
  "message": "Lô thuốc LO001 chỉ còn 30 không đủ để hủy 50",
  "data": null,
  "errorCode": "INSUFFICIENT_STOCK"
}
```

## Performance Considerations

### Indexing Recommendations
```sql
-- Index cho tra cứu nhanh phiếu hủy theo ngày
CREATE INDEX IX_PhieuHuy_NgayHuy ON PhieuHuy(NgayHuy);

-- Index cho tra cứu theo loại hủy
CREATE INDEX IX_PhieuHuy_LoaiHuy ON PhieuHuy(LoaiHuy);

-- Index cho tra cứu theo hóa đơn
CREATE INDEX IX_PhieuHuy_MaHD ON PhieuHuy(MaHD);

-- Index cho chi tiết phiếu hủy
CREATE INDEX IX_ChiTietPhieuHuy_MaPH ON ChiTietPhieuHuy(MaPH);
CREATE INDEX IX_ChiTietPhieuHuy_MaLo ON ChiTietPhieuHuy(MaLo);
```

### Query Optimization
- Sử dụng `Include()` để load navigation properties
- Phân trang cho danh sách phiếu hủy
- Cache thông tin thuốc thường xuyên truy cập

## Test API

### PowerShell

```powershell
# Hủy hóa đơn với xử lý linh hoạt
$huyHoaDonBody = @{
    maHD = "HDOL20251118000001"
    maNV = "NV001"
    ghiChu = "Test hủy hóa đơn linh hoạt"
    xuLyThuocs = @(
        @{
            maLo = "LO001"
            loaiXuLy = "HOAN_LAI"
            ghiChu = "Thuốc còn hạn dài"
        }
        @{
            maLo = "LO002"
            loaiXuLy = "HUY"
            soLuongHuy = 5
            lyDoHuy = "Sắp hết hạn"
            ghiChu = "Thuốc sắp hết hạn"
        }
    )
} | ConvertTo-Json -Depth 4

Invoke-WebRequest -Uri "https://localhost:5001/api/PhieuHuy/HuyHoaDon" -Method POST -Body $huyHoaDonBody -ContentType "application/json" -SkipCertificateCheck
```

## Ví dụ sử dụng chi tiết

### Ví dụ 1: Hủy thuốc từ kho

**Request:**
```http
POST /api/PhieuHuy/huy-tu-kho
Content-Type: application/json

{
  "maNhanVien": "NV001",
  "lyDoHuy": "Thuốc sắp hết hạn sử dụng",
  "chiTietPhieuHuy": [
    {
      "maLo": "LO001",
      "soLuongHuy": 20,
      "lyDoChiTiet": "Hạn sử dụng còn 2 tháng"
    },
    {
      "maLo": "LO002",
      "soLuongHuy": 15,
      "lyDoChiTiet": "Bao bì bị hỏng"
    }
  ]
}
```

**Response:**
```json
{
  "status": 0,
  "message": "Tạo phiếu hủy thành công",
  "data": {
    "maPhieuHuy": "PH20241201001",
    "ngayHuy": "2024-12-01T10:30:00",
    "tongSoLuongHuy": 35,
    "chiTietPhieuHuy": [
      {
        "maLo": "LO001",
        "tenThuoc": "Paracetamol 500mg",
        "soLuongHuy": 20,
        "donGia": 5000
      },
      {
        "maLo": "LO002",
        "tenThuoc": "Ibuprofen 200mg",
        "soLuongHuy": 15,
        "donGia": 3000
      }
    ]
  }
}
```

### Ví dụ 2: Hủy thuốc từ hóa đơn

**Request:**
```http
POST /api/PhieuHuy/huy-tu-hoa-don
Content-Type: application/json

{
  "maHoaDon": "HD20241128001",
  "maNhanVien": "NV001",
  "lyDoHuy": "Khách hàng yêu cầu hủy đơn",
  "chiTietXuLy": [
    {
      "maLo": "LO003",
      "loaiXuLy": "HUY",
      "soLuongHuy": 5,
      "lyDoChiTiet": "Bao bì bị rách"
    },
    {
      "maLo": "LO004",
      "loaiXuLy": "HOAN_LAI",
      "soLuongHuy": 0,
      "lyDoChiTiet": "Thuốc còn tốt, hoàn lại kho"
    }
  ]
}
```

**Response:**
```json
{
  "status": 0,
  "message": "Xử lý hủy hóa đơn thành công",
  "data": {
    "maPhieuHuy": "PH20241201002",
    "maHoaDon": "HD20241128001",
    "trangThaiGiaoHang": -2,
    "tongSoLuongHuy": 5,
    "tongSoLuongHoanLai": 10,
    "chiTietXuLy": [
      {
        "maLo": "LO003",
        "tenThuoc": "Amoxicillin 500mg",
        "loaiXuLy": "HUY",
        "soLuongHuy": 5
      },
      {
        "maLo": "LO004",
        "tenThuoc": "Vitamin C 1000mg",
        "loaiXuLy": "HOAN_LAI",
        "soLuongHoanLai": 10
      }
    ]
  }
}
```

### Ví dụ 3: Lấy danh sách phiếu hủy

**Request:**
```http
GET /api/PhieuHuy?page=1&pageSize=10&fromDate=2024-11-01&toDate=2024-12-01
```

**Response:**
```json
{
  "status": 0,
  "message": "Lấy danh sách phiếu hủy thành công",
  "data": {
    "totalCount": 25,
    "page": 1,
    "pageSize": 10,
    "items": [
      {
        "maPhieuHuy": "PH20241201001",
        "ngayHuy": "2024-12-01T10:30:00",
        "maNhanVien": "NV001",
        "tenNhanVien": "Nguyễn Văn A",
        "loaiHuy": "TU_KHO",
        "lyDoHuy": "Thuốc sắp hết hạn",
        "tongSoLuongHuy": 35,
        "tongGiaTri": 175000
      }
    ]
  }
}
```

## Testing Guide

### Unit Tests

```csharp
[Test]
public async Task HuyTuKho_ValidData_ShouldCreatePhieuHuy()
{
    // Arrange
    var request = new PhieuHuyTuKhoDto
    {
        MaNhanVien = "NV001",
        LyDoHuy = "Test hủy",
        ChiTietPhieuHuy = new List<ChiTietPhieuHuyDto>
        {
            new ChiTietPhieuHuyDto { MaLo = "LO001", SoLuongHuy = 5 }
        }
    };

    // Act
    var result = await _service.HuyTuKhoAsync(request);

    // Assert
    Assert.IsNotNull(result.Data);
    Assert.AreEqual("PH", result.Data.MaPhieuHuy.Substring(0, 2));
}
```

### Integration Tests

```csharp
[Test]
public async Task HuyTuHoaDon_ShouldUpdateInventoryAndOrderStatus()
{
    // Arrange: Tạo hóa đơn test
    var hoaDon = await CreateTestHoaDonAsync();

    // Act: Hủy hóa đơn
    var result = await _service.HuyTuHoaDonAsync(new PhieuHuyTuHoaDonDto
    {
        MaHoaDon = hoaDon.MaHoaDon,
        MaNhanVien = "NV001",
        ChiTietXuLy = new List<ChiTietXuLyDto>
        {
            new ChiTietXuLyDto { MaLo = "LO001", LoaiXuLy = "HUY", SoLuongHuy = 5 }
        }
    });

    // Assert: Kiểm tra tồn kho giảm
    var tonKho = await _context.TonKho.FindAsync("LO001");
    Assert.AreEqual(tonKho.SoLuongCon, initialStock - 5);

    // Assert: Kiểm tra trạng thái hóa đơn
    var updatedHoaDon = await _context.HoaDon.FindAsync(hoaDon.MaHoaDon);
    Assert.AreEqual(updatedHoaDon.TrangThaiGiaoHang, -2);
}
```

### API Testing với Postman

1. **Import collection:**
```json
{
  "info": {
    "name": "PhieuHuy API Tests",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "item": [
    {
      "name": "Hủy từ kho",
      "request": {
        "method": "POST",
        "header": [
          {
            "key": "Content-Type",
            "value": "application/json"
          }
        ],
        "body": {
          "mode": "raw",
          "raw": "{\"maNhanVien\":\"NV001\",\"lyDoHuy\":\"Test\",\"chiTietPhieuHuy\":[{\"maLo\":\"LO001\",\"soLuongHuy\":5}]}"
        },
        "url": {
          "raw": "{{baseUrl}}/api/PhieuHuy/huy-tu-kho",
          "host": ["{{baseUrl}}"],
          "path": ["api", "PhieuHuy", "huy-tu-kho"]
        }
      }
    }
  ]
}
```

2. **Environment variables:**
```json
{
  "baseUrl": "https://localhost:5001"
}
```

## Troubleshooting

### Vấn đề thường gặp

1. **Lỗi "Mã lô không tồn tại"**
   - Kiểm tra chính tả mã lô
   - Đảm bảo lô thuốc đã được nhập vào hệ thống

2. **Lỗi "Không đủ số lượng tồn kho"**
   - Kiểm tra số lượng tồn kho hiện tại
   - Có thể có đơn hàng khác đã sử dụng số lượng này

3. **Lỗi "Hóa đơn không tồn tại"**
   - Xác nhận mã hóa đơn chính xác
   - Kiểm tra trạng thái hóa đơn (chỉ xử lý hóa đơn chưa giao)

4. **Lỗi transaction timeout**
   - Giảm số lượng chi tiết xử lý cùng lúc
   - Tối ưu hóa query performance

### Debug Tips

- Sử dụng SQL Profiler để theo dõi queries
- Kiểm tra logs trong Application Insights
- Validate data trước khi gửi request
- Test từng API endpoint riêng lẻ trước khi tích hợp

## Maintenance

### Database Maintenance
```sql
-- Backup trước khi maintenance
BACKUP DATABASE QLTiemThuoc TO DISK = 'D:\Backup\QLTiemThuoc.bak';

-- Rebuild indexes hàng tháng
ALTER INDEX ALL ON PhieuHuy REBUILD;
ALTER INDEX ALL ON ChiTietPhieuHuy REBUILD;

-- Archive old records (quá 2 năm)
INSERT INTO PhieuHuy_Archive SELECT * FROM PhieuHuy WHERE NgayHuy < DATEADD(YEAR, -2, GETDATE());
DELETE FROM PhieuHuy WHERE NgayHuy < DATEADD(YEAR, -2, GETDATE());
```

### Code Maintenance
- Review code hàng quý
- Update dependencies
- Thêm unit tests cho business logic mới
- Document API changes

---

*Tài liệu này được tạo tự động và có thể được cập nhật. Vui lòng kiểm tra phiên bản mới nhất trước khi sử dụng.*
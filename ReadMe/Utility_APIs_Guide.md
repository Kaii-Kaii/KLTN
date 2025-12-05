# Hướng dẫn các API phụ trợ

## Tổng quan

Module này bao gồm các API hỗ trợ khác như quản lý hình ảnh và dữ liệu tham khảo.

## 1. Images API - Quản lý hình ảnh

### Các endpoint chính

#### 1.1 Lấy danh sách file

**GET** `/api/Images/List`

#### 1.2 Upload từ URL external

**POST** `/api/Images/UploadExternal`

**Request Body:**
```json
{
  "url": "https://example.com/image.jpg"
}
```

#### 1.3 Upload file

**POST** `/api/Images/UploadFile`

**Request Body:** FormData
```
file: [file] (image file)
```

#### 1.4 Upload trực tiếp vào thư mục product

**POST** `/api/Images/UploadToProduct`

**Request Body:** FormData
```
file: [file] (image file)
```

#### 1.5 Lấy file temp để preview

**GET** `/api/Images/GetTemp?filename={filename}`

#### 1.6 Chuyển file từ temp sang product

**POST** `/api/Images/FinalizeImage`

**Request Body:**
```json
{
  "fileName": "temp_image.jpg"
}
```

## 2. LieuDung API - Quản lý liều dùng

### Các endpoint chính

#### 2.1 Lấy danh sách tất cả liều dùng

**GET** `/api/LieuDung`

**Response (thành công):**
```json
{
  "status": 0,
  "message": "Success",
  "data": [
    {
      "maLD": "LD001",
      "tenLieuDung": "Uống"
    },
    {
      "maLD": "LD002",
      "tenLieuDung": "Bôi ngoài da"
    }
  ]
}
```

## Luồng upload hình ảnh

### Upload thông thường:
1. POST `/api/Images/UploadFile` - Upload file vào temp
2. GET `/api/Images/GetTemp` - Preview file
3. POST `/api/Images/FinalizeImage` - Chuyển vào product folder

### Upload trực tiếp:
1. POST `/api/Images/UploadToProduct` - Upload trực tiếp vào product

### Upload từ URL:
1. POST `/api/Images/UploadExternal` - Download và lưu từ URL

## Ví dụ sử dụng từ frontend

### JavaScript / Fetch API

```javascript
// Upload file thông thường
const formData = new FormData();
formData.append('file', imageFile);

const uploadResponse = await fetch('/api/Images/UploadFile', {
  method: 'POST',
  body: formData
});

// Preview temp file
const tempImage = await fetch('/api/Images/GetTemp?filename=temp_file.jpg');
const imageBlob = await tempImage.blob();

// Finalize image
const finalizeResponse = await fetch('/api/Images/FinalizeImage', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({ fileName: 'temp_file.jpg' })
});

// Upload trực tiếp vào product
const productFormData = new FormData();
productFormData.append('file', imageFile);

const productResponse = await fetch('/api/Images/UploadToProduct', {
  method: 'POST',
  body: productFormData
});

// Lấy danh sách liều dùng
const lieuDungList = await fetch('/api/LieuDung');
const lieuDungData = await lieuDungList.json();
```

## Lưu ý kỹ thuật

### Images API:
- UploadFile: Lưu vào thư mục temp trước
- UploadToProduct: Lưu trực tiếp vào product folder, xử lý tên trùng lặp
- GetTemp: Trả về file binary để preview
- FinalizeImage: Chuyển từ temp sang product folder
- File naming: Tự động sanitize và xử lý duplicate names
- Supported formats: JPG, PNG, etc.

### LieuDung API:
- Mã liều dùng (MaLD): Unique identifier
- Tên liều dùng (TenLieuDung): Tên hiển thị
- Dữ liệu thường cố định, ít thay đổi

## Test API

### Sử dụng Swagger UI
1. Chạy: `dotnet run --launch-profile "https"`
2. Mở: `https://localhost:port/swagger`
3. Tìm endpoints trong `Images` và `LieuDung`

### Sử dụng PowerShell

```powershell
# Lấy danh sách file hình ảnh
Invoke-WebRequest -Uri "https://localhost:5001/api/Images/List" -Method GET -SkipCertificateCheck

# Lấy danh sách liều dùng
Invoke-WebRequest -Uri "https://localhost:5001/api/LieuDung" -Method GET -SkipCertificateCheck
```

## Hỗ trợ

Kiểm tra logs server nếu gặp lỗi. Đảm bảo:
- File là hình ảnh hợp lệ
- URL accessible (cho UploadExternal)
- Permissions ghi file vào thư mục
- Dung lượng file không quá lớn
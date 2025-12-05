# Hướng dẫn tạo tài khoản

## Endpoints

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| POST | `/api/TaiKhoan` | Tạo tài khoản mới |
| GET | `/api/TaiKhoan/CheckUsername?username={username}` | Kiểm tra tên đăng nhập tồn tại |
| POST | `/api/TaiKhoan/Login` | Đăng nhập |
| POST | `/api/TaiKhoan/SendOtp` | Gửi OTP để reset password |
| POST | `/api/TaiKhoan/ResetPassword` | Đặt lại mật khẩu |
| GET | `/api/TaiKhoan/ConfirmEmail?token={token}` | Xác thực email |
| GET | `/api/TaiKhoan` | Lấy tất cả tài khoản |

## Workflow: Tạo tài khoản

1. **Gửi yêu cầu đăng ký:**
```json
POST /api/TaiKhoan
{
  "tenDangNhap": "user1",
  "matKhau": "pass123",
  "email": "user@example.com"
}
```

2. **Hệ thống gửi email xác thực** chứa link: `/api/TaiKhoan/ConfirmEmail?token={token}`

3. **Người dùng click link** để xác thực email (set `ISEMAILCONFIRMED = 1`)

4. **Có thể đăng nhập** sau khi email được xác thực

## Workflow: Reset Password

1. **Gửi yêu cầu OTP:**
```json
POST /api/TaiKhoan/SendOtp
{
  "tenDangNhap": "user1",
  "email": "user@example.com"
}
```

2. **Hệ thống gửi OTP 6 số** về email

3. **Gửi yêu cầu reset mật khẩu:**
```json
POST /api/TaiKhoan/ResetPassword
{
  "tenDangNhap": "user1",
  "email": "user@example.com",
  "otp": 123456,
  "matKhauMoi": "newpass123"
}
```

## Endpoints chi tiết

### 1. Tạo tài khoản
```
POST /api/TaiKhoan

Request: { tenDangNhap, matKhau, email }
Response: { message: "Tạo tài khoản thành công. Vui lòng kiểm tra email để xác thực." }
Error: { message: "Tên đăng nhập đã tồn tại." }
```

### 2. Kiểm tra tên đăng nhập
```
GET /api/TaiKhoan/CheckUsername?username=user1

Response: { exists: true/false }
```

### 3. Đăng nhập
```
POST /api/TaiKhoan/Login

Request: { tenDangNhap, matKhau }
Response: { message, maTK, tenDangNhap, email }
Error: { message: "Sai tên đăng nhập hoặc mật khẩu." }
       { message: "Tài khoản chưa xác thực email." }
```

### 4. Gửi OTP
```
POST /api/TaiKhoan/SendOtp

Request: { tenDangNhap, email }
Response: { message: "OTP đã được gửi về email của bạn." }
Error: { message: "Không tìm thấy tài khoản..." }
```

### 5. Đặt lại mật khẩu
```
POST /api/TaiKhoan/ResetPassword

Request: { tenDangNhap, email, otp, matKhauMoi }
Response: { message: "Đổi mật khẩu thành công. Vui lòng đăng nhập lại." }
Error: { message: "OTP không đúng hoặc đã hết hạn." }
```

### 6. Xác thực email
```
GET /api/TaiKhoan/ConfirmEmail?token={token}

Response: HTML thành công (checkmark xanh)
Error: HTML "Token không hợp lệ hoặc đã xác thực."
```

## Cấu hình Email

⚠️ **Hiện tại hardcode** email credentials trong controller. Khuyến nghị:

Thêm vào `.env`:
```
EmailSettings__SmtpHost=smtp.gmail.com
EmailSettings__SmtpPort=587
EmailSettings__SmtpUsername=your-email@gmail.com
EmailSettings__SmtpPassword=your-app-password
```

Sửa controller để đọc từ biến môi trường.

## Bảo mật

⚠️ **Cần cải thiện:**
- [ ] Hash passwords (hiện lưu raw)
- [ ] Bảo vệ email credentials
- [ ] Rate limiting (brute force)
- [ ] Expiration time cho OTP
- [ ] JWT tokens thay vì trả về username
- [ ] CORS chặt chẽ hơn

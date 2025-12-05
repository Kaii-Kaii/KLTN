using System.ComponentModel.DataAnnotations;

namespace BE_QLTiemThuoc.DTOs
{
    public class SendOtpRequest
    {
        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        public string TenDangNhap { get; set; }

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }
    }

    public class SendOtpResponse
    {
        public string Message { get; set; }
    }

    public class ResetPasswordRequest
    {
        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        public string TenDangNhap { get; set; }

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }

        [Required(ErrorMessage = "OTP không được để trống")]
        public int Otp { get; set; }

        [Required(ErrorMessage = "Mật khẩu mới không được để trống")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        public string MatKhauMoi { get; set; }
    }

    public class ResetPasswordResponse
    {
        public string Message { get; set; }
    }

    public class ResetPasswordByAdminRequest
    {
        [Required(ErrorMessage = "Mật khẩu mới không được để trống")]
        public string NewPassword { get; set; }
    }
}
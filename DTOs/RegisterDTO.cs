using System.ComponentModel.DataAnnotations;

namespace BE_QLTiemThuoc.DTOs
{
    public class RegisterRequest
    {
        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        public string TenDangNhap { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        public string MatKhau { get; set; }

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }
    }

    public class RegisterResponse
    {
        public string Message { get; set; }
    }

    public class CheckUsernameResponse
    {
        public bool Exists { get; set; }
    }
}
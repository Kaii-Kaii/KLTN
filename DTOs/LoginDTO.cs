using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BE_QLTiemThuoc.DTOs
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        public string TenDangNhap { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        public string MatKhau { get; set; }
    }

    public class GoogleLoginRequest
    {
        [Required(ErrorMessage = "ID Token không được để trống")]
        [JsonPropertyName("idToken")]
        public string IdToken { get; set; }
        
        [JsonPropertyName("email")]
        public string? Email { get; set; }
        
        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }
        
        [JsonPropertyName("photoURL")]
        public string? PhotoURL { get; set; }
    }

    public class LoginResponse
    {
        public string Message { get; set; }
        public string? MaTK { get; set; }
        public string? TenDangNhap { get; set; }
        public string? Email { get; set; }
        public string? MaKH { get; set; }
        public string? MaNV { get; set; }
        public int? ChucVu { get; set; }
        public string? VaiTro { get; set; }
        public bool HasCustomerInfo { get; set; }
        public bool IsAdmin { get; set; }
        public string? Token { get; set; }
    }
}
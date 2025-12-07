using System.ComponentModel.DataAnnotations;

namespace BE_QLTiemThuoc.Dto
{
    /// <summary>
    /// DTO để tạo nhân viên cùng với tài khoản đăng nhập
    /// </summary>
    public class NhanVienCreateDto
    {
        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Họ tên phải từ 3 đến 100 ký tự")]
        public string? HoTen { get; set; }

        [Required(ErrorMessage = "Ngày sinh là bắt buộc")]
        public DateTime NgaySinh { get; set; }

        [Required(ErrorMessage = "Giới tính là bắt buộc")]
        [StringLength(20, ErrorMessage = "Giới tính không quá 20 ký tự")]
        public string? GioiTinh { get; set; }

        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        [RegularExpression(@"^\d{10,11}$", ErrorMessage = "Số điện thoại phải là 10-11 chữ số")]
        public string? DienThoai { get; set; }

        [Required(ErrorMessage = "Địa chỉ là bắt buộc")]
        [StringLength(255, MinimumLength = 5, ErrorMessage = "Địa chỉ phải từ 5 đến 255 ký tự")]
        public string? DiaChi { get; set; }

        [Required(ErrorMessage = "Chức vụ là bắt buộc")]
        public int ChucVu { get; set; }

        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập phải từ 3 đến 50 ký tự")]
        public string? TenDangNhap { get; set; }
    }

    /// <summary>
    /// DTO để trả về thông tin nhân viên đã tạo cùng với tài khoản
    /// </summary>
    public class NhanVienCreateResponseDto
    {
        public string? MaNV { get; set; }
        public string? HoTen { get; set; }
        public DateTime? NgaySinh { get; set; }
        public string? GioiTinh { get; set; }
        public string? DienThoai { get; set; }
        public string? DiaChi { get; set; }
        public int? ChucVu { get; set; }
        public string? TenDangNhap { get; set; }
        public DateTime NgayTao { get; set; }
        public string? ChucVuText { get; set; }
        public string? Message { get; set; } = "Nhân viên đã được tạo thành công";
    }
}

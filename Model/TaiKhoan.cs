using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;


namespace BE_QLTiemThuoc.Model
{
    public class TaiKhoan
    {
        [Key]
        public string? MaTK { get; set; }

        public string? TenDangNhap { get; set; }
        public string? MatKhau { get; set; }
        public string? VaiTro { get; set; }

        // Khóa ngoại dạng string
        public string? MaKH { get; set; }

        [ForeignKey(nameof(MaKH))]
        public KhachHang? KhachHang { get; set; }

        public string? MaNV { get; set; }

        [ForeignKey(nameof(MaNV))]
        public NhanVien? NhanVien { get; set; }

        public string? EMAIL { get; set; }
        public int? ISEMAILCONFIRMED { get; set; }
        public string? EMAILCONFIRMATIONTOKEN { get; set; }
        public int? OTP { get; set; }
    }
}

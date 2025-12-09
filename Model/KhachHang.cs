using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BE_QLTiemThuoc.Model
{
    public class KhachHang
    {
        [Key]
        public string? MAKH { get; set; }
        public string? HoTen { get; set; }
        public DateTime? NgaySinh { get; set; }  // <- Có thể null
        public string? GioiTinh { get; set; }
        public string? DiaChi { get; set; }
        public string? DienThoai { get; set; }
        
        // Navigation property to TaiKhoan
        public virtual ICollection<TaiKhoan>? TaiKhoans { get; set; }
    }
}

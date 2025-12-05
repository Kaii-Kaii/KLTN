using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE_QLTiemThuoc.Model
{
    [Table("HoaDon")]
    public class HoaDon
    {
        [Key]
        [MaxLength(20)]
        public string MaHD { get; set; } = string.Empty;

        [Required]
        public DateTime NgayLap { get; set; }

        [MaxLength(10)]
        public string? MaKH { get; set; }

        [MaxLength(10)]
        public string? MaNV { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TongTien { get; set; }

        [MaxLength(255)]
        public string? GhiChu { get; set; }

        // -3: chưa hoàn tất xử lý huỷ, -2: đã hoàn tất xử lý huỷ, -1: huỷ
        // 0: đã đặt, 1: đã xác nhận, 2: đã giao, 3: đã nhận
        public int? TrangThaiGiaoHang { get; set; }

        // 1: TIENMAT, 2: ONLINE, 3: OCD
        public int? PhuongThucTT { get; set; }

        // 1: đã thanh toán, 0: đã hoàn (cho trường hợp huỷ)
        public bool? TrangThai { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TienThanhToan { get; set; }

        [MaxLength(100)]
        public string? OrderCode { get; set; }

        // Navigation properties
        [ForeignKey("MaKH")]
        public KhachHang? KhachHang { get; set; }

        [ForeignKey("MaNV")]
        public NhanVien? NhanVien { get; set; }
    }
}

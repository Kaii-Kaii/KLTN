using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE_QLTiemThuoc.Model
{
    [Table("LichSuThanhToan")]
    public class LichSuThanhToan
    {
        [Key]
        [StringLength(30)]
        public string MaThanhToan { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string? MaHD { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal SoTienThanhToan { get; set; }

        public int? PhuongThucTT { get; set; }

        public DateTime? NgayThanhToan { get; set; }

        public bool? TrangThai { get; set; }

        [StringLength(100)]
        public string? MaGiaoDichOnline { get; set; }

        [StringLength(30)]
        public string? SoTaiKhoan { get; set; }

        [StringLength(100)]
        public string? TenChuTK { get; set; }

        [StringLength(100)]
        public string? NganHang { get; set; }

        [StringLength(255)]
        public string? GhiChu { get; set; }
    }
}

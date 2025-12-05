using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BE_QLTiemThuoc.Model.Thuoc;

namespace BE_QLTiemThuoc.Model.Kho
{
    [Table("PhieuNhap")]
    public class PhieuNhap
    {
        [Key]
        [StringLength(20)]
        public string MaPN { get; set; } = null!;

    [Required]
    [Column(TypeName = "date")] // match SQL: PhieuNhap.NgayNhap DATE
    public DateTime NgayNhap { get; set; }

        [StringLength(10)]
        public string? MaNCC { get; set; }

        [StringLength(10)]
        public string? MaNV { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TongTien { get; set; }

        public string? GhiChu { get; set; }
    }
}

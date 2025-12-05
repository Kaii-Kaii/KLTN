using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE_QLTiemThuoc.Model.Kho
{
    [Table("PhieuHuy")]
    public class PhieuHuy
    {
        [Key]
        [StringLength(20)]
        public string MaPH { get; set; } = null!;

        [StringLength(20)]
        public string? MaPXH { get; set; } // Liên kết với PXH

        [Required]
        public DateTime NgayHuy { get; set; }

        [Required]
        [StringLength(10)]
        public string MaNV { get; set; } = null!;

        // Database only has: MaPH, MaPXH, NgayHuy, MaNV, GhiChu

        [StringLength(255)]
        public string? GhiChu { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TongTien { get; set; }

        // Navigation properties
        public virtual ICollection<ChiTietPhieuHuy> ChiTietPhieuHuys { get; set; } = new List<ChiTietPhieuHuy>();
    }
}
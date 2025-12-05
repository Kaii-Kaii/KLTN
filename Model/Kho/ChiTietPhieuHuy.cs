using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE_QLTiemThuoc.Model.Kho
{
    [Table("ChiTietPhieuHuy")]
    public class ChiTietPhieuHuy
    {
        [Key]
        [StringLength(20)]
        public string MaCTPH { get; set; } = null!;
        
        // LoaiHuy column does not exist in DB

        [Required]
        [StringLength(20)]
        public string MaPH { get; set; } = null!;

        [Required]
        [StringLength(20)]
        public string MaLo { get; set; } = null!;

        [Required]
        [StringLength(10)]
        public string MaThuoc { get; set; } = null!;

        [Required]
        [StringLength(10)]
        public string MaLoaiDonVi { get; set; } = null!;

        [Required]
        public int SoLuong { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DonGia { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ThanhTien { get; set; }

        [StringLength(1000)]
        public string? GhiChu { get; set; }

        // Navigation properties can be added later if needed
    }
}
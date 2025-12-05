using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE_QLTiemThuoc.Model.Kho
{
    [Table("TON_KHO")]
    public class TonKho
    {
        [Key]
        [StringLength(20)]
        public string MaLo { get; set; } = null!; // Primary Key

        [Required]
        [StringLength(10)]
        public string MaThuoc { get; set; } = null!;

        // Navigation property
        [ForeignKey("MaThuoc")]
        public virtual Thuoc.Thuoc? Thuoc { get; set; }

    [Required]
    [Column(TypeName = "date")] // match SQL: TON_KHO.HanSuDung DATE NOT NULL
    public DateTime HanSuDung { get; set; }

        [Required]
        public bool TrangThaiSeal { get; set; } = false; // 0 = Chưa bóc seal, 1 = Đã bóc seal

        [Required]
        [StringLength(10)]
        public string MaLoaiDonViTinh { get; set; } = null!; // MaLoaiDonViTinh references LoaiDonVi.MaLoaiDonVi

        [Required]
        public int SoLuongNhap { get; set; }

        [Required]
        public int SoLuongCon { get; set; }

        [StringLength(255)]
        public string? GhiChu { get; set; }
    }
}

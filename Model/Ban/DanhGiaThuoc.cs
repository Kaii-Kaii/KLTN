using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE_QLTiemThuoc.Model.Ban
{
 [Table("DanhGiaThuoc")]
 public class DanhGiaThuoc
 {
 [Key]
 [StringLength(20)]
 public string MaDanhGia { get; set; } = null!;

 [Required]
 [StringLength(10)]
 public string MaKH { get; set; } = null!;

 [Required]
 [StringLength(10)]
 public string MaThuoc { get; set; } = null!;

 [Range(1,5)]
 public int SoSao { get; set; }

 [StringLength(500)]
 public string? NoiDung { get; set; }

 public DateTime NgayDanhGia { get; set; } = DateTime.UtcNow;
 }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE_QLTiemThuoc.Model.Ban
{
 [Table("BinhLuan")]
 public class BinhLuan
 {
 [Key]
 [StringLength(20)]
 public string MaBL { get; set; } = null!;

 [Required]
 [StringLength(20)]
 public string MaThuoc { get; set; } = null!; // product id

 [StringLength(20)]
 public string? MaKH { get; set; } // customer who asked

 [StringLength(20)]
 public string? MaNV { get; set; } // staff who replied

 [Required]
 [StringLength(1000)]
 public string NoiDung { get; set; } = null!;

 public DateTime ThoiGian { get; set; } = DateTime.UtcNow;

 [StringLength(20)]
 public string? TraLoiChoBinhLuan { get; set; } // parent comment id

 // navigation self-reference
 public BinhLuan? Parent { get; set; }
 public List<BinhLuan> Replies { get; set; } = new();
 }
}

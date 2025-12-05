using System;

namespace BE_QLTiemThuoc.Model.Chat
{
 public class TinNhan
 {
 public long MaTN { get; set; }
 public long MaCuocTroChuyen { get; set; }
 public bool LaKhachGui { get; set; }
 public string? MaNV { get; set; }
 public string NoiDung { get; set; } = string.Empty;
 public DateTime ThoiGian { get; set; }

 // Navigation
 public CuocTroChuyen? CuocTroChuyen { get; set; }
 }
}

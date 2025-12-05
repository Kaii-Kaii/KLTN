using System.Collections.Generic;

namespace BE_QLTiemThuoc.Model.Chat
{
 public class CuocTroChuyen
 {
 public long MaCuocTroChuyen { get; set; }
 public string MaKH { get; set; } = string.Empty;

 // Navigation
 public ICollection<TinNhan> TinNhans { get; set; } = new List<TinNhan>();
 }
}

namespace BE_QLTiemThuoc.Dto
{
 public class BinhLuanCreateDto
 {
 public string MaThuoc { get; set; } = null!;
 public string? MaKH { get; set; } // one of MaKH or MaNV must be provided (not both)
 public string? MaNV { get; set; }
 public string NoiDung { get; set; } = null!;
 public string? TraLoiChoBinhLuan { get; set; }
 }

 public class BinhLuanUpdateDto
 {
 public string NoiDung { get; set; } = null!;
 }

 public class BinhLuanViewDto
 {
 public string MaBL { get; set; } = null!;
 public string MaThuoc { get; set; } = null!;
 public string? MaKH { get; set; }
 public string? TenKH { get; set; }
 public string? MaNV { get; set; }
 public string? TenNV { get; set; }
 public string NoiDung { get; set; } = null!;
 public DateTime ThoiGian { get; set; }
 public string? TraLoiChoBinhLuan { get; set; }
 public List<BinhLuanViewDto> Replies { get; set; } = new();
 }

 public class AdminRootStatusDto
 {
 public BinhLuanViewDto Root { get; set; } = null!;
 public int Status { get; set; } //0 = ch?a tr? l?i,1 = ?� tr? l?i
 }
}

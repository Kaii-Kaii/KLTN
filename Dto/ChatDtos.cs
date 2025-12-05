namespace BE_QLTiemThuoc.Dto
{
 public class ChatCreateMessageDto
 {
 public long MaCuocTroChuyen { get; set; }
 public bool LaKhachGui { get; set; } // true = KH, false = NV
 public string? MaNV { get; set; } // required if LaKhachGui=false
 public string NoiDung { get; set; } = string.Empty;
 }

 public class ChatCreateConversationDto
 {
 public string MaKH { get; set; } = string.Empty;
 }

 public class ChatMessageDto
 {
 public long MaTN { get; set; }
 public bool LaKhachGui { get; set; }
 public string? MaNV { get; set; }
 public string NoiDung { get; set; } = string.Empty;
 public DateTime ThoiGian { get; set; }
 }

 public class ConversationSummaryDto
 {
 public long MaCuocTroChuyen { get; set; }
 public string MaKH { get; set; } = string.Empty;
 public string? TenKH { get; set; }
 public string? LastNoiDung { get; set; }
 public DateTime? LastThoiGian { get; set; }
 public bool? LastLaKhachGui { get; set; }
 public bool ChuaTraLoi { get; set; }
 public int TongTinNhan { get; set; }
 }

 public class ConversationWithMessagesDto
 {
 public long MaCuocTroChuyen { get; set; }
 public string MaKH { get; set; } = string.Empty;
 public string? TenKH { get; set; }
 public List<ChatMessageDto> Messages { get; set; } = new();
 }
}

using System.Collections.Generic;
using System.Threading.Tasks;
using BE_QLTiemThuoc.Repositories;
using BE_QLTiemThuoc.Model.Chat;
using BE_QLTiemThuoc.Dto;

namespace BE_QLTiemThuoc.Services
{
 public class ChatService
 {
 private readonly ChatRepository _repo;
 public ChatService(ChatRepository repo){ _repo = repo; }

 public Task<CuocTroChuyen> CreateConversationAsync(ChatCreateConversationDto dto)
 => _repo.CreateConversationAsync(dto.MaKH);

 public Task<CuocTroChuyen> GetOrCreateConversationAsync(string maKH)
 => _repo.GetOrCreateConversationAsync(maKH);

 public Task<ConversationWithMessagesDto?> GetConversationWithMessagesAsync(string maKH, int take)
 => _repo.GetConversationWithMessagesAsync(maKH, take);

 public async Task<TinNhan> CreateMessageAsync(ChatCreateMessageDto dto)
 {
 if(!dto.LaKhachGui && string.IsNullOrWhiteSpace(dto.MaNV))
 throw new ArgumentException("MaNV is required when LaKhachGui=false");

 var m = new TinNhan{
 MaCuocTroChuyen = dto.MaCuocTroChuyen,
 LaKhachGui = dto.LaKhachGui,
 MaNV = dto.LaKhachGui ? null : dto.MaNV,
 NoiDung = dto.NoiDung,
 ThoiGian = DateTime.UtcNow
 };
 return await _repo.AddMessageAsync(m);
 }

 public Task<List<TinNhan>> GetMessagesAsync(long maCuoc, int skip, int take)
 => _repo.GetMessagesAsync(maCuoc, skip, take);

 public Task<List<ConversationSummaryDto>> GetConversationSummariesAsync(int skip, int take, bool? onlyUnanswered)
 => _repo.GetConversationSummariesAsync(skip, take, onlyUnanswered);
 }
}
